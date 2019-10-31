using System;

namespace Umbrella2.IO.FITS.Formats
{
	/// <summary>
	/// Module for reading from and writing to floating-point FITS data arrays.
	/// Functions provide for converting memory-mapped file data to IEEE floating point.
	/// </summary>
	public static class IntegerDataset
    {
#pragma warning disable 1591 
		public static unsafe void Read8(IntPtr Pointer, double[,] Data, int Hstart, int Hend, int Wstart, int Wend, int Stride)
		{
			int Width = Wend-Wstart;
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = Hstart; i < Hend; i++)
			{
				for (j = Wstart; j < Wend; j++, b++)
				{
					Data[i, j] = (double) (*b);
				}
				b += Stride - Width;
			}
		}

		public static unsafe void Write8(IntPtr Pointer, double[,] Data, int Stride)
		{
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = 0; i < Data.GetLength(0); i++)
			{
				for (j = 0; j < Data.GetLength(1); j++, b++)
				{
					*b = (byte) ((int) Data[i, j]);
				}
				b += Stride - Data.GetLength(1);
			}
		}

		public static unsafe void Read16(IntPtr Pointer, double[,] Data, int Hstart, int Hend, int Wstart, int Wend, int Stride)
		{
			int Width = Wend - Wstart;
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = Hstart; i < Hend; i++)
			{
				for (j = Wstart; j < Wend; j++, b++)
				{
					Data[i, j] = (double) ((*((sbyte*)b) * 256 + (*++b)));
				}
				b += Stride - Width * 2;
			}
		}

		public static unsafe void Write16(IntPtr Pointer, double[,] Data, int Stride)
		{
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = 0; i < Data.GetLength(0); i++)
			{
				for (j = 0; j < Data.GetLength(1); j++, b++)
				{
					int dd = (int) Data[i, j];
					*b++ = (byte) (dd / 256);
					*b = (byte) (dd);
				}
				b += Stride - Data.GetLength(1) * 2;
			}
		}

		public static unsafe void Read32(IntPtr Pointer, double[,] Data, int Hstart, int Hend, int Wstart, int Wend, int Stride)
		{
			int Width = Wend - Wstart;
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = Hstart; i < Hend; i++)
			{
				for (j = Wstart; j < Wend; j++, b++)
				{
					Data[i, j] = (double) ((*((sbyte*) b) * 256 + (*++b))) * 65536;
					Data[i, j] += (double) ((*(++b) * 256 + (*++b)));
				}
				b += Stride - Width * 4;
			}
		}

		public static unsafe void Write32(IntPtr Pointer, double[,] Data, int Stride)
		{
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = 0; i < Data.GetLength(0); i++)
			{
				for (j = 0; j < Data.GetLength(1); j++, b++)
				{
					int dd = (int) Data[i, j];
					*b++ = (byte) (dd / 16777216);
					*b++ = (byte) (dd / 65536);
					*b++ = (byte) (dd / 256);
					*b = (byte) (dd);

				}
				b += Stride - Data.GetLength(1) * 4;
			}
		}

		public static unsafe void Read64(IntPtr Pointer, double[,] Data, int Hstart, int Hend, int Wstart, int Wend, int Stride)
		{
			int Width = Wend - Wstart;
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = Hstart; i < Hend; i++)
			{
				for (j = Wstart; j < Wend; j++, b++)
				{
					Data[i, j] = (double) ((*((sbyte*) b) * 256 + (*++b)));
					Data[i, j] = Data[i,j] * 65536 + (double) ((*(++b) * 256 + (*++b)));
					Data[i, j] = Data[i, j] * 65536 + (double) ((*(++b) * 256 + (*++b)));
					Data[i, j] = Data[i, j] * 65536 + (double) ((*(++b) * 256 + (*++b)));
				}
				b += Stride - Width * 8;
			}
		}

		public static unsafe void Write64(IntPtr Pointer, double[,] Data, int Stride)
		{
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = 0; i < Data.GetLength(0); i++)
			{
				for (j = 0; j < Data.GetLength(1); j++, b++)
				{
					long dd = (long) Data[i, j];
					const long Div = ((long) int.MaxValue) + 1;
					*b++ = (byte) (dd / Div / 16777216);
					*b++ = (byte) (dd / Div / 65536);
					*b++ = (byte) (dd / Div / 256);
					*b++ = (byte) (dd / Div);
					*b++ = (byte) (dd / 16777216);
					*b++ = (byte) (dd / 65536);
					*b++ = (byte) (dd / 256);
					*b = (byte) (dd);
				}
				b += Stride - Data.GetLength(1) * 8;
			}
		}
#pragma warning restore 1591
	}
}
