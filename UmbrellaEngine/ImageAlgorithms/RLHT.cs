using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.Algorithms.Geometry;
using static System.Math;

namespace Umbrella2.Algorithms.Images
{
	public static class RLHT
	{
		public static HTResult RunRLHT(double[,] Input, double IncTh, double MinLength, double MinFlux, double StrongHoughTh)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double Rhomax = Sqrt(Height * Height + Width * Width);
			return RunRLHT(Input, IncTh, MinLength, MinFlux, StrongHoughTh, 4, 4, 0, Sqrt(Height * Height + Width * Width), 0, 2 * PI);
		}

		public static HTResult RefineRLHT(double[,] Input, double IncTh, double MinLength, double MinFlux, double StrongHoughTh, double Rho, double Theta)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(1, Height), Atan2(1, Width));
			return RunRLHT(Input, IncTh, MinLength, MinFlux, StrongHoughTh, 1, 1, Rho - 10, Rho + 10, Theta - 10 * ThetaUnit, Theta + 10 * ThetaUnit);
		}

		public static HTResult RunRLHT(double[,] Input, double IncTh,double MinLen, double MinFx, double SHTh, double SkA, double SkR, double StRad, double EndRad, double StAng, double EndAng)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(SkA, Height), Atan2(SkA, Width));
			double NTheta = (EndAng - StAng) / ThetaUnit;
			double[,] HTMatrix = new double[(int) Round((EndRad - StRad) / SkR), (int) Round(NTheta)];
			int NRd = HTMatrix.GetLength(0);
			int NTh = HTMatrix.GetLength(1);
			int i, j;
			List<Vector> HoughPowerul = new List<Vector>();
			for (i = 0; i < NRd; i++)
			{
				for (j = 0; j < NTh; j++)
				{
					double Theta = j * ThetaUnit + StAng;
					if (Theta > PI / 2) if (Theta < PI) continue;
					Lineover(Input, Height, Width, SkR * i + StRad, Theta, IncTh, out HTMatrix[i, j]);
					if (HTMatrix[i, j] > SHTh) HoughPowerul.Add(new Vector() { X = SkR * i + StRad, Y = Theta });
				}
			}
			return new HTResult() { HTMatrix = HTMatrix, StrongPoints = HoughPowerul };
		}

		public struct HTResult
		{
			internal double[,] HTMatrix;
			//internal List<Segment> Segments;
			internal List<Vector> StrongPoints;
		}

		public struct Segment
		{
			internal Vector Start, End;
			internal double Intensity;
			internal double Flux;
			internal double Angle;
			internal double Radius;

			internal double Length { get => Sqrt((Start.X - End.X) * (Start.X - End.X) + (Start.Y - End.Y) * (Start.Y - End.Y)); }

			public override string ToString() { return Start.ToString() + "  ---->  " + End.ToString() + "    |  " + Intensity.ToString("E5"); }
		}

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
				double LgMult = Atan(LongAvg * RevIC - 1) / PI + 0.5;
				LongValue = LongValue * LgMult + LongAvg;
				double XLgMult = Atan(LongValue * RevIC - 1) / PI + 0.5;
				double CVal = ShortAvg * 2 * XLgMult;
				HTSum += CVal;

				
				RollingPtr = (RollingPtr + 1) % LongLength;
			}
			HoughSum = HTSum;
		}
	}
}
