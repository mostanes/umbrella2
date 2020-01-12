using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Umbrella2.Pipeline.EIOAlgorithms;
using Umbrella2.Pipeline.ExtraIO;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2.Visualizer.Winforms
{
	public partial class TrackletOutput
	{
		/// <summary>
		/// Updates the tracklet information after adding new ones.
		/// </summary>
		void RefreshTracklets()
		{
			if (contextMenuStrip3.Items.Count == 0)
			{
				contextMenuStrip3.Items.Add("View properties", null, (sender, e) => ViewObjectProperties());
			}

			RefreshTrackletList();
			System.Threading.Tasks.Task tk = new System.Threading.Tasks.Task(() => SkyBotLookupNames(5.0));
			tk.Start();
		}

		void ViewObjectProperties()
		{
			Tracklet tk = Tracklets[SelectedTracklet];
			PropertyViewer pw = new PropertyViewer("Tracklet", tk);
			pw.AddProperties("Tracklet", tk.VelReg, tk.Velocity);
			foreach (var det in tk.Detections) pw.AddObject("Detection at " + det.Time.Time, det);
			pw.ShowProperties();
			pw.Show();
		}

		/// <summary>
		/// Refreshs the list of tracklets after a change.
		/// </summary>
		void RefreshTrackletList()
		{
			int cnt = 0;
			checkedListBox1.Items.Clear();
			foreach (Tracklet t in m_tracklets)
			{
				double ArcsecVelocity = t.Velocity.ArcSecMin;
				string Name = "Tracklet " + (cnt++);
				if (t.TryFetchProperty(out ObjectIdentity id)) if (id.Name != null) Name = id.Name;
				checkedListBox1.Items.Add(Name + ", velocity = " + ArcsecVelocity.ToString("G5") + "\"/min");
			}
		}

		/// <summary>
		/// Generates the report from the selected trackets.
		/// </summary>
		/// <param name="Report"><see cref="StringBuilder"/> to collect the report.</param>
		private void CreateMPCReport(StringBuilder Report)
		{
			/* For each validated tracklet */
			foreach (int idx in checkedListBox1.CheckedIndices)
			{
				/* Write a line for each detection */
				foreach (ImageDetection md in m_tracklets[idx].Detections)
				{
					/* Skip if no detection on a particular image */
					if (md == null) continue;
					/* Prepare a MPC report line.
					 * Currently does not compute magnitude.
					 * There is also no support for providing a PublishingNote
					 */
					double? Mg = null;
					if (md.TryFetchProperty(out ObjectPhotometry oph))
						if (oph.Magnitude != 0 & !double.IsNaN(oph.Magnitude))
							Mg = oph.Magnitude;
					MPCOpticalReportFormat.ObsInstance instance = new MPCOpticalReportFormat.ObsInstance()
					{
						Coordinates = md.Barycenter.EP,
						DetectionAsterisk = false,
						N2 = MPCOpticalReportFormat.Note2.CCD,
						Mag = Mg,
						MagBand = Band,
						ObservatoryCode = ObservatoryCode,
						ObsTime = md.Time.Time + new TimeSpan(md.Time.Exposure.Ticks / 2),
						PubNote = MPCOpticalReportFormat.PublishingNote.none,
					};
					if (m_tracklets[idx].TryFetchProperty(out ObjectIdentity objid))
					{ instance.PackedMPN = objid.PackedMPN; instance.ObjectDesignation = objid.PackedPD; }

					Report.AppendLine(MPCOpticalReportFormat.GenerateLine(instance));
				}
				Report.AppendLine();
			}
		}

		private void TrackletOutput_KeyPress(object sender, KeyPressEventArgs e)
		{ try { HandleKeyPress(e.KeyChar); } catch { } }


		/// <summary>
		/// Function for looking up the names of objects.
		/// </summary>
		/// <param name="ArcLengthSec">Lookup radius.</param>
		private void SkyBotLookupNames(double ArcLengthSec)
		{
			lock (m_tracklets)
			{
				foreach (IO.Image img in ImageSet)
				{
					SkyBotImageData skid = img.GetProperty<SkyBotImageData>();

					foreach (Tracklet tk in Tracklets)
						skid.TryPair(tk, ArcLengthSec);
				}

				for (int i = 0; i < Tracklets.Count; i++)
				{
					Tracklet tk = Tracklets[i];
					ObjectIdentity objid;
					if (!tk.TryFetchProperty(out objid)) objid = new ObjectIdentity();
					objid.ComputeNamescoreWithDefault(tk, null, ReportFieldName, CCDNumber, i);
					tk.SetResetProperty(objid);
				}
			}
			this.Invoke((Action)RefreshTrackletList);
		}
	}
}
