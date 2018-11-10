using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Umbrella2.Algorithms.Images;
using Umbrella2.Pipeline.ExtraIO;
using static Umbrella2.Pipeline.ExtraIO.EquatorialPointStringFormatter;

namespace Umbrella2.Visualizers.Winforms
{
	/// <summary>
	/// Provides a visualization mechanism for tracklets.
	/// </summary>
	public partial class TrackletOutput : Form
	{
		FitsView ImageView;
		List<Tracklet> m_tracklets;
		int SelectedTracklet;
		MedianDetection SelectedDetection;
		public string ObservatoryCode;
		public string ReportName;
		public int CCDNumber;
		public string FieldName;
		readonly string ListName;
		public MPCOpticalReportFormat.MagnitudeBand Band;

		public List<Tracklet> Tracklets { get { return m_tracklets; } set { m_tracklets = value; RefreshTracklets(); } }

		public TrackletOutput(string Name)
		{
			ListName = Name;
			InitializeComponent();
			ManualIC();
		}

		private void ManualIC()
		{
			ImageView = new FitsView();
			ImageView.Dock = DockStyle.Fill;
			this.panel1.Controls.Add(ImageView);
			ImageView.Show();

			Text = "Tracklet Viewer for " + ListName;
		}

		private void TrackletOutput_Load(object sender, EventArgs e) { RefreshTracklets(); }

		/// <summary>
		/// Update the view to match the loaded tracklets.
		/// </summary>
		void RefreshTracklets()
		{
			int cnt = 0;
			checkedListBox1.Items.Clear();
			foreach(Tracklet t in m_tracklets)
			{
				double ArcsecVelocity = t.Velocity * 3600 * 180 / Math.PI * 60;
				checkedListBox1.Items.Add("Tracklet " + (cnt++) + ", velocity = " + ArcsecVelocity.ToString("G5") + "\"/min");
			}
		}

		private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			/* Check if a tracklet is selected */
			if (checkedListBox1.SelectedIndex == -1) return;
			SelectedTracklet = checkedListBox1.SelectedIndex;
			SelectedTrackletChanged();
		}

		/// <summary>
		/// The selected tracklet changed.
		/// </summary>
		private void SelectedTrackletChanged()
		{
			Tracklet t = m_tracklets[SelectedTracklet];
			dataGridView1.Rows.Clear();
			for (int i = 0; i < t.MergedDetections.Length; i++)
				if (t.MergedDetections[i] != null)
				{
					MedianDetection det = t.MergedDetections[i];
					dataGridView1.Rows.Add(i, det.BarycenterPP.X.ToString("G6"), det.BarycenterPP.Y.ToString("G6"), det.BarycenterEP.FormatToString(Format.MPC_RA), det.BarycenterEP.FormatToString(Format.MPC_Dec), det.PixelEllipse.ToString());
				}
		}

		private void dataGridView1_SelectionChanged(object sender, EventArgs e)
		{
			/* Check if a detection is selected */
			if (dataGridView1.SelectedRows.Count == 1)
				SelectObject(dataGridView1.SelectedRows[0].Index);
			else
			{
				SelectedDetection = null;
				ImageView.Image = null;
			}
		}

		/// <summary>
		/// The selected tracklet detection changed.
		/// </summary>
		/// <param name="Index">Detection number.</param>
		private void SelectObject(int Index)
		{
			int ImageNumber = (int) dataGridView1.Rows[Index].Cells[0].Value;
			SelectedDetection = m_tracklets[SelectedTracklet].MergedDetections[ImageNumber];
			/* Prepare view */
			ImageView.Image = SelectedDetection.ParentImage;
			ImageView.Center = new Point((int) SelectedDetection.BarycenterPP.X, (int) SelectedDetection.BarycenterPP.Y);
			/* Scale the image accordingly */
			ImageStatistics ImStat = ImageView.Image.GetProperty<ImageStatistics>();
			ImageView.Scaler = new LinearScaler(ImStat.ZeroLevel - ImStat.StDev, ImStat.ZeroLevel + 7 * ImStat.StDev);
			/* Show image and highlight */
			ImageView.Refresh();
			ImageView.HighlightPixels(SelectedDetection.PixelPoints);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			StringBuilder Report = new StringBuilder();
			Report.AppendLine("MPC Report CCD" + CCDNumber.ToString());

			/* For each validated tracklet */
			foreach (int idx in checkedListBox1.CheckedIndices)
			{
				/* Write a line for each detection */
				foreach (MedianDetection md in m_tracklets[idx].MergedDetections)
				{
					/* Skip if no detection on a particular image */
					if (md == null) continue;
					/* Prepare a MPC report line.
					 * Currently does not compute magnitude nor identifies the object.
					 * There is also no support for providing a PublishingNote
					 */
					MPCOpticalReportFormat.ObsInstance instance = new MPCOpticalReportFormat.ObsInstance()
					{
						Coordinates = md.BarycenterEP,
						DetectionAsterix = false,
						Mag = 0,
						MagBand = Band,
						ObservatoryCode = ObservatoryCode,
						ObsTime = md.Time.Time,
						PubNote = MPCOpticalReportFormat.PublishingNote.none,
						ObjectDesignation = new string(' ', 7)
					};
					Report.AppendLine(MPCOpticalReportFormat.GenerateLine(instance));
				}
				Report.AppendLine();
			}
			System.IO.File.AppendAllText(ReportName, Report.ToString());
		}
	}
}
