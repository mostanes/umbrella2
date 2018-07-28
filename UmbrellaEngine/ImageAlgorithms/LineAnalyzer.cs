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
		internal static List<LineDetection> AnalyzeLine(double[,] Input, bool[,] AnalyzeMask, int Height, int Width, double Rho, double Theta, double HighTh, double LowTh, int MaxIgnore, int PSFSize, int OX, int OY)
		{
			Vector LineVector = new Vector() { X = Cos(Theta), Y = Sin(Theta) };
			Vector LineOrigin = new Vector() { X = -Rho * Sin(Theta), Y = Rho * Cos(Theta) };
			Vector LONormal = new Vector() { X = -Sin(Theta), Y = Cos(Theta) };
			var r = LineIntersection.IntersectLeft(LineOrigin, LineVector, Width, Height);
			if (r == null) { return null; }
			Vector LeftIntersect = r.Item1;
			double LDist = r.Item2;
			r = LineIntersection.IntersectRight(LineOrigin, LineVector, Width, Height);
			if (r == null) { return null; }
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

			bool OnSegment = false;
			int LastK = 0;
			List<Vector> LineIntervals = new List<Vector>();
			List<DetectionBlob> Blobs = new List<DetectionBlob>();

			for (k = 0; k < N; k++, pt.Increment(LineVector))
			{
				int l;
				Vector vl = pt;
				for (l = -PSFSize; l < PSFSize; l++, vl.Increment(LONormal))
				{
					int X = (int) Round(vl.X);
					int Y = (int) Round(vl.Y);
					if (X < 0 || X >= Width) continue;
					if (Y < 0 || Y >= Height) continue;
					if (AnalyzeMask[Y, X]) continue;
					double Val = Input[Y, X];

					if (Val > HighTh)
					{
						DetectionBlob db = BitmapFill(Input, new IntPoint() { X = X, Y = Y }, AnalyzeMask, LowTh, Theta);
						LineIntervals.Add(new Vector() { X = db.LineStart, Y = db.LineEnd });
						Blobs.Add(db);
					}
				}
			}

			List<DetectionSegment> FoundSegments = new List<DetectionSegment>();
			DetectionSegment cseg = default(DetectionSegment);
			for (k = 0; k < LineIntervals.Count; k++)
			{
				if (cseg.Blobs != null)
				{
					if (Min(LineIntervals[k].X, LineIntervals[k].Y) < cseg.End + MaxIgnore)
					{
						cseg.Blobs.Add(Blobs[k]);
						cseg.End = Max(cseg.End, Max(LineIntervals[k].Y, LineIntervals[k].X));
						continue;
					}
					else { FoundSegments.Add(cseg); cseg = default(DetectionSegment); }
				}
					cseg.Blobs = new List<DetectionBlob>();
					cseg.Angle = Theta;
					cseg.Blobs.Add(Blobs[k]);
					cseg.Start = Min(LineIntervals[k].X, LineIntervals[k].Y);
					cseg.End = Max(LineIntervals[k].Y, LineIntervals[k].X);
			}
			if (cseg.Blobs != null) FoundSegments.Add(cseg);
			List<LineDetection> Detections = FoundSegments.Select((x) => MergeBlobs(x, Input, OX, OY)).ToList();
			return Detections;
		}

		struct DetectionSegment
		{
			internal List<DetectionBlob> Blobs;
			internal double Angle;
			internal double Start;
			internal double End;
		}

		internal struct IntPoint { internal int X, Y; }

		struct DetectionBlob
		{
			internal List<IntPoint> Points;
			internal double LineStart, LineEnd;
		}

		/// <summary>
		/// Gets the connected component starting from a point on an image.
		/// </summary>
		/// <param name="Input">Input Image.</param>
		/// <param name="StartPoint">Starting point.</param>
		/// <param name="Mask">Already processed components mask.</param>
		/// <param name="LowThreshold">Threshold for component discrimination.</param>
		/// <param name="Angle">Angle of the line; used for distance projections.</param>
		/// <returns>The connected component blob.</returns>
		static DetectionBlob BitmapFill(double[,] Input, IntPoint StartPoint, bool[,] Mask, double LowThreshold, double Angle)
		{
			Queue<IntPoint> PointQ = new Queue<IntPoint>();
			PointQ.Enqueue(StartPoint);
			List<IntPoint> DiscoveredPoints = new List<IntPoint>();
			
			double LineMin = double.MaxValue, LineMax = double.MinValue;
			while (PointQ.Count > 0)
			{
				IntPoint pt = PointQ.Dequeue();
				if (pt.X < 0 || pt.X >= Mask.GetLength(1)) continue;
				if (pt.Y < 0 || pt.Y >= Mask.GetLength(0)) continue;
				if (Mask[pt.Y, pt.X]) continue;
				double Val = Input[pt.Y, pt.X];
				double MinMax = pt.X * Cos(Angle) + pt.Y * Sin(Angle);
				if (MinMax < LineMin) LineMin = MinMax;
				if (MinMax > LineMax) LineMax = MinMax;
				if (Val > LowThreshold)
				{
					Mask[pt.Y, pt.X] = true;
					
					DiscoveredPoints.Add(pt);
					PointQ.Enqueue(new IntPoint() { X = pt.X - 1, Y = pt.Y });
					PointQ.Enqueue(new IntPoint() { X = pt.X + 1, Y = pt.Y });
					PointQ.Enqueue(new IntPoint() { X = pt.X, Y = pt.Y - 1 });
					PointQ.Enqueue(new IntPoint() { X = pt.X, Y = pt.Y + 1 });
				}
			}
			DetectionBlob db = new DetectionBlob() { Points = DiscoveredPoints, LineStart = LineMin, LineEnd = LineMax };
			return db;
		}

		internal class LineDetection
		{
			internal List<PixelPoint> Points;
			internal List<double> PointValues;
			internal double EigenValue1;
			internal double EigenValue2;
			internal double EigenAngle1;
			internal double EigenAngle2;
			internal PixelPoint PointsCenter;
			internal PixelPoint Barycenter;
			internal double Flux;

			public override string ToString()
			{
				return "[LD]:[" + Barycenter + "]:{Cnr=" + Points.Count + ", a=" + (Sqrt(EigenValue1) * 2).ToString("G6") + ", b=" + (Sqrt(EigenValue2) * 2).ToString("G6") + ", uX=" +
					Cos(EigenAngle1).ToString("G6") + ", uY=" + Sin(EigenAngle1).ToString("G6") + "}";
			}

		}

		/// <summary>
		/// Merges related connected classes in one LineDetection.
		/// </summary>
		/// <param name="segment">Segment pieces holder.</param>
		/// <param name="Input">Input image.</param>
		/// <param name="OX">Delta between the data array and actual position, X component.</param>
		/// <param name="OY">Delta between the data array and actual position, Y component.</param>
		/// <returns>A LineDetection from the connected classes.</returns>
		static LineDetection MergeBlobs(DetectionSegment segment, double[,] Input, int OX, int OY)
		{
			double Xmean = 0, Ymean = 0;
			double XX = 0, XY = 0, YY = 0;
			double Flux = 0;
			double XBmean = 0, YBmean = 0;
			List<double> PValues = new List<double>();
			List<IntPoint> MergedPoints = segment.Blobs.Aggregate(new List<IntPoint>(), (x, y) => { x.AddRange(y.Points); return x; });
			foreach (IntPoint pt in MergedPoints)
			{
				double Val = Input[pt.Y, pt.X];
				Xmean += pt.X; Ymean += pt.Y;
				XBmean += Val * pt.X; YBmean += Val * pt.Y;
				XX += pt.X * pt.X; XY += pt.X * pt.Y; YY += pt.Y * pt.Y;
				Flux += Val;
				PValues.Add(Val);
			}
			Xmean /= MergedPoints.Count;
			Ymean /= MergedPoints.Count;
			XBmean /= Flux;
			YBmean /= Flux;
			XX /= MergedPoints.Count;
			XY /= MergedPoints.Count;
			YY /= MergedPoints.Count;
			XX -= Xmean * Xmean;
			XY -= Xmean * Ymean;
			YY -= Ymean * Ymean;

			double Msq = Sqrt(XX * XX + 4 * XY * XY - 2 * XX * YY + YY * YY);
			double L1 = 1.0 / 2 * (XX + YY - Msq);
			double L2 = 1.0 / 2 * (XX + YY + Msq);
			double A1 = Atan2(2 * XY, -(-XX + YY + Msq));
			double A2 = Atan2(2 * XY, -(-XX + YY - Msq));

			LineDetection ld = new LineDetection()
			{
				Points = MergedPoints.Select((x) => new PixelPoint() { X = x.X + OX, Y = x.Y + OY }).ToList(),
				EigenValue1 = L1,
				EigenValue2 = L2,
				EigenAngle1 = A1,
				EigenAngle2 = A2,
				Barycenter = new PixelPoint() { X = XBmean + OX, Y = YBmean + OY },
				PointsCenter = new PixelPoint() { X = Xmean + OX, Y = Ymean + OY },
				Flux = Flux,
				PointValues = PValues
			};

			return ld;
		}
	}
}
