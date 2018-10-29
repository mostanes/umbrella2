using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Umbrella2.Algorithms.Filtering
{
	/// <summary>
	/// Represents a fixed star; used for filtering.
	/// </summary>
	struct Star
	{
		internal EquatorialPoint EqCenter;
		internal PixelPoint PixCenter;
		internal double PixRadius;
	}

	/// <summary>
	/// Class representing information about fixed stars. Used for filtering.
	/// </summary>
	class StarData
	{
		List<Star> FixedStarList;

		/// <summary>
		/// Marks detections that cross near a star (and therefore could well be parts of the star's halo.
		/// </summary>
		/// <param name="Detections">List of the detections to analyze.</param>
		public void MarkStarCrossed(IEnumerable<MedianDetection> Detections)
		{
			foreach (MedianDetection d in Detections)
				foreach (Star s in FixedStarList)
					if ((s.PixCenter ^ d.BarycenterPP) < s.PixRadius) { d.StarPolluted = true; break; }
		}
	}
}
