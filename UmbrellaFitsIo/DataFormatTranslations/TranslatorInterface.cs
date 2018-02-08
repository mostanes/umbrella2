using System;
namespace Umbrella2.IO.FITS.Formats
{
    delegate void DataReader(IntPtr Pointer, double[,] Data, int Hstart, int Hend, int Wstart, int Wend, int Stride);
	delegate void DataWriter(IntPtr Pointer, double[,] Data, int Stride);
}
