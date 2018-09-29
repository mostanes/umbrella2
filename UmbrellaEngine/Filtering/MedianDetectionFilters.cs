using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.Algorithms.Filtering
{
	/// <summary>
	/// Provides filtering for sources.
	/// </summary>
	public static class MedianDetectionFilters
	{
		/// <summary>
		/// Runs the given filters in parallel over the input.
		/// </summary>
		/// <param name="Input">Input sources.</param>
		/// <param name="Filters">Filters to be run. Each filter should return true for the source to pass.</param>
		/// <returns>Filtered input sources.</returns>
		public static List<MedianDetection> Filter(List<MedianDetection> Input, params Predicate<MedianDetection>[] Filters)
		{
			return Input.AsParallel().Where((x) => Filters.All((f) => f(x))).ToList();
		}

		/// <summary>
		/// Filters out sources which are too bright for their thickness. Used mainly for preventing white bands from being interpreted as sources.
		/// </summary>
		public struct BrightnessThicknessFilter
		{
			public double BrightnessThreshold;
			public double ThicknessThreshold;

			bool Filter(MedianDetection Input) =>
				!(Input.Flux > BrightnessThreshold * Input.PixelPoints.Count && Input.PixelEllipse.SemiaxisMinor < ThicknessThreshold);


			public static implicit operator Predicate<MedianDetection>(BrightnessThicknessFilter f) => f.Filter;
		}
	}
}
