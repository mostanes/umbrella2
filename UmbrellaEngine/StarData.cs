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
		internal SourceEllipse Shape;
	}

	/// <summary>
	/// Class representing information about fixed stars. Used for filtering.
	/// </summary>
	public class StarData
	{
		internal List<Star> FixedStarList = new List<Star>();

		/// <summary>
		/// Marks detections that cross near a star (and therefore could well be parts of the star's halo.
		/// </summary>
		/// <param name="Detections">List of the detections to analyze.</param>
		public void MarkStarCrossed(IEnumerable<MedianDetection> Detections, double StarMultiplier)
		{
			foreach (MedianDetection d in Detections)
				foreach (Star s in FixedStarList)
					if ((s.PixCenter ^ d.BarycenterPP) < s.PixRadius * StarMultiplier) { d.StarPolluted = true; break; }
		}
	}
}
