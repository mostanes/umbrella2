using System.Collections.Generic;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2.Algorithms.Filtering
{
	/// <summary>
	/// Represents a fixed star; used for filtering.
	/// </summary>
	public struct Star
	{
		public EquatorialPoint EqCenter;
		public PixelPoint PixCenter;
		public double PixRadius;
		public SourceEllipse Shape;
		public double Flux;
	}

	/// <summary>
	/// Class representing information about fixed stars. Used for filtering.
	/// </summary>
	public class StarData
	{
		public List<Star> FixedStarList = new List<Star>();

		/// <summary>
		/// Marks detections that cross near a star (and therefore could well be parts of the star's halo.
		/// </summary>
		/// <param name="Detections">List of the detections to analyze.</param>
		/// <param name="StarMultiplier">Ratio between the radius of star pollution marking and the star radius.</param>
		public void MarkStarCrossed(IEnumerable<ImageDetection> Detections, double StarMultiplier)
		{
			foreach (ImageDetection d in Detections)
				foreach (Star s in FixedStarList)
					if ((s.PixCenter ^ d.Barycenter.PP) < s.PixRadius * StarMultiplier) { d.FetchOrCreate<PairingProperties>().StarPolluted = true; break; }
		}
	}
}
