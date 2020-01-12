using System;
using System.Collections.Generic;
using Umbrella2.Algorithms.Filtering;

namespace Umbrella2.Algorithms.Detection
{
	public static class TrackletsDeduplication
	{
		const double Arc1Sec = Math.PI / 180 / 3600;
		const int MatchOut = 2;

		/// <summary>
		/// In-place deduplicates the list of tracklets at a given detection separation.
		/// </summary>
		/// <param name="Tracklets">Tracklets to deduplicate.</param>
		/// <param name="Separation">Separation between 2 detections considered the same. Value in arcseconds.</param>
		public static void Deduplicate(List<Tracklet> Tracklets, double Separation)
		{
			Separation *= Arc1Sec;
			for (int i = 0; i < Tracklets.Count; i++)
			{
				bool Iout = false;
				for (int j = i + 1; j < Tracklets.Count; j++)
				{
					int Match = 0;
					foreach (ImageDetection imdi in Tracklets[i].Detections)
						foreach (ImageDetection imdj in Tracklets[j].Detections)
							if ((imdi.Barycenter.EP ^ imdj.Barycenter.EP) < Separation)
								Match++;
					if (Match >= MatchOut)
					{
						if (Tracklets[i].VelReg.S_TD + Tracklets[i].VelReg.S_TR < Tracklets[j].VelReg.S_TD + Tracklets[j].VelReg.S_TR)
						{
							Tracklets.RemoveAt(j);
							j--;
							continue;
						}
						else { Iout = true; break; }
					}
				}
				if (Iout)
				{
					Tracklets.RemoveAt(i);
					i--;
					continue;
				}
			}
		}
	}
}
