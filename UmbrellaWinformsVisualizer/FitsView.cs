﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ImagingToolkit.ImageIO;
using Umbrella2.IO.FITS;

namespace Umbrella2.Visualizers.Winforms
{
	/// <summary>
	/// WinForms control to display a FITS image (or a portion of it).
	/// </summary>
	public partial class FitsView : UserControl
	{
		/// <summary>
		/// Image to be displayed on the control.
		/// </summary>
		public FitsImage Image { get; set; }
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

		void ResizeBitmap() { Data = new ByteBitmap(Width, Height); }

		void ReadBitmap()
		{
			TopLeft = new Point(Center.X - Width / 2, Center.Y - Height / 2);
			Display = new Rectangle(TopLeft.X, TopLeft.Y, Width, Height);
			var ImageData = Image.LockData(Display, true);
			for (int i = 0; i < Height; i++)
				for (int j = 0; j < Width; j++)
				{
					byte Value = Scaler.GetValue(ImageData.Data[i, j]);
					for (int k = 0; k < 3; k++)
						Data.Data[i, j, k] = Value;
				}
			Image.ExitLock(ImageData);
		}

		void ShowBitmap() { try { pictureBox1.Image = Data.GetWindowsBitmap(); } catch { } }

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