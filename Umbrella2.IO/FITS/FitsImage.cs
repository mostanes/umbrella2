using System;
using System.Collections.Generic;
using System.Drawing;
using Umbrella2.Framework;
using Umbrella2.IO.FITS.Formats;
using Umbrella2.WCS;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;

namespace Umbrella2.IO.FITS
{

	/// <summary>
	/// Class representing a FITS image from a FITS file.
	/// </summary>
	public class FitsImage : Image
	{
		/// <summary>
		/// Readers-Writers lock for portions of the image.
		/// </summary>
		readonly RWLockArea ImageLock;

		/// <summary>
		/// File containing the image.
		/// </summary>
		public readonly FitsFile File;

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

		/// <summary>Pass-through constructor to Image.</summary>
		protected FitsImage(int Number, IWCSProjection Projection, HeaderTable Header, uint Width, uint Height) :
			base(Number, Projection, Header, Width, Height)
		{ }

		/// <summary>Expansion of FICHV to pass-through constructor.</summary>
		protected FitsImage(int Number, FICHV data) : base(Number, data.WCS, data.Header, data.Width, data.Height)
		{ ImageLock = new RWLockArea(); }

		/// <summary>
		/// Retrieves an image from a FITS file.
		/// </summary>
		/// <param name="File">Input file.</param>
		/// <param name="Number">Image number in multi-image (MEF) FITS files.</param>
		/// <param name="SkipWCS">Whether to parse the WCS headers.</param>
		public FitsImage(FitsFile File, int Number = 0, bool SkipWCS = false) :
			this(Number, ParseHeaderTable(Number == 0 ? File.PrimaryTable : File.MEFHeaderTable[Number - 1], SkipWCS))
		{
			try
			{
				/* Computes BytesPerPixel and selects reading/writing functions */
				BytesPerPixel = (byte)Math.Abs((Header["BITPIX"].Int / 8));
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
			rp.Intersect(new Rectangle(0, 0, (int)Width - 1, (int)Height - 1));
			IntPtr Pointer;
			var ImPos = GetPositionInFile(rp);
			Pointer = File.GetDataView(ImageNumber - 1, ImPos.Item1, ImPos.Item2);
			Reader(Pointer, imData.Data, rp.Y - imData.Position.Y, rp.Bottom - imData.Position.Y, rp.X - imData.Position.X, rp.Right - imData.Position.X, (int)Width * BytesPerPixel);
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
			if (!Data.ReadOnly) { Writer(Pointer, Data.Data, (int)Width * BytesPerPixel); }
			File.ReleaseView(Pointer);
		}

		/// <summary>
		/// Locks and returns the data of an image. Can be used for reading and writing.
		/// </summary>
		/// <param name="Area">Area of interest in the image.</param>
		/// <param name="FillZero">True for padding out of image margins with zero. Must be false for write access.</param>
		/// <param name="RO">Whether the data is read-only.</param>
		/// <returns>An ImageData container.</returns>
		public override ImageData LockData(Rectangle Area, bool FillZero, bool RO = true)
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
		public override ImageData SwitchLockData(ImageData Data, int NewX, int NewY, bool FillZero, bool RO = true)
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
		public override void ExitLock(ImageData Data)
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
		Tuple<int, int> GetPositionInFile(Rectangle Location)
		{
			int Start = (int)(((Location.Y * Width + Location.X) * BytesPerPixel));
			int Length = (int)(Location.Height * Width * BytesPerPixel);
			return new Tuple<int, int>(Start, Length);
		}

		/// <summary>
		/// Parses a <see cref="FICHV"/> out of the raw header table.
		/// </summary>
		/// <returns>The <see cref="FICHV"/> header table.</returns>
		/// <param name="Header">Image's raw header.</param>
		/// <param name="SkipWCS">If set to <c>true</c>, skip reading the WCS.</param>
		public static FICHV ParseHeaderTable(HeaderTable Header, bool SkipWCS)
		{
			FICHV data = new FICHV();
			data.Header = Header;
			try
			{
				/* Parse image size */
				data.Width = (uint)Header["NAXIS1"].Int;
				data.Height = (uint)Header["NAXIS2"].Int;
				if (data.Width > MaxSize || data.Height > MaxSize) throw new FITSFormatException("Image too large for Umbrella2.");

				data.WCS = SkipWCS ? null : ParseWCS(Header);

				/* Computes BytesPerPixel and selects reading/writing functions */
				data.BitPix = Header["BITPIX"].Int;
			}
			catch (Exception ex) { throw new FITSFormatException("Cannot understand FITS file.", ex); }
			return data;
		}

		/// <summary>
		/// Parses the WCS records into a <see cref="WCSViaProjection"/>.
		/// </summary>
		/// <param name="Header">Image header.</param>
		private static WCSViaProjection ParseWCS(HeaderTable Header)
		{
			/* Parse axis types and projection algorithm */
			string Axis1 = Header["CTYPE1"].AsString;
			string Axis2 = Header["CTYPE2"].AsString;
			string Algorithm = Axis1.Substring(5, 3);
			string Nm1 = Axis1.Substring(0, 4);
			string Nm2 = Axis2.Substring(0, 4);

			/* Parses the order of the axes and checks consistency of projection algorithm between the axes. */
			bool RAFirst;
			if (Nm1.ToUpper() == "RA--" && Nm2.ToUpper() == "DEC-") RAFirst = true;
			else if (Nm1.ToUpper() == "DEC-" && Nm2.ToUpper() == "RA--") RAFirst = false;
			else throw new FITSFormatException("Cannot understand axis format");
			if (Axis2.Substring(5, 3) != Algorithm) throw new Exception("Projection Algorithm Mismatch.");
			/* Computes the linear transformation part of the WCS projection */
			WCSLinPart linpart;
			double RA0 = (RAFirst ? Header["CRVAL1"] : Header["CRVAL2"]).FloatingPoint;
			double Dec0 = (RAFirst ? Header["CRVAL2"] : Header["CRVAL1"]).FloatingPoint;
			double X0 = Header["CRPIX1"].FloatingPoint;
			double Y0 = Header["CRPIX2"].FloatingPoint;
			if (RAFirst) linpart = new WCSLinPart(Header["CD1_1"].FloatingPoint, Header["CD1_2"].FloatingPoint, Header["CD2_1"].FloatingPoint, Header["CD2_2"].FloatingPoint, X0, Y0);
			else linpart = new WCSLinPart(Header["CD2_1"].FloatingPoint, Header["CD2_2"].FloatingPoint, Header["CD1_1"].FloatingPoint, Header["CD1_2"].FloatingPoint, X0, Y0);

			/* Finds the appropriate projection transform */
			WCSProjectionTransform ipt;
			if (Header["CUNIT1"].AsString != "deg     " || Header["CUNIT2"].AsString != "deg     ")
				throw new FITSFormatException("Unknown unit types for WCS axes.");
			try { ipt = Umbrella2.WCS.Projections.WCSProjections.GetProjectionTransform(Algorithm, RA0 * Math.PI / 180, Dec0 * Math.PI / 180); }
			catch (KeyNotFoundException ex) { throw new FITSFormatException("Cannot understand projection algorithm", ex); }

			return new WCSViaProjection(ipt, linpart);
		}

		/// <summary>
		/// Creates a deep copy of the image's shallow header (FICHV defined headers).
		/// </summary>
		public FICHV CopyHeader()
		{
			int BitPix = Header["BITPIX"].Int;
			FICHV f = new FICHV() { BitPix = BitPix, Width = Width, Height = Height, WCS = Transform };
			return f;
		}
	}
}
