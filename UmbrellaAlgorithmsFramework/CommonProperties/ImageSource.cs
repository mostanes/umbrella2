using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.IO.FITS;

namespace Umbrella2.PropertyModel.CommonProperties
{
	public class ImageSource : ImageProperties
	{
		private ImageSet Set;
		private FitsImage CurrentImage;

		public ImageSource(FitsImage Image) : base(Image)
		{ CurrentImage = Image; }

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

		public static explicit operator ImageSet(ImageSource Source) => Source.Set;

		public override List<ElevatedRecord> GetRecords()
		{
			throw new NotImplementedException();
		}
	}

	public class ImageSet
	{
		public readonly FitsImage Original;
		readonly Dictionary<string, FitsImage> Variants;

		public ImageSet(FitsImage Original) { this.Original = Original; Variants = new Dictionary<string, FitsImage>(); }

		public void AppendImage(FitsImage Image, string Name) { lock (Variants) { Variants.Add(Name, Image); } }

		public Dictionary<string, FitsImage> FetchVariants() { lock (Variants) return new Dictionary<string, FitsImage>(Variants); }
	}
}
