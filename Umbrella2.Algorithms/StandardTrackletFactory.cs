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
				ObjectPoints ojp = imd.FetchProperty<ObjectPoints>();
				Pixels.AddRange(ojp.PixelPoints);
				Values.AddRange(ojp.PixelValues);
				imd.TryFetchProperty<PairingProperties>(out KeptProp);
			}
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
			List<ImageDetection> DetectionsList = new List<ImageDetection>();
			PixelPoint[] ValidPP = new PixelPoint[Detections.Length];
			DateTime[] ValidTimes = new DateTime[Detections.Length];
			DateTime ZeroTime = DateTime.Now;
			WCS.IWCSProjection Projection = null;
			for (int i = 0; i < Detections.Length; i++)
			{
				ValidPP[i] = Detections[i].Barycenter.PP;
				ValidTimes[i] = Detections[i].Time.Time;
				ZeroTime = ValidTimes[i];
				Projection = Detections[i].ParentImage.Transform;
			}
			var Xreg = LinearRegression.ComputeLinearRegression(ValidPP.Select((x) => x.X).ToArray(), ValidTimes.Select((x) => (x - ZeroTime).TotalSeconds).ToArray());
			var Yreg = LinearRegression.ComputeLinearRegression(ValidPP.Select((x) => x.Y).ToArray(), ValidTimes.Select((x) => (x - ZeroTime).TotalSeconds).ToArray());
			var XYreg = LinearRegression.ComputeLinearRegression(ValidPP);
			PixelVelocity pv = new PixelVelocity() { Xvel = 1 / Xreg.Slope, Yvel = 1 / Yreg.Slope };
			TrackletVelocityRegression tvr = new TrackletVelocityRegression() { R_TX = Xreg.PearsonR, R_TY = Yreg.PearsonR, R_XY = XYreg.PearsonR };
			double Velocity = Math.Sqrt(pv.Xvel * pv.Xvel + pv.Yvel * pv.Yvel) * Projection.GetEstimatedWCSChainDerivative();
			TrackletVelocity tvel = new TrackletVelocity() { PixelVelocity = pv, EquatorialVelocity = Velocity };

			return new Tracklet(Detections, tvel, tvr);
		}
	}
}
