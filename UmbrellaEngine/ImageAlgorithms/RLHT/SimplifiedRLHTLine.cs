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
		static void SimpleLineover(double[,] Input, int Height, int Width, double Rho, double Theta, ImageParameters DetectionParameters, out double HoughSum, out double LineLength)
		{
			/* Set up geometry */
			Vector LineVector = new Vector() { X = Cos(Theta), Y = Sin(Theta) };
			Vector LineOrigin = new Vector() { X = -Rho * Sin(Theta), Y = Rho * Cos(Theta) };
			var r = LineIntersection.IntersectLeft(LineOrigin, LineVector, Width, Height);
			if (r == null) { HoughSum = 0; LineLength = 0; return; }
			Vector LeftIntersect = r.Item1;
			double LDist = r.Item2;
			r = LineIntersection.IntersectRight(LineOrigin, LineVector, Width, Height);
			if (r == null) { HoughSum = 0; LineLength = 0; return; }
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
			LineLength = End - Start;
			int N = (int) LineLength;
			Vector pt;

			if (N < 2 * DetectionParameters.LongAvgLength) { HoughSum = 0; return; /* If image segment is too short, ignore */ }

			int LongLength = DetectionParameters.LongAvgLength;
			float LongAvg;
			float[] LastVars = new float[LongLength];
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
			
			for (k = LongLength; k < N; k++, pt.Increment(LineVector))
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

					HTSum += CVal;
				}

				RollingPtr = (RollingPtr + 1) % LongLength;
			}
			HoughSum = HTSum;
		}
	}
}
