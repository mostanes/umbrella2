using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Umbrella2.IO.FITS
{
	public class NSStreamFitsFile : FitsFile
	{
		byte[] Data;
		int CC;
		GCHandle Handle;

		protected NSStreamFitsFile(byte[] Data, string Path, bool OutputImage, MEFImageNumberGetter numberGetter, FitsFileBuilder Headers) :
			base(Path, OutputImage, numberGetter, Headers)
		{
			this.Data = Data;
		}

		public static NSStreamFitsFile OpenFile(Stream str, int Length, string Path, MEFImageNumberGetter numberGetter = null)
		{
			byte[] Data = new byte[Length];
			str.Read(Data, 0, Data.Length);

			FitsFileBuilder Headers;
			using (MemoryStream ms = new MemoryStream(Data))
				Headers = HeaderIO.ReadFileHeaders(ms, Length, numberGetter);

			return new NSStreamFitsFile(Data, Path, false, numberGetter, Headers);

		}

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

		internal override void ReleaseView(IntPtr View)
		{
			lock(Data)
			{
				CC--;
				if (CC == 0)
					Handle.Free();
			}
		}

		public void Close()
		{
			Data = null;
		}
	}
}
