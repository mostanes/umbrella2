using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.Algorithms.Images
{
	/// <summary>
	/// Class containing filtering algorithms that combine median filtering with averaging.
	/// </summary>
	public static class RestrictedMean
	{
		/// <summary>
		/// Common algorithm parameters for RestrictedMean algorithms.
		/// </summary>
		/// <param name="PSFRadius">Radius of the PSF importance distribution.</param>
		public static SchedCore.AlgorithmRunParameters Parameters(int PSFRadius) => new SchedCore.AlgorithmRunParameters()
		{
			FillZero = true,
			InputMargins = PSFRadius,
			Xstep = 0,
			Ystep = 50
		};

		/// <summary>
		/// Filters the input using a restricted mean filter. The argument given is the PSF importance distribution.
		/// </summary>
		public static SchedCore.SimpleMap<double[]> RestrictedMeanFilter => RestrictedMeanAlgorithm;

		/// <summary>
		/// Filters the input using a median filter that also considers the closest neighbor pixel values.
		/// </summary>
		public static SchedCore.SimpleMap<double[], ImageStatistics> MultiMedianFilter => MultiMedianAlgorithm;

		/// <summary>
		/// Computes a weighted mean using a subset of the data.
		/// </summary>
		/// <remarks>The PSF distribution is not the optical PSF, rather it is the distribution of the importance of pixel values around a point.</remarks>
		/// <param name="Input">Input data.</param>
		/// <param name="Output">Output data.</param>
		/// <param name="PSF">Point spread function distribution.</param>
		public static void RestrictedMeanAlgorithm(double[,] Input, double[,] Output, double[] PSF)
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
					Buffer.BlockCopy(PSF, 0, DPSF, 0, sizeof(double) * PSF.Length);
					Array.Sort(MedValues, DPSF);
					double w;
					for (s = 0, w = 0, k = Size; k <= PSF.Length - Size; k++)
					{ s += MedValues[k] * DPSF[k]; w += DPSF[k]; }
					Output[i, j] = s / w;
				}
		}

		/// <summary>
		/// Computes the mean of median and first neighbors.
		/// </summary>
		/// <remarks>The PSF distribution is not the optical PSF, rather it is the distribution of the importance of pixel values around a point.</remarks>
		/// <param name="Input">Input data.</param>
		/// <param name="Output">Output data.</param>
		/// <param name="PSF">Point spread function distribution.</param>
		/// <param name="imStat">Precomputed image information.</param>
		static void MultiMedianAlgorithm(double[,] Input, double[,] Output, double[] PSF, ImageStatistics imStat)
		{
			int OW = Output.GetLength(1);
			int OH = Output.GetLength(0);
			int i, j, k, l;
			int Size = (int) Math.Round(Math.Sqrt(PSF.Length));
			double[] MedValues = new double[PSF.Length];
			double[] DPSF = new double[PSF.Length];
			double[] MCentral = new double[9];
			int cnt;
			double s;
			int SzD = Size / 2;
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					cnt = 0;
					for (k = 0; k < Size; k++) for (l = 0; l < Size; l++)
						{ MedValues[cnt] = Input[i + k, j + l]; cnt++; }
					cnt = 0;
					for (k = i + SzD - 1; k < i + SzD + 2; k++) for (l = j + SzD - 1; l < j + SzD + 2; l++)
							MCentral[cnt++] = Input[k, l];
					Buffer.BlockCopy(PSF, 0, DPSF, 0, sizeof(double) * PSF.Length);
					Array.Sort(MedValues, DPSF);
					Array.Sort(MCentral);
					for (k = 0, s = 0; s < 0.5; k++) s += DPSF[k];
					Output[i, j] = 0.5 * (MedValues[k - 1] + MedValues[k + 1]) + MedValues[k];
					Output[i, j] /= 2;
					Output[i, j] *= MCentral[4] / imStat.ZeroLevel;
				}
		}
	}
}
