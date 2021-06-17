using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;


namespace Umbrella2.IO.FITS
{
	public class MMapFitsFile : FitsFile
	{
		MemoryMappedFile mmap;
		MemoryMappedFileAccess access;

		readonly Dictionary<IntPtr, MemoryMappedViewAccessor> OpenViews;

		/// <summary>
		/// Opens a FITS File handle from a file on a local disk.
		/// </summary>
		/// <param name="Path">Path to where the image is stored.</param>
		/// <param name="OutputImage">Specifies whether the image is an input or an output one.</param>
		/// <param name="numberGetter">Delegate that generates the image numbers in a MEF FITS.</param>
		private MMapFitsFile(string Path, bool OutputImage, MEFImageNumberGetter numberGetter, FitsFileBuilder Headers, MemoryMappedFile Handle, MemoryMappedFileAccess Access) :
			base(Path, OutputImage, numberGetter, Headers)
		{
			OpenViews = new Dictionary<IntPtr, MemoryMappedViewAccessor>();
			mmap = Handle;
			access = Access;
		}

		/// <summary>
		/// Open file for reading.
		/// </summary>
		/// <returns>The opened file.</returns>
		/// <param name="Path">Path to the file.</param>
		/// <param name="numberGetter">Delegate that generates the image numbers in a MEF FITS.</param>
		public static MMapFitsFile OpenReadFile(string Path, MEFImageNumberGetter numberGetter = null)
		{
			FileInfo info = new FileInfo(Path);
			MemoryMappedFile mmap = MemoryMappedFile.CreateFromFile(Path, FileMode.Open, Guid.NewGuid().ToString(), 0, MemoryMappedFileAccess.Read);

			Stream stream = mmap.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
			FitsFileBuilder builder = HeaderIO.ReadFileHeaders(stream, info.Length, numberGetter);
			stream.Dispose();

			return new MMapFitsFile(Path, false, numberGetter, builder, mmap, MemoryMappedFileAccess.Read);
		}

		/// <summary>
		/// Opens the file for writing.
		/// </summary>
		/// <returns>The opened file.</returns>
		/// <param name="Path">Path to the output file.</param>
		/// <param name="Headers">FITS Headers to be written to the file.</param>
		public static MMapFitsFile OpenWriteFile(string Path, HeaderTable Headers)
		{
			FileInfo info = new FileInfo(Path);

			int HLength = ((Headers.Count * 80) + 2879) / 2880 * 2880;
			int DLength = HeaderIO.ComputeDataArrayLength(Headers) + 2879;
			DLength = DLength / 2880 * 2880;
			int FLength = HLength + DLength;

			MemoryMappedFile mmap = MemoryMappedFile.CreateFromFile(Path, FileMode.Create, Guid.NewGuid().ToString(), FLength, MemoryMappedFileAccess.ReadWrite);
			Stream s = mmap.CreateViewStream();
			FitsFileBuilder builder = new FitsFileBuilder() { PrimaryTable = new HeaderTable() };
			foreach (var w in Headers) { builder.PrimaryTable.Add(w.Key, w.Value); s.Write(((FITSMetadataRecord)w.Value).ToRawRecord(), 0, 80); }
			s.Write(Encoding.UTF8.GetBytes("END".PadRight(80)), 0, 80);
			while (s.Position < HLength) s.Write(Encoding.UTF8.GetBytes(string.Empty.PadRight(80)), 0, 80);
			builder.PrimaryDataPointer = HLength;
			s.Dispose();

			return new MMapFitsFile(Path, true, null, builder, mmap, MemoryMappedFileAccess.ReadWrite);
		}

		/// <summary>
		/// Memory-maps an area in the file.
		/// </summary>
		/// <param name="Position">Position in the file where the view should start.</param>
		/// <param name="Length">Length of the mapped file view.</param>
		/// <returns>Pointer to the memory mapped view.</returns>
		internal override unsafe IntPtr GetView(int Position, int Length)
		{
			lock (OpenViews)
			{
				int MP = Position - Position % 65536; /* Working around weird Windows things... */
				lock (this)
					if (mmap == null) /* This handles the case where the handle has been released. */
					{
						mmap = MemoryMappedFile.CreateFromFile(Path, FileMode.Open, Guid.NewGuid().ToString(), 0, MemoryMappedFileAccess.Read);
						access = MemoryMappedFileAccess.Read;
					}
				MemoryMappedViewAccessor va = mmap.CreateViewAccessor(MP, Length + Position % 65536, access);
				byte* pr = (byte*)0;
				va.SafeMemoryMappedViewHandle.AcquirePointer(ref pr);
				pr += (Position % 65536); /* Working around weird Windows things... */
				IntPtr ptr = (IntPtr)pr;
				OpenViews.Add(ptr, va);
				return ptr;
			}
		}

		/// <summary>
		/// Releases the memory mapped file view (and associated resources).
		/// </summary>
		/// <param name="View">Pointer to the memory mapped file view.</param>
		internal override void ReleaseView(IntPtr View)
		{
			lock (OpenViews)
			{
				OpenViews[View].SafeMemoryMappedViewHandle.ReleasePointer();
				OpenViews[View].Flush();
				OpenViews[View].Dispose();
				OpenViews.Remove(View);
			}
		}

		/// <summary>
		/// Releases the file handle (memory mapping). Writing may not occur after the release. Reading reopens the handle.
		/// </summary>
		internal override void ReleaseHandle()
		{ lock (this) { mmap.Dispose(); mmap = null; } }
	}
}
