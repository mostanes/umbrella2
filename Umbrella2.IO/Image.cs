using System;
using System.Collections.Generic;
using System.Drawing;
using Umbrella2.WCS;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;

namespace Umbrella2.IO
{
	public abstract class Image
	{
		public readonly uint Width, Height;

		/// <summary>
		/// The number of the image in a multi-image file.
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
		/// Creates a new instance from a set of headers.
		/// </summary>
		/// <param name="Headers">Image headers.</param>
		protected Image(ICHV Headers) : this(Headers.ImageNumber, Headers.WCS, Headers.Header, Headers.Width, Headers.Height)
		{ }

		/// <summary>
		/// Creates a new instance from a set of headers and properties.
		/// </summary>
		/// <param name="Headers">Image headers.</param>
		/// <param name="Properties">Image properties.</param>
		protected Image(ICHV Headers, Dictionary<Type, ImageProperties> Properties) : this(Headers)
		{ PropertiesDictionary = Properties; }

		/// <summary>
		/// Returns all associated image properties.
		/// </summary>
		/// <returns>All image properties.</returns>
		public Dictionary<Type, ImageProperties> GetAllProperties() => PropertiesDictionary;

		/// <summary>Gets the Image's headers.</summary>
		public ICHV GetICHV() => new ICHV() { Header = Header, Height = Height, ImageNumber = ImageNumber, WCS = Transform, Width = Width };

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

