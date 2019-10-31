using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImagingToolkit.ImageIO
{
	/// <summary>
	/// Bitmap with a byte backend; based on Windows Bitmaps.
	/// </summary>
	public class ByteBitmap
	{
		public readonly int Width;
		public readonly int Height;
		/// <summary>
		/// Color data. Indices: y, x, channel
		/// </summary>
		public byte[,,] Data;

		/// <summary>
		/// Creates a new ByteBitmap with a specified width and height.
		/// </summary>
		public ByteBitmap(int Width, int Height)
		{
			this.Width = Width;
			this.Height = Height;
			Data = new byte[Height, Width, 3];
		}

		/// <summary>
		/// Creates a new ByteBitmap from an existing Windows Bitmap.
		/// </summary>
		public ByteBitmap(Bitmap b)
		{
			Width = b.Width;
			Height = b.Height;
			Data = new byte[Height, Width, 3];

			BitmapData x = b.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			int i, j, l;
			IntPtr scan = x.Scan0;

			byte[] datarow = new byte[3 * b.Width];
			for (i = 0; i < Height; i++)
			{
				Marshal.Copy(scan, datarow, 0, datarow.Length);
				for (j = 0, l = 0; j < Width; j++)
				{
					Data[i, j, 0] = datarow[l++];
					Data[i, j, 1] = datarow[l++];
					Data[i, j, 2] = datarow[l++];
				}
				scan += x.Stride;
			}

			b.UnlockBits(x);
		}

		/// <summary>
		/// Gets a Windows Bitmap from the current ByteBitmap.
		/// </summary>
		public Bitmap GetWindowsBitmap()
		{
			Bitmap wb = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
			var x = wb.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
			int i, j, l;
			IntPtr scan = x.Scan0;

			byte[] datarow = new byte[3 * Width];
			for (i = 0; i < Height; i++)
			{
				l = 0;
				for (j = 0; j < Width; j++)
				{
					datarow[l++] = Data[i, j, 0];
					datarow[l++] = Data[i, j, 1];
					datarow[l++] = Data[i, j, 2];
				}
				Marshal.Copy(datarow, 0, scan, datarow.Length);
				scan += x.Stride;
			}

			wb.UnlockBits(x);
			return wb;
		}
	}
}
