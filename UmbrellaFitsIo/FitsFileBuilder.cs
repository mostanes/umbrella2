using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.FITS.ElevatedRecord>;

namespace Umbrella2.IO.FITS
{
	public partial class FitsFile
	{
		class FitsFileBuilder
		{
			internal List<ElevatedRecord> PrimaryHeader;
			internal HeaderTable PrimaryTable;
			internal int PrimaryDataPointer;
			internal List<List<ElevatedRecord>> ExtensionHeaders;
			internal List<int> ExtensionDataPointers;
			internal Dictionary<int, List<ElevatedRecord>> MEFImagesHeaders;
			internal Dictionary<int, HeaderTable> MEFHeaderTable;
			internal Dictionary<int, int> MEFDataPointers;
		}
	}
}
