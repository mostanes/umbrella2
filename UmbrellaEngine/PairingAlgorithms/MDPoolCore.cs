﻿using System;
using System.Collections.Generic;
using Umbrella2.Algorithms.Misc;

namespace Umbrella2.Algorithms.Pairing
{
	/// <summary>
	/// Class of common code for Pool Algorithms.
	/// </summary>
	public abstract class MDPoolCore
	{
		/// <summary>Depth of the quad tree.</summary>
		const int PoolDepth = 10;
		/// <summary>Quad Tree that represents the source pool.</summary>
		private protected QuadTree<MedianDetection> DetectionPool;
		/// <summary>List of the sources in the pool.</summary>
		protected readonly List<MedianDetection> PoolList;
		/* Edges of the pool in equatorial coordinates */
		private double Topmost, Lowermost, Leftmost, Rightmost;
		/// <summary>List of all the times at which we have sources in the pool.</summary>
		protected readonly List<DateTime> ObsTimes;

		/// <summary>Initializes a new instance.</summary>
		public MDPoolCore()
		{
			PoolList = new List<MedianDetection>(); ObsTimes = new List<DateTime>();
			Topmost = double.MaxValue; Lowermost = double.MinValue; Leftmost = double.MaxValue; Rightmost = double.MinValue;
		}

		/// <summary>
		/// Preloads detections into the search structures.
		/// </summary>
		/// <param name="Detections">Detected sources.</param>
		public void LoadDetections(List<MedianDetection> Detections)
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

		/// <summary>Generates the search structures.</summary>
		public void GeneratePool()
		{
			DetectionPool = new QuadTree<MedianDetection>(PoolDepth, Topmost, Lowermost, Leftmost, Rightmost);
			foreach (MedianDetection md in PoolList) DetectionPool.Add(md, md.BarycenterEP.RA, md.BarycenterEP.Dec);
		}

		/// <summary>
		/// Pairs the sources into tracklets.
		/// </summary>
		/// <returns>The list of tracklets found by the algorithm.</returns>
		public abstract List<Tracklet> FindTracklets();
	}
}
