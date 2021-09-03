using System;
using System.Collections.Generic;
using Umbrella2.Algorithms.Images;
using Umbrella2.IO;
using Umbrella2.IO.FITS.KnownKeywords;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2.Algorithms.Detection
{
	/// <summary>
	/// Provides functions for recovering tracklets and detections on a (different) set of images.
	/// </summary>
	public class ApproxRecover
	{
		/// <summary>Detection window size.</summary>
		public int HalfLength = 25;
		/// <summary>Detection threshold in standard deviations.</summary>
		public double ThresholdMultiplier = 1.5;
		/// <summary>Minimum number of pixels for a valid positive detection.</summary>
		public int MinPix = 7;
		/// <summary>Movement threshold over which the detection is not considered a fixed star.</summary>
		public double MinMoveArcSec = 0.2;
		/// <summary>Threshold number of detections on different images (noise and fixed star) after which the detection is considered bogus.</summary>
		public int CrossMatchRemove;
		/// <summary>Threshold for the ratio of pixels over which the detection's noise counter is increased.</summary>
		public double NoisePixelThreshold = 0.75;
		/// <summary>Threshold for the ratio of fluxes over which the detection's fixed star counter is increased.</summary>
		public double StarFluxThreshold = 0.6;
		/// <summary>Maximum radius of a recovered detection.</summary>
		public double RecoverRadius = 30;
		/// <summary>Minimum number of detections for a valid recovered tracklet.</summary>
		public int MinDetections = 3;

		public ApproxRecover()
		{
		}

		/// <summary>
		/// Attempts to recover a detection on an image.
		/// </summary>
		/// <returns>The recovered object.</returns>
		/// <param name="Location">Location which to check.</param>
		/// <param name="Radius">Maximum radius of the detection.</param>
		/// <param name="RecoveryImage">Image on which to perform the recovery.</param>
		private DotDetector.DotDetection Recover(PixelPoint Location, double Radius, Image RecoveryImage)
		{
			/* Area in the image to retrieve */
			System.Drawing.Rectangle r = new System.Drawing.Rectangle((int)Math.Round(Location.X) - HalfLength, (int)Math.Round(Location.Y) - HalfLength,
					2 * HalfLength, 2 * HalfLength);
				
			bool[,] Mask = new bool[2 * HalfLength, 2 * HalfLength];
			/* Mask areas outside Radius */
			for (int i = 0; i < 2 * HalfLength; i++) for (int j = 0; j < 2 * HalfLength; j++)
					if (!InDisk(i, j, Radius))
						Mask[i, j] = true;

			ImageData dt = RecoveryImage.LockData(r, true);

			/* Compute background level of the area */
			var sts = RecoveryImage.GetProperty<ImageStatistics>();
			ComputeSmartStats(dt.Data, 10 * sts.StDev, out double Median, out double MedSigma);

			/* Perform recovery */
			DotDetector.IntPoint Position = new DotDetector.IntPoint() { X = HalfLength, Y = HalfLength };
			var det = DotDetector.BitmapFill(dt.Data, Position, Mask, ThresholdMultiplier * MedSigma, r.X, r.Y);

			RecoveryImage.ExitLock(dt);

			return det;
		}

		/// <summary>Checks if the given point is within a given <paramref name="Radius"/> of the center (<see cref="HalfLength"/>).</summary>
		private bool InDisk(int X, int Y, double Radius) => ((X - HalfLength) * (X - HalfLength) + (Y - HalfLength) * (Y - HalfLength)) < Radius * Radius;

		/// <summary>
		/// Attempts to recover a detection on a given image, comparing with the entire set of exposures.
		/// </summary>
		/// <returns><c>true</c>, if detection was recovered, <c>false</c> otherwise.</returns>
		/// <param name="DetPos">Position of the detection to recover.</param>
		/// <param name="Img">Image on which to recover.</param>
		/// <param name="Radius">Maximum radius of the detection.</param>
		/// <param name="InputImages">Input images.</param>
		/// <param name="Recovered">Recovered detection.</param>
		public bool RecoverDetection(Position DetPos, Image Img, double Radius, IEnumerable<Image> InputImages, out ImageDetection Recovered)
		{
			Recovered = null;
			DotDetector.DotDetection dd = Recover(DetPos.PP, Radius, Img);
			if (dd.Pixels.Count < MinPix) return false;
			int NoiseCnt = 0;
			int FixedCnt = 0;
			foreach (Image img in InputImages)
			{
				/* Noise scanner -- same position in pixel coordinates */
				DotDetector.DotDetection detN = Recover(dd.Barycenter, Radius, img);
				PixelPoint npp = img.Transform.GetPixelPoint(Img.Transform.GetEquatorialPoint(dd.Barycenter));
				/* Star scanner -- same position in equatorial coordinates */
				DotDetector.DotDetection detF = Recover(npp, Radius, img);
				if (detN.Pixels.Count > NoisePixelThreshold * dd.Pixels.Count)
					NoiseCnt++;
				if (detF.Flux > StarFluxThreshold * dd.Flux)
				{
					EquatorialPoint org = DetPos.EP;
					EquatorialPoint nw = img.Transform.GetEquatorialPoint(detF.Barycenter);
					if ((org ^ nw) < MinMoveArcSec)
						FixedCnt++;
				}
			}
			if (NoiseCnt > CrossMatchRemove)
				return false;
			if (FixedCnt > CrossMatchRemove)
				return false;

			Recovered = StandardDetectionFactory.CreateDetection(Img, dd.Pixels, dd.PixelValues);
			return true;
		}

		/// <summary>
		/// Recovers the tracklet on a given set of images (typically pipeline input ones).
		/// </summary>
		/// <returns><c>true</c>, if tracklet was recovered, <c>false</c> otherwise.</returns>
		/// <param name="tvr"><see cref="TrackletVelocityRegression"/> from which the positions are computed.</param>
		/// <param name="InputImages">Images on which to perform the recovery.</param>
		/// <param name="Recovered">Recovered tracklet.</param>
		public bool RecoverTracklet(TrackletVelocityRegression tvr, IEnumerable<Image> InputImages, out Tracklet Recovered)
		{
			List<ImageDetection> Detections = new List<ImageDetection>();
			foreach(Image img in InputImages)
			{
				/* Compute location */
				ObservationTime obTime = img.GetProperty<ObservationTime>();
				double Secs = (obTime.Time - tvr.ZeroTime).TotalSeconds;
				EquatorialPoint eqp = new EquatorialPoint()
				{ RA = tvr.P_TR.Slope * Secs + tvr.P_TR.Intercept, Dec = tvr.P_TD.Slope * Secs + tvr.P_TD.Intercept };
				Position p = new Position(eqp, img.Transform.GetPixelPoint(eqp));
				/* Perform recovery */
				if (RecoverDetection(p, img, RecoverRadius, InputImages, out ImageDetection RecD))
					Detections.Add(RecD);
			}
			if(Detections.Count >= MinDetections)
			{
				Recovered = StandardTrackletFactory.CreateTracklet(Detections.ToArray());
				return true;
			}
			else { Recovered = null;  return false; }
		}

		private static void ComputeSmartStats(double[,] Data, double Sigma1, out double Median, out double MedSigma)
		{
			double[] Cd = new double[Data.Length];
			Buffer.BlockCopy(Data, 0, Cd, 0, Data.Length);
			Array.Sort(Cd);
			double Median1 = Cd[Cd.Length / 2];
			/*double Sigma1 = 0;
			for (int i = 0; i < Cd.Length; i++) Sigma1 += (Cd[i] - Median1) * (Cd[i] - Median1);
			Sigma1 = Math.Sqrt(Sigma1 / Cd.Length);*/
			int P1, P2;
			for (P1 = 0; Cd[P1] < Median1 - Sigma1; P1++) ;
			for (P2 = Cd.Length - 1; Cd[P2] > Median1 + Sigma1; P2--) ;
			double Median2 = Cd[(P1 + P2) / 2];
			double Sigma2 = 0;
			for (int i = P1; i < P2; i++) Sigma2 += (Cd[i] - Median2) * (Cd[i] - Median2);
			Sigma2 = Math.Sqrt(Sigma2 / (P2 - P1));
			Median = Median2;
			MedSigma = Sigma2;
		}
	}
}
