using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.Algorithms.Geometry;
using static System.Math;

namespace Umbrella2.Algorithms.Images
{
	class LineAnalyzer
	{
		static void AnalyzeLine(double[,] Input, bool[,] AnalyzeMask, int Height, int Width, double Rho, double Theta, double HighTh, double LowTh, int MaxIgnore)
		{
			Vector LineVector = new Vector() { X = Cos(Theta), Y = Sin(Theta) };
			Vector LineOrigin = new Vector() { X = -Rho * Sin(Theta), Y = Rho * Cos(Theta) };
			var r = LineIntersection.IntersectLeft(LineOrigin, LineVector, Width, Height);
			if (r == null) { return; }
			Vector LeftIntersect = r.Item1;
			double LDist = r.Item2;
			r = LineIntersection.IntersectRight(LineOrigin, LineVector, Width, Height);
			if (r == null) { return; }
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
			Vector pt = StVec;

			List<RLHT.Segment> SegmentsFound = new List<RLHT.Segment>();
			RLHT.Segment cseg = default(RLHT.Segment);
			bool OnSegment = false;
			int LastK = 0;

			for (k = 0; k < N; k++, pt.Increment(LineVector))
			{
				int X = (int) Math.Round(pt.X);
				int Y = (int) Math.Round(pt.Y);
				if (AnalyzeMask[Y, X]) continue;
				double Val = Input[Y, X];

				if (Val > HighTh) BitmapFill(Input, new IntPoint() { X = X, Y = Y }, AnalyzeMask, LowTh);
				if (Val >= LowTh)
				{
					if (!OnSegment) { cseg.Start = pt; }
					LastK = k;
					cseg.End = pt;
				}
				if (Val < LowTh / 2) if (OnSegment) if (k - LastK > MaxIgnore) { OnSegment = false; SegmentsFound.Add(cseg); }
			}
			if (OnSegment) SegmentsFound.Add(cseg);
		}

		struct IntPoint { internal int X, Y; }

		struct DetectionBlob
		{
			internal List<IntPoint> Points;
			internal double EigenValue1;
			internal double EigenValue2;
			internal double EigenAngle1;
			internal double EigenAngle2;
			internal PixelPoint PointsCenter;
			internal PixelPoint Barycenter;
		}

		static void BitmapFill(double[,] Input, IntPoint StartPoint, bool[,] Mask, double LowThreshold)
		{
			Queue<IntPoint> PointQ = new Queue<IntPoint>();
			PointQ.Enqueue(StartPoint);
			List<IntPoint> DiscoveredPoints = new List<IntPoint>();
			double Xmean = 0, Ymean = 0;
			double XX = 0, XY = 0, YY = 0;
			double Flux = 0;
			double XBmean = 0, YBmean = 0;
			while (PointQ.Count > 0)
			{
				IntPoint pt = PointQ.Dequeue();
				if (pt.X < 0 || pt.X >= Mask.GetLength(1)) continue;
				if (pt.Y < 0 || pt.Y >= Mask.GetLength(0)) continue;
				if (Mask[pt.Y, pt.X]) continue;
				double Val = Input[pt.Y, pt.X];
				if (Val > LowThreshold)
				{
					Mask[pt.Y, pt.X] = true;
					Xmean += pt.X; Ymean += pt.Y;
					XBmean += Val * pt.X; YBmean += Val * pt.Y;
					XX += pt.X * pt.X * Val; XY += pt.X * pt.Y * Val; YY += pt.Y * pt.Y * Val;
					Flux += Val;
					DiscoveredPoints.Add(pt);
					PointQ.Enqueue(new IntPoint() { X = pt.X - 1, Y = pt.Y });
					PointQ.Enqueue(new IntPoint() { X = pt.X + 1, Y = pt.Y });
					PointQ.Enqueue(new IntPoint() { X = pt.X, Y = pt.Y - 1 });
					PointQ.Enqueue(new IntPoint() { X = pt.X, Y = pt.Y + 1 });
				}
			}
			Xmean /= DiscoveredPoints.Count;
			Ymean /= DiscoveredPoints.Count;
			XBmean /= Flux;
			YBmean /= Flux;
			XX /= Flux;
			XY /= Flux;
			YY /= Flux;
			XX -= XBmean * XBmean;
			XY -= XBmean * YBmean;
			YY -= YBmean * YBmean;



			double Msq = Sqrt(XX * XX + 4 * XY * XY - 2 * XX * YY + YY * YY);
			double L1 = 1.0 / 2 * (XX + YY - Msq);
			double L2 = 1.0 / 2 * (XX + YY + Msq);
			double A1 = Atan2(-(-XX + YY + Msq), 2 * XY);
			double A2 = Atan2(-(-XX + YY - Msq), 2 * XY);

			DetectionBlob db = new DetectionBlob() { Points = DiscoveredPoints, EigenValue1 = L1, EigenValue2 = L2, EigenAngle1 = A1, EigenAngle2 = A2 };
			
		}
	}
}
