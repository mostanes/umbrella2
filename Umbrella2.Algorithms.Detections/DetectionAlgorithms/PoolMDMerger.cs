using System;
using System.Collections.Generic;
using Umbrella2.Algorithms.Misc;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2.Algorithms.Detection
{
	/// <summary>
	/// Holds detections and performs merging of source detections in tracklets.
	/// </summary>
	/// <remarks>
	/// This code is a first-fit solution to the problem of detection merging.
	/// In particular, the search function and associates should be reimplemented with a carefully designed algorithm.
	/// </remarks>
	[Obsolete]
	public class PoolMDMerger
	{
		const int PoolDepth = 10;
		QuadTree<ImageDetection> DetectionPool;
		List<ImageDetection> PoolList;
		double Topmost, Lowermost, Leftmost, Rightmost;
		List<DateTime> ObsTimes;
		double LongTrailHighThreshold = 30;
		double LongTrailLowThreshold = 10;
		double AngleDistanceDifferenceThreshold = 10;
		List<ImageDetection[][]> CandidatePairings;
		const double MaxArcsecVDot = 16;
		const double MinArcsecVDot = 0.2;
		static double MaxVDD = MaxArcsecVDot * Math.PI / 180 / 3600;
		static double MinVDD = MinArcsecVDot * Math.PI / 180 / 3600;

		/// <summary>
		/// Initializes a new instance.
		/// </summary>
		public PoolMDMerger(DateTime[] ObservationTimes)
		{
			PoolList = new List<ImageDetection>(); ObsTimes = new List<DateTime>(ObservationTimes);
			Topmost = double.MaxValue; Lowermost = double.MinValue; Leftmost = double.MaxValue; Rightmost = double.MinValue;
		}

		/// <summary>
		/// Preloads detections into the search structures.
		/// </summary>
		/// <param name="Detections">Detected sources.</param>
		public void LoadDetections(List<ImageDetection> Detections)
		{
			if (DetectionPool != null) throw new NotSupportedException("Cannot modify the detection pool after it is generated");
			PoolList.AddRange(Detections);
			foreach (ImageDetection md in Detections)
			{
				EquatorialPoint ep = md.Barycenter.EP;
				if (ep.Dec < Topmost) Topmost = ep.Dec;
				if (ep.Dec > Lowermost) Lowermost = ep.Dec;
				if (ep.RA < Leftmost) Leftmost = ep.RA;
				if (ep.RA > Rightmost) Rightmost = ep.RA;
			}
		}

		/// <summary>
		/// Generates the search structures.
		/// </summary>
		public void GeneratePool()
		{
			DetectionPool = new QuadTree<ImageDetection>(PoolDepth, Topmost, Lowermost, Leftmost, Rightmost);
			foreach (ImageDetection md in PoolList) DetectionPool.Add(md, md.Barycenter.EP.RA, md.Barycenter.EP.Dec);
		}

		public bool PairPossible(ImageDetection a, ImageDetection b)
		{
			PairingProperties App = a.FetchOrCreate<PairingProperties>(), Bpp = b.FetchOrCreate<PairingProperties>();
			if (App.IsPaired || Bpp.IsPaired) return false;
			if (App.StarPolluted || Bpp.StarPolluted) return false;
			if (a.Time.Time == b.Time.Time) return false;
			TimeSpan DeltaTime = a.Time.Time - b.Time.Time;
			//if ((a.LargestDistance + b.LargestDistance) * Math.Abs(DeltaTime.TotalSeconds) < (a.Barycenter.EP ^ b.Barycenter.EP) * (a.Time.Exposure.TotalSeconds + b.Time.Exposure.TotalSeconds) / 2) return false;

			SourceEllipse aPel = a.FetchProperty<ObjectSize>().PixelEllipse, bPel = b.FetchProperty<ObjectSize>().PixelEllipse;
			if(aPel.SemiaxisMajor > LongTrailHighThreshold*LongTrailHighThreshold)
			{
				if (bPel.SemiaxisMajor < LongTrailLowThreshold * LongTrailLowThreshold) return false;
			}
			double DeltaAngle = aPel.SemiaxisMajorAngle - bPel.SemiaxisMajorAngle;
			double Length = aPel.SemiaxisMajor + bPel.SemiaxisMajor;
			if (DeltaAngle * DeltaAngle * Math.Sqrt(Length) > AngleDistanceDifferenceThreshold) return false;

			return true;
		}

		public void TryPair(ImageDetection a, ImageDetection b)
		{
			TimeSpan DeltaTime = b.Time.Time - a.Time.Time;
			var Line = b.Barycenter.EP - a.Barycenter.EP;
			double PairEstimatedDistance = ~Line;
			double PairEstimatedDistanceError = (a.FetchProperty<ObjectSize>().PixelEllipse.SemiaxisMajor + b.FetchProperty<ObjectSize>().PixelEllipse.SemiaxisMajor) / 2;
			PairEstimatedDistanceError *= a.ParentImage.Transform.GetEstimatedWCSChainDerivative();
			double PairEstimatedVelocity = PairEstimatedDistance / DeltaTime.TotalSeconds;
			double PairEstimatedVelocityError = PairEstimatedDistanceError / DeltaTime.TotalSeconds;
			List<List<ImageDetection>> DetectedInPool = new List<List<ImageDetection>>();
			List<ImageDetection[]> DIPAr = new List<ImageDetection[]>();
			foreach (DateTime dt in ObsTimes)
			{
				TimeSpan tsp = dt - b.Time.Time;
				double EstDistance = PairEstimatedVelocity * tsp.TotalSeconds;
				double EstDistError = Math.Abs(PairEstimatedVelocityError * tsp.TotalSeconds) + PairEstimatedDistanceError;
				EquatorialPoint EstimatedPoint = Line + EstDistance;
				var DetectionsList = DetectionPool.Query(EstimatedPoint.Dec, EstimatedPoint.RA, EstDistError);
				DetectionsList.RemoveAll((x) => ((x.Barycenter.EP ^ EstimatedPoint) > EstDistError) || (x.Time.Time != dt) || x.FetchOrCreate<PairingProperties>().IsDotDetection);
				//DetectedInPool.Add(DetectionsList);
				DIPAr.Add(DetectionsList.ToArray());
			}
			int i, c = 0;
			for (i = 0; i < DIPAr.Count; i++) if (DIPAr[i].Length != 0) c++;
			if (c >= 3)
			{
				CandidatePairings.Add(DIPAr.ToArray());
				foreach (ImageDetection[] mdl in DIPAr) foreach (ImageDetection m in mdl) m.FetchOrCreate<PairingProperties>().IsPaired = true;
			}
		}

		public void TryPairDot(ImageDetection a, ImageDetection b)
		{
			if (a.FetchProperty<PairingProperties>().IsPaired || b.FetchProperty<PairingProperties>().IsPaired) return;
			TimeSpan DeltaTime = b.Time.Time - a.Time.Time;
			var Line = b.Barycenter.EP - a.Barycenter.EP;
			double PairEstimatedDistance = ~Line;
			if (PairEstimatedDistance > MaxVDD * DeltaTime.TotalMinutes) return;
			if (PairEstimatedDistance < MinVDD * DeltaTime.TotalMinutes) return;
			double PairEstimatedDistanceError = 2 * (a.FetchProperty<ObjectSize>().PixelEllipse.SemiaxisMajor + b.FetchProperty<ObjectSize>().PixelEllipse.SemiaxisMajor);
			PairEstimatedDistanceError *= a.ParentImage.Transform.GetEstimatedWCSChainDerivative();
			double PairEstimatedVelocity = PairEstimatedDistance / DeltaTime.TotalSeconds;
			double PairEstimatedVelocityError = PairEstimatedDistanceError / DeltaTime.TotalSeconds;
			List<List<ImageDetection>> DetectedInPool = new List<List<ImageDetection>>();
			List<ImageDetection[]> DIPAr = new List<ImageDetection[]>();
			foreach (DateTime dt in ObsTimes)
			{
				TimeSpan tsp = dt - b.Time.Time;
				double EstDistance = PairEstimatedVelocity * tsp.TotalSeconds;
				double EstDistError = Math.Abs(PairEstimatedVelocityError * tsp.TotalSeconds) + PairEstimatedDistanceError;
				EquatorialPoint EstimatedPoint = Line + EstDistance;
				var DetectionsList = DetectionPool.Query(EstimatedPoint.Dec, EstimatedPoint.RA, EstDistError);
				DetectionsList.RemoveAll((x) => ((x.Barycenter.EP ^ EstimatedPoint) > EstDistError) || (x.Time.Time != dt) || !x.FetchProperty<PairingProperties>().IsDotDetection);
				//DetectedInPool.Add(DetectionsList);
				DIPAr.Add(DetectionsList.ToArray());
			}
			int i, c = 0;
			for (i = 0; i < DIPAr.Count; i++) if (DIPAr[i].Length != 0) c++;
			if (c >= 3)
			{
				CandidatePairings.Add(DIPAr.ToArray());
				foreach (ImageDetection[] mdl in DIPAr) foreach (ImageDetection m in mdl) m.FetchProperty<PairingProperties>().IsPaired = true;
			}
		}

		/// <summary>
		/// Searches for tracklets from the given sources.
		/// </summary>
		/// <returns></returns>
		public List<ImageDetection[][]> Search()
		{
			CandidatePairings = new List<ImageDetection[][]>();
			int i, j;
			int[] DetectionPairs = new int[PoolList.Count];
			for (i = 0; i < PoolList.Count; i++) for (j = i + 1; j < PoolList.Count; j++)
				{
					if (PoolList[i].FetchOrCreate<PairingProperties>().IsDotDetection != PoolList[j].FetchOrCreate<PairingProperties>().IsDotDetection) continue;
					if (PoolList[i].FetchOrCreate<PairingProperties>().IsDotDetection) TryPairDot(PoolList[i], PoolList[j]);
					else
					{
						if (!PairPossible(PoolList[i], PoolList[j])) continue;
						else TryPair(PoolList[i], PoolList[j]);
					}
				}
			return CandidatePairings;
		}
	}
}
