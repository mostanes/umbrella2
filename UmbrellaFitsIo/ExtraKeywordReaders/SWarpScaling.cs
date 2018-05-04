using System;
using System.Collections.Generic;
using System.Linq;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.FITS.ElevatedRecord>;

namespace Umbrella2.IO.FITS.KnownKeywords
{
	public class SWarpScaling : ImageProperties
	{
		public readonly double FlxScale;
		public readonly double BackMean;
		public readonly double BackSig;
		const double Rescale = 60;
		readonly double XScale;

		public SWarpScaling(FitsImage File) : base(File)
		{
			HeaderTable ht = File.Header;
			if (!ht.ContainsKey("FLXSCALE")) throw new FormatException("FITS image does not implement FLXSCALE header");
			if (!ht.ContainsKey("BACKMEAN")) throw new FormatException("FITS image does not implement BACKMEAN header");
			if (!ht.ContainsKey("BACKSIG")) throw new FormatException("FITS image does not implement BACKSIG header");
			FlxScale = ht["FLXSCALE"].FloatingPoint;
			BackMean = ht["BACKMEAN"].FloatingPoint;
			BackSig = ht["BACKSIG"].FloatingPoint;
			XScale = FlxScale * Rescale;
		}

		public override List<ElevatedRecord> GetRecords()
		{
			throw new NotImplementedException();
		}

		public void ScaleData(double[,] Input)
		{
			for (int i = 0; i < Input.GetLength(0); i++) for (int j = 0; j < Input.GetLength(1); j++)
					Input[i, j] = (Input[i, j] - BackMean) * XScale;
		}
	}
}
