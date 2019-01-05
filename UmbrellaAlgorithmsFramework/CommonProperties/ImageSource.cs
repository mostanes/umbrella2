using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.IO.FITS;

namespace Umbrella2.PropertyModel.CommonProperties
{
	public class ImageSource : ImageProperties
	{
		public FitsImage Original;

		public ImageSource(FitsImage Image, FitsImage Original) : base(Image)
		{ }

		public ImageSource(FitsImage Image) : base(Image)
		{ }

		public override List<ElevatedRecord> GetRecords()
		{
			throw new NotImplementedException();
		}
	}
}
