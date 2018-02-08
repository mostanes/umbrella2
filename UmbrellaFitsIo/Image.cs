using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Runtime.Serialization;
using UmbrellaFitsIo.FrameworkSupport;
using Umbrella2.WCS;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.FITS.ElevatedRecord>;
using Umbrella2.IO.FITS.Formats;
using UmbrellaFitsIo.DataFormatTranslations;

namespace Umbrella2.IO.FITS
{
	/// <summary>
	/// Image data from a FITS File.
	/// The data is in the form [y, x].
	/// </summary>
	[Serializable]
	public struct ImageData
	{
		readonly public Rectangle Position;
		public double[,] Data;
		readonly public FitsImage Parent;
		readonly public bool ReadOnly;
		readonly internal Guid FDGuid;

		public ImageData(Rectangle Location, double[,] ImageData, FitsImage Image, bool Readonly, Guid UID)
		{
			Position = Location;
			Data = ImageData;
			Parent = Image;
			ReadOnly = Readonly;
			FDGuid = UID;
		}
	}

	/// <summary>
	/// Represents a set of image properties that can be parsed from the header.
	/// </summary>
	public abstract class ImageProperties
	{
		public ImageProperties(FitsFile File)
		{ }

		public abstract List<ElevatedRecord> GetRecords();
	}


	public class FitsImage
	{
		readonly RWLockArea ImageLock;
		public readonly uint Width, Height;

		public readonly int ImageNumber;

		/// <summary>
		/// World Coordinate System Transformation.
		/// </summary>
		public readonly WCSViaProjection Transform;

		/// <summary>
		/// Containing file.
		/// </summary>
		public readonly FitsFile File;
		
		/// <summary>
		/// FITS Image Headers.
		/// </summary>
		public readonly HeaderTable Header;

		/// <summary>
		/// Extra Image Properties.
		/// </summary>
		readonly Dictionary<Type, ImageProperties> PropertiesDictionary;
		
		/// <summary>Method to read/parse memory to double[,].</summary>
		readonly DataReader Reader;
		/// <summary>Method to write/serialize from double[,] to memory.</summary>
		readonly DataWriter Writer;
		// <summary>Caches the image size.</summary>
		//readonly Rectangle SelfSize;
		/// <summary>Sanity check for image Width and Height.</summary>
		const int MaxSize = 1000000;
		readonly byte BytesPerPixel;

		public readonly bool RAFirst;

		protected FitsImage() { ImageLock = new RWLockArea(); PropertiesDictionary = new Dictionary<Type, ImageProperties>(); }

		public FitsImage(FitsFile File, int Number = 0) : this()
		{
			ImageNumber = Number;
			if (Number == 0) Header = File.PrimaryTable;
			else Header = File.MEFHeaderTable[Number - 1];
			try
			{
				Width = (uint) Header["NAXIS1"].Int;
				Height = (uint) Header["NAXIS2"].Int;
				if (Width > MaxSize || Height > MaxSize) throw new FITSFormatException("Image too large for Umbrella2.");

				string Axis1 = Header["CTYPE1"].GetFixedString;
				string Axis2 = Header["CTYPE2"].GetFixedString;
				string Algorithm = Axis1.Substring(5, 3);
				string Nm1 = Axis1.Substring(0, 4);
				string Nm2 = Axis2.Substring(0, 4);

				if (Nm1.ToUpper() == "RA--" && Nm2.ToUpper() == "DEC-") RAFirst = true;
				else if (Nm1.ToUpper() == "DEC-" && Nm2.ToUpper() == "RA--") RAFirst = false;
				else throw new FITSFormatException("Cannot understand axis format");
				if (Axis2.Substring(5, 3) != Algorithm) throw new Exception("Projection Algorithm Mismatch.");
				double RA0 = (RAFirst ? Header["CRVAL1"] : Header["CRVAL2"]).FloatingPoint;
				double Dec0 = (RAFirst ? Header["CRVAL2"] : Header["CRVAL1"]).FloatingPoint;
				double X0 = Header["CRPIX1"].FloatingPoint;
				double Y0 = Header["CRPIX2"].FloatingPoint;
				WCSLinPart linpart;
				if (RAFirst) linpart = new WCSLinPart(Header["CD1_1"].FloatingPoint, Header["CD1_2"].FloatingPoint, Header["CD2_1"].FloatingPoint, Header["CD2_2"].FloatingPoint, X0, Y0);
				else linpart = new WCSLinPart(Header["CD2_1"].FloatingPoint, Header["CD2_2"].FloatingPoint, Header["CD1_1"].FloatingPoint, Header["CD1_2"].FloatingPoint, X0, Y0);
				

				if (Header["CUNIT1"].GetFixedString != "deg     " || Header["CUNIT2"].GetFixedString != "deg     ") throw new FITSFormatException("Wrong unit types for axes");

				WCSProjectionTransform ipt;
				try { ipt = Umbrella2.WCS.Projections.WCSProjections.GetProjectionTransform(Algorithm, RA0 * Math.PI / 180, Dec0 * Math.PI / 180); }
				catch (KeyNotFoundException ex) { throw new FITSFormatException("Cannot understand projection algorithm", ex); }

				WCSViaProjection wvp = new WCSViaProjection(ipt, linpart);
				Transform = wvp;

				BytesPerPixel = (byte) Math.Abs((Header["BITPIX"].Int / 8));
				var RW = GetRW(Header["BITPIX"].Int);
				
				Reader = RW.Item1;
				Writer = RW.Item2;
			}
			catch (Exception ex) { throw new FITSFormatException("Cannot understand FITS file.", ex); }
			this.File = File;
		}

		public FitsImage(FitsFile File, uint Width, uint Height, WCSViaProjection Transform, int BitPix, List<ImageProperties> ExtraProperties = null, bool ReverseAxis = false) : this()
		{
			if (Width > MaxSize || Height > MaxSize) throw new FITSFormatException("Image too large for Umbrella2.");
			this.File = File;
			this.Width = Width;
			this.Height = Height;
			this.Transform = Transform;
			BytesPerPixel = (byte) Math.Abs(BitPix / 8);
			var RW = GetRW(BitPix);
			Reader = RW.Item1;
			Writer = RW.Item2;
			RAFirst = !ReverseAxis;
			HeaderTable table = GetHeader(BitPix);
			if (ExtraProperties != null)
				foreach (ImageProperties prop in ExtraProperties) foreach (ElevatedRecord er in prop.GetRecords()) table.Add(er.Name, er);
			File.SetPrimaryHeaders(table);
		}

		public T GetProperty<T>() where T : ImageProperties
		{
			Type t = typeof(T);
			if (!PropertiesDictionary.ContainsKey(t)) PropertiesDictionary.Add(t, (ImageProperties) Activator.CreateInstance(t, this));
			return (T) PropertiesDictionary[t];
		}

		static Tuple<DataReader, DataWriter> GetRW(int BitPix)
		{
			switch (BitPix)
			{
				case 8:
					return new Tuple<DataReader, DataWriter>(IntegerDataset.Read8, IntegerDataset.Write8);
				case 16:
					return new Tuple<DataReader, DataWriter>(IntegerDataset.Read16, IntegerDataset.Write16);
				case 32:
					return new Tuple<DataReader, DataWriter>(IntegerDataset.Read32, IntegerDataset.Write32);
				case 64:
					return new Tuple<DataReader, DataWriter>(IntegerDataset.Read64, IntegerDataset.Write64);
				case -32:
					return new Tuple<DataReader, DataWriter>(FPDataset.Read32, FPDataset.Write32);
				case -64:
					return new Tuple<DataReader, DataWriter>(FPDataset.Read64, FPDataset.Write64);
			}
			throw new FITSFormatException("BITPIX field not conforming to FITS standard");
		}

		void CheckMargins(Rectangle Area)
		{
			if (Area.Bottom > Height || Area.Right > Width) throw new ArgumentOutOfRangeException("Attempted reading outside of image bounds.");
			if (Area.X < 0 || Area.X > Width) throw new ArgumentOutOfRangeException("Attempted reading outside of image bounds.");
			if (Area.Y < 0 || Area.Y > Height) throw new ArgumentOutOfRangeException("Attempted reading outside of image bounds.");
		}

		void ReadData(ImageData imData)
		{
			Rectangle rp = imData.Position;
			rp.Intersect(new Rectangle(0, 0, (int) Width - 1, (int) Height - 1));
			//if (imData.Position.Width != rp.Width) throw new NotSupportedException("Cannot fill with 0 on X axis.");
			IntPtr Pointer;
			var ImPos = GetPositionInFile(rp);
			if (ImageNumber == 0) Pointer = File.GetDataView(-1, ImPos.Item1, ImPos.Item2);
			else Pointer = File.GetDataView(ImageNumber, ImPos.Item1, ImPos.Item2);
			Reader(Pointer, imData.Data, rp.Y - imData.Position.Y, rp.Bottom - imData.Position.Y, rp.X - imData.Position.X, rp.Right - imData.Position.X, (int) Width * BytesPerPixel);
			File.ReleaseView(Pointer);
			/*
			if (rp.Y == imData.Position.Y)
				for (int i = rp.Height; i < imData.Position.Height; i++) for (int j = 0; j < rp.Width; j++) imData.Data[i, j] = 0;
			else
				for (int i = 0; i < imData.Position.Height - rp.Height; i++) for (int j = 0; j < rp.Width; j++) imData.Data[i, j] = 0;
			*/
		}

		void WriteData(ImageData Data)
		{
			IntPtr Pointer;
			var ImPos = GetPositionInFile(Data.Position);
			if (ImageNumber == 0) Pointer = File.GetDataView(-1, ImPos.Item1, ImPos.Item2);
			else Pointer = File.GetDataView(ImageNumber, ImPos.Item1, ImPos.Item2);
			if (!Data.ReadOnly) { Writer(Pointer, Data.Data, (int) Width * BytesPerPixel); }
			File.ReleaseView(Pointer);
		}

		public ImageData LockData(Rectangle Area, bool FillZero, bool RO = true)
		{
			FillZero &= RO;
			if (!FillZero) CheckMargins(Area);

			Guid Token = ImageLock.EnterLock(Area, !RO);
			ImageData imData = new ImageData(Area, new double[Area.Height, Area.Width], this, RO, Token);
			ReadData(imData);
			return imData;
		}

		public ImageData SwitchLockData(ImageData Data, int NewX, int NewY, bool FillZero, bool RO = true)
		{
			Rectangle Area = new Rectangle(NewX, NewY, Data.Position.Width, Data.Position.Height);
			FillZero &= RO;
			if (!FillZero) CheckMargins(Area);

			if (!Data.ReadOnly) WriteData(Data);

			ImageLock.ExitLock(Data.FDGuid);
			Guid Token = ImageLock.EnterLock(Area, !RO);
			ImageData imData = new ImageData(Area, Data.Data, this, RO, Token);
			ReadData(imData);
			return imData;
		}

		public void ExitLock(ImageData Data)
		{
			if (!Data.ReadOnly)
				WriteData(Data);
			ImageLock.ExitLock(Data.FDGuid);
		}

		Tuple<int,int> GetPositionInFile(Rectangle Location)
		{
			int Start = (int) (((Location.Y * Width + Location.X) * BytesPerPixel));
			int Length = (int) (Location.Height * Width * BytesPerPixel);
			return new Tuple<int, int>(Start, Length);
		}

		HeaderTable GetHeader(int Bitpix)
		{
			string AlgName = Transform.ProjectionTransform.Name;
			Transform.ProjectionTransform.GetReferencePoints(out double RA, out double Dec);
			string T1 = " '" + (RAFirst ? "RA---" : "DEC--") + AlgName + "'";
			string T2 = " '" + (RAFirst ? "DEC--" : "RA---") + AlgName + "'";
			string V1 = "  " + ((RAFirst ? RA : Dec) * 180 / Math.PI).ToString("0.000000000000E+00");
			string V2 = "  " + ((RAFirst ? Dec : RA) * 180 / Math.PI).ToString("0.000000000000E+00");
			double[] Matrix = Transform.LinearTransform.Matrix;
			Dictionary<string, string> records = new Dictionary<string, string>()
			{
				{"SIMPLE", "   T" }, {"BITPIX", "   " + Bitpix.ToString()}, {"NAXIS"," 2"}, {"NAXIS1", "  " + Width.ToString()}, {"NAXIS2", "  " + Height.ToString()},
				{"CTYPE1", T1 }, {"CTYPE2", T2 }, { "CUNIT1", " 'deg     '"}, {"CUNIT2", " 'deg     '"}, {"CRVAL1", V1 }, {"CRVAL2", V2 },
				{"CRPIX1", "  "+ (RAFirst?Matrix[4]:Matrix[5]).ToString("0.000000000000E+00") }, {"CRPIX2", "  " +(RAFirst?Matrix[5]:Matrix[4]).ToString("0.000000000000E+00") },
				{"CD1_1", "  "+ (RAFirst?Matrix[0]:Matrix[2]).ToString("0.000000000000E+00") }, {"CD1_2", "  "+ (RAFirst?Matrix[1]:Matrix[3]).ToString("0.000000000000E+00") },
				{"CD2_1", "  "+ (RAFirst?Matrix[2]:Matrix[0]).ToString("0.000000000000E+00") }, {"CD2_2", "  "+ (RAFirst?Matrix[3]:Matrix[1]).ToString("0.000000000000E+00") },
				{"END", "" }
			};
			HeaderTable het = records.ToDictionary((x) => x.Key, (x) => new ElevatedRecord(x.Key, x.Value));
			return het;
		}
	}
}
