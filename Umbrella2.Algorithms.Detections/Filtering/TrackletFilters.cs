using System;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.PropertyModel.CommonProperties;

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
			if (Input.VelReg.R_TX * Input.VelReg.R_TX < TimeRsquared) return 0;
			if (Input.VelReg.R_TY * Input.VelReg.R_TY < TimeRsquared) return 0;

			List<PixelPoint> Points = new List<PixelPoint>();
			double MeanR = 0;
			int count = 0;
			bool IsDot = false;
			/* Computes the PearsonR for each source and takes the mean */
			foreach (ImageDetection md in Input.Detections) if (md != null)
				{
					var PixP = md.FetchProperty<ObjectPoints>().PixelPoints;
					Points.AddRange(PixP);
					var mlinr = Misc.LinearRegression.ComputeLinearRegression(PixP);

					MeanR += Math.Abs(mlinr.PearsonR);
					count++;
					if (md.TryFetchProperty(out PairingProperties PairProp))
						if (PairProp.IsDotDetection) IsDot = true;
				}
			MeanR /= count;
			/* If not line-like but not a dot detection, drop */
			if (!IsDot && MeanR < IndividualRsquared) return 0;
			var LRP = Misc.LinearRegression.ComputeLinearRegression(Points);
			double RR = Math.Abs(LRP.PearsonR);
			if (IsDot) RR += (1 - Math.Abs(MeanR)) / Input.Velocity.ArcSecMin;
			return RR;
		}

		/// <summary>Implicitly converts itself to the signature of a filter.</summary>
		/// <param name="f">Instance to convert to a filter.</param>
		public static implicit operator Predicate<Tracklet>(LinearityTest f) => f.Filter;
	}
}
