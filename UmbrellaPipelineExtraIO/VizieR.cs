using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Umbrella2;


namespace Umbrella2.Pipeline.ExtraIO
{
	public static class VizieR
	{
		/* USNO B1.0 Catalogue URL in VizieR */
		public static string VizieRURL = "http://vizier.ast.cam.ac.uk/viz-bin/asu-tsv?-out.max=1000&-sort=_r&-order=I&-oc.form=sexa&-c.eq=J2000&-c.u=arcmin&-c.geom=r&-source=I/284/out&-c=";

		public struct StarInfo
		{
			public EquatorialPoint Coordinate;
			public double Magnitude;
		}

		public static List<StarInfo> GetVizieRObjects(EquatorialPoint Center, double Radius, double LowMagLimit)
		{
			WebClient client = new WebClient();
			double RadiusArcMin = Radius * 180 * 60 / Math.PI;
			string URL = VizieRURL + Center.FormatToString(EquatorialPointStringFormatter.Format.MPC) + "&-c.r=" + RadiusArcMin.ToString("0.00");
			string Data = "";
			try
			{ Data = client.DownloadString(URL); }
			catch (WebException ex)
			{ return new List<StarInfo>(); }
			string[] lines = Data.Split('\n');
			int i;
			List<string[]> objsString = new List<string[]>();
			/* Skip to beginning of table */
			for (i = 0; i < lines.Length; i++) if (lines[i].StartsWith("----")) break;
			/* Read each line and split values (table is TSV) */
			for (i++; i < lines.Length; i++) objsString.Add(lines[i].Split('\t'));
			List<StarInfo> sti = new List<StarInfo>(objsString.Count);
			/* Foreach entry */
			foreach (string[] sk in objsString)
			{
				/* Check if it matches the 14-entry format recognized */
				if (sk.Length != 14) continue;
				/* The next 3 should be in order: */
				sk[1] = sk[1].Trim(); /* RA */
				sk[2] = sk[2].Trim(); /* Dec */
				sk[12] = sk[12].Trim(); /* R2mag */
				if (string.IsNullOrEmpty(sk[1]) | string.IsNullOrEmpty(sk[2]) | string.IsNullOrEmpty(sk[12])) continue;
				StarInfo sif = new StarInfo();
				double RA = double.Parse(sk[1]) * Math.PI / 180;
				double Dec = double.Parse(sk[2]) * Math.PI / 180;
				EquatorialPoint EqP = new EquatorialPoint() { RA = RA, Dec = Dec };
				sif.Coordinate = EqP;
				sif.Magnitude = double.Parse(sk[12]);
				sti.Add(sif);
			}
			return sti.Where((x) => x.Magnitude < LowMagLimit).ToList();
		}
	}
}
