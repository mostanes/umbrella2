using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Umbrella2.Algorithms.Images;
using Umbrella2.IO;
using Umbrella2.Pipeline.ExtraIO;
using Umbrella2.PropertyModel.CommonProperties;
using static Umbrella2.Pipeline.ExtraIO.EquatorialPointStringFormatter;

namespace Umbrella2.Visualizer.Winforms
{
	/// <summary>
	/// Provides a visualization mechanism for tracklets.
	/// </summary>
	public partial class TrackletOutput : Form
	{
		List<Tracklet> m_tracklets;
		int SelectedTracklet;
		ImageDetection SelectedDetection;

		/// <summary>MPC Observatory code.</summary>
		public string ObservatoryCode;
		/// <summary>Name of the file to which the report is written.</summary>
		public string ReportName;
		/// <summary>Number of the processed CCD.</summary>
		public int CCDNumber;
		/// <summary>Name of the processed field.</summary>
		public string FieldName;
		/// <summary>Name of the processed field as given to the objects in the report.</summary>
		public string ReportFieldName;
		/// <summary>Band of the observations.</summary>
		public MPCOpticalReportFormat.MagnitudeBand Band;
		/// <summary>All input images.</summary>
		public IList<IO.Image> ImageSet;

		/// <summary>Name of the list of tracklets.</summary>
		readonly string ListName;
		/// <summary>Name of the currently viewed image in its image set.</summary>
		string CurrentImageName;
		/// <summary>Images that can be loaded for viewing.</summary>
		/// <remarks>Used for selecting between differently processed images of the object. It is a cache of the displayed image's image set.</remarks>
		Dictionary<string, IO.Image> Images;
		/// <summary>Timer for the blink function.</summary>
		Timer BlinkTimer;
		/// <summary>Blink image number.</summary>
		int BlinkID;

		/// <summary>Disable the object number update callback. Used when updating tracklets to prevent the callback from firing while the list mutates.</summary>
		bool SuspendObjectsUpdate = false;

		/// <summary>
		/// Displayed tracklets.
		/// </summary>
		public List<Tracklet> Tracklets { get { return m_tracklets; } set { m_tracklets = value; RefreshTracklets(); } }

		/// <param name="Name">Shown name of the current list.</param>
		public TrackletOutput(string Name)
		{
			ListName = Name;
			InitializeComponent();
			Text = "Tracklet Viewer for " + ListName;
		}

		private void TrackletOutput_Load(object sender, EventArgs e) { RefreshTracklets(); }

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
			SuspendObjectsUpdate = true;
			DateTime? tm = null;
			for (int i = 0; i < t.Detections.Length; i++)
				if (t.Detections[i] != null)
				{
					ImageDetection det = t.Detections[i];
					if (tm == null) tm = det.Time.Time;
					var PE = det.FetchProperty<ObjectSize>().PixelEllipse;
					dataGridView1.Rows.Add(i, det.Barycenter.PP.X.ToString("G6"), det.Barycenter.PP.Y.ToString("G6"),
						det.Barycenter.EP.FormatToString(Format.MPC_RA), det.Barycenter.EP.FormatToString(Format.MPC_Dec),
						PE.SemiaxisMajor.ToString("G4") + ";" + PE.SemiaxisMinor.ToString("G4"),
						(det.Time.Time - tm.Value).TotalSeconds);
				}
			SuspendObjectsUpdate = false;
			UpdateProperties();
			dataGridView1_SelectionChanged(null, null);
		}

		/// <summary>
		/// Update the properties grid for the tracklet.
		/// </summary>
		void UpdateProperties()
		{
			double mpi = Math.Pow((3600 * 180 / Math.PI), 2);

			dataGridView2.Rows.Clear();
			Tracklet t = m_tracklets[SelectedTracklet];
			TrackletVelocityRegression tvr = t.VelReg;
			{
				List<object[]> PropertySet = new List<object[]>() { new object[] { "RA-Dec R", tvr.R_RD.ToString("G6") },
				 new object[] { "T-RA R", tvr.R_TR.ToString("G6") }, new object[] { "T-Dec R", tvr.R_TD.ToString("G6") },
				 	new object[]{"T-RA S", (tvr.S_TR *mpi).ToString("G6") },new object[]{"T-Dec S", (tvr.S_TD *mpi).ToString("G6") } };

				AddTrackletProperties(PropertySet.ToArray());
			}
		}

		void AddTrackletProperties(object[][] Properties)
		{ foreach (object[] Row in Properties) dataGridView2.Rows.Add(Row); }

		private void dataGridView1_SelectionChanged(object sender, EventArgs e)
		{
			if (SuspendObjectsUpdate) return;
			/* Check if a detection is selected */
			if (dataGridView1.SelectedRows.Count == 1)
				SelectObject(dataGridView1.SelectedRows[0].Index);
			else
			{
				SelectedDetection = null;
				ImageView.Image = null;
			}
		}


		void UpdateImage()
		{
			if (Images != null) { ImageView.Image = Images[CurrentImageName]; }

			ImageView.Center = new Point((int)SelectedDetection.Barycenter.PP.X, (int)SelectedDetection.Barycenter.PP.Y);
			/* Scale the image accordingly */
			ImageStatistics ImStat = ImageView.Image.GetProperty<ImageStatistics>();
			ImageView.Scaler = new LinearScaler(ImStat.ZeroLevel - ImStat.StDev, ImStat.ZeroLevel + 7 * ImStat.StDev);
			/* Show image and highlight */
			ImageView.Refresh();
			if (SelectedDetection.TryFetchProperty(out ObjectPoints objp))
				ImageView.HighlightPixels(objp.PixelPoints);
		}

		/// <summary>
		/// The selected tracklet detection changed.
		/// </summary>
		/// <param name="Index">Detection number.</param>
		private void SelectObject(int Index)
		{
			int ImageNumber = (int)dataGridView1.Rows[Index].Cells[0].Value;
			SelectedDetection = m_tracklets[SelectedTracklet].Detections[ImageNumber];
			/* Prepare view */
			IO.Image Image = SelectedDetection.ParentImage;
			EnsureDetectionCMS();

			ImageSet ImSet = ((ImageSet)Image.GetProperty<ImageSource>());
			if (ImSet != null)
			{
				Images = ImSet.FetchVariants();
				contextMenuStrip1.Items.Clear();
				var Entries = ImSet.FetchVariants();
				foreach (var Entry in Entries)
				{
					contextMenuStrip1.Items.Add(Entry.Key, null, new EventHandler((o, e) => { CurrentImageName = Entry.Key; UpdateImage(); }));
					if (CurrentImageName == null) if (Entry.Value == Image) CurrentImageName = Entry.Key;
				}
			}

			UpdateDetectionProperties();

			ImageView.Image = Image;
			UpdateImage();
		}

		private void UpdateDetectionProperties()
		{
			dataGridView3.Rows.Clear();
			if (SelectedDetection.TryFetchProperty(out PairingProperties pp))
			{
				dataGridView3.Rows.Add("Star polluted", pp.StarPolluted);
				dataGridView3.Rows.Add("Detection Algorithm(s)", pp.Algorithm);
			}
			if(SelectedDetection.TryFetchProperty(out ObjectPhotometry photo))
			{
				dataGridView3.Rows.Add("Flux", photo.Flux.ToString("G6"));
				if (photo.Magnitude != 0)
					dataGridView3.Rows.Add("Magnitude", photo.Magnitude.ToString("G6"));
			}
			if(SelectedDetection.TryFetchProperty(out ObjectPoints px))
			{
				dataGridView3.Rows.Add("Pixel count", px.PixelPoints.Length);
			}
		}

		/// <summary>Ensures that the detection <see cref="ContextMenuStrip"/> is populated.</summary>
		private void EnsureDetectionCMS()
		{
			if (contextMenuStrip2.Items.Count == 0)
			{
				double ArcMult = Math.PI / 180 / 3600;
				contextMenuStrip2.Items.Add("Blink", null, (sender, e) => BlinkOnDetection());
				contextMenuStrip2.Items.Add("Filter detection out", null, (sender, e) => { Filter(SelectedDetection); });
				contextMenuStrip2.Items.Add("Filter 5\" out", null, (sender, e) => { FilterByDetection(SelectedDetection, ConditionRadius, 5.0 * ArcMult); });
				contextMenuStrip2.Items.Add("Filter 25\" out", null, (sender, e) => { FilterByDetection(SelectedDetection, ConditionRadius, 25.0 * ArcMult); });
				contextMenuStrip2.Items.Add("Filter 100\" out", null, (sender, e) => { FilterByDetection(SelectedDetection, ConditionRadius, 100.0 * ArcMult); });
				contextMenuStrip2.Items.Add("Filter X line 10px", null, (sender, e) => { FilterByDetection(SelectedDetection, ConditionX, 10); });
				contextMenuStrip2.Items.Add("Filter Y line 10px", null, (sender, e) => { FilterByDetection(SelectedDetection, ConditionY, 10); });
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			System.Text.StringBuilder Report = new System.Text.StringBuilder();
			CreateMPCReport(Report);
			System.IO.File.AppendAllText(ReportName, Report.ToString());
		}

	}
}
