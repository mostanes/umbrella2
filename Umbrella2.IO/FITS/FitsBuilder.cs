using System;
using System.Collections.Generic;
using System.Linq;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;

namespace Umbrella2.IO.FITS
{
	public static class FitsBuilder
	{
		/*/// <summary>
		/// Creates a new FITS image.
		/// </summary>
		/// <param name="File">File backing the image.</param>
		/// <param name="Header">Entries in the FITS image header.</param>
		/// <param name="ExtraProperties">Extra image properties to write in the header.</param>
		/// <param name="ReverseAxis">Reverses the order of the axis in the header.</param>
		public FitsImage(FitsFile File, FICHV Header, List<ImageProperties> ExtraProperties = null, bool ReverseAxis = false) :
		this(0, Header)
		{
			if (Width > MaxSize || Height > MaxSize) throw new FITSFormatException("Image too large for Umbrella2.");
			this.File = File;
			BytesPerPixel = (byte)Math.Abs(Header.BitPix / 8);
			var RW = GetRW(Header.BitPix);
			Reader = RW.Item1;
			Writer = RW.Item2;
			RAFirst = !ReverseAxis;
			this.Header = FitsBuilder.GetHeader(this, Header.BitPix);
			if (ExtraProperties != null)
				foreach (ImageProperties prop in ExtraProperties) foreach (MetadataRecord er in prop.GetRecords()) Header.Add(er.Name, er);
			File.SetPrimaryHeaders(Header);
		}*/


		/// <summary>
		/// Computes the headers for a new FITS image.
		/// </summary>
		/// <param name="Core">Header values.</param>
		/// <returns>A HeaderTable instance for the new FITS image.</returns>
		static HeaderTable GetHeader(FICHV Core)
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
