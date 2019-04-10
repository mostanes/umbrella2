using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Runtime.Serialization;
using Umbrella2.WCS;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.FITS.ElevatedRecord>;
using Umbrella2.IO.FITS.Formats;
using Umbrella2.Framework;

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
		/// <summary>
		/// Creates a new instance of the image properties for the given image.
		/// </summary>
		/// <param name="Image">The image for which the properties are extracted.</param>
		public ImageProperties(FitsImage Image)
		{ }

		/// <summary>
		/// Gets the list of FITS Header tags associated with the property.
		/// </summary>
		/// <returns>A list of FITS Header tags.</returns>
		public abstract List<ElevatedRecord> GetRecords();
	}

	/// <summary>
	/// Class representing a FITS image from a FITS file.
	/// </summary>
	public class FitsImage
	{
		/// <summary>
		/// Readers-Writers lock for portions of the image.
		/// </summary>
		readonly RWLockArea ImageLock;

		public readonly uint Width, Height;

		/// <summary>
		/// The number of the image in the FITS file.
		/// </summary>
		public readonly int ImageNumber;

		/// <summary>
		/// World Coordinate System Transformation.
		/// </summary>
		public readonly WCSViaProjection Transform;

		/// <summary>
		/// File containing the image.
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
		/// <summary>Sanity check for image Width and Height.</summary>
		const int MaxSize = 1000000;
		/// <summary>The number of bytes for each pixel. Abs(BITPIX)/8.</summary>
		readonly byte BytesPerPixel;

		/// <summary>True if Right ascension is AXIS1. False otherwise.</summary>
		protected readonly bool RAFirst;

		/// <summary>Common constructor code.</summary>
		protected FitsImage() { ImageLock = new RWLockArea(); PropertiesDictionary = new Dictionary<Type, ImageProperties>(); }

		/// <summary>
		/// Retrieves an image from a FITS file.
		/// </summary>
		/// <param name="File">Input file.</param>
		/// <param name="Number">Image number in multi-image (MEF) FITS files.</param>
		public FitsImage(FitsFile File, int Number = 0) : this()
		{
			ImageNumber = Number;
			if (Number == 0) Header = File.PrimaryTable;
			else Header = File.MEFHeaderTable[Number - 1];
			try
			{
				/* Parse image size */
				Width = (uint) Header["NAXIS1"].Int;
				Height = (uint) Header["NAXIS2"].Int;
				if (Width > MaxSize || Height > MaxSize) throw new FITSFormatException("Image too large for Umbrella2.");

				/* Parse axis types and projection algorithm */
				string Axis1 = Header["CTYPE1"].GetFixedString;
				string Axis2 = Header["CTYPE2"].GetFixedString;
				string Algorithm = Axis1.Substring(5, 3);
				string Nm1 = Axis1.Substring(0, 4);
				string Nm2 = Axis2.Substring(0, 4);

				/* Parses the order of the axes and checks consistency of projection algorithm between the axes. */
				if (Nm1.ToUpper() == "RA--" && Nm2.ToUpper() == "DEC-") RAFirst = true;
				else if (Nm1.ToUpper() == "DEC-" && Nm2.ToUpper() == "RA--") RAFirst = false;
				else throw new FITSFormatException("Cannot understand axis format");
				if (Axis2.Substring(5, 3) != Algorithm) throw new Exception("Projection Algorithm Mismatch.");
				/* Computes the linear transformation part of the WCS projection */
				double RA0 = (RAFirst ? Header["CRVAL1"] : Header["CRVAL2"]).FloatingPoint;
				double Dec0 = (RAFirst ? Header["CRVAL2"] : Header["CRVAL1"]).FloatingPoint;
				double X0 = Header["CRPIX1"].FloatingPoint;
				double Y0 = Header["CRPIX2"].FloatingPoint;
				WCSLinPart linpart;
				if (RAFirst) linpart = new WCSLinPart(Header["CD1_1"].FloatingPoint, Header["CD1_2"].FloatingPoint, Header["CD2_1"].FloatingPoint, Header["CD2_2"].FloatingPoint, X0, Y0);
				else linpart = new WCSLinPart(Header["CD2_1"].FloatingPoint, Header["CD2_2"].FloatingPoint, Header["CD1_1"].FloatingPoint, Header["CD1_2"].FloatingPoint, X0, Y0);


				if (Header["CUNIT1"].GetFixedString != "deg     " || Header["CUNIT2"].GetFixedString != "deg     ") throw new FITSFormatException("Wrong unit types for axes");

				/* Retrieves the projection algorithm */
				WCSProjectionTransform ipt;
				try { ipt = Umbrella2.WCS.Projections.WCSProjections.GetProjectionTransform(Algorithm, RA0 * Math.PI / 180, Dec0 * Math.PI / 180); }
				catch (KeyNotFoundException ex) { throw new FITSFormatException("Cannot understand projection algorithm", ex); }

				Transform = new WCSViaProjection(ipt, linpart);

				/* Computes BytesPerPixel and selects reading/writing functions */
				BytesPerPixel = (byte) Math.Abs((Header["BITPIX"].Int / 8));
				var RW = GetRW(Header["BITPIX"].Int);

				Reader = RW.Item1;
				Writer = RW.Item2;

				/* Loads SWarp scaling to the properties dictionary */
				try { GetProperty<KnownKeywords.SWarpScaling>(); }
				catch { }
			}
			catch (Exception ex) { throw new FITSFormatException("Cannot understand FITS file.", ex); }
			this.File = File;
		}

		/// <summary>
		/// Retrieves an image from a FITS file, with optional WCS headers.
		/// </summary>
		/// <param name="File">Input file.</param>
		/// <param name="Number">Image number in multi-image (MEF) FITS files.</param>
		/// <param name="SkipWCS">Force ignore image WCS headers.</param>
		public FitsImage(FitsFile File, bool SkipWCS, int Number = 0) : this()
		{
			ImageNumber = Number;
			if (Number == 0) Header = File.PrimaryTable;
			else Header = File.MEFHeaderTable[Number - 1];
			try
			{
				/* Parse image size */
				Width = (uint) Header["NAXIS1"].Int;
				Height = (uint) Header["NAXIS2"].Int;
				if (Width > MaxSize || Height > MaxSize) throw new FITSFormatException("Image too large for Umbrella2.");

				if (!SkipWCS)
				{
					try
					{
						/* Parse axis types and projection algorithm */
						string Axis1 = Header["CTYPE1"].GetFixedString;
						string Axis2 = Header["CTYPE2"].GetFixedString;
						string Algorithm = Axis1.Substring(5, 3);
						string Nm1 = Axis1.Substring(0, 4);
						string Nm2 = Axis2.Substring(0, 4);

						/* Parses the order of the axes and checks consistency of projection algorithm between the axes. */
						if (Nm1.ToUpper() == "RA--" && Nm2.ToUpper() == "DEC-") RAFirst = true;
						else if (Nm1.ToUpper() == "DEC-" && Nm2.ToUpper() == "RA--") RAFirst = false;
						else throw new FITSFormatException("Cannot understand axis format");
						if (Axis2.Substring(5, 3) != Algorithm) throw new Exception("Projection Algorithm Mismatch.");
						/* Computes the linear transformation part of the WCS projection */
						double RA0 = (RAFirst ? Header["CRVAL1"] : Header["CRVAL2"]).FloatingPoint;
						double Dec0 = (RAFirst ? Header["CRVAL2"] : Header["CRVAL1"]).FloatingPoint;
						double X0 = Header["CRPIX1"].FloatingPoint;
						double Y0 = Header["CRPIX2"].FloatingPoint;
						WCSLinPart linpart;
						if (RAFirst) linpart = new WCSLinPart(Header["CD1_1"].FloatingPoint, Header["CD1_2"].FloatingPoint, Header["CD2_1"].FloatingPoint, Header["CD2_2"].FloatingPoint, X0, Y0);
						else linpart = new WCSLinPart(Header["CD2_1"].FloatingPoint, Header["CD2_2"].FloatingPoint, Header["CD1_1"].FloatingPoint, Header["CD1_2"].FloatingPoint, X0, Y0);


						if (Header["CUNIT1"].GetFixedString != "deg     " || Header["CUNIT2"].GetFixedString != "deg     ") throw new FITSFormatException("Wrong unit types for axes");

						/* Retrieves the projection algorithm */
						WCSProjectionTransform ipt;
						try { ipt = Umbrella2.WCS.Projections.WCSProjections.GetProjectionTransform(Algorithm, RA0 * Math.PI / 180, Dec0 * Math.PI / 180); }
						catch (KeyNotFoundException ex) { throw new FITSFormatException("Cannot understand projection algorithm", ex); }

						Transform = new WCSViaProjection(ipt, linpart);
					}
					catch { }
				}
				
				/* Computes BytesPerPixel and selects reading/writing functions */
				BytesPerPixel = (byte) Math.Abs((Header["BITPIX"].Int / 8));
				var RW = GetRW(Header["BITPIX"].Int);

				Reader = RW.Item1;
				Writer = RW.Item2;

				/* Loads SWarp scaling to the properties dictionary */
				try { GetProperty<KnownKeywords.SWarpScaling>(); }
				catch { }
			}
			catch (Exception ex) { throw new FITSFormatException("Cannot understand FITS file.", ex); }
			this.File = File;
		}

		/// <summary>
		/// Creates a new FITS image.
		/// </summary>
		/// <param name="File">File backing the image.</param>
		/// <param name="Width">Image width.</param>
		/// <param name="Height">Image height.</param>
		/// <param name="Transform">WCS transformation.</param>
		/// <param name="BitPix">BITPIX value.</param>
		/// <param name="ExtraProperties">Extra image properties to write in the header.</param>
		/// <param name="ReverseAxis">Reverses the order of the axis in the header.</param>
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
			Header = GetHeader(BitPix);
			if (ExtraProperties != null)
				foreach (ImageProperties prop in ExtraProperties) foreach (ElevatedRecord er in prop.GetRecords()) Header.Add(er.Name, er);
			File.SetPrimaryHeaders(Header);
		}

		/// <summary>
		/// Fetches the image properties of given type for the image. Caches the instance.
		/// </summary>
		/// <typeparam name="T">Type of the image properties.</typeparam>
		/// <returns>The image properties instance associated with the image.</returns>
		public T GetProperty<T>() where T : ImageProperties
		{
			Type t = typeof(T);
			lock (PropertiesDictionary)
				if (!PropertiesDictionary.ContainsKey(t)) PropertiesDictionary.Add(t, (ImageProperties) Activator.CreateInstance(t, this));
			return (T) PropertiesDictionary[t];
		}

		/// <summary>
		/// Selects the reading/writing functions for a given BITPIX value.
		/// </summary>
		/// <param name="BitPix">BITPIX value.</param>
		/// <returns>Delegates to conversion functions.</returns>
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

		/// <summary>
		/// Checks whether the area of interest is within the boundaries of the image.
		/// </summary>
		/// <param name="Area">Area of interest.</param>
		void CheckMargins(Rectangle Area)
		{
			if (Area.Bottom > Height || Area.Right > Width) throw new ArgumentOutOfRangeException("Attempted reading outside of image bounds.");
			if (Area.X < 0 || Area.X > Width) throw new ArgumentOutOfRangeException("Attempted reading outside of image bounds.");
			if (Area.Y < 0 || Area.Y > Height) throw new ArgumentOutOfRangeException("Attempted reading outside of image bounds.");
		}

		/// <summary>
		/// Reads data from file.
		/// </summary>
		/// <param name="imData">Data container.</param>
		void ReadData(ImageData imData)
		{
			Rectangle rp = imData.Position;
			rp.Intersect(new Rectangle(0, 0, (int) Width - 1, (int) Height - 1));
			IntPtr Pointer;
			var ImPos = GetPositionInFile(rp);
			Pointer = File.GetDataView(ImageNumber - 1, ImPos.Item1, ImPos.Item2);
			Reader(Pointer, imData.Data, rp.Y - imData.Position.Y, rp.Bottom - imData.Position.Y, rp.X - imData.Position.X, rp.Right - imData.Position.X, (int) Width * BytesPerPixel);
			File.ReleaseView(Pointer);

			if (PropertiesDictionary.ContainsKey(typeof(KnownKeywords.SWarpScaling)))
				(PropertiesDictionary[typeof(KnownKeywords.SWarpScaling)] as KnownKeywords.SWarpScaling).ScaleData(imData.Data);
		}

		/// <summary>
		/// Writes data to the file.
		/// </summary>
		/// <param name="Data">Data to be written.</param>
		void WriteData(ImageData Data)
		{
			IntPtr Pointer;
			var ImPos = GetPositionInFile(Data.Position);
			if (ImageNumber == 0) Pointer = File.GetDataView(-1, ImPos.Item1, ImPos.Item2);
			else Pointer = File.GetDataView(ImageNumber, ImPos.Item1, ImPos.Item2);
			if (!Data.ReadOnly) { Writer(Pointer, Data.Data, (int) Width * BytesPerPixel); }
			File.ReleaseView(Pointer);
		}

		/// <summary>
		/// Locks and returns the data of an image. Can be used for reading and writing.
		/// </summary>
		/// <param name="Area">Area of interest in the image.</param>
		/// <param name="FillZero">True for padding out of image margins with zero. Must be false for write access.</param>
		/// <param name="RO">Whether the data is read-only.</param>
		/// <returns>An ImageData container.</returns>
		public ImageData LockData(Rectangle Area, bool FillZero, bool RO = true)
		{
			FillZero &= RO;
			if (!FillZero) CheckMargins(Area);

			Guid Token = ImageLock.EnterLock(Area, !RO);
			ImageData imData = new ImageData(Area, new double[Area.Height, Area.Width], this, RO, Token);
			ReadData(imData);
			return imData;
		}

		/// <summary>
		/// Replaces the data view with another at different coordinates, flushing any writable data.
		/// Same as ExitLock followed by LockData, however does not require a new data buffer allocation.
		/// </summary>
		/// <param name="Data">Previous data.</param>
		/// <param name="NewX">New X coordinate.</param>
		/// <param name="NewY">New Y coordinate.</param>
		/// <param name="FillZero">True for padding out of image margins with zero. Must be false for write access.</param>
		/// <param name="RO">Whether the data is read-only.</param>
		/// <returns>An ImageData container.</returns>
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

		/// <summary>
		/// Exits the lock on a region of image, flushing any writable data.
		/// </summary>
		/// <param name="Data">The data container.</param>
		public void ExitLock(ImageData Data)
		{
			if (!Data.ReadOnly)
				WriteData(Data);
			ImageLock.ExitLock(Data.FDGuid);
		}

		/// <summary>
		/// Returns the position of relevant image data in file.
		/// </summary>
		/// <param name="Location">Area of interest.</param>
		/// <returns>A tuple containing the pointer in file to the start of the data and its length.</returns>
		Tuple<int,int> GetPositionInFile(Rectangle Location)
		{
			int Start = (int) (((Location.Y * Width + Location.X) * BytesPerPixel));
			int Length = (int) (Location.Height * Width * BytesPerPixel);
			return new Tuple<int, int>(Start, Length);
		}

		/// <summary>
		/// Computes the headers for a new FITS image.
		/// </summary>
		/// <param name="Bitpix">Image BITPIX parameter.</param>
		/// <returns>A HeaderTable instance for the new FITS image.</returns>
		HeaderTable GetHeader(int Bitpix)
		{
			Dictionary<string, string> records = (Transform == null ? GetHeaderWithoutTransform(Bitpix) : GetHeaderWithTransform(Bitpix));
			HeaderTable het = records.ToDictionary((x) => x.Key, (x) => new ElevatedRecord(x.Key, x.Value));
			return het;
		}

		/// <summary>Computes the headers when the input image has WCS coordinates.</summary>
		Dictionary<string, string> GetHeaderWithTransform(int Bitpix)
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
				{"CD2_1", "  "+ (RAFirst?Matrix[2]:Matrix[0]).ToString("0.000000000000E+00") }, {"CD2_2", "  "+ (RAFirst?Matrix[3]:Matrix[1]).ToString("0.000000000000E+00") }
			};
			return records;
		}

		/// <summary>Computes the headers when the input image has no WCS information.</summary>
		Dictionary<string, string> GetHeaderWithoutTransform(int Bitpix)
		{
			Dictionary<string, string> records = new Dictionary<string, string>()
			{ {"SIMPLE", "   T" }, {"BITPIX", "   " + Bitpix.ToString()}, {"NAXIS"," 2"}, {"NAXIS1", "  " + Width.ToString()}, {"NAXIS2", "  " + Height.ToString()} };
			return records;
		}
	}

	/// <summary>
	/// Represents a serializable handle for a FitsImage, so that results of the pipeline can be saved to disk and recalled later with full access to information.
	/// </summary>
	[Serializable]
	public class FitsImageReference
	{
		/// <summary>
		/// Path to the FITS file holding the image.
		/// </summary>
		public string Path;
		/// <summary>
		/// Which image in the file it refers to.
		/// </summary>
		public int ImageNumber;
		/// <summary>
		/// A list of all images opened via reference, so that they do not collide.
		/// </summary>
		private static Dictionary<string, Dictionary<int, FitsImage>> ImageReferences = new Dictionary<string, Dictionary<int, FitsImage>>();
		/// <summary>
		/// A list of all FITS files opened via reference, so that they do not collide.
		/// </summary>
		private static Dictionary<string, FitsFile> FileReferences = new Dictionary<string, FitsFile>();

		/// <summary>
		/// Creates a reference from a path and an image number.
		/// </summary>
		/// <param name="Path">Path to the file.</param>
		/// <param name="ImageNumber">Image number.</param>
		public FitsImageReference(string Path, int ImageNumber)
		{
			this.Path = Path;
			this.ImageNumber = ImageNumber;
		}

		/// <summary>
		/// Acquires the FitsImage associated with the reference.
		/// </summary>
		/// <returns>The FitsImage associated to this reference.</returns>
		public FitsImage AcquireImage()
		{
			lock (ImageReferences)
			{
				if (!FileReferences.ContainsKey(Path))
				{ ImageReferences.Add(Path, new Dictionary<int, FitsImage>()); FileReferences.Add(Path, new FitsFile(Path, false)); }
				if (!ImageReferences[Path].ContainsKey(ImageNumber))
					ImageReferences[Path].Add(ImageNumber, new FitsImage(FileReferences[Path], ImageNumber));
				return ImageReferences[Path][ImageNumber];
			}
		}

		public static implicit operator FitsImage(FitsImageReference reference) => reference.AcquireImage();

		/// <summary>
		/// Creates a reference from an existing FitsImage.
		/// </summary>
		/// <param name="Image">Image to reference.</param>
		public FitsImageReference(FitsImage Image) : this(Image.File.Path, Image.ImageNumber)
		{
			lock (ImageReferences)
			{
				if (!FileReferences.ContainsKey(Path))
				{ ImageReferences.Add(Path, new Dictionary<int, FitsImage>()); FileReferences.Add(Path, Image.File); }
				if (!ImageReferences[Path].ContainsKey(ImageNumber))
					ImageReferences[Path].Add(ImageNumber, Image);
			}
		}
	}
}
