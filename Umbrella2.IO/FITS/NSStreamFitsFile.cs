using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Umbrella2.IO.FITS
{
	public class NSStreamFitsFile : FitsFile
	{
		byte[] Data;
		Dictionary<IntPtr, GCHandle> OpenHandles = new Dictionary<IntPtr, GCHandle>();

		protected NSStreamFitsFile(Stream str, string Path, bool OutputImage, MEFImageNumberGetter numberGetter, FitsFileBuilder Headers) :
			base(Path, OutputImage, numberGetter, Headers)
		{
			Data = new byte[str.Length];
			str.Read(Data, 0, (int)str.Length);
		}

		internal override IntPtr GetView(int Position, int Length)
		{
			GCHandle gch = GCHandle.Alloc(Data, GCHandleType.Pinned);
			IntPtr ptr = gch.AddrOfPinnedObject();
			OpenHandles.Add(ptr, gch);
			return ptr;
		}

		internal override void ReleaseView(IntPtr View)
		{
			if (!OpenHandles.ContainsKey(View))
				throw new ArgumentException("Not an open view", nameof(View));
			GCHandle gch = OpenHandles[View];
			gch.Free();
			OpenHandles.Remove(View);
		}
	}
}
