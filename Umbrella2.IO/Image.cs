using System;
using System.Collections.Generic;
using System.Drawing;
using Umbrella2.WCS;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;


namespace Umbrella2.IO
{
    /// <summary>
    /// Image data from a FITS File.
    /// The data is in the form [y, x].
    /// </summary>
    [Serializable]
	public struct ImageData
	{
		/// <summary>The position in the image of the current data.</summary>
		readonly public Rectangle Position;
		/// <summary>The pixel values in the image. First index is the Y axis.</summary>
		public double[,] Data;
		/// <summary>The image to which this data belongs.</summary>
		readonly public Image Parent;
		/// <summary>Whether the data is readonly or not.</summary>
		readonly public bool ReadOnly;
		readonly internal Guid FDGuid;

		public ImageData(Rectangle Location, double[,] ImageData, Image Image, bool Readonly, Guid UID)
		{
			Position = Location;
			Data = ImageData;
			Parent = Image;
			ReadOnly = Readonly;
			FDGuid = UID;
		}
	}

	/// <summary>
	/// Represents a set of image properties that can be parsed from image metadata.
	/// </summary>
	public abstract class ImageProperties
	{
		/// <summary>
		/// Creates a new instance of the image properties for the given image.
		/// </summary>
		/// <param name="Image">The image for which the properties are extracted.</param>
		public ImageProperties(Image Image)
		{ }

		/// <summary>
		/// Gets the list of metadata records associated with the property.
		/// </summary>
		/// <returns>A list of metadata records.</returns>
		public abstract List<MetadataRecord> GetRecords();
	}

	public abstract class Image
	{
		public readonly uint Width, Height;

		/// <summary>
		/// The number of the image in the FITS file.
		/// </summary>
		public readonly int ImageNumber;

		/// <summary>
		/// World Coordinate System Transformation.
		/// </summary>
		public readonly IWCSProjection Transform;
		
		/// <summary>
		/// FITS Image Headers.
		/// </summary>
		public readonly HeaderTable Header;

		/// <summary>
		/// Extra Image Properties.
		/// </summary>
		protected readonly Dictionary<Type, ImageProperties> PropertiesDictionary;

		protected Image(int ImageNumber, IWCSProjection Transform, HeaderTable Header, uint Width, uint Height)
		{
			this.ImageNumber = ImageNumber;
			this.Transform = Transform;
			this.Header = Header;
			this.Width = Width;
			this.Height = Height;
			this.PropertiesDictionary = new Dictionary<Type, ImageProperties>();
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
		/// Locks and returns the data of an image. Can be used for reading and writing.
		/// </summary>
		/// <param name="Area">Area of interest in the image.</param>
		/// <param name="FillZero">True for padding out of image margins with zero. Must be false for write access.</param>
		/// <param name="RO">Whether the data is read-only.</param>
		/// <returns>An ImageData container.</returns>
		public abstract ImageData LockData(Rectangle Area, bool FillZero, bool RO = true);

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
		public abstract ImageData SwitchLockData(ImageData Data, int NewX, int NewY, bool FillZero, bool RO = true);

		/// <summary>
		/// Exits the lock on a region of image, flushing any writable data.
		/// </summary>
		/// <param name="Data">The data container.</param>
		public abstract void ExitLock(ImageData Data);
	}
}

