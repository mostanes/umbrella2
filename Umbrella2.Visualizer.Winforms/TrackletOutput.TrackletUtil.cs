using System;
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
			RefreshTrackletList();
			System.Threading.Tasks.Task tk = new System.Threading.Tasks.Task(() => SkyBotLookupNames(5.0));
			tk.Start();
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
			Report.AppendLine("MPC Report CCD" + CCDNumber.ToString());

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
					MPCOpticalReportFormat.ObsInstance instance = new MPCOpticalReportFormat.ObsInstance()
					{
						Coordinates = md.Barycenter.EP,
						DetectionAsterisk = false,
						N2 = MPCOpticalReportFormat.Note2.CCD,
						Mag = null,
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

				foreach (Tracklet tk in Tracklets)
				{
					if (tk.TryFetchProperty(out ObjectIdentity objid))
						objid.ComputeNamescore(tk);
				}
			}
			this.Invoke((Action)RefreshTrackletList);
		}
	}
}
