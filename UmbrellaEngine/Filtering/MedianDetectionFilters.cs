using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.Algorithms.Filtering
{
	public static class MedianDetectionFilters
	{
		public static List<MedianDetection> Filter(List<MedianDetection> Input, params Predicate<MedianDetection>[] Filters)
		{
			return Input.AsParallel().Where((x) => Filters.All((f) => f(x))).ToList();
		}

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
