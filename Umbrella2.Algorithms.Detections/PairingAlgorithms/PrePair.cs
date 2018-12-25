using System;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.Algorithms.Misc;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2.Algorithms.Pairing
{
	/// <summary>
	/// Attempts to merge detections that appear to be the same object.
	/// </summary>
	public static class PrePair
	{
		/// <summary>
		/// Match detections and merge those that seem to belong to the same object.
		/// </summary>
		/// <param name="RawDetections">Input set of detections.</param>
		/// <param name="MaxDistance">Maximum distance possible between two detections part of the same object.</param>
		/// <param name="MixMatch">Number of overlapping pixels before two detections are considered part of the same object.</param>
		public static void MatchDetections(List<ImageDetection> RawDetections, double MaxDistance, int MixMatch)
		{
			int i, j;
			List<HashSet<PixelPoint>> LHP = RawDetections.Select((x) => new HashSet<PixelPoint>(x.FetchProperty<ObjectPoints>().PixelPoints)).ToList();
			for (i = 0; i < RawDetections.Count; i++) for (j = i + 1; j < RawDetections.Count; j++)
				{
					/* Must be two detections captured at the same time */
					if (RawDetections[i].Time.Time != RawDetections[j].Time.Time) continue;
					/* Check distance */
					double D0 = (RawDetections[i].Barycenter.PP ^ RawDetections[j].Barycenter.PP);
					double D1 = (RawDetections[i].FetchProperty<ObjectSize>().PixelEllipse.SemiaxisMajor + RawDetections[j].FetchProperty<ObjectSize>().PixelEllipse.SemiaxisMajor);
					if (D0 - D1 > MaxDistance) continue;

					HashSet<PixelPoint> PixPi = LHP[i], PixPj = LHP[j];

					bool FlagAnyCond = false;
					/* If there are MinPix overlapping pixels, merge detections */
					if (PixPi.Overlaps(PixPj))
						FlagAnyCond = true;
					if (!FlagAnyCond)
					{
						/* Detections that are somewhat linear are checked for colinearity with others */
						IEnumerable<PixelPoint> Plist = PixPi.Concat(PixPj);
						LinearRegression.LinearRegressionParameters pc = LinearRegression.ComputeLinearRegression(Plist);
						LinearRegression.LinearRegressionParameters p1 = LinearRegression.ComputeLinearRegression(PixPi);
						LinearRegression.LinearRegressionParameters p2 = LinearRegression.ComputeLinearRegression(PixPj);
						if (Math.Abs(pc.PearsonR) > Math.Abs(p1.PearsonR) && Math.Abs(pc.PearsonR) > Math.Abs(p2.PearsonR)
							&& Math.Abs(pc.PearsonR) < Math.Abs(p1.PearsonR) + Math.Abs(p2.PearsonR)) FlagAnyCond = true;
					}
					/* If any merging condition is satisfied, merge the detections */
					if (FlagAnyCond)
					{
						LHP[i].UnionWith(LHP[j]);
						LHP.RemoveAt(j);
						RawDetections.RemoveAt(j);
						j--;
					}
				}
			for (i = 0; i < LHP.Count; i++) RawDetections[i] = StandardDetectionFactory.CreateDetection(RawDetections[i].ParentImage, LHP[i]);
		}
	}
}
