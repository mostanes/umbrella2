using System;
namespace Umbrella2.IO.FITS.Formats
{
	/// <summary>
	/// Delegate for reading IEEE floating point data from the memory-mapped image.
	/// </summary>
	/// <param name="Pointer">Pointer to image data.</param>
	/// <param name="Data">Destination array.</param>
	/// <param name="Hstart">Y coordinate from which to start.</param>
	/// <param name="Hend">Y coordinate at which to end.</param>
	/// <param name="Wstart">X coordinate at which to start.</param>
	/// <param name="Wend">X coordinate at which to end.</param>
	/// <param name="Stride">Data stride.</param>
    delegate void DataReader(IntPtr Pointer, double[,] Data, int Hstart, int Hend, int Wstart, int Wend, int Stride);

	/// <summary>
	/// Delegate for writing IEEE floating point data to the memory-mapped image.
	/// </summary>
	/// <param name="Pointer">Pointer to image data.</param>
	/// <param name="Data">Source array.</param>
	/// <param name="Stride">Data stride.</param>
	delegate void DataWriter(IntPtr Pointer, double[,] Data, int Stride);
}
