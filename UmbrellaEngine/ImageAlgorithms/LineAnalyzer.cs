using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.Algorithms.Geometry;
using static System.Math;

namespace Umbrella2.Algorithms.Images
{
	/// <summary>
	/// Algorithm that analyzes line using a hysteresis connected component algorithm for detecting luminous blobs and merges the blobs into line segments.
	/// </summary>
	class LineAnalyzer
	{
		/// <summary>
		/// Scans a line on the image for line segments using a hysteresis connected component algorithm.
		/// </summary>
		/// <param name="Input">Input data.</param>
		/// <param name="AnalyzeMask">Mask for marking visited pixels.</param>
		/// <param name="Height">Data height.</param>
		/// <param name="Width">Data width.</param>
		/// <param name="Rho">Distance from origin to line.</param>
		/// <param name="Theta">Line angle.</param>
		/// <param name="HighTh">Upper hysteresis threshold.</param>
		/// <param name="LowTh">Lower hysteresis threshold.</param>
		/// <param name="MaxIgnore">Maximum interblob distance.</param>
		/// <param name="ScanWidth">Width of the scanned area.</param>
		/// <param name="OX">Image data origin X coordinate.</param>
		/// <param name="OY">Image data origin Y coordinate.</param>
		/// <returns>A list of line segment detections.</returns>
		internal static List<LineDetection> AnalyzeLine(double[,] Input, bool[,] AnalyzeMask, int Height, int Width, double Rho, double Theta, double HighTh, double LowTh, int MaxIgnore, int ScanWidth, int OX, int OY)
		{
			/* Unit vector in the direction of the line */
			Vector LineVector = new Vector() { X = Cos(Theta), Y = Sin(Theta) };
			/* Origin of the line */
			Vector LineOrigin = new Vector() { X = -Rho * Sin(Theta), Y = Rho * Cos(Theta) };
			/* Unit vector perpendicular to the line */
			Vector LONormal = new Vector() { X = -Sin(Theta), Y = Cos(Theta) };

			/* Compute the intersections with the bounding box */
			var r = LineIntersection.IntersectLeft(LineOrigin, LineVector, Width, Height);
			if (r == null) { return null; }
			Vector LeftIntersect = r.Item1;
			double LDist = r.Item2;
			r = LineIntersection.IntersectRight(LineOrigin, LineVector, Width, Height);
			if (r == null) { return null; }
			Vector RightIntersect = r.Item1;
			double RDist = r.Item2;

			/* Sort the intersections */
			double Start = Min(LDist, RDist);
			double End = Max(LDist, RDist);
			Vector StVec, EVec;

			if (Start == LDist && End == RDist) { StVec = LeftIntersect; EVec = RightIntersect; }
			else if (Start == RDist && End == LDist) { StVec = RightIntersect; EVec = LeftIntersect; }
			else throw new ApplicationException("Geometry error.");

			/* Scan line for blobs */
			int k;
			int N = (int) (End - Start);
			Vector pt = StVec;

			List<Vector> LineIntervals = new List<Vector>();
			List<DetectionBlob> Blobs = new List<DetectionBlob>();

			for (k = 0; k < N; k++, pt.Increment(LineVector))
			{
				int l;
				Vector vl = pt;
				for (l = -ScanWidth; l < ScanWidth; l++, vl.Increment(LONormal))
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

			/* Merge blobs into line segments */
			List<DetectionSegment> FoundSegments = new List<DetectionSegment>();
			DetectionSegment cseg = default(DetectionSegment); /* Current segment */
			for (k = 0; k < LineIntervals.Count; k++)
			{
				if (cseg.Blobs != null)
				{
					/* If still within threshold of current segment */
					if (Min(LineIntervals[k].X, LineIntervals[k].Y) < cseg.End + MaxIgnore)
					{
						cseg.Blobs.Add(Blobs[k]);
						cseg.End = Max(cseg.End, Max(LineIntervals[k].Y, LineIntervals[k].X));
						continue;
					}
					else { FoundSegments.Add(cseg); cseg = default(DetectionSegment); }
				}
				/* Create new current segment */
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

		/// <summary>
		/// A candidate line segment detection.
		/// </summary>
		struct DetectionSegment
		{
			/// <summary>
			/// List of blobs that make up the line segment.
			/// </summary>
			internal List<DetectionBlob> Blobs;
			/// <summary>
			/// Angle of the segment.
			/// </summary>
			internal double Angle;
			/// <summary>
			/// Projection on the line of the point closest to origin.
			/// </summary>
			internal double Start;
			/// <summary>
			/// Projection on the line of the point farthest from origin.
			/// </summary>
			internal double End;
		}

		/// <summary>
		/// Pixel on the image.
		/// </summary>
		internal struct IntPoint { internal int X, Y; }

		/// <summary>
		/// Represents a detected light blob.
		/// </summary>
		struct DetectionBlob
		{
			/// <summary>
			/// Coordinates of the blob's pixels.
			/// </summary>
			internal List<IntPoint> Points;
			/// <summary>
			/// Projection on the line of the point closest to the origin.
			/// </summary>
			internal double LineStart;
			/// <summary>
			/// Projection on the line of the point farthest from the origin.
			/// </summary>
			internal double LineEnd;
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

		/// <summary>
		/// A processed line segment detection.
		/// </summary>
		internal class LineDetection
		{
			/// <summary>
			/// Coordinates of the line segment's points.
			/// </summary>
			internal List<PixelPoint> Points;
			/// <summary>
			/// Values of the line segment's points.
			/// </summary>
			internal List<double> PointValues;
			/// <summary>
			/// First eigenvalue of the (bounding ellipse) covariance matrix.
			/// </summary>
			internal double EigenValue1;
			/// <summary>
			/// Second eigenvalue of the (bounding ellipse) covariance matrix.
			/// </summary>
			internal double EigenValue2;
			/// <summary>
			/// First eigenvector of the (bounding ellipse) covariance matrix. Should be perpendicular to the second.
			/// </summary>
			internal double EigenAngle1;
			/// <summary>
			/// Second eigenvector of the (bounding ellipse) covariance matrix. Should be perpendicular to the first.
			/// </summary>
			internal double EigenAngle2;
			/// <summary>
			/// Center of the ellipse by taking in account only pixel positions.
			/// </summary>
			internal PixelPoint PointsCenter;
			/// <summary>
			/// Center of the ellipse by taking in account pixel values.
			/// </summary>
			internal PixelPoint Barycenter;
			/// <summary>
			/// Amount of luminous flux emitted from the line segment.
			/// </summary>
			internal double Flux;

			public override string ToString()
			{
				return "[LD]:[" + Barycenter + "]:{Cnr=" + Points.Count + ", a=" + (Sqrt(EigenValue1) * 2).ToString("G6") + ", b=" + (Sqrt(EigenValue2) * 2).ToString("G6") + ", uX=" +
					Cos(EigenAngle1).ToString("G6") + ", uY=" + Sin(EigenAngle1).ToString("G6") + "}";
			}

		}

		/// <summary>
		/// Merges line segment blobs (connected components) in one LineDetection.
		/// </summary>
		/// <param name="segment">Detected blobs.</param>
		/// <param name="Input">Input image.</param>
		/// <param name="OX">Delta between the data array and actual position, X component.</param>
		/// <param name="OY">Delta between the data array and actual position, Y component.</param>
		/// <returns>A LineDetection from the blobs.</returns>
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
