using System;
using System.IO;
using Antmicro.Migrant;
using Antmicro.Migrant.Customization;

namespace UmbrellaDataInterchangeFormatter
{
	class UDIFWriter
	{
		Stream WriteStream;
		Serializer MasterSerializer;
		Serializer.OpenStreamSerializer Serializer;

		public UDIFWriter(Stream s)
		{
			WriteStream = s;
			MasterSerializer = new Serializer(new Settings(versionTolerance: VersionToleranceLevel.AllowAssemblyVersionChange));
			Serializer = MasterSerializer.ObtainOpenStreamSerializer(WriteStream);
		}

		public void SetSurrogate<T, U>(Func<T, U> SurrogateGenerator) where U : class
		{
			var w = MasterSerializer.ForObject<T>();
			w.SetSurrogate(SurrogateGenerator);
			Serializer = MasterSerializer.ObtainOpenStreamSerializer(WriteStream);
		}

		public void BeginData()
		{
			byte[] Header = new byte[8];
			System.Text.Encoding.UTF8.GetBytes("UDIFV2.1", 0, 4, Header, 0);
			WriteStream.Write(Header, 0, 8);
		}

		public void Write<T>(T o)
		{ Serializer.Serialize(o); }

		public void Flush()
		{ Serializer.Dispose(); Serializer = MasterSerializer.ObtainOpenStreamSerializer(WriteStream); }

	}

	class UDIFReader
	{
		Stream ReadStream;
		Serializer MasterSerializer;
		Serializer.OpenStreamDeserializer Deserializer;

		public UDIFReader(Stream s)
		{
			ReadStream = s;
			MasterSerializer = new Serializer(new Settings(versionTolerance: VersionToleranceLevel.AllowAssemblyVersionChange));
			Deserializer = MasterSerializer.ObtainOpenStreamDeserializer(ReadStream);
		}

		public void SetSurrogate<T, U>(Func<U, T> ObjectFromSurrogate) where T : class
		{
			var w = MasterSerializer.ForSurrogate<U>();
			w.SetObject(ObjectFromSurrogate);
			Deserializer = MasterSerializer.ObtainOpenStreamDeserializer(ReadStream);
		}

		public bool CheckHeader()
		{
			try
			{
				byte[] Header = new byte[8];
				ReadStream.Read(Header, 0, 8);
				string str = System.Text.Encoding.UTF8.GetString(Header);
				return str == "UDIFV2.1";
			}
			catch { return false; }
		}

		public T Read<T>() => Deserializer.Deserialize<T>();
	}
}
