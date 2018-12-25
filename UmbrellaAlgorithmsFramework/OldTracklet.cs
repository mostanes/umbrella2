using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.Algorithms.Misc;

namespace Umbrella2
{
	/// <summary>
	/// Represents a tracklet.
	/// </summary>
	/// <remarks>
	/// The interface is volatile for now.
	/// </remarks>
	[Obsolete]
	public class OldTracklet
	{
		readonly MedianDetection[][] Detections;
		public readonly MedianDetection[] MergedDetections;
		public readonly PixelPoint[] PixelBarycenters;
		public readonly double PixelVelocityX;
		public readonly double PixelVelocityY;
		public readonly double Velocity;
		public double LinearityPearsonR;
		public double TimeXPearsonR;
		public double TimeYPearsonR;

		public OldTracklet(MedianDetection[][] Detections)
		{
			this.Detections = Detections;
			MergedDetections = new MedianDetection[Detections.Length];
			PixelBarycenters = new PixelPoint[Detections.Length];
			List<PixelPoint> ValidPP = new List<PixelPoint>();
			List<DateTime> ValidTimes = new List<DateTime>();
			DateTime ZeroTime = DateTime.Now;
			WCS.WCSViaProjection Projection = null;
			for (int i = 0; i < MergedDetections.Length; i++)
				if (Detections[i].Length != 0)
				{
					List<PixelPoint> Points = new List<PixelPoint>();
					foreach (MedianDetection m in Detections[i])
						Points.AddRange(m.PixelPoints);
					List<double> Values = new List<double>();
					foreach (MedianDetection m in Detections[i])
						Values.AddRange(m.PixelValues);
					MergedDetections[i] = new MedianDetection(Detections[i][0].ParentImage.Transform, Detections[i][0].ParentImage, Points, Values);
					MergedDetections[i].IsDotDetection = Detections[i][0].IsDotDetection;
					PixelBarycenters[i] = MergedDetections[i].BarycenterPP;
					ZeroTime = MergedDetections[i].Time.Time;
					Projection = MergedDetections[i].ParentImage.Transform;
					ValidPP.Add(PixelBarycenters[i]);
					ValidTimes.Add(MergedDetections[i].Time.Time);
				}
			var Xreg = LinearRegression.ComputeLinearRegression(ValidPP.Select((x) => x.X).ToArray(), ValidTimes.Select((x) => (x - ZeroTime).TotalSeconds).ToArray());
			var Yreg = LinearRegression.ComputeLinearRegression(ValidPP.Select((x) => x.Y).ToArray(), ValidTimes.Select((x) => (x - ZeroTime).TotalSeconds).ToArray());
			PixelVelocityX = 1 / Xreg.Slope;
			PixelVelocityY = 1 / Yreg.Slope;
			TimeXPearsonR = Xreg.PearsonR;
			TimeYPearsonR = Yreg.PearsonR;
			Velocity = Math.Sqrt(PixelVelocityX * PixelVelocityX + PixelVelocityY * PixelVelocityY) * Projection.GetEstimatedWCSChainDerivative();
		}
	}
}
