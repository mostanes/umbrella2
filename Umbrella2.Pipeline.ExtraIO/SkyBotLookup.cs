using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace Umbrella2.Pipeline.ExtraIO
{
	/// <summary>
	/// Provides an API for accessing the SkyBot services.
	/// </summary>
	public static class SkyBoTLookup
	{
		const string SkybotURL = "http://vo.imcce.fr/webservices/skybot/skybotconesearch_query.php?EPOCH=";

		/// <summary>
		/// Represents an object returned by SkyBoT.
		/// </summary>
		public struct SkybotObject
		{
			/// <summary>
			/// Name of the object.
			/// </summary>
			public readonly string Name;
			/// <summary>
			/// Position of the object.
			/// </summary>
			public readonly EquatorialPoint Position;
			/// <summary>
			/// Time at which the object is at the specified position.
			/// </summary>
			public readonly DateTime TimeCoordinate;
			/// <summary>
			/// The object's permanent designation.
			/// </summary>
			public readonly int? PermanentDesignation;
			/// <summary>
			/// The object's asteroid class.
			/// </summary>
			public readonly string Class;

			public SkybotObject(string Name, string Position, DateTime Time, int? PermDesignation, string Class)
			{
				this.Name = Name;
				this.Position = EquatorialPointStringFormatter.ParseFromMPCString(Position);
				this.TimeCoordinate = Time;
				this.PermanentDesignation = PermDesignation;
				this.Class = Class;
			}
		}

		/// <summary>
		/// Retrieves a list of objects around a given location.
		/// </summary>
		/// <param name="Location">Location around which to search.</param>
		/// <param name="Radius">Radius of the search (in radians).</param>
		/// <param name="Time">The time at which to search for objects.</param>
		/// <returns>The list of objects.</returns>
		public static List<SkybotObject> GetObjects(EquatorialPoint Location, double Radius, DateTime Time)
		{
			/* Prepares the query */
			string url = SkybotURL;
			double JulianDate = (Time - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays + 2440587.5;
			url += JulianDate.ToString() + "&RA=" + (Location.RA * 180 / Math.PI).ToString() + "&DEC=" + (Location.Dec * 180 / Math.PI).ToString();
			url += "&SR=" + (Radius * 180 / Math.PI).ToString() + "&VERB=1";
			string result = string.Empty;
			/* Tries querying the SkyBoT server, returining nothing if it fails. */
			try
			{
				result = (new WebClient()).DownloadString(url);
			}
			catch
			{
				return new List<SkybotObject>();
			}
			/* Parsing Skybot output: specific selection of vot:TABLEDATA and namespace cleanup */
			int sel1 = result.IndexOf("<vot:TABLEDATA>", StringComparison.InvariantCulture);
			if (sel1 == -1) return new List<SkybotObject>();
			string data = result.Substring(sel1);
			int sel2 = data.IndexOf("</vot:TABLEDATA>", StringComparison.InvariantCulture);
			data = data.Substring(0, sel2 + 16);
			data = data.Replace("vot:", "");
			/* Parsing xml output */
			XDocument xdoc = XDocument.Parse(data);
			List<SkybotObject> objs = new List<SkybotObject>();
			XElement key = xdoc.Elements().ToArray()[0];
			foreach (XElement xe in key.Elements())
			{
#warning Manually parsed. Does not verify API headers.
				var z = xe.Elements().ToArray();
				string permdes = z[0].Value;
				string name = z[1].Value;
				string ra = z[2].Value;
				string dec = z[3].Value;
				string cls = z[4].Value;
				int? pd = int.TryParse(permdes, out int pdi) ? (int?)pdi : null;
				SkybotObject oj = new SkybotObject(name, ra + " " + dec, Time, pd, cls);
				objs.Add(oj);
			}
			return objs;
		}
	}
}