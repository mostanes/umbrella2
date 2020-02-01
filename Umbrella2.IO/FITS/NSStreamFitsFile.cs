using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Umbrella2.IO.FITS
{
	/// <summary>
	/// Represents a FITS file that was read from a non-seekable stream. The file is kept in-memory.
	/// </summary>
	public class NSStreamFitsFile : FitsFile
	{
		/// <summary>File data.</summary>
		byte[] Data;
		/// <summary>Number of open views.</summary>
		int CC;
		/// <summary>Handle for the in-memory pinned data.</summary>
		GCHandle Handle;

		/// <summary>Wrapper for underlying constructor.</summary>
		protected NSStreamFitsFile(byte[] Data, string Path, bool OutputImage, MEFImageNumberGetter numberGetter, FitsFileBuilder Headers) :
			base(Path, OutputImage, numberGetter, Headers)
		{
			this.Data = Data;
		}

		/// <summary>
		/// Opens a file from the given stream.
		/// </summary>
		/// <returns>The opened file.</returns>
		/// <param name="str">Input stream.</param>
		/// <param name="Length">Data length.</param>
		/// <param name="Path">Path to the data.</param>
		/// <param name="numberGetter">MEF naming policy.</param>
		public static NSStreamFitsFile OpenFile(Stream str, int Length, string Path, MEFImageNumberGetter numberGetter = null)
		{
			byte[] Data = new byte[Length];
			str.Read(Data, 0, Data.Length);

			FitsFileBuilder Headers;
			using (MemoryStream ms = new MemoryStream(Data))
				Headers = HeaderIO.ReadFileHeaders(ms, Length, numberGetter);

			return new NSStreamFitsFile(Data, Path, false, numberGetter, Headers);

		}

		/// <summary>Ensures the data is pinned in memory and returns a view.</summary>
		internal override IntPtr GetView(int Position, int Length)
		{
			GCHandle gch;
			lock (Data)
			{
				if (CC == 0)
					Handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
				gch = Handle;
				CC++;
			}
			IntPtr ptr = gch.AddrOfPinnedObject();
			return (ptr + Position);
		}

		/// <summary>Releases the view.</summary>
		internal override void ReleaseView(IntPtr View)
		{
			lock(Data)
			{
				CC--;
				if (CC == 0)
					Handle.Free();
			}
		}

		/// <summary>Clears in-memory data.</summary>
		public void Close()
		{ Data = null; }

		/// <summary>
		/// No-op, as no handles to release.
		/// </summary>
		internal override void ReleaseHandle() { }
	}
}
