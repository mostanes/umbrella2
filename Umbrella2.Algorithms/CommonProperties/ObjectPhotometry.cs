using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.PropertyModel;

namespace Umbrella2.PropertyModel.CommonProperties
{
	/// <summary>
	/// Photometry measurements on the object.
	/// </summary>
	public class ObjectPhotometry : IExtensionProperty
	{
		/// <summary>The object's flux as measured on the image.</summary>
		[PropertyDescription(true)]
		public double Flux;
		/// <summary>The object's magnitude. Zero or <see cref="double.NaN"/> values represent no magnitude.</summary>
		[PropertyDescription(true)]
		public double Magnitude;
	}
}
