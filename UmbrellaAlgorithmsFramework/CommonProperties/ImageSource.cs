using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.IO.FITS;

namespace Umbrella2.PropertyModel.CommonProperties
{
	/// <summary>
	/// Represents the <see cref="ImageSet"/> an image belongs to.
	/// </summary>
	public class ImageSource : ImageProperties
	{
		private ImageSet Set;
		private FitsImage CurrentImage;

		public ImageSource(FitsImage Image) : base(Image)
		{ CurrentImage = Image; }

		/// <summary>
		/// Adds an image to the set of another (original) image.
		/// </summary>
		/// <param name="Element">Image from which the current one is derived.</param>
		/// <param name="Name">Name of the current image in the set.</param>
		public void AddToSet(FitsImage Element, string Name)
		{
			Set = ((ImageSet) Element.GetProperty<ImageSource>());
			if (Set == null)
			{
				Set = new ImageSet(Element);
				Element.GetProperty<ImageSource>().Set = Set;
			}
			Set.AppendImage(CurrentImage, Name);
		}

		/// <summary>Retrieves the image's <see cref="ImageSet"/>.</summary>
		public static explicit operator ImageSet(ImageSource Source) => Source.Set;

		public override List<ElevatedRecord> GetRecords()
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Represents a set of images of the same sky surface, each processed differently.
	/// </summary>
	public class ImageSet
	{
		/// <summary>Source image of the sky surface.</summary>
		public readonly FitsImage Original;
		/// <summary>The differently processed images available.</summary>
		readonly Dictionary<string, FitsImage> Variants;

		/// <summary>Creates an <see cref="ImageSet"/> from a source image.</summary>
		public ImageSet(FitsImage Original) { this.Original = Original; Variants = new Dictionary<string, FitsImage>(); }

		/// <summary>
		/// Appends an image to the set of derived images (variants).
		/// </summary>
		/// <param name="Image">Derived image (variant).</param>
		/// <param name="Name">Name of the variant.</param>
		public void AppendImage(FitsImage Image, string Name) { lock (Variants) { Variants.Add(Name, Image); } }

		/// <summary>
		/// Fetches the set of derived images (variants) of the original image.
		/// </summary>
		/// <returns>A name-indexed dictionary containing the variants.</returns>
		public Dictionary<string, FitsImage> FetchVariants() { lock (Variants) return new Dictionary<string, FitsImage>(Variants); }
	}
}
