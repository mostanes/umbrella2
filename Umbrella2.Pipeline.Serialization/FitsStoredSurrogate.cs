using System;
using System.Collections.Generic;
using Umbrella2.IO.FITS;

namespace Umbrella2.Pipeline.Serialization
{
	public class FitsStoredSurrogate
	{
		public string Path;
		public string ReaderType;
		public int Number;
		public bool SkipWCS;

		public static Dictionary<string, Func<string, FitsFile>> Readers;

		public FitsStoredSurrogate()
		{}

		public FitsStoredSurrogate(FitsImage Img)
		{
			Path = Img.File.Path;
			Number = Img.ImageNumber;
			SkipWCS = Img.Transform == null;
			ReaderType = Img.File.GetType().FullName;
		}

		public bool TryGetImage(out FitsImage Image)
		{
			if (!Readers.ContainsKey(ReaderType)) { Image = null;  return false; }
			try
			{
				FitsFile ff = Readers[ReaderType](Path);
				Image = new FitsImage(ff, Number, SkipWCS);
				return true;
			}
			catch { Image = null; return false; }
		}
	}
}
