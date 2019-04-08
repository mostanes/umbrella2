using System.Collections;
using System.Collections.Generic;
using Umbrella2.IO.FITS;
using static Umbrella2.Algorithms.Images.SchedCore;

namespace Umbrella2.Algorithms.Images
{
	/// <summary>
	/// Support for badpixel removal. Currently removes badpixels by masking.
	/// </summary>
	public static class BadpixelFilter
	{
		/// <summary>
		/// ParallelAlgorithm options.
		/// </summary>
		public static AlgorithmRunParameters Parameters = new AlgorithmRunParameters()
		{
			FillZero = false,
			InputMargins = 0,
			Xstep = 0,
			Ystep = 50
		};

		/// <summary>
		/// Creates a new BadPixel filter from a badpixel image.
		/// </summary>
		/// <param name="BadpixelFile">Badpixel input image: masked pixels are non-zero.</param>
		public static BitArray[] CreateFilter(FitsImage BadpixelFile)
		{
			PositionDependentExtractor<BitArray[]> Algo = DetectSources;
			BitArray[] Mask = new BitArray[BadpixelFile.Height];
			for (int i = 0; i < BadpixelFile.Height; i++) Mask[i] = new BitArray((int) BadpixelFile.Width);
			Algo.Run(Mask, BadpixelFile, Parameters);
			return Mask;
		}

		/// <summary>
		/// Badpixel filter function.
		/// </summary>
		public static PositionDependentMap<BitArray[]> Filter = MaskBadpixel;

		static void DetectSources(double[,] Input, ImageSegmentPosition Position, BitArray[] Mask)
		{
			for (int i = 0; i < Input.GetLength(0); i++) for (int j = 0; j < Input.GetLength(1); j++)
				{
					int Y = i + (int) System.Math.Round(Position.Alignment.Y);
					int X = j + (int) System.Math.Round(Position.Alignment.X);
					Mask[Y][X] = (Input[i, j] != 0);
				}
		}

		static void MaskBadpixel(double[,] Input, double[,] Output, ImageSegmentPosition InputPosition, ImageSegmentPosition OutputPosition, BitArray[] Mask)
		{
			System.Diagnostics.Debug.Assert(InputPosition.Alignment.X == OutputPosition.Alignment.X);
			System.Diagnostics.Debug.Assert(InputPosition.Alignment.Y == OutputPosition.Alignment.Y);

			for (int i = 0; i < Input.GetLength(0); i++) for (int j = 0; j < Input.GetLength(1); j++)
				{
					int Y = i + (int) System.Math.Round(InputPosition.Alignment.Y);
					int X = j + (int) System.Math.Round(InputPosition.Alignment.X);
					Output[i, j] = (Mask[Y][X] ? 0.0 : Input[i, j]);
				}
		}
	}
}
