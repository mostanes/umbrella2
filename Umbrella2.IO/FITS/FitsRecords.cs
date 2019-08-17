using System;
using System.Collections.Generic;
using System.Text;

namespace Umbrella2.IO.FITS
{
	/// <summary>
	/// FITS Keyword Record. Raw form.
	/// </summary>
	struct KeywordRecord
	{
		public readonly string Name;
		public readonly string Data;
		public readonly bool HasEqual;

		public KeywordRecord(byte[] Raw)
		{
			Name = Encoding.UTF8.GetString(Raw, 0, 8);
			Data = Encoding.UTF8.GetString(Raw, 9, 71);
			HasEqual = Raw[8] == (byte) '=';
		}

		internal static MetadataRecord Elevate(KeywordRecord kwr)
		{
			MetadataRecord ev = new FITSMetadataRecord(kwr.Name.TrimEnd(' '), kwr.Data);
			return ev;
		}
	}

	public class FITSFormatException : Exception
	{
		public FITSFormatException() : base("Invalid FITS file")
		{ }

		public FITSFormatException(string message) : base(message)
		{ }

		public FITSFormatException(string message, Exception innerException) : base(message, innerException)
		{ }
	}

	public static class HeaderTableUtil
	{
		public static void CheckRecord(this Dictionary<string, MetadataRecord> Table, string Key)
		{ if (!Table.ContainsKey(Key)) throw new FormatException("FITS Image does not implement " + Key + " record."); }
	}
}
