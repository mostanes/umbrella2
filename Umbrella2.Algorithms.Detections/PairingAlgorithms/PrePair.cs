using System;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.Algorithms.Misc;

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
		public static void MatchDetections(List<MedianDetection> RawDetections, double MaxDistance, int MixMatch)
		{
			int i, j;
			for (i = 0; i < RawDetections.Count; i++) for (j = i + 1; j < RawDetections.Count; j++)
				{
					/* Must be two detections captured at the same time */
					if (RawDetections[i].Time.Time != RawDetections[j].Time.Time) continue;
					/* Check distance */
					double D0 = (RawDetections[i].BarycenterPP ^ RawDetections[j].BarycenterPP);
					double D1 = (RawDetections[i].PixelEllipse.SemiaxisMajor + RawDetections[j].PixelEllipse.SemiaxisMajor);
					if (D0 - D1 > MaxDistance) continue;

					bool FlagAnyCond = false;
					/* If there are MinPix overlapping pixels, merge detections */
					if (RawDetections[i].PixelPoints.Intersect(RawDetections[j].PixelPoints).Count() > MixMatch)
						FlagAnyCond = true;
					if (!FlagAnyCond)
					{
						/* Detections that are somewhat linear are checked for colinearity with others */
						IEnumerable<PixelPoint> Plist = RawDetections[i].PixelPoints.Concat(RawDetections[j].PixelPoints);
						LinearRegression.LinearRegressionParameters pc = LinearRegression.ComputeLinearRegression(Plist);
						LinearRegression.LinearRegressionParameters p1 = LinearRegression.ComputeLinearRegression(RawDetections[i].PixelPoints);
						LinearRegression.LinearRegressionParameters p2 = LinearRegression.ComputeLinearRegression(RawDetections[j].PixelPoints);
						if (Math.Abs(pc.PearsonR) > Math.Abs(p1.PearsonR) && Math.Abs(pc.PearsonR) > Math.Abs(p2.PearsonR)
							&& Math.Abs(pc.PearsonR) < Math.Abs(p1.PearsonR) + Math.Abs(p2.PearsonR)) FlagAnyCond = true;
					}
					/* If any merging condition is satisfied, merge the detections */
					if(FlagAnyCond)
					{ 
						RawDetections[i] = MergeMD(RawDetections[i], RawDetections[j]);
						RawDetections.RemoveAt(j);
						j--;
					}
				}
		}

		/// <summary>
		/// Creates a new MedianDetection by the merger of 2 others MedianDetections.
		/// </summary>
		/// <param name="a">First detection to be merged.</param>
		/// <param name="b">Second detection to be merged.</param>
		/// <returns>A new instance of MedianDetection containing the pixels of the input MedianDetections.</returns>
		static MedianDetection MergeMD(MedianDetection a, MedianDetection b)
		{
			/* Uniquely merge pixels */
			HashSet<PixelPoint> hsp = new HashSet<PixelPoint>(a.PixelPoints);
			for(int i=0;i<b.PixelPoints.Count;i++)
			{
				if (hsp.Add(b.PixelPoints[i]))
					a.PixelValues.Add(b.PixelValues[i]);
			}
			MedianDetection mmd = new MedianDetection(a.ParentImage.Transform, a.ParentImage, hsp.ToList(), a.PixelValues);
			return mmd;
		}
	}
}
