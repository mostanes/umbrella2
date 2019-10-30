using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.Algorithms.Misc;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2
{
	/// <summary>
	/// A set of standard methods for creating Tracklets.
	/// </summary>
	public static class StandardTrackletFactory
	{
		/// <summary>
		/// Creates a new ImageDetection by merging the blobs of other detections. Requires the input ImageDetections to have <see cref="ObjectPoints"/> property.
		/// </summary>
		/// <param name="Detections">The list of input detections to merge.</param>
		/// <returns>A new instance of ImageDetection.</returns>
		public static ImageDetection MergeStandardDetections(ImageDetection[] Detections)
		{
			List<PixelPoint> Pixels = new List<PixelPoint>();
			List<double> Values = new List<double>();
			PairingProperties KeptProp = null;
			foreach(ImageDetection imd in Detections)
			{
				if (imd.TryFetchProperty(out ObjectPoints ojp))
				{
					Pixels.AddRange(ojp.PixelPoints);
					Values.AddRange(ojp.PixelValues);
					imd.TryFetchProperty<PairingProperties>(out KeptProp);
				}
			}
#warning This is not fine.
			if (Pixels.Count == 0) return Detections[0];
			ImageDetection Result = StandardDetectionFactory.CreateDetection(Detections[0].ParentImage, Pixels, Values);
			if (KeptProp != null) Result.SetResetProperty(KeptProp);
			return Result;
		}

		/// <summary>
		/// Creates a tracklet from a set of detections.
		/// </summary>
		/// <param name="Detections">Input detections; one per image.</param>
		/// <returns>A new Tracklet instance.</returns>
		public static Tracklet CreateTracklet(ImageDetection[] Detections)
		{
			Array.Sort(Detections, (x, y) => (int)(x.Time.Time.Ticks - y.Time.Time.Ticks));

			EquatorialPoint[] ValidEP = new EquatorialPoint[Detections.Length];
			double[] ValidTimes = new double[Detections.Length];
			DateTime ZeroTime = Detections[0].Time.Time;
			WCS.IWCSProjection Projection = Detections[0].ParentImage.Transform;

			for (int i = 0; i < Detections.Length; i++)
			{
				ValidEP[i] = Detections[i].Barycenter.EP;
				ValidTimes[i] = (Detections[i].Time.Time - ZeroTime).TotalSeconds;
			}

			var XRA = ValidEP.Select((x) => x.RA).ToArray();
			var XDec = ValidEP.Select((x) => x.Dec).ToArray();
			var RAreg = LinearRegression.ComputeLinearRegression(ValidTimes, XRA);
			var Decreg = LinearRegression.ComputeLinearRegression(ValidTimes, XDec);
			var RADecreg = LinearRegression.ComputeLinearRegression(XRA, XDec);
			double ResRA = LineFit.ComputeResidualSqSum(RAreg, ValidTimes, XRA);
			double ResDec = LineFit.ComputeResidualSqSum(Decreg, ValidTimes, XDec);
			EquatorialVelocity ev = new EquatorialVelocity() { RAvel = RAreg.Slope, Decvel = Decreg.Slope };
			TrackletVelocityRegression tvr = new TrackletVelocityRegression() { R_TR = RAreg.PearsonR, R_TD = Decreg.PearsonR, R_RD = RADecreg.PearsonR,
				S_TR = ResRA, S_TD = ResDec };
			TrackletVelocity tvel = new TrackletVelocity() { EquatorialVelocity = ev, PixelVelocity = Projection.GetPixelVelocity(ev) };
			tvel.SphericalVelocity = tvel.PixelVelocity.Magnitude * Projection.GetEstimatedWCSChainDerivative();

			return new Tracklet(Detections, tvel, tvr);
		}
	}
}
