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
		public static HTResult RunRLHT(double[,] Input, double IncTh, double SegCrTh, double SegDropTh, double MinLength, double MinFlux, double StrongHoughTh)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double Rhomax = Sqrt(Height * Height + Width * Width);
			return RunRLHT(Input, IncTh, SegCrTh, SegDropTh, MinLength, MinFlux, StrongHoughTh, 4, 4, 0, Sqrt(Height * Height + Width * Width), 0, 2 * PI);
		}

		public static HTResult RunRLHT(double[,] Input, double IncTh, double SegCrTh, double SegDropTh, double MinLen, double MinFx, double SHTh, double SkA, double SkR, double StRad, double EndRad, double StAng, double EndAng)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(SkA, Height), Atan2(SkA, Width));
			double NTheta = (EndAng - StAng) / ThetaUnit;
			double[,] HTMatrix = new double[(int) Round(EndRad / SkR), (int) Round(NTheta)];
			int NRd = HTMatrix.GetLength(0);
			int NTh = HTMatrix.GetLength(1);
			int i, j;
			List<Segment> SegSet = new List<Segment>();
			List<Vector> HoughPowerul = new List<Vector>();
			for (i = 0; i < NRd; i++)
			{
				for (j = 0; j < NTh; j++)
				{
					double Theta = j * ThetaUnit + StAng;
					if (Theta > PI / 2) if (Theta < PI) continue;
					Lineover(Input, Height, Width, SkR * i + StRad, Theta, IncTh, SegCrTh, SegDropTh, out List<Segment> Segments, out HTMatrix[i, j]);
					if (Segments != null) if (Segments.Count > 0)
							SegSet.AddRange(Segments.Where((x) => x.Length > MinLen & x.Flux > MinFx));
					if (HTMatrix[i, j] > SHTh) HoughPowerul.Add(new Vector() { X = SkR * i + StRad, Y = Theta });
				}
			}
			return new HTResult() { HTMatrix = HTMatrix, Segments = SegSet, StrongPoints = HoughPowerul };
		}

		public struct HTResult
		{
			internal double[,] HTMatrix;
			internal List<Segment> Segments;
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

		static void Lineover(double[,] Input, int Height, int Width, double Rho, double Theta, double IncTh, double SegOnTh, double SegDropTh, out List<Segment> Segments, out double HoughSum)
		{
			Vector LineVector = new Vector() { X = Cos(Theta), Y = Sin(Theta) };
			Vector LineOrigin = new Vector() { X = -Rho * Sin(Theta), Y = Rho * Cos(Theta) };
			var r = LineIntersection.IntersectLeft(LineOrigin, LineVector, Width, Height);
			if (r == null) { Segments = null; HoughSum = 0; return; }
			Vector LeftIntersect = r.Item1;
			double LDist = r.Item2;
			r = LineIntersection.IntersectRight(LineOrigin, LineVector, Width, Height);
			if (r == null) { Segments = null; HoughSum = 0; return; }
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

			if (N < 10) { Segments = null; HoughSum = 0; return; }

			const int ShortLength = 5;
			const int LongLength = 35;
			const double LongMultiplier = 5;
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

			bool OnSegment = false;
			Vector SegmentStart = default(Vector);
			Vector SegmentEnd = default(Vector);
			double SegInt = 0;
			int SegCount = 0;
			Segments = new List<Segment>();
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
				//double ShMult = Atan(ShortAvg / IncTh - 1) / PI + 0.5;
				double LgMult = Atan(LongAvg * RevIC - 1) / PI + 0.5;
				//ShortValue = ShortValue * ShMult + ShortAvg;
				LongValue = LongValue * LgMult + LongAvg;
				//double XShMult = Atan(ShortValue / IncTh - 1) / PI + 0.5;
				double XLgMult = Atan(LongValue * RevIC - 1) / PI + 0.5;
				//double CVal = ShortAvg * 2 * (XShMult + LongMultiplier * XLgMult) / (1 + LongMultiplier);
				double CVal = ShortAvg * 2 * XLgMult;
				HTSum += CVal;

				if (CVal > SegOnTh)
				{
					if (!OnSegment)
					{
						OnSegment = true;
						SegmentStart = pt;
						SegInt = 0;
						SegCount = 0;
					}
					else
					{ SegInt += Val; SegCount++; SegmentEnd = pt; }
				}
				if (CVal < SegDropTh)
				{
					if (OnSegment)
					{
						OnSegment = false;
						if (SegInt > SegCount * SegDropTh)
							Segments.Add(new Segment() { Start = SegmentStart, End = SegmentEnd, Intensity = SegInt / SegCount, Flux = SegInt, Angle = Theta, Radius = Rho });
					}
				}
				
				RollingPtr = (RollingPtr + 1) % LongLength;
			}
			HoughSum = HTSum;
		}
	}
}
