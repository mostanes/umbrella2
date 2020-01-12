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

	/// <summary>
	/// Image Core Header Values. A wrapper for the core data in Images' header data.
	/// </summary>
	public class ICHV
	{
		/// <summary>Image width.</summary>
		public uint Width;
		/// <summary>Image height.</summary>
		public uint Height;
		/// <summary>Image WCS.</summary>
		public IWCSProjection WCS;
		/// <summary>Header table.</summary>
		public HeaderTable Header;
		/// <summary>The number of the image in a multi-image file.</summary>
		public int ImageNumber;
	}
}
