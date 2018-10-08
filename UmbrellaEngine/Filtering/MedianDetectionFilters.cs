using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;

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

		public struct LinearityThresholdFilter
		{
			public double MaxLineThickness;

			bool Filter(MedianDetection Input) { double Width = ComputeWidth(Input); return (Width <= MaxLineThickness); }

			double ComputeWidth(MedianDetection Input)
			{
				double X0Angle = Input.PixelEllipse.SemiaxisMajorAngle;
				/* Rotation matrix: { { Cos(-X0Angle), -Sin(-X0Angle) }, { Sin(-X0Angle), Cos(-X0Angle) } } */
				double YSsum = 0, Ysum = 0;
				foreach (PixelPoint pp in Input.PixelPoints)
				{
					double nY = pp.Y * Cos(X0Angle) - pp.X * Sin(X0Angle);
					YSsum += nY * nY;
					Ysum += nY;
				}
				YSsum /= Input.PixelPoints.Count;
				Ysum /= Input.PixelPoints.Count;
				YSsum -= Ysum * Ysum;

				return 2 * Sqrt(YSsum);
			}

			public static implicit operator Predicate<MedianDetection>(LinearityThresholdFilter f) => f.Filter;
		}
	}
}
