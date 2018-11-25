using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.Algorithms.Filtering;
using Umbrella2.Algorithms.Misc;
using static Umbrella2.Pipeline.ExtraIO.VizieR;

namespace Umbrella2.Pipeline.EIOAlgorithms
{
	/// <summary>
	/// Provides an algorithm for calibrating image Zero Point using VizieR.
	/// </summary>
	public static class VizieRCalibration
	{
		const double Separation = Math.PI / 180 / 1200;
		const double CalibMinR = 0.90;

		/// <summary>
		/// Calibrates an input image using VizieR stars catalogs.
		/// </summary>
		/// <param name="VizieRStars">List of VizieR stars.</param>
		/// <param name="DetectedStars">List of locally detected stars.</param>
		/// <returns>The Zero Point magnitude.</returns>
		public static double Calibrate(List<StarInfo> VizieRStars, List<Star> DetectedStars)
		{
			double T = double.MinValue, B = double.MaxValue, L = double.MaxValue, R = double.MinValue;
			List<Tuple<Star, StarInfo>> Pairs = new List<Tuple<Star, StarInfo>>();
			foreach (Star s in DetectedStars)
			{
				if (s.EqCenter.RA > T) T = s.EqCenter.RA;
				if (s.EqCenter.RA < B) B = s.EqCenter.RA;
				if (s.EqCenter.Dec > R) R = s.EqCenter.Dec;
				if (s.EqCenter.Dec < L) L = s.EqCenter.Dec;
			}
			QuadTree<Star> Tree = new QuadTree<Star>(10, T, B, L, R);

			foreach(StarInfo si in VizieRStars)
			{
				var Stars = Tree.Query(si.Coordinate.RA, si.Coordinate.Dec, Separation);
				if (Stars.Count == 1)
					Pairs.Add(new Tuple<Star, StarInfo>(Stars[0], si));
			}

			if (Pairs.Count < 5) throw new IndexOutOfRangeException("Could not find enough pairs for calibration.");

			LinearRegression.LinearRegressionParameters LRP = LinearRegression.ComputeLinearRegression(Pairs.Select((x) => Math.Log10(x.Item1.Flux)).ToArray(), Pairs.Select((x) => x.Item2.Magnitude).ToArray());
			if (LRP.PearsonR * LRP.PearsonR < CalibMinR * CalibMinR)
				throw new ArgumentOutOfRangeException("Could not calibrate the fluxes with enough accuracy.");

			return LRP.Intercept;
		}
	}
}
