using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;

namespace Umbrella2.IO.FITS
{
	/// <summary>
	/// A handle to a FITS File on the disk. Used to read/write data.
	/// </summary>
	public abstract class FitsFile
	{
		public readonly string Path;

		public readonly HeaderTable PrimaryTable;
		protected int PrimaryDataPointer;
		protected readonly List<int> ExtensionDataPointers;
		public readonly Dictionary<int, HeaderTable> MEFHeaderTable;
		protected readonly Dictionary<int, int> MEFDataPointers;

		protected readonly bool OutputFile;

		public delegate int MEFImageNumberGetter(int ExtensionNumber, HeaderTable Header);

		internal static int DefaultGetter(int ExtensionNumber, HeaderTable Header)
		{ if (Header.ContainsKey("IMAGEID")) return Header["IMAGEID"].Int - 1; else return ExtensionNumber; }

		protected FitsFile(string Path, bool OutputImage, MEFImageNumberGetter numberGetter, FitsFileBuilder Headers)
		{
			OutputFile = OutputImage;
			this.Path = Path;

			PrimaryTable = Headers.PrimaryTable;
			PrimaryDataPointer = Headers.PrimaryDataPointer;
			ExtensionDataPointers = Headers.ExtensionDataPointers;
			MEFHeaderTable = Headers.MEFHeaderTable;
			MEFDataPointers = Headers.MEFDataPointers;
		}

		/// <summary>
		/// Memory-maps an area in the file.
		/// </summary>
		/// <param name="Position">Position in the file where the view should start.</param>
		/// <param name="Length">Length of the mapped file view.</param>
		/// <returns>Pointer to the memory mapped view.</returns>
		internal abstract unsafe IntPtr GetView(int Position, int Length);

		/// <summary>
		/// Memory maps image data.
		/// </summary>
		/// <param name="Dataset">Image number.</param>
		/// <param name="DSetPosition">Position in the data array at which the view should start.</param>
		/// <param name="Length">Length of the area viewed.</param>
		/// <returns>Pointer to the memory mapped view of the data.</returns>
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

		/// <summary>
		/// Releases the memory mapped file view (and associated resources).
		/// </summary>
		/// <param name="View">Pointer to the memory mapped file view.</param>
		internal abstract void ReleaseView(IntPtr View);

		/// <summary>
		/// Releases the file handle. Writing may not occur after the release. Reading reopens the handle.
		/// </summary>
		internal abstract void ReleaseHandle();

		/// <summary>
		/// Disposes of all handles and large in-memory resources.
		/// </summary>
		public virtual void Close()
        {
			ReleaseHandle();
        }
	}
}
