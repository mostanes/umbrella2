using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Umbrella2.Pipeline.ExtraIO
{
	/// <summary>
	/// Provides an API for accessing the SkyBot services.
	/// </summary>
	public static class SkyBoTLookup
	{
		const string SkybotURL = "http://vo.imcce.fr/webservices/skybot/skybotconesearch_query.php?EPOCH=";
		const string NSInterface = "http://vo.imcce.fr/webservices/skybot/skybotconesearch_query.php?";
		const string NSParameters = "-ep={0}&-ra={1}&-dec={2}&-rd={3}&-mime=votable&-output=basic&-loc={4}";
		private const string VOTxmlns = "http://www.ivoa.net/xml/VOTable/v1.3";

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
		/// Generates an URL for the Simple Cone Search interface, to be used with <see cref="GetObjects(string, DateTime, out List{SkybotObject})"/>.
		/// </summary>
		/// <returns>The URL to be used with <see cref="GetObjects(string, DateTime, out List{SkybotObject})"/>.</returns>
		/// <param name="Location">Location around which to search.</param>
		/// <param name="Radius">Search radius.</param>
		/// <param name="Time">The time at which to search for objects.</param>
		public static string GenerateSCSUrl(EquatorialPoint Location, double Radius, DateTime Time)
		{
			string url = SkybotURL;
			double JulianDate = (Time - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays + 2440587.5;
			url += JulianDate.ToString() + "&RA=" + (Location.RA * 180 / Math.PI).ToString() + "&DEC=" + (Location.Dec * 180 / Math.PI).ToString();
			url += "&SR=" + (Radius * 180 / Math.PI).ToString() + "&VERB=1";
			return url;
		}

		/// <summary>
		/// Generates an URL for the non-standard interface to SkyBoT.
		/// </summary>
		/// <returns>The URL to be used with <see cref="GetObjects(string, DateTime, out List{SkybotObject})"/>.</returns>
		/// <param name="Location">Location around which to search.</param>
		/// <param name="Radius">Search radius.</param>
		/// <param name="Time">The time at which to search for objects.</param>
		/// <param name="ObsCode">Observatory code.</param>
		public static string GenerateNSUrl(EquatorialPoint Location, double Radius, DateTime Time, string ObsCode)
		{
			double JulianDate = (Time - new DateTime(1970, 1, 1, 0, 0, 0)).TotalDays + 2440587.5;
			string Params = string.Format(NSParameters, JulianDate, (Location.RA * 180 / Math.PI), (Location.Dec * 180 / Math.PI),
				(Radius * 180 / Math.PI), ObsCode);
			return NSInterface + Params;
		}

		/// <summary>
		/// Retrieves the list of objects from the given url.
		/// </summary>
		/// <remarks>
		/// If the function returned false, but the object list is non-null, then requried fields in the VOTable were missing.
		/// </remarks>
		/// <returns><c>true</c>, if succeded in obtaining, <c>false</c> otherwise.</returns>
		/// <param name="Url">The URL from which to retrieve objects.</param>
		/// <param name="Time">Time corresponding to the given URL.</param>
		/// <param name="Objects">The resulting list of objects.</param>
		public static bool GetObjects(string Url, DateTime Time, out List<SkybotObject> Objects)
		{
			string result = string.Empty;
			/* Tries querying the SkyBoT server, returining false if it fails. */
			using (WebClient client = new WebClient())
				try
				{ client.Proxy = null; result = client.DownloadString(Url); }
				catch
				{ Objects = null; return false; }

			XmlNamespaceManager xmgr = new XmlNamespaceManager(new NameTable());
			xmgr.AddNamespace("vot", VOTxmlns);
			XDocument Doc = XDocument.Parse(result);
			XElement Root = Doc.Root;
			/* Parse column headers */
			var Columns = Root.XPathSelectElements("/vot:VOTABLE/vot:RESOURCE/vot:TABLE/vot:FIELD", xmgr);
			Dictionary<string, int> Cset = new Dictionary<string, int>();
			int csn = 0;
			foreach (var clmn in Columns)
				Cset.Add(clmn.Attribute("name").Value, csn++);

			/* Parse table */
			var Rows = Root.XPathSelectElements("/vot:VOTABLE/vot:RESOURCE/vot:TABLE/vot:DATA/vot:TABLEDATA/vot:TR", xmgr);
			Objects = new List<SkybotObject>();
			foreach(var Row in Rows)
			{
				List<string> Values = Row.Elements().Select((x) => x.Value).ToList();

				try
				{
					int? pd = int.TryParse(Values[Cset["Num"]], out int pdi) ? (int?)pdi : null;
					SkybotObject sko = new SkybotObject(Values[Cset["Name"]], Values[Cset["RA"]] + " " + Values[Cset["DEC"]], Time, pd, Values[Cset["Class"]]);
					Objects.Add(sko);
				}
				catch (KeyNotFoundException) { return false; }
			}
			return true;
		}

		/// <summary>
		/// Retrieves a list of objects around a given location.
		/// </summary>
		/// <param name="Location">Location around which to search.</param>
		/// <param name="Radius">Radius of the search (in radians).</param>
		/// <param name="Time">The time at which to search for objects.</param>
		/// <returns>The list of objects.</returns>
		[Obsolete]
		public static List<SkybotObject> GetObjects(EquatorialPoint Location, double Radius, DateTime Time)
		{
			/* Prepares the query */
			string url = GenerateSCSUrl(Location, Radius, Time);
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