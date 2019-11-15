using System;
using System.Collections.Generic;
using Umbrella2.Algorithms.Images;
using Umbrella2.IO;
using Umbrella2.IO.FITS.KnownKeywords;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2.Algorithms.Detection
{
	public class ApproxRecover
	{
		public int HalfLength = 25;
		public double ThresholdMultiplier = 1.5;
		public int MinPix = 7;
		public double MinMoveArcSec = 0.2;
		public int CrossMatchRemove;
		public double RecoverRadius = 30;

		public ApproxRecover()
		{
		}

		private DotDetector.DotDetection Recover(PixelPoint Location, bool Spiral, double Radius, Image RecoveryImage)
		{
			System.Drawing.Rectangle r = new System.Drawing.Rectangle((int)Math.Round(Location.X) - HalfLength, (int)Math.Round(Location.Y) - HalfLength,
					2 * HalfLength, 2 * HalfLength);

			var sts = RecoveryImage.GetProperty<ImageStatistics>();
			bool[,] Mask = new bool[2 * HalfLength, 2 * HalfLength];
			for (int i = 0; i < 2 * HalfLength; i++) for (int j = 0; j < 2 * HalfLength; j++)
					if (!InDisk(i, j, Radius))
						Mask[i, j] = true;

			ImageData dt = RecoveryImage.LockData(r, true);


			ComputeSmartStats(dt.Data, 10 * sts.StDev, out double Median, out double MedSigma);

			DotDetector.IntPoint Position = new DotDetector.IntPoint() { X = HalfLength, Y = HalfLength };
			var det = DotDetector.BitmapFill(dt.Data, Position, Mask, ThresholdMultiplier * MedSigma, r.X, r.Y);

			RecoveryImage.ExitLock(dt);

			return det;
		}

		private bool InDisk(int X, int Y, double Radius) => ((X - HalfLength) * (X - HalfLength) + (Y - HalfLength) * (Y - HalfLength)) < Radius * Radius;

		public bool RecoverDetection(Position DetPos, Image Img, double Radius, IEnumerable<Image> InputImages, out ImageDetection Recovered)
		{
			Recovered = null;
			DotDetector.DotDetection dd = Recover(DetPos.PP, true, Radius, Img);
			if (dd.Pixels.Count < MinPix) return false;
			int NoiseCnt = 0;
			int FixedCnt = 0;
			foreach (Image img in InputImages)
			{
				DotDetector.DotDetection detN = Recover(dd.Barycenter, false, Radius, img);
				PixelPoint npp = img.Transform.GetPixelPoint(Img.Transform.GetEquatorialPoint(dd.Barycenter));
				DotDetector.DotDetection detF = Recover(npp, false, Radius, img);
				if (detN.Pixels.Count > 0.75 * dd.Pixels.Count)
					NoiseCnt++;
				if (detF.Flux > 0.6 * dd.Flux)
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

		public bool RecoverTracklet(TrackletVelocityRegression tvr, IEnumerable<Image> InputImages, out Tracklet Recovered)
		{
			List<ImageDetection> Detections = new List<ImageDetection>();
			foreach(Image img in InputImages)
			{
				ObservationTime obTime = img.GetProperty<ObservationTime>();
				double Secs = (obTime.Time - tvr.ZeroTime).TotalSeconds;
				EquatorialPoint eqp = new EquatorialPoint()
				{ RA = tvr.P_TR.Slope * Secs + tvr.P_TR.Intercept, Dec = tvr.P_TD.Slope * Secs + tvr.P_TD.Intercept };
				Position p = new Position(eqp, img.Transform.GetPixelPoint(eqp));
				if (RecoverDetection(p, img, RecoverRadius, InputImages, out ImageDetection RecD))
					Detections.Add(RecD);
			}
			if(Detections.Count >= 3)
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
