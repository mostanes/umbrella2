using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.Algorithms.Misc;

namespace Umbrella2.Algorithms.Detection
{
	public class PoolMDMerger
	{
		const int PoolDepth = 10;
		QuadTree<MedianDetection> DetectionPool;
		List<MedianDetection> PoolList;
		double Topmost, Lowermost, Leftmost, Rightmost;
		List<DateTime> ObsTimes;
		double LongTrailHighThreshold = 30;
		double LongTrailLowThreshold = 10;
		double AngleDistanceDifferenceThreshold = 10;

		public PoolMDMerger()
		{
			PoolList = new List<MedianDetection>(); ObsTimes = new List<DateTime>();
			Topmost = double.MaxValue; Lowermost = double.MinValue; Leftmost = double.MaxValue; Rightmost = double.MinValue;
		}

		public void LoadMedians(List<MedianDetection> Detections)
		{
			if (DetectionPool != null) throw new NotSupportedException("Cannot modify the detection pool after it is generated");
			PoolList.AddRange(Detections);
			foreach (MedianDetection md in Detections)
			{
				if (md.BarycenterEP.Dec < Topmost) Topmost = md.BarycenterEP.Dec;
				if (md.BarycenterEP.Dec > Lowermost) Lowermost = md.BarycenterEP.Dec;
				if (md.BarycenterEP.RA < Leftmost) Leftmost = md.BarycenterEP.RA;
				if (md.BarycenterEP.RA > Rightmost) Rightmost = md.BarycenterEP.RA;
				if (!ObsTimes.Contains(md.Time.Time)) ObsTimes.Add(md.Time.Time);
			}
		}

		public void GeneratePool()
		{
			DetectionPool = new QuadTree<MedianDetection>(PoolDepth, Topmost, Lowermost, Leftmost, Rightmost);
			foreach (MedianDetection md in PoolList) DetectionPool.Add(md, md.BarycenterEP.RA, md.BarycenterEP.Dec);
		}

		public bool PairPossible(MedianDetection a, MedianDetection b)
		{
			if (a.Time.Time == b.Time.Time) return false;
			TimeSpan DeltaTime = a.Time.Time - b.Time.Time;
			if ((a.LargestDistance + b.LargestDistance) * Math.Abs(DeltaTime.TotalSeconds) < (a.BarycenterEP ^ b.BarycenterEP) * (a.Time.Exposure.TotalSeconds + b.Time.Exposure.TotalSeconds) / 2) return false;

			if(a.PixelEllipse.SemiaxisMajor > LongTrailHighThreshold*LongTrailHighThreshold)
			{
				if (b.PixelEllipse.SemiaxisMajor < LongTrailLowThreshold * LongTrailLowThreshold) return false;
			}
			double DeltaAngle = a.PixelEllipse.SemiaxisMajorAngle - b.PixelEllipse.SemiaxisMajorAngle;
			double Length = a.PixelEllipse.SemiaxisMajor + b.PixelEllipse.SemiaxisMajor;
			if (DeltaAngle * DeltaAngle * Math.Sqrt(Length) > AngleDistanceDifferenceThreshold) return false;

			return true;
		}

		public void TryPair(MedianDetection a, MedianDetection b)
		{
			TimeSpan DeltaTime = b.Time.Time - a.Time.Time;
			var Line = b.BarycenterEP - a.BarycenterEP;
			double PairEstimatedDistance = ~Line;
			double PairEstimatedDistanceError = a.LargestDistance + b.LargestDistance;
			double PairEstimatedVelocity = PairEstimatedDistance / DeltaTime.TotalSeconds;
			double PairEstimatedVelocityError = PairEstimatedDistanceError / DeltaTime.TotalSeconds;
			List<List<MedianDetection>> DetectedInPool = new List<List<MedianDetection>>();
			foreach (DateTime dt in ObsTimes)
			{
				TimeSpan tsp = dt - b.Time.Time;
				double EstDistance = PairEstimatedVelocity * tsp.TotalSeconds;
				double EstDistError = Math.Abs(PairEstimatedVelocityError * tsp.TotalSeconds) + PairEstimatedDistanceError;
				EquatorialPoint EstimatedPoint = Line + EstDistance;
				var DetectionsList = DetectionPool.Query(EstimatedPoint.Dec, EstimatedPoint.RA, EstDistError);
				DetectionsList.RemoveAll((x) => ((x.BarycenterEP ^ EstimatedPoint) > EstDistError) || (x.Time.Time != dt));
				DetectedInPool.Add(DetectionsList);
			}
			;
		}

		public void Search()
		{
			int i, j;
			int cnt = 1;
			int[] DetectionPairs = new int[PoolList.Count];
			for (i = 0; i < PoolList.Count; i++) for (j = i + 1; j < PoolList.Count; j++)
				{
					if (!PairPossible(PoolList[i], PoolList[j])) continue;
					else TryPair(PoolList[i], PoolList[j]);
				}
		}
	}
}
