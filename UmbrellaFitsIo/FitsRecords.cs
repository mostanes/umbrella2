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
	}

	/// <summary>
	/// FITS Keyword Record. Data can be extracted easier from it.
	/// </summary>
	public struct ElevatedRecord
	{
		public readonly string Name;
		public readonly string DataString;

		public ElevatedRecord(string Name, string Data)
		{
			this.Name = Name;
			DataString = Data;
		}

		internal static ElevatedRecord ToElevated(KeywordRecord kwr)
		{
			ElevatedRecord ev = new ElevatedRecord(kwr.Name.TrimEnd(' '), kwr.Data);
			return ev;
		}

		/// <summary>
		/// Formats an <see cref="ElevatedRecord"/> as an 80-byte field ready to be written to disk.
		/// </summary>
		/// <param name="record">Instance to be formatted.</param>
		/// <returns>A byte array containing the binary representation of the record.</returns>
		internal static byte[] ToRawRecord(ElevatedRecord record)
		{
			byte[] Record = Encoding.UTF8.GetBytes(new string(' ', 80));
			if (record.Name.Length > 8) throw new FITSFormatException("Keyword names must be at most 8 characters long.");
			if (record.DataString.Length > 71) throw new FITSFormatException("Keyword values must be at most 71 characters long.");
			Encoding.UTF8.GetBytes(record.Name, 0, record.Name.Length, Record, 0);
			Record[8] = (byte) '=';
			Encoding.UTF8.GetBytes(record.DataString, 0, record.DataString.Length, Record, 9);
			return Record;
		}

		internal byte[] ToRawRecord() => ToRawRecord(this);

		/// <summary>Reads the data string as a FITS value-typed string.</summary>
		/// <returns>The value encoded in the DataString.</returns>
		string GetValueTypedValue()
		{
			/* FIXME: Does not handle keywords with single quotes in character strings */
#warning FIXME
			if (DataString[0] != ' ') throw new FITSFormatException("Record is not value type.");
			int i, f;
			for (i = 0; i < DataString.Length && DataString[i] == ' '; i++) ;
			if (i == DataString.Length) throw new FITSFormatException("Field is empty");
			bool q = DataString[i] == '\'';
			f = i;
			char sc = q ? '\'' : ' ';
			if (q) { i++; }
			for (; i < DataString.Length && DataString[i] != sc; i++) ;
			if (q) i++;
			return DataString.Substring(f, i - f);
		}

		long GetIntegerValue()
		{
			string s = GetValueTypedValue();
			return long.Parse(s);
		}

		/// <summary>Parses the value as a <see cref="long"/>.</summary>
		public long Long
		{ get { return GetIntegerValue(); } }

		/// <summary>Parses the value as an <see cref="int"/>.</summary>
		public int Int
		{ get { return (int) GetIntegerValue(); } }

		/// <summary>Parses the value as a <see cref="short"/>.</summary>
		public short Short
		{ get { return (short) GetIntegerValue(); } }

		/// <summary>Parses the value as an <see cref="sbyte"/>.</summary>
		public sbyte SByte
		{ get { return (sbyte) GetIntegerValue(); } }

		/// <summary>Parses the value as a <see cref="byte"/>.</summary>
		public byte Byte
		{ get { return (byte) GetIntegerValue(); } }

		/// <summary>Parses the value as a <see cref="string"/> from the encoding of a FITS fixed string.</summary>
		public string GetFixedString
		{
			get
			{
				string s = GetValueTypedValue();
				if (s[0] != '\'' | s[s.Length - 1] != '\'')
					throw new FormatException("Not a fixed string");
				return s.Substring(1, s.Length - 2);
			}
		}

		/// <summary>Parses the value as a <see cref="bool"/>.</summary>
		public bool Bool
		{
			get
			{
				string s = GetValueTypedValue();
				if (s == "T") return true;
				if (s == "F") return false;
				throw new FormatException("Not a boolean value");
			}
		}

		/// <summary>Parses the value as a <see cref="double"/>.</summary>
		public double FloatingPoint
		{ get { string s = GetValueTypedValue(); return double.Parse(s); } }

		public override string ToString()
		{ return Name + ":" + DataString; }
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
		public static void CheckRecord(this Dictionary<string, ElevatedRecord> Table, string Key)
		{ if (!Table.ContainsKey(Key)) throw new FormatException("FITS Image does not implement " + Key + " record."); }
	}
}
