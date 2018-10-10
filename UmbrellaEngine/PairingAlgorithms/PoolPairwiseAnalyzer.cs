using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Umbrella2.Algorithms.Pairing
{
	class PoolPairwiseAnalyzer : MDPoolCore
	{
		List<Tracklet> Tracklets;

		public PoolPairwiseAnalyzer() : base()
		{ }

		bool VerifyPair(MedianDetection a, MedianDetection b)
		{
			double MsizeLD = a.LargestDistance + b.LargestDistance;
			double Mdistance = a.BarycenterEP ^ b.BarycenterEP;
			double DeltaABTimeS = (b.Time.Time - a.Time.Time).TotalSeconds;
			double TExpTime = a.Time.Exposure.TotalSeconds + b.Time.Exposure.TotalSeconds;

			/* If the distance is too large for light sources too small, no need to check further */
			if (MsizeLD * DeltaABTimeS < DeltaABTimeS * TExpTime) return false;

			return true;
		}

		void AnalyzePair(MedianDetection a, MedianDetection b)
		{

		}

		struct SearchParameters
		{
			internal double AngularRadius;
			internal double MinVelocity;
			internal double MaxVelocity;
		}

		SearchParameters ComputeSearchDisk(MedianDetection m)
		{
			SearchParameters SP = new SearchParameters();
			SP.AngularRadius = Math.Abs(Math.Atan2(m.PixelEllipse.SemiaxisMinor, m.PixelEllipse.SemiaxisMajor));
			SP.MinVelocity = m.PixelEllipse.SemiaxisMajor / m.Time.Exposure.TotalSeconds;
			SP.MaxVelocity = m.PixelEllipse.SemiaxisMajor * m.PixelEllipse.SemiaxisMajor / m.PixelEllipse.SemiaxisMinor / m.Time.Exposure.TotalSeconds;
			return SP;
		}

		public override List<Tracklet> FindTracklets()
		{
			int i, j;
			for(i=0;i<PoolList.Count;i++)for(j=0;j<PoolList.Count;j++)
				{
					if (PoolList[i].Time.Time == PoolList[j].Time.Time)
						continue;
					if (VerifyPair(PoolList[i], PoolList[j]))
						AnalyzePair(PoolList[i], PoolList[j]);
				}

			return Tracklets;
		}
	}
}
