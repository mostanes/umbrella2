using System;

namespace UmbrellaFitsIo.DataFormatTranslations
{
    public static class IntegerDataset
    {
        public static unsafe void Read8(IntPtr Pointer, double[,] Data, int Stride)
		{
			int Height = Data.GetLength(0);
			int Width = Data.GetLength(1);
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = 0; i < Height; i++)
			{
				for (j = 0; j < Width; j++, b++)
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

		public static unsafe void Read16(IntPtr Pointer, double[,] Data, int Stride)
		{
			int Height = Data.GetLength(0);
			int Width = Data.GetLength(1);
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = 0; i < Height; i++)
			{
				for (j = 0; j < Width; j++, b++)
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
					*b = (byte) (dd / 256);
					*b++ = (byte) (dd);
				}
				b += Stride - Data.GetLength(1) * 2;
			}
		}

		public static unsafe void Read32(IntPtr Pointer, double[,] Data, int Stride)
		{
			int Height = Data.GetLength(0);
			int Width = Data.GetLength(1);
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = 0; i < Height; i++)
			{
				for (j = 0; j < Width; j++, b++)
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
					*b = (byte) (dd / 16777216);
					*b++ = (byte) (dd / 65536);
					*b++ = (byte) (dd / 256);
					*b++ = (byte) (dd);

				}
				b += Stride - Data.GetLength(1) * 4;
			}
		}

		public static unsafe void Read64(IntPtr Pointer, double[,] Data, int Stride)
		{
			int Height = Data.GetLength(0);
			int Width = Data.GetLength(1);
			int i, j;
			byte* b = (byte*) Pointer;
			for (i = 0; i < Height; i++)
			{
				for (j = 0; j < Width; j++, b++)
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
					*b = (byte) (dd / Div / 16777216);
					*b++ = (byte) (dd / Div / 65536);
					*b++ = (byte) (dd / Div / 256);
					*b++ = (byte) (dd / Div);
					*b++ = (byte) (dd / 16777216);
					*b++ = (byte) (dd / 65536);
					*b++ = (byte) (dd / 256);
					*b++ = (byte) (dd);
				}
				b += Stride - Data.GetLength(1) * 8;
			}
		}
	}
}
