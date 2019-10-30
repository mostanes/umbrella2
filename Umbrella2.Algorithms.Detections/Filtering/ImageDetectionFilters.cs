using System;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.PropertyModel.CommonProperties;
using static System.Math;

namespace Umbrella2.Algorithms.Filtering
{
	/// <summary>
	/// Provides filtering for sources.
	/// </summary>
	public static class ImageDetectionFilterTools
	{
		/// <summary>
		/// Runs the given filters in parallel over the input.
		/// </summary>
		/// <param name="Input">Input sources.</param>
		/// <param name="Filters">Filters to be run. Each filter should return true for the source to pass.</param>
		/// <returns>Filtered input sources.</returns>
		public static List<ImageDetection> Filter(List<ImageDetection> Input, params Predicate<ImageDetection>[] Filters)
		{
			return Input.AsParallel().Where((x) => Filters.All((f) => f(x))).ToList();
		}
	}

	public interface IImageDetectionFilter
	{
		bool Filter(ImageDetection Input);
	}

	/// <summary>
	/// Filters out sources which are too bright for their thickness. Used mainly for preventing white bands from being interpreted as sources.
	/// </summary>
	public class BrightnessThicknessFilter : IImageDetectionFilter
	{
		public double BrightnessThreshold;
		public double ThicknessThreshold;

		public bool Filter(ImageDetection Input) =>
			!(Input.FetchProperty<ObjectPhotometry>().Flux > BrightnessThreshold * Input.FetchProperty<ObjectPoints>().PixelPoints.Length &&
			Input.FetchProperty<ObjectSize>().PixelEllipse.SemiaxisMinor < ThicknessThreshold);


		public static implicit operator Predicate<ImageDetection>(BrightnessThicknessFilter f) => f.Filter;
	}

	public class LinearityThresholdFilter : IImageDetectionFilter
	{
		public double MaxLineThickness;

		public bool Filter(ImageDetection Input) { double Width = ComputeWidth(Input); return (Width <= MaxLineThickness); }

		double ComputeWidth(ImageDetection Input)
		{
			double X0Angle = Input.FetchProperty<ObjectSize>().PixelEllipse.SemiaxisMajorAngle;
			/* Rotation matrix: { { Cos(-X0Angle), -Sin(-X0Angle) }, { Sin(-X0Angle), Cos(-X0Angle) } } */
			double YSsum = 0, Ysum = 0;
			if (Input.TryFetchProperty(out ObjectPoints op))
			{
				var pixelPoints = op.PixelPoints;
				foreach (PixelPoint pp in pixelPoints)
				{
					double nY = pp.Y * Cos(X0Angle) - pp.X * Sin(X0Angle);
					YSsum += nY * nY;
					Ysum += nY;
				}
				YSsum /= pixelPoints.Length;
				Ysum /= pixelPoints.Length;
				YSsum -= Ysum * Ysum;

				return 2 * Sqrt(YSsum);
			}
			else return 0;
		}

		public static implicit operator Predicate<ImageDetection>(LinearityThresholdFilter f) => f.Filter;
	}
}
