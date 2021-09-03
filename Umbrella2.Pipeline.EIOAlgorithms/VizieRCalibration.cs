using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.Algorithms.Detection;
using Umbrella2.Algorithms.Filtering;
using Umbrella2.Algorithms.Misc;
using Umbrella2.IO;
using static Umbrella2.Pipeline.ExtraIO.VizieR;

namespace Umbrella2.Pipeline.EIOAlgorithms
{
	/// <summary>
	/// Provides an algorithm for calibrating image Zero Point using VizieR.
	/// </summary>
	public static class VizieRCalibration
	{
		const double Arc1Sec = Math.PI / 180 / 3600;
		/// <summary>Minimum Pearson R for a valid calibration.</summary>
		const double CalibMinR = 0.90;
		/// <summary>Maximum ratio to <see cref="CalibrationArgs.PositionError"/> at which stars are considered double.</summary>
		const int DoubleStarRatio = 5;

		/// <summary>
		/// Calibrates an input image using VizieR stars catalogs.
		/// </summary>
		/// <param name="VizieRStars">List of VizieR stars.</param>
		/// <param name="DetectedStars">List of locally detected stars.</param>
		/// <param name="PositionError">Maximum position error of stars. Value in arcseconds.</param>
		/// <returns>The Zero Point magnitude.</returns>
		public static double Calibrate(List<StarInfo> VizieRStars, List<Star> DetectedStars, double PositionError)
		{
			double T = double.MaxValue, B = double.MinValue, L = double.MaxValue, R = double.MinValue;
			List<Tuple<Star, StarInfo>> Pairs = new List<Tuple<Star, StarInfo>>();
			foreach (Star s in DetectedStars)
			{
				if (s.EqCenter.Dec < T) T = s.EqCenter.Dec;
				if (s.EqCenter.Dec > B) B = s.EqCenter.Dec;
				if (s.EqCenter.RA > R) R = s.EqCenter.RA;
				if (s.EqCenter.RA < L) L = s.EqCenter.RA;
			}
			QuadTree<Star> Tree = new QuadTree<Star>(10, T, B, L, R);
			foreach (Star s in DetectedStars)
				Tree.Add(s, s.EqCenter.RA, s.EqCenter.Dec);

			foreach (StarInfo si in VizieRStars)
			{
				var Stars = Tree.Query(si.Coordinate.RA, si.Coordinate.Dec, Arc1Sec * PositionError);
				var St2 = Tree.Query(si.Coordinate.RA, si.Coordinate.Dec, Arc1Sec * PositionError * 5);
				if (St2.Count == 1 & Stars.Count == 1)
					Pairs.Add(new Tuple<Star, StarInfo>(Stars[0], si));
			}

			if (Pairs.Count < 5) throw new IndexOutOfRangeException("Could not find enough pairs for calibration.");

			var Rpairs1 = Pairs.ToArray();
			var ZPSet = Pairs.Select((x) => x.Item2.Magnitude + 2.5 * Math.Log10(x.Item1.Flux)).ToArray();
			Array.Sort(ZPSet, Rpairs1);
			var Rpairs2 = Rpairs1.Skip(Rpairs1.Length / 4).Take(Rpairs1.Length / 2).ToList();

			LinearRegression.LinearRegressionParameters LRP = LinearRegression.ComputeLinearRegression(Rpairs2.Select((x) => Math.Log10(x.Item1.Flux)).ToArray(), Rpairs2.Select((x) => x.Item2.Magnitude).ToArray());
			if (LRP.PearsonR * LRP.PearsonR < CalibMinR * CalibMinR)
				throw new ArgumentOutOfRangeException("Could not calibrate the fluxes with enough accuracy.");

			return ZPSet[ZPSet.Length / 2];
		}

		/// <summary>
		/// Parameters of the calibration algorithm.
		/// </summary>
		public struct CalibrationArgs
		{
			/// <summary>Maximum distance between a star on VizieR and another on the image that are paired.</summary>
			public double PositionError;
			/// <summary>Value for <see cref="DotDetector.HighThresholdMultiplier"/> when detecting stars.</summary>
			public double StarHighThreshold;
			/// <summary>Value for <see cref="DotDetector.LowThresholdMultiplier"/> when detecting stars.</summary>
			public double StarLowThreshold;
			/// <summary>Value for <see cref="DotDetector.NonrepresentativeThreshold"/> when detecting stars.</summary>
			public double NonRepThreshold;
			/// <summary>Minimum flux for stars used in calibration.</summary>
			public double MinFlux;
			/// <summary>Maximum flux for stars used in calibration.</summary>
			public double MaxFlux;
			/// <summary>Maximum intensity for each pixel for stars used in calibration.</summary>
			public double ClippingPoint;
			/// <summary>Maximum magnitude of objects from VizieR used in calibration.</summary>
			public double MaxVizierMag;
		}

		/// <summary>
		/// Calibrate the zero point of the specified image.
		/// </summary>
		/// <returns>The Zero Point magnitude.</returns>
		/// <param name="Img">Image to calibrate.</param>
		/// <param name="Args">Calibration parameters.</param>
		/// <returns>The Zero Point magnitude.</returns>
		public static double CalibrateImage(Image Img, CalibrationArgs Args)
		{
			DotDetector StarDetector = new DotDetector()
			{
				HighThresholdMultiplier = Args.StarHighThreshold,
				LowThresholdMultiplier = Args.StarLowThreshold,
				MinPix = 15,
				NonrepresentativeThreshold = Args.NonRepThreshold
			};
			StarDetector.Parameters.Xstep = 0;
			StarDetector.Parameters.Ystep = 200;

			var StList = StarDetector.DetectRaw(Img);
			var LocalStars = StList.Where((x) => x.Flux > Args.MinFlux & x.Flux < Args.MaxFlux).Where((x) => !x.PixelValues.Any((y) => y > Args.ClippingPoint)).
				Select((x) => new Star() { EqCenter = Img.Transform.GetEquatorialPoint(x.Barycenter), Flux = x.Flux, PixCenter = x.Barycenter }).ToList();
			PixelPoint PixC = new PixelPoint() { X = Img.Width / 2, Y = Img.Height / 2 };
			PixelPoint DiagF = new PixelPoint() { X = Img.Width, Y = Img.Height };
			double Diag = Img.Transform.GetEquatorialPoint(new PixelPoint() { X = 0, Y = 0 }) ^ Img.Transform.GetEquatorialPoint(DiagF);
			EquatorialPoint EqCenter = Img.Transform.GetEquatorialPoint(PixC);
			List<StarInfo> RemoteStars = GetVizieRObjects(EqCenter, Diag / 2, Args.MaxVizierMag);
			for (int i = 0; i < RemoteStars.Count; i++) for (int j = i + 1; j < RemoteStars.Count; j++)
					if ((RemoteStars[i].Coordinate ^ RemoteStars[j].Coordinate) < Arc1Sec * Args.PositionError * DoubleStarRatio)
					{ RemoteStars.RemoveAt(j); RemoteStars.RemoveAt(i); i--; break; }

			return Calibrate(RemoteStars, LocalStars, Args.PositionError);
		}
	}
}
