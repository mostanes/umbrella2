using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Umbrella2;


namespace Umbrella2.Pipeline.ExtraIO
{
	/// <summary>
	/// Provides an API for accessing VizieR services.
	/// </summary>
	public static class VizieR
	{
		/* USNO B1.0 Catalogue URL in VizieR */
		private const string USNOB10 = "http://vizier.ast.cam.ac.uk/viz-bin/asu-tsv?-out.max=1000&-sort=_r&-order=I&-oc.form=sexa&-c.eq=J2000&-c.u=arcmin&-c.geom=r&-source=I/284/out&-c=";

		/// <summary>
		/// URL used for querying VizieR.
		/// </summary>
		public static string VizieRURL = USNOB10;

		/// <summary>
		/// Star data as provided from VizieR.
		/// </summary>
		public struct StarInfo
		{
			public EquatorialPoint Coordinate;
			public double Magnitude;
		}

		/// <summary>
		/// Retrieves a list of reference stars around a given position.
		/// </summary>
		/// <param name="Center">Location around which to search.</param>
		/// <param name="Radius">Radius (in radians) of the search cone.</param>
		/// <param name="LowMagLimit">Lowest star magnitude to include in results.</param>
		/// <returns>A list of StarInfo containing the data of the reference stars.</returns>
		public static List<StarInfo> GetVizieRObjects(EquatorialPoint Center, double Radius, double LowMagLimit)
		{
			double RadiusArcMin = Radius * 180 * 60 / Math.PI;
			string URL = VizieRURL + Center.FormatToString(EquatorialPointStringFormatter.Format.MPC) + "&-c.r=" + RadiusArcMin.ToString("0.00");
			string Data = "";
			using (WebClient client = new WebClient())
				try
				{ client.Proxy = null; Data = client.DownloadString(URL); }
				catch (WebException)
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
