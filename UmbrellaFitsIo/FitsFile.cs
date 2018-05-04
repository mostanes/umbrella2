using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.FITS.ElevatedRecord>;

namespace Umbrella2.IO.FITS
{
	/// <summary>
	/// A handle to a FITS File on the disk. Used to read/write data.
	/// </summary>
	public partial class FitsFile
	{
		MemoryMappedFile mmap;
		internal readonly string Path;

		public readonly List<ElevatedRecord> PrimaryHeader;
		public readonly HeaderTable PrimaryTable;
		public int PrimaryDataPointer;
		public readonly List<List<ElevatedRecord>> ExtensionHeaders;
		public readonly List<int> ExtensionDataPointers;
		public readonly Dictionary<int, List<ElevatedRecord>> MEFImagesHeaders;
		public readonly Dictionary<int, HeaderTable> MEFHeaderTable;
		public readonly Dictionary<int, int> MEFDataPointers;

		readonly Dictionary<IntPtr, MemoryMappedViewAccessor> OpenViews;
		readonly bool OutputFile;

		public delegate int MEFImageNumberGetter(int ExtensionNumber, HeaderTable Header);

		static int DefaultGetter(int ExtensionNumber, HeaderTable Header)
		{ if (Header.ContainsKey("IMAGEID")) return Header["IMAGEID"].Int - 1; else return ExtensionNumber; }

		private FitsFile()
		{
			OpenViews = new Dictionary<IntPtr, MemoryMappedViewAccessor>();
		}

		/// <summary>
		/// Opens a FITS File handle from a file on a local disk.
		/// </summary>
		/// <param name="Path"></param>
		public FitsFile(string Path, bool OutputImage, MEFImageNumberGetter numberGetter = null) : this()
		{
			OutputFile = OutputImage;
			this.Path = Path;
			if (!OutputImage)
			{
				/* Open file. We use mmaped files for IO. Use a stream to load metadata. */
				FileInfo info = new FileInfo(Path);
				mmap = MemoryMappedFile.CreateFromFile(Path, FileMode.Open, Guid.NewGuid().ToString(), 0, MemoryMappedFileAccess.ReadWrite);
				Stream stream = mmap.CreateViewStream();
				FitsFileBuilder builder = HeaderIO.ReadFileHeaders(stream, info.Length, numberGetter);
				PrimaryTable = builder.PrimaryTable;
				PrimaryHeader = builder.PrimaryHeader;
				PrimaryDataPointer = builder.PrimaryDataPointer;
				ExtensionHeaders = builder.ExtensionHeaders;
				ExtensionDataPointers = builder.ExtensionDataPointers;
				MEFImagesHeaders = builder.MEFImagesHeaders;
				MEFHeaderTable = builder.MEFHeaderTable;
				MEFDataPointers = builder.MEFDataPointers;
				stream.Dispose();
			}
			else
			{ 
				PrimaryTable = new HeaderTable();
				MEFHeaderTable = new Dictionary<int, HeaderTable>();
				MEFDataPointers = new Dictionary<int, int>();
			}
		}
		
		/// <summary>
		/// Sets the primary headers for an output file.
		/// </summary>
		public void SetPrimaryHeaders(HeaderTable Headers)
		{
			if (!OutputFile) throw new InvalidOperationException("Attempted writing to an input file.");

			int HLength = ((Headers.Count * 80) + 2879) / 2880 * 2880;
			int DLength = HeaderIO.ComputeDataArrayLength(Headers) + 2879;
			DLength = DLength / 2880 * 2880;
			int FLength = HLength + DLength;

			mmap = MemoryMappedFile.CreateFromFile(Path, FileMode.Create, Guid.NewGuid().ToString(), FLength, MemoryMappedFileAccess.ReadWrite);
			Stream s = mmap.CreateViewStream();
			foreach (var w in Headers) { PrimaryTable.Add(w.Key, w.Value); s.Write(w.Value.ToRawRecord(), 0, 80); }
			s.Write(Encoding.UTF8.GetBytes("END".PadRight(80)), 0, 80);
			PrimaryDataPointer = HLength;
			s.Dispose();
		}

		internal unsafe IntPtr GetView(int Position, int Length)
		{
			lock (OpenViews)
			{
				int MP = Position - Position % 65536;
				MemoryMappedViewAccessor va = mmap.CreateViewAccessor(MP, Length + Position % 65536);
				byte* pr = (byte*) 0;
				va.SafeMemoryMappedViewHandle.AcquirePointer(ref pr);
				pr += (Position % 65536); /* Working around weird Windows things... */
				IntPtr ptr = (IntPtr) pr;
				OpenViews.Add(ptr, va);
				return ptr;
			}
		}

		internal IntPtr GetDataView(int Dataset, int DSetPosition, int Length)
		{
			if (OutputFile)
			{
				if (Dataset == -1)
				{
					if (PrimaryTable.Count == 0) throw new InvalidOperationException("Cannot access the data array when the headers are uninitialized.");
				}
				else
				{
					if (!MEFHeaderTable.ContainsKey(Dataset)) throw new InvalidOperationException("Cannot find the headers for Image " + Dataset.ToString());
					if (MEFHeaderTable[Dataset].Count == 0) throw new InvalidOperationException("Cannot access the data array when the headers are uninitialized.");
				}
			}

			int FilePosition = (Dataset == -1 ? PrimaryDataPointer : MEFDataPointers[Dataset]) + DSetPosition;
			return GetView(FilePosition, Length);
		}

		internal void ReleaseView(IntPtr View)
		{
			lock (OpenViews)
			{
				OpenViews[View].SafeMemoryMappedViewHandle.ReleasePointer();
				OpenViews[View].Flush();
				OpenViews[View].Dispose();
				OpenViews.Remove(View);
			}
		}
	}
}
