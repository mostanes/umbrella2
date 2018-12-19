using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.PropertyModel;

namespace Umbrella2.PropertyModel.CommonProperties
{
	public class ObjectPhotometry : IExtensionProperty
	{
		/// <summary>The object's flux as measured on the image.</summary>
		[PropertyDescription(true)]
		public double Flux;
	}
}
