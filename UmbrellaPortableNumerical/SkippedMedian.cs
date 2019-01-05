using System;
using static Umbrella2.Algorithms.Images.Median.EstimatorFR;

namespace Umbrella2.Algorithms.Images.Median
{
	public static class SkippedMedian
	{
		/// <summary>
		/// Amount of standard deviations to include downwards.
		/// </summary>
		static double SDCountD = 0.35;
		/// <summary>
		/// Amount of standard deviations to include upwards.
		/// </summary>
		static double SDCountU = 0.3;
		/// <summary>
		/// Weight threshold for the eventual float rounding mismatches.
		/// </summary>
		static double IndistinguishableWeight = Math.Pow(10, -6);
		/// <summary>
		/// Number of input pixels passing through to quickselect.
		/// </summary>
		static long AvCount = 0;
		/// <summary>
		/// Number of output pixels processed.
		/// </summary>
		static long AvRun = 0;
		/// <summary>
		/// Number of median prediction hits.
		/// </summary>
		static long CPred = 0;

		static int Skip = 4;

		static double AvgQselCount => AvCount * 1.0 / AvRun;

		/// <summary>
		/// Computes the weighted median of the input.
		/// </summary>
		/// <param name="Input">Input data.</param>
		/// <param name="Output">Output data.</param>
		/// <param name="PSF">PSF importance distribution / median weights.</param>
		public static void EstimatorFRMedian(double[,] Input, double[,] Output, double[] PSF)
		{
			int OW = Output.GetLength(1);
			int OH = Output.GetLength(0);
			int i, j, k, l;
			int Size = (int) Math.Round(Math.Sqrt(PSF.Length));
			double[] MedValuesM = new double[PSF.Length];
			double[] WeightsM = new double[PSF.Length];
			int SzD = Size / 2;
			int SkipCnt = 0;
			unsafe
			{
				fixed (double* MedValues = MedValuesM, Weights = WeightsM)
				{
					for (i = 0; i < OH; i++)
					{
						double SumV = 0, SumVSq = 0;
						for (k = 0; k < Size; k++) for (l = 0; l < Size; l++)
							{
								SumV += Input[i + k, l];
								SumVSq += Input[i + k, l] * Input[i + k, l];
							}
						double Esti1 = 0, Esti2 = 0;
						SkipCnt = 0;
						for (j = 0; j < OW; j++)
						{
							for (k = 0; k < Size; k++)
							{ double Val = Input[i + k, j + Size - 1] - Input[i + k, j]; SumV += Val; SumVSq += Input[i + k, j + Size - 1] * Input[i + k, j + Size - 1] - Input[i + k, j] * Input[i + k, j]; }

							/* Compute moments and linear median estimation */
							double Mean = SumV / (Size * Size);
							double SD = Math.Sqrt(SumVSq / (Size * Size) - Mean * Mean);
							double EstMed = Esti1 * 2.0 - Esti2;
							double EstMean = 0.5 * (Mean + EstMed);

							/* Check whether the estimate makes sense, if not go for default - mean */
							if (Math.Abs(EstMean - Mean) > SD) EstMean = Mean;
							/* Compute thresholds */
							double DTh = Mean - SD * SDCountD, UTh = Mean + SD * SDCountU;

							if (SkipCnt == 0)
							{
								int Csel = FFRSelectZero(Input, PSF, i, j, Size, UTh, DTh, MedValues, Weights, out double LowerW, out int LowI, out double HigherW, out int HighI);
								if (LowerW >= 0.5 || HigherW >= 0.5)
								{
									if (Math.Abs(LowerW - HigherW) < IndistinguishableWeight) { Output[i, j] = Mean; continue; }
									if (LowerW > 0.5)
										Csel = FFRSelectZero(Input, PSF, i, j, Size, DTh, Mean - 1.001 * SD, MedValues, Weights, out LowerW, out LowI, out HigherW, out HighI);
									else
										Csel = FFRSelectZero(Input, PSF, i, j, Size, Mean + 1.001 * SD, UTh, MedValues, Weights, out LowerW, out LowI, out HigherW, out HighI);
								}
								else CPred++;
								AvRun++;
								if (Csel == 0) Output[i, j] = Mean;
								else Output[i, j] = CallQsel(MedValues, Weights, Csel, LowerW, LowI, HigherW, HighI);
								Esti2 = Esti1;
								Esti1 = Output[i, j];
							}
							else { Output[i, j] = Output[i, j - 1]; }
							SkipCnt++;
							SkipCnt %= Skip;
						}
					}
				}
			}
		}
	}
}
