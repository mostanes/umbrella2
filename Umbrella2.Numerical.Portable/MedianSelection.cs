using System;

namespace Umbrella2.Algorithms.Images.Median
{
	/// <summary>
	/// Implements quickselect for weighted medians.
	/// </summary>
	/// <remarks>
	/// Uses median of 3 with fat partitions.
	/// </remarks>
	public static class MedianSelection
	{
		static long XQDepth = 0;
		static long XQNum = 0;

		static double AvgDepth => 2.0 * XQDepth / XQNum;

		/// <summary>
		/// Performs the library quicksort to find weighted median.
		/// </summary>
		/// <param name="Input">Input values.</param>
		/// <param name="Weights">Weights that correspond to the input values.</param>
		/// <returns>The median.</returns>
		internal static double StdSelect(double[] Input, double[] Weights)
		{
			int k; double s;
			Array.Sort(Input, Weights);
			for (k = 0, s = 0; s < 0.5; k++) s += Weights[k];
			return 0.25 * (Input[k - 1] + Input[k + 1]) + Input[k] * 0.5;
		}

		/// <summary>
		/// Applies the quickselect algorithm to get the weighted median.
		/// </summary>
		/// <param name="Input">Input values.</param>
		/// <param name="Weights">Weights that correspond to input values.</param>
		/// <returns>The median.</returns>
		public static double Quickselect(double[] Input, double[] Weights)
		{
			unsafe
			{
				fixed (double* InputPtr = Input, WeightsPtr = Weights)
					return QuickselectInternal(InputPtr, WeightsPtr, 0, Input.Length - 1, 0);
			}
		}

		/// <summary>
		/// Internal recursive Quickselect.
		/// </summary>
		/// <param name="Input">Input values.</param>
		/// <param name="Weights">Input weights.</param>
		/// <param name="Start">Start of the interval in Input to search.</param>
		/// <param name="End">End of the interval in Input to search.</param>
		/// <param name="ReqDep">Call depth. Used for performance statistics.</param>
		/// <returns>The median value.</returns>
		/// <remarks>This code is marked unsafe to skip bounds checking.</remarks>
		internal unsafe static double QuickselectInternal(double* Input, double* Weights, int Start, int End, int ReqDep)
		{
			XQDepth += ReqDep;
			XQNum++;

			if (Start == End)
				return Input[Start];

			if (End - Start == 1)
				return (Input[End] + Input[Start]) * 0.5;

			if (End - Start == 2)
				return 0.25 * (Input[End] + Input[Start]) + Input[End - 1] * 0.5;

			PivotAndPartition(Input, Weights, Start, End, out int PvS, out int PvL, out int PivotLow, out int PivotHigh, out double LowW, out double HighW, out double PivotW);
			double PivotVal = Input[PivotLow];

			if (LowW < 0.50000001 && HighW < 0.50000001) return Input[PivotLow];

			if (HighW > LowW) { Weights[PvL] += LowW + PivotW; return QuickselectInternal(Input, Weights, PivotHigh + 1, End, ReqDep + 1); }
			else { Weights[PvS] += HighW + PivotW; return QuickselectInternal(Input, Weights, Start, PivotLow - 1, ReqDep + 1); }
		}

		/// <summary>
		/// Finds a pivot (currently median of 3) and partitions the data.
		/// </summary>
		/// <param name="Input">Input values.</param>
		/// <param name="Weights">Input weights.</param>
		/// <param name="Start">Start of the interval in Input.</param>
		/// <param name="End">End of the interval in Input.</param>
		/// <param name="PvS">First value larger than the pivot.</param>
		/// <param name="PvL">First value smaller than the pivot.</param>
		/// <param name="PivotLow">Start of the pivot interval.</param>
		/// <param name="PivotHigh">End of the pivot interval.</param>
		/// <param name="LowW">Sum of weights for data lower than the pivot.</param>
		/// <param name="HighW">Sum of weights for data higher than the pivot.</param>
		/// <param name="PivotW">Sum of weights for pivot-valued data.</param>
		unsafe static void PivotAndPartition(double* Input, double* Weights, int Start, int End, out int PvS, out int PvL, out int PivotLow, out int PivotHigh, out double LowW, out double HighW, out double PivotW)
		{
			int Mid = (Start + End) / 2;
			if (Input[Mid] < Input[Start])
				Swap(Input, Weights, Mid, Start);
			if (Input[End] < Input[Start])
				Swap(Input, Weights, End, Start);
			if (Input[Mid] < Input[End])
				Swap(Input, Weights, Mid, End);

			double Value = Input[End];
			double Vs = double.MinValue;
			double Vl = double.MaxValue;
			int StoreI = Start;
			PvS = PvL = -1;
			LowW = HighW = PivotW = 0;
			int Pvcnt = 1;
			int i;
			for (i = Start; i <= End - Pvcnt; i++)
				if (Input[i] < Value)
				{
					if (Input[i] > Vs) { Vs = Input[i]; PvS = StoreI; }
					LowW += Weights[i];
					Swap(Input, Weights, StoreI, i);
					if (PvL == StoreI) PvL = i;
					StoreI++;
				}
				else
				{
					if (Input[i] > Value)
					{ HighW += Weights[i]; if (Input[i] < Vl) { Vl = Input[i]; PvL = i; } }
					else
					{
						Swap(Input, Weights, End - Pvcnt, i);
						/* if (PvL == End - Pvcnt) PvL = i;  unnecessary since PvL <= i <(=) End-Pvcnt */
						Pvcnt++;
						i--;
					}
				}
			PivotLow = StoreI;
			StoreI--;

			for (i = 1; i <= Pvcnt; i++)
			{
				PivotW += Weights[End - Pvcnt + i];
				Swap(Input, Weights, StoreI + i, End - Pvcnt + i);
				if (PvL == StoreI + i) PvL = End - Pvcnt + i;
			}
			PivotHigh = StoreI + Pvcnt;
		}

		/// <summary>
		/// Swap routine. Aggressively inlined (MethodImpl 256).
		/// </summary>
		/// <param name="Input">Input values.</param>
		/// <param name="Weights">Input weights.</param>
		/// <param name="a">First element to swap.</param>
		/// <param name="b">Second element to swap.</param>
		[System.Runtime.CompilerServices.MethodImpl(256)]
		unsafe static void Swap(double* Input, double* Weights, int a, int b)
		{
			double c = Input[a];
			Input[a] = Input[b];
			Input[b] = c;
			c = Weights[a];
			Weights[a] = Weights[b];
			Weights[b] = c;
		}
	}
}
