using System;
using System.Collections.Generic;
using System.Linq;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;

namespace Umbrella2.IO.FITS
{
	/// <summary>
	/// Provides functions for building FITS Images
	/// </summary>
	public static class FitsBuilder
	{
		/// <summary>
		/// Computes the headers for a new FITS image.
		/// </summary>
		/// <param name="Core">Header values.</param>
		/// <returns>A HeaderTable instance for the new FITS image.</returns>
		public static HeaderTable GetHeader(FICHV Core)
		{
			Dictionary<string, string> records = (Core.WCS == null ? GetHeaderWithoutTransform(Core) : GetHeaderWithTransform(Core, true));
			HeaderTable het = records.ToDictionary((x) => x.Key, (x) => (MetadataRecord)new FITSMetadataRecord(x.Key, x.Value));
			return het;
		}


		/// <summary>Computes the headers when the input image has WCS coordinates.</summary>
		static Dictionary<string, string> GetHeaderWithTransform(FICHV Core, bool RAFirst)
		{
			WCS.WCSViaProjection Transform = (WCS.WCSViaProjection)Core.WCS;
			string AlgName = Transform.ProjectionTransform.Name;
			Transform.ProjectionTransform.GetReferencePoints(out double RA, out double Dec);
			string T1 = " '" + (RAFirst ? "RA---" : "DEC--") + AlgName + "'";
			string T2 = " '" + (RAFirst ? "DEC--" : "RA---") + AlgName + "'";
			string V1 = "  " + ((RAFirst ? RA : Dec) * 180 / Math.PI).ToString("0.000000000000E+00");
			string V2 = "  " + ((RAFirst ? Dec : RA) * 180 / Math.PI).ToString("0.000000000000E+00");
			double[] Matrix = Transform.LinearTransform.Matrix;
			Dictionary<string, string> records = new Dictionary<string, string>()
			{
				{"SIMPLE", "   T" }, {"BITPIX", "   " + Core.BitPix.ToString()}, {"NAXIS"," 2"}, {"NAXIS1", "  " + Core.Width.ToString()}, {"NAXIS2", "  " + Core.Height.ToString()},
				{"CTYPE1", T1 }, {"CTYPE2", T2 }, { "CUNIT1", " 'deg     '"}, {"CUNIT2", " 'deg     '"}, {"CRVAL1", V1 }, {"CRVAL2", V2 },
				{"CRPIX1", "  "+ (RAFirst?Matrix[4]:Matrix[5]).ToString("0.000000000000E+00") }, {"CRPIX2", "  " +(RAFirst?Matrix[5]:Matrix[4]).ToString("0.000000000000E+00") },
				{"CD1_1", "  "+ (RAFirst?Matrix[0]:Matrix[2]).ToString("0.000000000000E+00") }, {"CD1_2", "  "+ (RAFirst?Matrix[1]:Matrix[3]).ToString("0.000000000000E+00") },
				{"CD2_1", "  "+ (RAFirst?Matrix[2]:Matrix[0]).ToString("0.000000000000E+00") }, {"CD2_2", "  "+ (RAFirst?Matrix[3]:Matrix[1]).ToString("0.000000000000E+00") }
			};
			return records;
		}

		/// <summary>Computes the headers when the input image has no WCS information.</summary>
		static Dictionary<string, string> GetHeaderWithoutTransform(FICHV Core)
		{
			Dictionary<string, string> records = new Dictionary<string, string>()
			{ {"SIMPLE", "   T" }, {"BITPIX", "   " + Core.BitPix.ToString()}, {"NAXIS"," 2"}, {"NAXIS1", "  " + Core.Width.ToString()}, {"NAXIS2", "  " + Core.Height.ToString()} };
			return records;
		}
	}
}
