using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Umbrella2.Algorithms.Images;
using Umbrella2.IO.FITS;
using Umbrella2.Pipeline.ExtraIO;
using Umbrella2.PropertyModel.CommonProperties;
using System.Linq;
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
		
		/// <summary>MPC Observatory code.</summary>
		public string ObservatoryCode;
		/// <summary>Name of the file to which the report is written.</summary>
		public string ReportName;
		/// <summary>Number of the processed CCD.</summary>
		public int CCDNumber;
		/// <summary>Name of the processed field.</summary>
		public string FieldName;
		/// <summary>Band of the observations.</summary>
		public MPCOpticalReportFormat.MagnitudeBand Band;

		/// <summary>Name of the list of tracklets.</summary>
		readonly string ListName;
		/// <summary>Name of the currently viewed image in its image set.</summary>
		string CurrentImageName;
		/// <summary>Images that can be loaded for viewing.</summary>
		/// <remarks>Used for selecting between differently processed images of the object. It is a cache of the displayed image's image set.</remarks>
		Dictionary<string, FitsImage> Images;
		
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
			ManualIC();
		}

		/// <summary>
		/// InitializeComponent for manually-added components.
		/// </summary>
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
			System.Threading.Tasks.Task tk = new System.Threading.Tasks.Task(() => SkyBotLookupNames(15.0));
			tk.Start();
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
			SuspendObjectsUpdate = true;
			for (int i = 0; i < t.Detections.Length; i++)
				if (t.Detections[i] != null)
				{
					ImageDetection det = t.Detections[i];
					dataGridView1.Rows.Add(i, det.Barycenter.PP.X.ToString("G6"), det.Barycenter.PP.Y.ToString("G6"), det.Barycenter.EP.FormatToString(Format.MPC_RA),
						det.Barycenter.EP.FormatToString(Format.MPC_Dec), det.FetchProperty<ObjectSize>().PixelEllipse.ToString());
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

			ImageView.Center = new Point((int) SelectedDetection.Barycenter.PP.X, (int) SelectedDetection.Barycenter.PP.Y);
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
			int ImageNumber = (int) dataGridView1.Rows[Index].Cells[0].Value;
			SelectedDetection = m_tracklets[SelectedTracklet].Detections[ImageNumber];
			/* Prepare view */
			IO.FITS.FitsImage Image = SelectedDetection.ParentImage;

			ImageSet ImSet = ((ImageSet) Image.GetProperty<ImageSource>());
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

			ImageView.Image = Image;
			UpdateImage();
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

		private void TrackletOutput_KeyPress(object sender, KeyPressEventArgs e)
		{ HandleKeyPress(e.KeyChar); }

		/// <summary>
		/// Provides custom navigation according to the key pressed.
		/// </summary>
		/// <param name="Key">Pressed key char.</param>
		private void HandleKeyPress(char Key)
		{
			int Index;
			Key = char.ToUpper(Key);
			switch (Key)
			{
				/* Next tracklet */
				case 'S':
					if (checkedListBox1.SelectedIndex + 1 < checkedListBox1.Items.Count) checkedListBox1.SelectedIndex++;
					break;
				/* Previous tracklet */
				case 'W':
					if (checkedListBox1.SelectedIndex > 0) checkedListBox1.SelectedIndex--;
					break;
				/* Next object */
				case 'D':
					if (dataGridView1.SelectedRows.Count >= 1)
					{
						Index = dataGridView1.SelectedRows[0].Index; dataGridView1.ClearSelection();
						if (Index + 1 < dataGridView1.Rows.Count) dataGridView1.Rows[Index + 1].Selected = true;
						else dataGridView1.Rows[Index].Selected = true;
					}
					if (dataGridView1.SelectedRows.Count == 0) dataGridView1.Rows[0].Selected = true;
					break;
				/* Previous object */
				case 'A':
					if (dataGridView1.SelectedRows.Count >= 1)
					{
						Index = dataGridView1.SelectedRows[0].Index; dataGridView1.ClearSelection();
						if (Index > 0) dataGridView1.Rows[Index - 1].Selected = true;
						else dataGridView1.Rows[Index].Selected = true;
					}
					if (dataGridView1.SelectedRows.Count == 0) dataGridView1.Rows[dataGridView1.Rows.Count - 1].Selected = true;
					break;
				/* Previous image */
				case 'Q':
					var Keys = System.Linq.Enumerable.ToList(Images.Keys);
					Index = Keys.IndexOf(CurrentImageName);
					if (Index > 0) CurrentImageName = Keys[Index - 1];
					UpdateImage();
					break;
				/* Next image */
				case 'E':
					Keys = System.Linq.Enumerable.ToList(Images.Keys);
					Index = Keys.IndexOf(CurrentImageName);
					if (Index < Keys.Count - 1) CurrentImageName = Keys[Index + 1];
					UpdateImage();
					break;
				/* Select for reporting */
				case ' ':
					checkedListBox1.SetItemChecked(checkedListBox1.SelectedIndex, !checkedListBox1.GetItemChecked(checkedListBox1.SelectedIndex));
					break;
			}
		}

		/// <summary>
		/// Function for looking up the names of objects.
		/// </summary>
		/// <param name="ArcLengthSec">Lookup radius.</param>
		private void SkyBotLookupNames(double ArcLengthSec)
		{
			
			double ALength = ArcLengthSec / 3600.0 / 180.0 * Math.PI;
			foreach(Tracklet tk in Tracklets)
			{
				var LookupResults = SkyBoTLookup.GetObjects(tk.Detections[0].Barycenter.EP, ALength, tk.Detections[0].Time.Time);
				if (LookupResults.Count == 1)
					tk.AppendProperty(new ObjectIdentity() { Name = LookupResults[0].Name });
				else if(LookupResults.Count > 1)
				{
					Comparison<SkyBoTLookup.SkybotObject> skcompare = (x, y) => (x.Position ^ tk.Detections[0].Barycenter.EP) > (y.Position ^ tk.Detections[0].Barycenter.EP) ? 1 : -1;
					LookupResults.Sort(skcompare);
					tk.AppendProperty(new ObjectIdentity() { Name = LookupResults[0].Name });
				}
			}
			
			for (int i = 0; i < Tracklets.Count; i++) if (Tracklets[i].TryFetchProperty(out ObjectIdentity id)) checkedListBox1.Items[i] = id.Name;
		}
	}
}
