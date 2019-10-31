using System.Collections.Generic;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2.Algorithms.Filtering
{
	/// <summary>
	/// Represents a fixed star; used for filtering.
	/// </summary>
	public struct Star
	{
		/// <summary>Star position in equatorial coordinates.</summary>
		public EquatorialPoint EqCenter;
		/// <summary>Star position in pixel coordinates.</summary>
		public PixelPoint PixCenter;
		/// <summary>Star radius in pixels.</summary>
		public double PixRadius;
		/// <summary>Elliptic fit of the star.</summary>
		public SourceEllipse Shape;
		/// <summary>Star flux.</summary>
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
		/// <param name="MinStarFlux">Minimum flux of a fixed object before it marks nearby objects as star-crossing.</param>
		public void MarkStarCrossed(IEnumerable<ImageDetection> Detections, double StarMultiplier, double MinStarFlux)
		{
			foreach (ImageDetection d in Detections)
				foreach (Star s in FixedStarList)
					if (s.Flux > MinStarFlux && (s.PixCenter ^ d.Barycenter.PP) < s.PixRadius * StarMultiplier) { d.FetchOrCreate<PairingProperties>().StarPolluted = true; break; }
		}
	}
}
