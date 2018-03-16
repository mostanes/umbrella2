using System;
using Umbrella2.Algorithms.Geometry;
using static System.Math;

namespace Umbrella2.Algorithms.Images
{
	public static partial class RLHT
	{
		/// <summary>
		/// Runs the Hough Transform over a line.
		/// </summary>
		/// <param name="Input">Input image data.</param>
		/// <param name="Height">Image Height.</param>
		/// <param name="Width">Image Width.</param>
		/// <param name="Rho">Radial coordinate.</param>
		/// <param name="Theta">Angular coordinate.</param>
		/// <param name="IncTh">Increasing Threshold.</param>
		/// <param name="HoughSum">Hough transform output for given coordinates.</param>
		static void Lineover(double[,] Input, int Height, int Width, double Rho, double Theta, double IncTh, out double HoughSum)
		{
			Vector LineVector = new Vector() { X = Cos(Theta), Y = Sin(Theta) };
			Vector LineOrigin = new Vector() { X = -Rho * Sin(Theta), Y = Rho * Cos(Theta) };
			var r = LineIntersection.IntersectLeft(LineOrigin, LineVector, Width, Height);
			if (r == null) { HoughSum = 0; return; }
			Vector LeftIntersect = r.Item1;
			double LDist = r.Item2;
			r = LineIntersection.IntersectRight(LineOrigin, LineVector, Width, Height);
			if (r == null) { HoughSum = 0; return; }
			Vector RightIntersect = r.Item1;
			double RDist = r.Item2;

			double Start = Min(LDist, RDist);
			double End = Max(LDist, RDist);
			Vector StVec, EVec;

			if (Start == LDist && End == RDist) { StVec = LeftIntersect; EVec = RightIntersect; }
			else if (Start == RDist && End == LDist) { StVec = RightIntersect; EVec = LeftIntersect; }
			else throw new ApplicationException("Geometry error.");

			int k;
			int N = (int) (End - Start);
			Vector pt;

			if (N < 10) { HoughSum = 0; return; }

			const int ShortLength = 5;
			const int LongLength = 35;
			double ShortAvg, LongAvg;
			double[] LastVars = new double[LongLength];
			int RollingPtr;
			double ShortValue, LongValue;
			double HTSum;

			pt = StVec; RollingPtr = ShortLength; ShortAvg = 0;
			for (k = 0; k < ShortLength; k++, pt.Increment(LineVector))
			{
				int X = (int) Round(pt.X);
				int Y = (int) Round(pt.Y);
				double Val = Input[Y, X];
				ShortAvg += Val / ShortLength;
				LastVars[k] = Input[Y, X];
			}
			LongAvg = ShortAvg;
			LongValue = ShortAvg;
			ShortValue = ShortAvg;
			HTSum = LongValue + ShortValue;
			for (k = ShortLength; k < LongLength; k++) LastVars[k] = ShortAvg;

			double RevIC = 1 / IncTh;

			for (k = ShortLength; k < N; k++, pt.Increment(LineVector))
			{
				int X = (int) Round(pt.X);
				int Y = (int) Round(pt.Y);
				double Val = Input[Y, X];
				int LV = (RollingPtr + LongLength - 5) % LongLength;
				ShortAvg += (Val - LastVars[LV]) / ShortLength;
				LongAvg += (Val - LastVars[RollingPtr]) / LongLength;
				LastVars[RollingPtr] = Val;
				double M1 = LongAvg * RevIC;
				double LgMult = M1 / (1 + M1);
				//double LgMult = Atan(LongAvg * RevIC - 1) / PI + 0.5;
				LongValue = LongValue * LgMult + LongAvg;
				double M2 = LongValue * RevIC;
				double XLgMult = M2 / (1 + M2);
				//double XLgMult = Atan(LongValue * RevIC - 1) / PI + 0.5;
				double CVal = ShortAvg * 2 * XLgMult;
				HTSum += CVal;


				RollingPtr = (RollingPtr + 1) % LongLength;
			}
			HoughSum = HTSum;
		}
	}
}
