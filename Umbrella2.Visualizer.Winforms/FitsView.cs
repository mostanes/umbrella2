using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ImagingToolkit.ImageIO;
using Umbrella2.IO;

namespace Umbrella2.Visualizer.Winforms
{
	/// <summary>
	/// WinForms control to display a FITS image (or a portion of it).
	/// </summary>
	public partial class FitsView : UserControl
	{
		/// <summary>
		/// Image to be displayed on the control.
		/// </summary>
		public IO.Image Image { get; set; }
		Point TopLeft;
		Rectangle Display;
		/// <summary>
		/// The coordinates of the central point.
		/// </summary>
		public Point Center { get; set; }

		/// <summary>Image scaling algorithm.</summary>
		public IFitsViewScaler Scaler;

		ByteBitmap Data;

		public FitsView()
		{
			InitializeComponent();
		}

		private static bool OnMono = Type.GetType("Mono.Runtime") != null;
		void ResizeBitmap() { Data = new ByteBitmap(OnMono ? Width - 10 : Width, OnMono ? Height - 10 : Height); }

		void ReadBitmap()
		{
			TopLeft = new Point(Center.X - Data.Width / 2, Center.Y - Data.Height / 2);
			Display = new Rectangle(TopLeft.X, TopLeft.Y, Data.Width, Data.Height);
			var ImageData = Image.LockData(Display, true);
			for (int i = 0; i < Data.Height; i++)
				for (int j = 0; j < Data.Width; j++)
				{
					byte Value = Scaler.GetValue(ImageData.Data[i, j]);
					for (int k = 0; k < 3; k++)
						Data.Data[i, j, k] = Value;
				}
			Image.ExitLock(ImageData);
		}

		void ShowBitmap() { try { pictureBox1.Image = Data.GetWindowsBitmap(); pictureBox1.Refresh(); } catch { } }

		public override void Refresh()
		{ base.Refresh(); Reload(); }

		private void FitsView_Resize(object sender, EventArgs e) { ResizeBitmap(); }

		private void FitsView_Load(object sender, EventArgs e) { ResizeBitmap(); }

		public void HighlightPixels(IEnumerable<PixelPoint> Pixels)
		{
			foreach (PixelPoint p in Pixels)
				if (Display.Contains((int) p.X, (int) p.Y))
					Data.Data[(int) p.Y - TopLeft.Y, (int) p.X - TopLeft.X, 0] = Data.Data[(int) p.Y - TopLeft.Y, (int) p.X - TopLeft.X, 2] = 0;
			ShowBitmap();
		}

		public void Reload() { if (Image != null) { ReadBitmap(); ShowBitmap(); } }
	}
}
