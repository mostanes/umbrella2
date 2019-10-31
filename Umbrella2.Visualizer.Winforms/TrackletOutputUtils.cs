using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.Pipeline.EIOAlgorithms;
using Umbrella2.Pipeline.ExtraIO;


namespace Umbrella2.Visualizers.Winforms
{
	static class TrackletOutputUtils
	{
		const double RadiusMultiplier = 1.2;

		static EquatorialPoint ComputeBoundingDisk(IEnumerable<Tracklet> Tracklets, out double Radius)
		{
			double RAmin = 100, RAmax = -100, DecMin = 100, DecMax = -100;
			foreach (Tracklet tk in Tracklets) foreach (ImageDetection imd in tk.Detections)
				{
					EquatorialPoint q = imd.Barycenter.EP;
					if (q.RA < RAmin) RAmin = q.RA;
					if (q.RA > RAmax) RAmax = q.RA;
					if (q.Dec < DecMin) DecMin = q.RA;
					if (q.Dec > DecMax) DecMax = q.RA;
				}
			double CenterRA = (RAmax + RAmin) / 2, CenterDec = (DecMax + DecMin) / 2;
			double DeltRA2 = (RAmax - RAmin) / 2, DeltDec2 = (DecMax - DecMin) / 2;
			Radius = RadiusMultiplier * Math.Sqrt(CenterRA * CenterRA + CenterDec * CenterDec);
			return new EquatorialPoint() { RA = CenterRA, Dec = CenterDec };
		}
	}
}
