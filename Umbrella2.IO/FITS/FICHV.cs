using System;
using System.Collections.Generic;
using Umbrella2.WCS;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;

namespace Umbrella2.IO.FITS
{
	/// <summary>
	/// FITS Image Core Header Values. A wrapper for the core data in FITS Images' header data.
	/// </summary>
	public struct FICHV
	{
		/// <summary>Image width.</summary>
		public uint Width;
		/// <summary>Image height.</summary>
		public uint Height;
		/// <summary>Image WCS.</summary>
		public IWCSProjection WCS;
		/// <summary>Header table.</summary>
		public HeaderTable Header;
		/// <summary>BITPIX value.</summary>
		public int BitPix;

		/// <summary>
		/// Creates a shallow clone of the FICHV, except for the header table, which is regenerated.
		/// </summary>
		/// <param name="Original">Header to clone.</param>
		public FICHV CloneCore(FICHV Original) 
		{
			FICHV f = new FICHV() { BitPix = Original.BitPix, Height = Original.Height, Width = Original.Width, WCS = Original.WCS };
			f.Header = FitsBuilder.GetHeader(f);
			return f;
		}

		/// <summary>
		/// Changes the BITPIX entry of this header.
		/// </summary>
		/// <returns>This instance.</returns>
		/// <param name="BitPix">The new value for <see cref="BitPix"/>.</param>
		public FICHV ChangeBitPix(int BitPix)
		{
			this.BitPix = BitPix;
			Header = FitsBuilder.GetHeader(this);
			return this;
		}
	}

	public class FitsFileBuilder
	{
		internal List<MetadataRecord> PrimaryHeader;
		internal HeaderTable PrimaryTable;
		internal int PrimaryDataPointer;
		internal List<List<MetadataRecord>> ExtensionHeaders;
		internal List<int> ExtensionDataPointers;
		internal Dictionary<int, List<MetadataRecord>> MEFImagesHeaders;
		internal Dictionary<int, HeaderTable> MEFHeaderTable;
		internal Dictionary<int, int> MEFDataPointers;
	}
}
