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
			/* Set up geometry */
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

			/* Computational part starts here */

			int k;
			int N = (int) (End - Start);
			Vector pt;

			if (N < 10) { HoughSum = 0; return; }

			const int ShortLength = 5;
			const int LongLength = 35;
			float ShortAvg, LongAvg;
			float[] LastVars = new float[LongLength];
			int RollingPtr;
			float ShortValue, LongValue;
			float HTSum;

			/* Computing initial values for rolling weights */

			pt = StVec; RollingPtr = ShortLength; ShortAvg = 0;
			for (k = 0; k < ShortLength; k++, pt.Increment(LineVector))
			{
				int X = (int) Round(pt.X);
				int Y = (int) Round(pt.Y);
				float Val = (float) Input[Y, X];
				ShortAvg += Val / ShortLength;
				LastVars[k] = (float) Input[Y, X];
			}
			LongAvg = ShortAvg;
			LongValue = ShortAvg;
			ShortValue = ShortAvg;
			HTSum = LongValue + ShortValue;
			for (k = ShortLength; k < LongLength; k++) LastVars[k] = ShortAvg;

			float RevIC = (float) (1 / IncTh);
			float RevSL = 1.0f / ShortLength;
			float RevLL = 1.0f / LongLength;
			//float RevPI = (float) (1 / PI);

			int LV = 0;
			for (k = ShortLength; k < N; k++, pt.Increment(LineVector))
			{
				/* Getting the value */
				int X = (int) Round(pt.X);
				int Y = (int) Round(pt.Y);
				float Val = (float) Input[Y, X];
				/* Computing the long and short averages */
				ShortAvg += (Val - LastVars[LV]) * RevSL;
				LongAvg += (Val - LastVars[RollingPtr]) * RevLL;
				LastVars[RollingPtr] = Val;
				/* Compute the weights using averages */
				/* First is the logarithm multiplier - the ratio of exponential decay of intensity */
				float M1 = LongAvg * RevIC;
				if (M1 < -0.8f) M1 = -0.8f;
				float LgMult = M1 / (1.0f + M1);
				//double LgMult = Atan(LongAvg * RevIC - 1) / PI + 0.5;
				/* The new weight is computed */
				
				LongValue = LongValue * LgMult + LongAvg;
				if (LongValue < 0) LongValue = 0;
				//double M2 = LongValue * RevIC - 1;
				//double XLgMult = M2 / (2 + M2);
				/* Scaling the weight on 0-1 */
				//float dXLgMult = (float) (Atan(LongValue * RevIC - 1) * RevPI + 0.5);
				float XLgMult = FAtanS(LongValue * RevIC);
				/* Computing the sum */
				float CVal = ShortAvg * 2 * XLgMult;
				HTSum += CVal;


				RollingPtr = (RollingPtr + 1) % LongLength;
				LV = (LV + 1) % LongLength;
			}
			HoughSum = HTSum;
		}

		const int FAtanCount = 50;
		static float[] FAtanValues = FAtanGen();
		static float RevPI100 = (float) (4 * FAtanCount / PI);
		static float FAtanS(float Tan)
		{
			if (Tan <= 2.0f && Tan >= 0.0f)
			{
				int pip = (int) (Tan * FAtanCount);
				return FAtanValues[pip];
			}
			else
			{
				Tan = 1.0f / (Tan - 1.0f) + 1.0f;
				int pip = (int) (Tan * FAtanCount);
				return 1.5f - FAtanValues[pip];
			}
		}

		static float[] FAtanGen()
		{
			FAtanValues = new float[2 * FAtanCount + 1];
			for (int i = 0; i < FAtanValues.Length; i++) FAtanValues[i] = (float) (Atan((i - FAtanCount) / (float) FAtanCount) / PI + 0.5);
			return FAtanValues;
		}
	}
}
