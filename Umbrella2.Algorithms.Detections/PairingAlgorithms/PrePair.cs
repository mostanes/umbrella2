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
		/// <param name="PSFMatch">Distance between the barycenters of 2 detections before they are considered the same (for external detections mostly).</param>
		public static void MatchDetections(List<ImageDetection> RawDetections, double MaxDistance, int MixMatch, double PSFMatch)
		{
			int i, j;
			List<HashSet<PixelPoint>> LHP = RawDetections.Select((x) => x.TryFetchProperty(out ObjectPoints op) ? new HashSet<PixelPoint>(op.PixelPoints) : null).ToList();
			List<PairingProperties> PairPropList = RawDetections.Select((x) => x.TryFetchProperty(out PairingProperties Prop) ? Prop : null).ToList();
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
					if (D0 < PSFMatch)
						FlagAnyCond = true;
					/* If any merging condition is satisfied, merge the detections */
					if (FlagAnyCond)
					{
						if (LHP[i] != null & LHP[j] != null)
							LHP[i].UnionWith(LHP[j]);
						else if (LHP[i] == null) LHP[i] = LHP[j];
						if (PairPropList[i] != null && PairPropList[j] != null)
							PairPropList[i].Algorithm |= PairPropList[i].Algorithm;
						LHP.RemoveAt(j);
						RawDetections.RemoveAt(j);
						PairPropList.RemoveAt(j);
						j--;
					}
				}
			for (i = 0; i < LHP.Count; i++)
			{
				if (LHP[i] != null)
				{
					try
					{
						RawDetections[i] = StandardDetectionFactory.CreateDetection(RawDetections[i].ParentImage, LHP[i]);
						if (PairPropList[i] != null) RawDetections[i].SetResetProperty(PairPropList[i]);
					}
					catch { RawDetections.RemoveAt(i); PairPropList.RemoveAt(i); LHP.RemoveAt(i); i--; }
				}
			}
		}
	}
}
