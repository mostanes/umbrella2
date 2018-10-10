using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.Algorithms.Misc;

namespace Umbrella2.Algorithms.Pairing
{
	public class PrePair
	{
		public static void MatchDetections(List<MedianDetection> RawDetections, double MaxDistance, double CorrelationThreshold, int MixMatch)
		{
			int i, j;
			for (i = 0; i < RawDetections.Count; i++) for (j = i + 1; j < RawDetections.Count; j++)
				{
					if (RawDetections[i].Time.Time != RawDetections[j].Time.Time) continue;
					double D0 = (RawDetections[i].BarycenterPP ^ RawDetections[j].BarycenterPP);
					double D1 = (RawDetections[i].PixelEllipse.SemiaxisMajor + RawDetections[j].PixelEllipse.SemiaxisMajor);
					if (D0 - D1 > MaxDistance) continue;
					bool FlagAnyCond = false;
					if (RawDetections[i].PixelPoints.Intersect(RawDetections[j].PixelPoints).Count() > MixMatch)
						FlagAnyCond = true;
					if (!FlagAnyCond)
					{
						IEnumerable<PixelPoint> Plist = RawDetections[i].PixelPoints.Concat(RawDetections[j].PixelPoints);
						LinearRegression.LinearRegressionParameters pc = LinearRegression.ComputeLinearRegression(Plist);
						LinearRegression.LinearRegressionParameters p1 = LinearRegression.ComputeLinearRegression(RawDetections[i].PixelPoints);
						LinearRegression.LinearRegressionParameters p2 = LinearRegression.ComputeLinearRegression(RawDetections[j].PixelPoints);
						if (Math.Abs(pc.PearsonR) > Math.Abs(p1.PearsonR) && Math.Abs(pc.PearsonR) > Math.Abs(p2.PearsonR)
							&& Math.Abs(pc.PearsonR) < Math.Abs(p1.PearsonR) + Math.Abs(p2.PearsonR)) FlagAnyCond = true;
					}
					if(FlagAnyCond)
					{ 
						RawDetections[i] = MergeMD(RawDetections[i], RawDetections[j]);
						RawDetections.RemoveAt(j);
						j--;
					}
				}
		}

		static MedianDetection MergeMD(MedianDetection a, MedianDetection b)
		{
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
