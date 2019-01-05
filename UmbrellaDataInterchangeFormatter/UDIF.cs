using System;
using System.IO;
using Antmicro.Migrant;
using Antmicro.Migrant.Customization;

namespace Umbrella2.DataInterchange.Formatter
{
	public class UDIFWriter
	{
		Stream WriteStream;
		Serializer MasterSerializer;
		Serializer.OpenStreamSerializer Serializer;

		public UDIFWriter(Stream s)
		{
			WriteStream = s;
			MasterSerializer = new Serializer(new Settings(versionTolerance: VersionToleranceLevel.AllowAssemblyVersionChange | VersionToleranceLevel.AllowFieldAddition | VersionToleranceLevel.AllowFieldRemoval | VersionToleranceLevel.AllowInheritanceChainChange));
		}

		public void SetSurrogate<T, U>(Func<T, U> SurrogateGenerator) where U : class
		{
			var w = MasterSerializer.ForObject<T>();
			w.SetSurrogate(SurrogateGenerator);
		}

		public void BeginData()
		{
			byte[] Header = new byte[8];
			System.Text.Encoding.UTF8.GetBytes("UDIFV2.1", 0, 8, Header, 0);
			WriteStream.Write(Header, 0, 8);
			Serializer = MasterSerializer.ObtainOpenStreamSerializer(WriteStream);
		}

		public void Write<T>(T o)
		{ Serializer.Serialize(o); }

		public void Flush()
		{ Serializer.Dispose(); Serializer = MasterSerializer.ObtainOpenStreamSerializer(WriteStream); }

	}

	public class UDIFReader
	{
		Stream ReadStream;
		Serializer MasterSerializer;
		Serializer.OpenStreamDeserializer Deserializer;

		public UDIFReader(Stream s)
		{
			ReadStream = s;
			MasterSerializer = new Serializer(new Settings(versionTolerance: VersionToleranceLevel.AllowAssemblyVersionChange | VersionToleranceLevel.AllowFieldAddition | VersionToleranceLevel.AllowFieldRemoval | VersionToleranceLevel.AllowInheritanceChainChange));
		}

		public void SetSurrogate<T, U>(Func<U, T> ObjectFromSurrogate) where T : class
		{
			var w = MasterSerializer.ForObject<U>();
			w.SetSurrogate(ObjectFromSurrogate);
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

		public void BeginRead()
		{
			if (CheckHeader())
				Deserializer = MasterSerializer.ObtainOpenStreamDeserializer(ReadStream);
			else throw new IOException("Wrong header type");
		}

		public T Read<T>() => Deserializer.Deserialize<T>();
	}
}
