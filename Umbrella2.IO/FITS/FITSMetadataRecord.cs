using System;
using System.Text;

namespace Umbrella2.IO.FITS
{
	public class FITSMetadataRecord : MetadataRecord
	{
		public FITSMetadataRecord(string Name, string Data) : base(Name, Data)
		{ }

		/// <summary>
		/// Formats an <see cref="MetadataRecord"/> as an 80-byte field ready to be written to disk.
		/// </summary>
		/// <param name="record">Instance to be formatted.</param>
		/// <returns>A byte array containing the binary representation of the record.</returns>
		internal static byte[] ToRawRecord(MetadataRecord record)
		{
			byte[] Record = Encoding.UTF8.GetBytes(new string(' ', 80));
			if (record.Name.Length > 8) throw new FITSFormatException("Keyword names must be at most 8 characters long.");
			if (record.DataString.Length > 71) throw new FITSFormatException("Keyword values must be at most 71 characters long.");
			Encoding.UTF8.GetBytes(record.Name, 0, record.Name.Length, Record, 0);
			Record[8] = (byte)'=';
			Encoding.UTF8.GetBytes(record.DataString, 0, record.DataString.Length, Record, 9);
			return Record;
		}

		internal byte[] ToRawRecord() => ToRawRecord(this);

		/// <summary>Reads the data string as a FITS value-typed string.</summary>
		/// <returns>The value encoded in the DataString.</returns>
		string GetValueTypedValue()
		{
			if (DataString[0] != ' ') throw new FITSFormatException("Record is not value type.");
			int i, f;
			for (i = 0; i < DataString.Length && DataString[i] == ' '; i++) ;
			if (i == DataString.Length) throw new FITSFormatException("Field is empty");
			bool q = DataString[i] == '\'';
			f = i;
			if (q)
			{
				i++;
				if (DataString[i] == '\'') return string.Empty;
				i++;
				for (; i < DataString.Length; i++) if (DataString[i] == '\'' && DataString[i - 1] != '\'') break;
				return DataString.Substring(f, i - f + 1).Replace("''", "'");
			}
			else
			{
				for (; i < DataString.Length && DataString[i] != '/'; i++) ;
				return DataString.Substring(f, i - f).TrimEnd();
			}
		}

		protected override long GetIntegerValue()
		{
			string s = GetValueTypedValue();
			return long.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
		}

		/// <summary>Parses the value as a <see cref="string"/> from the encoding of a FITS fixed string.</summary>
		public override string AsString
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
		public override bool Bool
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
		public override double FloatingPoint
		{ get { string s = GetValueTypedValue(); return double.Parse(s, System.Globalization.CultureInfo.InvariantCulture); } }

	}
}