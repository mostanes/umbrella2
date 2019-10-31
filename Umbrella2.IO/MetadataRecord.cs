using System;

namespace Umbrella2.IO
{
	/// <summary>
	/// Image metadata record. Image properties can be extracted from it.
	/// </summary>
	public abstract class MetadataRecord
	{
		public readonly string Name;
		public readonly string DataString;

		public MetadataRecord(string Name, string Data)
		{
			this.Name = Name;
			DataString = Data;
		}

		protected abstract long GetIntegerValue();

		/// <summary>Parses the value as a <see cref="long"/>.</summary>
		public virtual long Long
		{ get { return GetIntegerValue(); } }

		/// <summary>Parses the value as an <see cref="int"/>.</summary>
		public virtual int Int
		{ get { return (int)GetIntegerValue(); } }

		/// <summary>Parses the value as a <see cref="short"/>.</summary>
		public virtual short Short
		{ get { return (short)GetIntegerValue(); } }

		/// <summary>Parses the value as an <see cref="sbyte"/>.</summary>
		public virtual sbyte SByte
		{ get { return (sbyte)GetIntegerValue(); } }

		/// <summary>Parses the value as a <see cref="byte"/>.</summary>
		public virtual byte Byte
		{ get { return (byte)GetIntegerValue(); } }

		/// <summary>Parses the value as a <see cref="string"/> from the encoding of a FITS fixed string.</summary>
		public abstract string AsString
		{ get; }

		/// <summary>Parses the value as a <see cref="bool"/>.</summary>
		public abstract bool Bool
		{ get; }

		/// <summary>Parses the value as a <see cref="double"/>.</summary>
		public abstract double FloatingPoint
		{ get; }

		public override string ToString()
		{ return Name + ":" + DataString; }
	}
}
