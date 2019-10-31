using System;

namespace Umbrella2.Algorithms.Images.Median
{
	/// <summary>
	/// A median computation method that applies a Floyd-Rivest partitioning using estimated pivots before applying Quickselect.
	/// </summary>
	public static class EstimatorFR
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
					}
				}
			}
		}
		
		/// <summary>
		/// Selects input pixels in a given interval.
		/// </summary>
		/// <returns>Number of selected input pixels.</returns>
		unsafe internal static int FFRSelectZero(double[,] Input, double[] InputWeights, int i, int j, int Size, double UTh, double DTh, double* Values, double* VWeights, out double LowerW, out int LowI, out double HigherW, out int HighI)
		{
			int cnt = 0;
			HigherW = LowerW = 0;
			LowI = HighI = -1;
			int LI = -1, HI = -1;
			double LowVal = double.MaxValue, HighVal = double.MinValue;
			double LowTh = DTh, HighTh = UTh;
			int iSz = i + Size, jSz = j + Size;
			int ScanSize = Input.GetLength(1);
			fixed (double* InputPtr = Input, IWPtr = InputWeights)
			{
				for (int k = i, cel = 0; k < iSz; k++)
				{
					double* IEcPtr = &InputPtr[k * ScanSize];
					for (int l = j; l < jSz; l++, cel++)
					{
						double Val = IEcPtr[l];
						if (Val > LowTh)
						{
							if (Val < HighTh)
							{
								Values[cnt] = Val; VWeights[cnt] = IWPtr[cel];
								if (Val > HighVal) { HighVal = Val; HI = cnt; }
								if (Val < LowVal) { LowVal = Val; LI = cnt; }
								cnt++;
							}
							else HigherW += IWPtr[cel];
						}
						else LowerW += IWPtr[cel];
					}
				}
			}
			LowI = LI;
			HighI = HI;
			return cnt;
		}

		/// <summary>
		/// Calls Quickselect to find the median.
		/// </summary>
		/// <param name="SelectedInput">Input values array.</param>
		/// <param name="Weights">Input weights array.</param>
		/// <param name="Count">Amount of input data.</param>
		/// <param name="LowerW">Sum of weights for values smaller than the selected ones.</param>
		/// <param name="LowI">Index of the smallest value.</param>
		/// <param name="HigherW">Sum of weights for values larger than the selected ones.</param>
		/// <param name="HighI">Index of the highest value.</param>
		/// <returns>The median of the input values.</returns>
		unsafe internal static double CallQsel(double* SelectedInput, double* Weights, int Count, double LowerW, int LowI, double HigherW, int HighI)
		{
			/* Some runtime statistics about efficiency of the algorithm. */
			AvCount += Count;

			if (Count == 1) return SelectedInput[0];

			Weights[LowI] += LowerW;
			Weights[HighI] += HigherW;
			return MedianSelection.QuickselectInternal(SelectedInput, Weights, 0, Count - 1, 0);
		}
	}
}
