using System;
using static System.Math;
using Umbrella2.Algorithms.Geometry;
using System.Runtime.InteropServices;

namespace Umbrella2.Algorithms.Images
{
	public partial class RLHT
	{
		/// <summary>
		/// Runs the Hough Transform over a line.
		/// </summary>
		/// <param name="Input">Input image data.</param>
		/// <param name="Height">Image Height.</param>
		/// <param name="Width">Image Width.</param>
		/// <param name="Rho">Radial coordinate.</param>
		/// <param name="Theta">Angular coordinate.</param>
		/// <param name="DetectionParameters">Image-specific algorithm parameters.</param>
		/// <param name="HoughSum">Hough transform output for given coordinates.</param>
		/// <param name="LineLength">Length of the line scanned.</param>
		/// <param name="LastVars">Data pool to be recycled between calls. Initialized internally.</param>
		/// <param name="LineSkip">Amount of pixels by which to skip when computing the line score.</param>
		static void SimpleLineover(double[,] Input, int Height, int Width, double Rho, double Theta, ImageParameters DetectionParameters, out double HoughSum, out double LineLength, ref float[] LastVars, int LineSkip)
		{
			/* Set up geometry */
			Vector LineVector = new Vector() { X = Cos(Theta), Y = Sin(Theta) };
			Vector LineOrigin = new Vector() { X = -Rho * Sin(Theta), Y = Rho * Cos(Theta) };
			Vector LeftIntersect;
			double LDist;
			if (!LineIntersection.IntersectLeft(LineOrigin, LineVector, Width, Height, out LeftIntersect, out LDist)) { HoughSum = 0; LineLength = 0; return; }
			
			Vector RightIntersect;
			double RDist;
			if (!LineIntersection.IntersectRight(LineOrigin, LineVector, Width, Height, out RightIntersect, out RDist)) { HoughSum = 0; LineLength = 0; return; }

			double Start = Min(LDist, RDist);
			double End = Max(LDist, RDist);
			Vector StVec, EVec;

			if (Start == LDist && End == RDist) { StVec = LeftIntersect; EVec = RightIntersect; }
			else if (Start == RDist && End == LDist) { StVec = RightIntersect; EVec = LeftIntersect; }
			else throw new ApplicationException("Geometry error.");

			/* Computational part starts here */

			int k;
			LineLength = End - Start;
			int N = (int) LineLength;
			Vector pt;

			if (N < 2 * DetectionParameters.LongAvgLength) { HoughSum = 0; return; /* If image segment is too short, ignore */ }

			int LongLength = DetectionParameters.LongAvgLength;
			float LongAvg;
			if (LastVars == null) LastVars = new float[LongLength];
			int RollingPtr;
			float LongValue;
			float HTSum;

			/* Computing initial values for rolling weights */

			pt = StVec; RollingPtr = 0; LongAvg = 0;
			for (k = 0; k < LongLength; k++, pt.Increment(LineVector))
			{
				int X = (int) Round(pt.X);
				int Y = (int) Round(pt.Y);
				float Val = (float) Input[Y, X];
				LongAvg += Val;
				LastVars[k] = Val;
			}
			LongAvg /= LongLength;
			LongValue = LongAvg;
			HTSum = LongValue;

			float RevIC = (float) (1 / DetectionParameters.IncreasingThreshold);
			float LgBaseMul = (float) (DetectionParameters.DefaultRatio);
			float LgExtraMul = (float) (DetectionParameters.MaxRatio - DetectionParameters.DefaultRatio);
			float MaxMul = (float) DetectionParameters.MaxMultiplier;
			float Zero = (float) DetectionParameters.ZeroLevel;
			float RevLL = 1.0f / LongLength;
			float XRLL = RevIC * RevLL;

			Vector LineIncrement = LineSkip * LineVector;
			for (k = LongLength; k < N; k+=LineSkip, pt.Increment(LineIncrement))
			{
				/* Getting the value */
				int X = (int) Round(pt.X);
				int Y = (int) Round(pt.Y);
				float Val = (float) Input[Y, X] - Zero;
				/* Computing the long average */
				/* Automatic scaling with Increasing Threshold */
				LongAvg += (Val - LastVars[RollingPtr]) * XRLL;
				LastVars[RollingPtr] = Val;
				/* Compute the weights using averages */
				/* First is the logarithm multiplier - the ratio of exponential decay of intensity */
				/* Scale in interval [DefaultRatio, MaxRatio), with MaxRatio as the infinity limit of LongAvg */
				float LgMult = LongAvg < 0f ? LgBaseMul : LongAvg / (1.0f + LongAvg) * LgExtraMul + LgBaseMul;

				/* The new weight is computed */
				if (LineSkip > 2)
				{
					float flsk = FPow(LgMult, LineSkip);
					if (Abs(flsk - 1.0f) < 0.0001) LongValue = LongValue + LineSkip * LongAvg;
					else LongValue = LongValue * flsk + LongAvg * (flsk - 1.0f) / (LgMult - 1.0f);
				}
				else
					LongValue = LongValue * LgMult + LongAvg;

				if (LongValue < 0) LongValue = 0;
				
				if (Val > 0) /* Next part can be skipped if Val <= 0 */
				{
					/* Scaling the weight on 0.25-1 */
					float XLgMult = FAtanS(LongValue);
					/* Rescaling on 0.5-6.5 */
					XLgMult = 8 * XLgMult - 1.5f; /* 2 * (4*XLM - 0.75f) */

					/* Computing the sum */
					float CVal = Val * XLgMult;

					HTSum += CVal * LineSkip;
				}

				RollingPtr = (RollingPtr + LineSkip) % LongLength;
			}
			HoughSum = HTSum;
		}

		static float FPow(float Base, int Exponent)
		{
			float f1;
			switch (Exponent)
			{
				case 1: return Base;
				case 2: return Base * Base;
				case 3: return Base * Base * Base;
				case 4: f1 = Base * Base; return f1 * f1;
				case 5: f1 = Base * Base; return f1 * f1 * Base;
				case 6: f1 = Base * Base; return f1 * f1 * f1;
				case 7: f1 = Base * Base * Base; return f1 * f1 * Base;
				case 8: Base *= Base; Base *= Base; return Base * Base;
				case 9: f1 = Base * Base * Base; return f1 * f1 * f1;
				case 10: Base *= Base; f1 = Base * Base; return f1 * f1 * Base;
			}
			return (float) Pow(Base, Exponent);
		}
	}
}
