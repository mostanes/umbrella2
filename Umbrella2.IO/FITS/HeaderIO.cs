using System;
using System.Collections.Generic;
using System.IO;
using static Umbrella2.IO.FITS.FitsFile;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;

namespace Umbrella2.IO.FITS
{
	/// <summary>
	/// Contains functions for reading FITS headers.
	/// </summary>
	static class HeaderIO
	{
		/// <summary>
		/// Reads a FITS header from a stream.
		/// </summary>
		/// <param name="s">Input stream.</param>
		/// <param name="Length">Expected length of input stream.</param>
		/// <returns>A list with all raw keyword records in the header.</returns>
		static List<KeywordRecord> ReadHeader(Stream s, long Length)
		{
			List<KeywordRecord> Kwlist = new List<KeywordRecord>();
			bool HeaderEnd = false;
			while (!HeaderEnd & s.Position < Length)
			{
				int i;
				byte[] Buffer = new byte[80];
				for (i = 0; i < 36; i++)
				{
					s.Read(Buffer, 0, 80);
					KeywordRecord kr = new KeywordRecord(Buffer);
					Kwlist.Add(kr);
					if (kr.Name.TrimEnd(' ') == "END") HeaderEnd = true;
				}
			}
			return Kwlist;
		}

		/// <summary>
		/// Computes the length of a data array in a HDU.
		/// </summary>
		/// <param name="Header">The header for which to compute the array length.</param>
		/// <returns>Array length in bytes.</returns>
		public static int ComputeDataArrayLength(Dictionary<string, MetadataRecord> Header)
		{
			try
			{
				MetadataRecord bp = Header["BITPIX"];
				MetadataRecord naxr = Header["NAXIS"];
				int nax = naxr.Int;
				if (nax != 2)
				{
					if (nax == 0) return 0;
					else throw new NotSupportedException("Umbrella2 can only read 2D images.");
				}
				MetadataRecord n1 = Header["NAXIS1"];
				MetadataRecord n2 = Header["NAXIS2"];

				return Math.Abs(bp.Int) * n1.Int * n2.Int / 8;
			}
			catch (Exception ex)
			{ throw new FITSFormatException("Missing or malformed FITS header entries.", ex); }
		}

		/// <summary>
		/// Reads a FITS header from a stream.
		/// </summary>
		/// <param name="stream">Input stream.</param>
		/// <param name="Length">Expected length of the input stream.</param>
		/// <returns>A tuple containing a list and a dictionary of the header records.</returns>
		internal static Tuple<List<MetadataRecord>, HeaderTable> ReadHeaderFromStream(Stream stream, long Length)
		{
			/* Read the headers and create the ElevatedRecord entries. */
			List<KeywordRecord> prirec = ReadHeader(stream, Length);
			List<MetadataRecord> PrimaryHeader = new List<MetadataRecord>();
			foreach (KeywordRecord kr in prirec) if (kr.HasEqual) PrimaryHeader.Add(KeywordRecord.Elevate(kr));

			/* Give easy access to the metadata in the headers via the PrimaryTable. */
			Dictionary<string, bool> Unique = new Dictionary<string, bool>();
			HeaderTable PrimaryTable = new Dictionary<string, MetadataRecord>();
			foreach (MetadataRecord ere in PrimaryHeader)
			{
				if (Unique.ContainsKey(ere.Name)) { Unique[ere.Name] = false; continue; }
				Unique.Add(ere.Name, true);
				PrimaryTable.Add(ere.Name, ere);
			}
			foreach (MetadataRecord ere in PrimaryHeader) if (!Unique[ere.Name]) PrimaryTable.Remove(ere.Name);

			return new Tuple<List<MetadataRecord>, HeaderTable>(PrimaryHeader, PrimaryTable);
		}

		/// <summary>
		/// Reads the FITS headers from a stream.
		/// </summary>
		/// <param name="stream">Input stream.</param>
		/// <param name="Length">Expected stream length.</param>
		/// <param name="numberGetter">The function which assigns image numbers to FITS images in file.</param>
		/// <returns>A FitsFileBuilder containing the information from the headers.</returns>
		internal static FitsFileBuilder ReadFileHeaders(Stream stream, long Length, MEFImageNumberGetter numberGetter)
		{
			FitsFileBuilder builder = new FitsFileBuilder();
			var R1 = ReadHeaderFromStream(stream, Length);
			builder.PrimaryHeader = R1.Item1;
			builder.PrimaryTable = R1.Item2;

			/* Setup primary data array. Start looking for extensions. */
			builder.PrimaryDataPointer = (int)stream.Position;
			int ALength = ComputeDataArrayLength(builder.PrimaryTable);

			/* Align to 2880 */
			int AALength = (ALength + 2879) / 2880 * 2880;
			long NewPosition = stream.Position + AALength;
			if (NewPosition > Length)
				throw new FITSFormatException("Data array longer than file length.");
			stream.Position = NewPosition;

			/* Check if file is MEF. */
			if (stream.Position != Length)
			{
				if (numberGetter == null) numberGetter = DefaultGetter;
				builder.ExtensionHeaders = new List<List<MetadataRecord>>();
				builder.ExtensionDataPointers = new List<int>();
				builder.MEFDataPointers = new Dictionary<int, int>();
				builder.MEFImagesHeaders = new Dictionary<int, List<MetadataRecord>>();
				builder.MEFHeaderTable = new Dictionary<int, HeaderTable>();

				/* Read extension headers. */
				for (int exn = 0; stream.Position < Length; exn++)
				{
					var R2 = ReadHeaderFromStream(stream, Length);
					int Loc = (int)stream.Position;
					ALength = ComputeDataArrayLength(R2.Item2);
					builder.ExtensionHeaders.Add(R2.Item1);
					builder.ExtensionDataPointers.Add(Loc);

					MetadataRecord img;
					try { img = R2.Item2["XTENSION"]; } catch (Exception ex) { throw new FITSFormatException("Extension headers do not follow standard.", ex); }
					if (img.AsString == "IMAGE   ")
					{
						int ImNr = numberGetter(exn, R2.Item2);
						builder.MEFImagesHeaders.Add(ImNr, R2.Item1);
						builder.MEFHeaderTable.Add(ImNr, R2.Item2);
						builder.MEFDataPointers.Add(ImNr, Loc);
					}

					AALength = (ALength + 2879) / 2880 * 2880;
					NewPosition = stream.Position + AALength;
					if (NewPosition > Length)
						throw new FITSFormatException("Data array longer than file length.");
					stream.Position = NewPosition;
				}
			}

			return builder;
		}
	}
}
