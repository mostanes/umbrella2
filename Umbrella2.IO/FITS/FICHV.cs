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
		public uint Width;
		public uint Height;
		public IWCSProjection WCS;
		public HeaderTable Header;
		public int BitPix;
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
