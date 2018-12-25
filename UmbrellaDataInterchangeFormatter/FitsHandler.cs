using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.IO.FITS;

namespace UmbrellaDataInterchangeFormatter
{
	class FitsHandler
	{
		public enum SerializationType
		{
			ByPath,
			ByHash,
			ByData
		}

		public struct FitsSurrogate
		{
			string Path;
			byte[] Hash;
			FitsDataCapsule DataCapsule;
		}

		public class FitsDataCapsule
		{

		}

		public static FitsSurrogate SerializeFits(FitsImage Image, SerializationType Method)
		{
			throw new NotImplementedException();
		}
	}
}
