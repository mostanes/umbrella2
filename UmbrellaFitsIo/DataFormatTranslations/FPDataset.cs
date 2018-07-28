using System;

namespace UmbrellaFitsIo.DataFormatTranslations
{
	/// <summary>
	/// Module for reading from and writing to floating-point FITS data arrays.
	/// </summary>
	static class FPDataset
	{
		public static unsafe void Read32(IntPtr Pointer, double[,] Data, int Hstart, int Hend, int Wstart, int Wend, int Stride)
		{
			int Width = Wend - Wstart;
			int i, j;
			byte* b = (byte*) Pointer;
			uint c;
			for (i = Hstart; i < Hend; i++)
			{
				for (j = Wstart; j < Wend; j++, b++)
				{
					c = ((uint)((*b * 256 + (*++b)))) * 65536;
					c += (uint) ((*(++b) * 256 + (*++b)));
					Data[i, j] = (double) *((float*) (&c));
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
					float dh = (float) Data[i, j];
					uint dd = *((uint*) &dh);
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
			ulong c;
			for (i = Hstart; i < Hend; i++)
			{
				for (j = Wstart; j < Wend; j++, b++)
				{
					c = (ulong) ((*b * 256 + (*++b)));
					c = c * 65536 + (ulong) ((*(++b) * 256 + (*++b)));
					c = c * 65536 + (ulong) ((*(++b) * 256 + (*++b)));
					c = c * 65536 + (ulong) ((*(++b) * 256 + (*++b)));
					Data[i, j] = *((double*) (&c));
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
					double dh = Data[i, j];
					ulong dd = *((ulong*) &dh);
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
	}
}
