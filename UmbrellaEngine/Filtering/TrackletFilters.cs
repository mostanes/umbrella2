using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Umbrella2.Algorithms.Filtering
{
	/// <summary>
	/// Provides filtering for tracklets.
	/// </summary>
	public static class TrackletFilters
	{
		/// <summary>
		/// Runs the given filters in parallel over the input.
		/// </summary>
		/// <param name="Input">Input sources.</param>
		/// <param name="Filters">Filters to be run. Each filter should return true for the tracklet to pass.</param>
		/// <returns>Filtered tracklets.</returns>
		public static List<Tracklet> Filter(List<Tracklet> Input, params Predicate<Tracklet>[] Filters)
		{
			return Input.AsParallel().Where((x) => Filters.All((f) => f(x))).ToList();
		}
	}

	/// <summary>
	/// Linearity filter for tracklets.
	/// </summary>
	public class LinearityTest
	{
		const double LineRsquared = 0.8;
		const double TimeRsquared = 0.8;
		const double IndividualRsquared = 0.5;

		bool Filter(Tracklet Input) { double R = ComputePearsonR(Input); return (R * R > LineRsquared); }

		double ComputePearsonR(Tracklet Input)
		{
			if (Input.TimeXPearsonR * Input.TimeXPearsonR < TimeRsquared) return 0;
			if (Input.TimeYPearsonR * Input.TimeYPearsonR < TimeRsquared) return 0;

			List<PixelPoint> Points = new List<PixelPoint>();
			double MeanR = 0;
			int count = 0;
			bool IsDot = false;
			/* Computes the PearsonR for each source and takes the mean */
			foreach (MedianDetection md in Input.MergedDetections) if (md != null)
				{
					Points.AddRange(md.PixelPoints);
					var mlinr = Misc.LinearRegression.ComputeLinearRegression(md.PixelPoints);
					md.PearsonR = mlinr.PearsonR;
					MeanR += Math.Abs(mlinr.PearsonR);
					count++;
					if (md.IsDotDetection) IsDot = true;
				}
			MeanR /= count;
			/* If not line-like but not a dot detection, drop */
			if (!IsDot && MeanR < IndividualRsquared) return 0;
			var LRP = Misc.LinearRegression.ComputeLinearRegression(Points);
			return LRP.PearsonR;
		}

		public static implicit operator Predicate<Tracklet>(LinearityTest f) => f.Filter;
	}
}
