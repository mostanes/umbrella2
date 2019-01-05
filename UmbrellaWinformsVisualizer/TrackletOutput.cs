using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Umbrella2.Algorithms.Images;
using Umbrella2.Pipeline.ExtraIO;
using Umbrella2.PropertyModel.CommonProperties;
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
		ImageDetection SelectedDetection;
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
				double ArcsecVelocity = t.Velocity.EquatorialVelocity * 3600 * 180 / Math.PI * 60;
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
			for (int i = 0; i < t.Detections.Length; i++)
				if (t.Detections[i] != null)
				{
					ImageDetection det = t.Detections[i];
					dataGridView1.Rows.Add(i, det.Barycenter.PP.X.ToString("G6"), det.Barycenter.PP.Y.ToString("G6"), det.Barycenter.EP.FormatToString(Format.MPC_RA),
						det.Barycenter.EP.FormatToString(Format.MPC_Dec), det.FetchProperty<ObjectSize>().PixelEllipse.ToString());
				}
			UpdateProperties();
		}

		void UpdateProperties()
		{
			dataGridView2.Rows.Clear();
			Tracklet t = m_tracklets[SelectedTracklet];
			TrackletVelocityRegression tvr = t.VelReg;
			{
				object[][] PropertySet = new object[][] { new object[] { "X-Y R", tvr.R_XY }, new object[] { "T-X R", tvr.R_TX }, new object[] { "T-Y R", tvr.R_TY } };
				AddTrackletProperties(PropertySet);
			}
		}

		void AddTrackletProperties(object[][] Properties)
		{ foreach (object[] Row in Properties) dataGridView2.Rows.Add(Row); }

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
			SelectedDetection = m_tracklets[SelectedTracklet].Detections[ImageNumber];
			/* Prepare view */
			IO.FITS.FitsImage Image = SelectedDetection.ParentImage;
			if (Image.GetProperty<ImageSource>().Original != null) Image = Image.GetProperty<ImageSource>().Original;
			ImageView.Image = Image;
			ImageView.Center = new Point((int) SelectedDetection.Barycenter.PP.X, (int) SelectedDetection.Barycenter.PP.Y);
			/* Scale the image accordingly */
			ImageStatistics ImStat = ImageView.Image.GetProperty<ImageStatistics>();
			ImageView.Scaler = new LinearScaler(ImStat.ZeroLevel - ImStat.StDev, ImStat.ZeroLevel + 7 * ImStat.StDev);
			/* Show image and highlight */
			ImageView.Refresh();
			if (SelectedDetection.TryFetchProperty(out ObjectPoints objp))
				ImageView.HighlightPixels(objp.PixelPoints);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			StringBuilder Report = new StringBuilder();
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
					 * Currently does not compute magnitude nor identifies the object.
					 * There is also no support for providing a PublishingNote
					 */
					MPCOpticalReportFormat.ObsInstance instance = new MPCOpticalReportFormat.ObsInstance()
					{
						Coordinates = md.Barycenter.EP,
						DetectionAsterisk = false,
						N2 = MPCOpticalReportFormat.Note2.CCD,
						Mag = 0,
						MagBand = Band,
						ObservatoryCode = ObservatoryCode,
						ObsTime = md.Time.Time + new TimeSpan(md.Time.Exposure.Ticks / 2),
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
