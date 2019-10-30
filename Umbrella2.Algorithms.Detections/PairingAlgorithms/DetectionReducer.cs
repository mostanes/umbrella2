using System;
using System.Collections.Generic;
using Umbrella2.Algorithms.Filtering;
using Umbrella2.Algorithms.Misc;

namespace Umbrella2.Algorithms.Pairing
{
	/// <summary>
	/// Provides support for removing fixed stars from a set of detections.
	/// </summary>
	public class DetectionReducer
	{
		public double PairingRadius = 2;

		public DetectionReducer()
		{
			PoolList = new List<Star>();
			Topmost = double.MaxValue; Lowermost = double.MinValue; Leftmost = double.MaxValue; Rightmost = double.MinValue;
		}

		/// <summary>Depth of the quad tree.</summary>
		const int PoolDepth = 10;
		/// <summary>Quad Tree that represents the source pool.</summary>
		private protected QuadTree<Star> DetectionPool;
		/// <summary>List of the sources in the pool.</summary>
		protected readonly List<Star> PoolList;
		/* Edges of the pool in equatorial coordinates */
		private double Topmost, Lowermost, Leftmost, Rightmost;

		private double MaxRadius = 0;


		/// <summary>
		/// Preloads stars into the search structures.
		/// </summary>
		/// <param name="Detections">Stars.</param>
		public void LoadStars(List<Star> Detections)
		{
			if (DetectionPool != null) throw new NotSupportedException("Cannot modify the detection pool after it is generated");
			PoolList.AddRange(Detections);
			foreach (Star md in Detections)
			{
				EquatorialPoint ep = md.EqCenter;
				if (ep.Dec < Topmost) Topmost = ep.Dec;
				if (ep.Dec > Lowermost) Lowermost = ep.Dec;
				if (ep.RA < Leftmost) Leftmost = ep.RA;
				if (ep.RA > Rightmost) Rightmost = ep.RA;
				if (md.PixRadius > MaxRadius) MaxRadius = md.PixRadius;
			}
		}

		/// <summary>Generates the search structures.</summary>
		public void GeneratePool()
		{
			DetectionPool = new QuadTree<Star>(PoolDepth, Topmost, Lowermost, Leftmost, Rightmost);
			foreach (Star md in PoolList) DetectionPool.Add(md, md.EqCenter.RA, md.EqCenter.Dec);
		}

		/// <summary>
		/// Removes fixed stars from a list of detections.
		/// </summary>
		/// <returns>Reduced detections.</returns>
		/// <param name="Input">Raw detections.</param>
		public List<ImageDetection> Reduce(List<ImageDetection> Input)
		{
			List<ImageDetection> N = new List<ImageDetection>();
			double PR = PairingRadius * Math.PI / 180 / 3600;
			foreach (ImageDetection imd in Input)
			{
				EquatorialPoint p = imd.Barycenter.EP;
				double XPR = PR + MaxRadius * imd.ParentImage.Transform.GetEstimatedWCSChainDerivative();
				var P = DetectionPool.Query(p.Dec, p.RA, XPR);
				List<Star> spP = new List<Star>();
				foreach(Star s in P)
				{
					double SR = s.PixRadius * imd.ParentImage.Transform.GetEstimatedWCSChainDerivative();
					if (SR + PR > (s.EqCenter ^ p))
						spP.Add(s);
				}
				if (spP.Count == 0) N.Add(imd);
			}
			return N;
		}
	}
}
