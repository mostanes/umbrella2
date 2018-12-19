using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.Algorithms.Misc;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2
{
	public static class StandardTrackletFactory
	{
		public static ImageDetection MergeStandardDetections(ImageDetection[] Detections)
		{
			List<PixelPoint> Pixels = new List<PixelPoint>();
			List<double> Values = new List<double>();
			foreach(ImageDetection imd in Detections)
			{
				ObjectPoints ojp = imd.FetchProperty<ObjectPoints>();
				Pixels.AddRange(ojp.PixelPoints);
				Values.AddRange(ojp.PixelValues);
			}
			return StandardDetectionFactory.CreateDetection(Detections[0].ParentImage, Pixels, Values);
		}

		public static Tracklet CreateTracklet(ImageDetection[] Detections)
		{
			List<ImageDetection> DetectionsList = new List<ImageDetection>();
			PixelPoint[] ValidPP = new PixelPoint[Detections.Length];
			DateTime[] ValidTimes = new DateTime[Detections.Length];
			DateTime ZeroTime = DateTime.Now;
			WCS.WCSViaProjection Projection = null;
			for (int i = 0; i < Detections.Length; i++)
			{
				ValidPP[i] = Detections[i].Barycenter.PP;
				ValidTimes[i] = Detections[i].Time.Time;
				ZeroTime = ValidTimes[i];
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
