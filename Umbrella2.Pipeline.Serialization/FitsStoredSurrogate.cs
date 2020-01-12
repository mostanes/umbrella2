using System;
using System.Collections.Generic;
using Umbrella2.IO.FITS;

namespace Umbrella2.Pipeline.Serialization
{
	/// <summary>
	/// Reference to a FITS file designed to be stored to persistent media.
	/// </summary>
	public class FitsStoredSurrogate
	{
		/// <summary>Path to the FITS file.</summary>
		public string Path;
		/// <summary>Type of the reader to be used for the file.</summary>
		public string ReaderType;
		/// <summary>Number of the image in the FITS file.</summary>
		public int Number;
		/// <summary>If <c>true</c>, skip reading the WCS. Must be <c>true</c> for images without appropriate WCS headers.</summary>
		public bool SkipWCS;

		public static Dictionary<string, Func<string, FitsFile>> Readers;

		public FitsStoredSurrogate()
		{}

		/// <summary>Creates a reference from the given image.</summary>
		public FitsStoredSurrogate(FitsImage Img)
		{
			Path = Img.File.Path;
			Number = Img.ImageNumber;
			SkipWCS = Img.Transform == null;
			ReaderType = Img.File.GetType().FullName;
		}

		/// <summary>Attempts to retrieve the image from the reference.</summary>
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
