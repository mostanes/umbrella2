using System;
using System.Collections.Generic;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;

namespace Umbrella2.IO.FITS.KnownKeywords
{
	/// <summary>
	/// Handles scaling of image data according to SWarp headers.
	/// </summary>
	public class SWarpScaling : ImageProperties
	{
		/// <summary>If <code>true</code>, throw if SWarp headeres are not present in the image.
		/// If <code>false</code>, set scaling (<see cref="XScale"/>) to identity.</summary>
		public static bool ThrowSwarpHeaders = false;
		/// <summary>SWarp FLXSCALE parameter.</summary>
		public readonly double FlxScale;
		/// <summary>Background mean - SWarp BACKMEAN parameter.</summary>
		public readonly double BackMean;
		/// <summary>Background standard deviation - SWarp BACKSIG parameter.</summary>
		public readonly double BackSig;
		/* FIXME: I don't know what this constant was supposed to do and thus why its value is 60... */
#warning Unexplained code: Constant with unknown value.
		const double Rescale = 60;
		readonly double XScale;

		/// <summary>
		/// Switch for turning on/off SWarpScaling compensations.
		/// Implemented since some processing pipelines mess up the scaling.
		/// </summary>
		public static bool ApplyTransform;

		public SWarpScaling(Image File) : base(File)
		{
			HeaderTable ht = File.Header;
			if (ThrowSwarpHeaders)
			{
				ht.CheckThrowRecord("FLXSCALE");
				ht.CheckThrowRecord("BACKMEAN");
				ht.CheckThrowRecord("BACKSIG");
			}
			else
			{
				if (!ht.ContainsKey("FLXSCALE") || !ht.ContainsKey("BACKMEAN") || !ht.ContainsKey("BACKSIG"))
				{
					FlxScale = 0;
					BackMean = 0;
					BackSig = 0;
					XScale = 1;
					return;
				}
			}

			FlxScale = ht["FLXSCALE"].FloatingPoint;
			BackMean = ht["BACKMEAN"].FloatingPoint;
			BackSig = ht["BACKSIG"].FloatingPoint;
			XScale = FlxScale * Rescale;
		}

		public override List<MetadataRecord> GetRecords()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Scales image data according to SWarp headers.
		/// </summary>
		/// <param name="Input">Input image data.</param>
		public void ScaleData(double[,] Input)
		{
			if (ApplyTransform)
				for (int i = 0; i < Input.GetLength(0); i++) for (int j = 0; j < Input.GetLength(1); j++)
						Input[i, j] = (Input[i, j] - BackMean) * XScale;
		}
	}
}
