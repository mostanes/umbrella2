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
		/// <param name="DetectionParameters">Image-specific algorithm parameters.</param>
		/// <param name="HoughSum">Hough transform output for given coordinates.</param>
		static void Lineover(double[,] Input, int Height, int Width, double Rho, double Theta, ImageParameters DetectionParameters, out double HoughSum, out double LineLength)
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

			if (N < DetectionParameters.ShortAvgLength) { HoughSum = 0; return; }

			int ShortLength = DetectionParameters.ShortAvgLength;
			int LongLength = DetectionParameters.LongAvgLength;
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

			float RevIC = (float) (1 / DetectionParameters.IncreasingThreshold);
			float LgBaseMul = (float) (DetectionParameters.DefaultRatio);
			float LgExtraMul = (float) (DetectionParameters.MaxRatio - DetectionParameters.DefaultRatio);
			float MaxMul = (float) DetectionParameters.MaxMultiplier;
			float Zero = (float) DetectionParameters.ZeroLevel;
			float RevSL = 1.0f / ShortLength;
			float RevLL = 1.0f / LongLength;
			//float RevPI = (float) (1 / PI);

			int LV = 0;
			for (k = ShortLength; k < N; k++, pt.Increment(LineVector))
			{
				/* Getting the value */
				int X = (int) Round(pt.X);
				int Y = (int) Round(pt.Y);
				float Val = (float) Input[Y, X] - Zero;
				/* Computing the long and short averages */
				ShortAvg += (Val - LastVars[LV]) * RevSL;
				LongAvg += (Val - LastVars[RollingPtr]) * RevLL;
				LastVars[RollingPtr] = Val;
				/* Compute the weights using averages */
				/* First is the logarithm multiplier - the ratio of exponential decay of intensity */
				float M1 = 2 * LongAvg * RevIC;
				if (M1 < 0f) M1 = 0f;
				float LgMult = M1 / (1.0f + M1) * LgExtraMul + LgBaseMul;
				if (LgMult > MaxMul) LgMult = MaxMul;
				
				/* The new weight is computed */
				LongValue = LongValue * LgMult + LongAvg;
				if (LongValue < 0) LongValue = 0;

				/* Scaling the weight on 0.25-1 */
				float XLgMult = FAtanS(LongValue * RevIC + 0.5f);

				/* Computing the sum */
				float CVal = ShortAvg * 2 * XLgMult;
				if (CVal > 0)
					HTSum += CVal;


				RollingPtr = (RollingPtr + 1) % LongLength;
				LV = (LV + 1) % LongLength;
			}
			HoughSum = HTSum;
		}

		/// <summary>
		/// Number of entries in approximation table.
		/// </summary>
		const int FAtanCount = 50;
		/// <summary>
		/// The approximation table.
		/// </summary>
		static float[] FAtanValues = FAtanGen();

		/// <summary>
		/// Fast atan(-like) function with values scaled on 0.25-1.
		/// </summary>
		/// <remarks>
		/// Implementation is an approximation of 0.5 + Atan(Tan-1) / PI.
		/// </remarks>
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

		/// <summary>
		/// Computes the <code>FAtanS</code> tables.
		/// </summary>
		/// <returns>A table of 32-bit floats that approximate the Atan function.</returns>
		static float[] FAtanGen()
		{
			FAtanValues = new float[2 * FAtanCount + 1];
			for (int i = 0; i < FAtanValues.Length; i++) FAtanValues[i] = (float) (Atan((i - FAtanCount) / (float) FAtanCount) / PI + 0.5);
			return FAtanValues;
		}
	}
}
