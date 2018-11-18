using System;
using System.Linq;
using Umbrella2.IO.FITS;
using Umbrella2.WCS;
using Umbrella2.Algorithms.Images.Median;

namespace Umbrella2.Algorithms.Images
{
	public static partial class HardMedians
	{
		/// <summary>
		/// Filters the input using a weighted median filter. The argument is the PSF importance distribution (here it functions as the median weights).
		/// </summary>
		public static ParallelAlgorithmRunner.SimpleMap<double[]> WeightedMedian = EstimatorFR.EstimatorFRMedian;

		/// <summary>
		/// Algorithm parameters for the weighted median filter.
		/// </summary>
		/// <param name="PSFRadius">Radius of the PSF importance distribution.</param>
		public static ParallelAlgorithmRunner.AlgorithmRunParameters WeightedMedianParameters(int PSFRadius) => new ParallelAlgorithmRunner.AlgorithmRunParameters()
		{
			FillZero = true,
			InputMargins = PSFRadius,
			Ystep = 50,
			Xstep = 0
		};

		/// <summary>
		/// Algorithm parameters for a multi-image median filter.
		/// </summary>
		/// <remarks>
		/// The algorithm used does not match WCS at file reading, hence it must overscan (InputMargins > 0).
		/// The implicit InputMargins is 50, corresponding to a maximum displacement of 50px between the same WCS point on 2 different images.
		/// </remarks>
		public static ParallelAlgorithmRunner.AlgorithmRunParameters MultiImageMedianParameters => new ParallelAlgorithmRunner.AlgorithmRunParameters()
		{
			FillZero = true,
			InputMargins = 50,
			Ystep = 50,
			Xstep = 0
		};

		/// <summary>
		/// Computes the median image of multiple input images. WCS information must be passed to the algorithm.
		/// </summary>
		public static ParallelAlgorithmRunner.Combiner<object> MultiImageMedian => MultiImageMedianFilter;

		/// <summary>
		/// Computes the weighted median of the input.
		/// </summary>
		/// <param name="Input">Input data.</param>
		/// <param name="Output">Output data.</param>
		/// <param name="PSF">PSF importance distribution / median weights.</param>
		static void WeightedMedianAlgorithm(double[,] Input, double[,] Output, double[] PSF)
		{
			int OW = Output.GetLength(1);
			int OH = Output.GetLength(0);
			int i, j, k, l;
			int Size = (int) Math.Round(Math.Sqrt(PSF.Length));
			double[] MedValues = new double[PSF.Length];
			double[] DPSF = new double[PSF.Length];
			int cnt;
			double s;
			int SzD = Size / 2;
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					cnt = 0;
					for (k = 0; k < Size; k++) for (l = 0; l < Size; l++)
						{ MedValues[cnt] = Input[i + k, j + l]; cnt++; }
					Buffer.BlockCopy(PSF, 0, DPSF, 0, 8 * PSF.Length);
					
					Output[i, j] = MedianSelection.Quickselect(MedValues, DPSF);
				}
		}

		/// <summary>
		/// Performs a median filter between multiple data sets.
		/// </summary>
		/// <param name="Inputs">Input data.</param>
		/// <param name="Output">Output data.</param>
		/// <param name="InputAlignments">Alignments of input data.</param>
		/// <param name="OutputAlignments">Alignment of output data.</param>
		/// <param name="WCS">WCS projections.</param>
		static void MultiImageMedianFilter(double[][,] Inputs, double[,] Output, ParallelAlgorithmRunner.ImageSegmentPosition[] InputPositions, ParallelAlgorithmRunner.ImageSegmentPosition OutputPosition, object empty)
		{
			PixelPoint[] InputAlignments = InputPositions.Select((x) => x.Alignment).ToArray();
			PixelPoint OutputAlignment = OutputPosition.Alignment;
			WCSViaProjection[] InputImagesTransforms = InputPositions.Select((x) => x.WCS).ToArray();
			WCSViaProjection OutputImageTransform = OutputPosition.WCS;

			int OW = Output.GetLength(1);
			int OH = Output.GetLength(0);
			int i, j, k, c;
			double[] MedValues = new double[Inputs.Length];
			PixelPoint pxp = new PixelPoint();
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					pxp.X = j + OutputAlignment.X; pxp.Y = i + OutputAlignment.Y;
					EquatorialPoint eqp = OutputImageTransform.GetEquatorialPoint(pxp);
					c = 0;
					for (k = 0; k < Inputs.Length; k++)
					{
						PixelPoint pyp = InputImagesTransforms[k].GetPixelPoint(eqp);
						pyp.X = Math.Round(pyp.X - InputAlignments[k].X); pyp.Y = Math.Round(pyp.Y - InputAlignments[k].Y);
						if (pyp.X < 0 || pyp.X >= Inputs[k].GetLength(1)) continue;
						if (pyp.Y < 0 || pyp.Y >= Inputs[k].GetLength(0)) continue;
						double dex = Inputs[k][(int) pyp.Y, (int) pyp.X];
						MedValues[c] = dex;
						c++;
					}
					if (c == 0)
						continue;

					Array.Sort(MedValues);
					if (c % 2 == 1)
						Output[i, j] = MedValues[c / 2];
					else Output[i, j] = MedValues[c / 2 - 1];
				}
		}
	}
}
