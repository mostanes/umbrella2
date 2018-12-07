using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2;
using Umbrella2.PropertyModel;

namespace UmbrellaAlgorithmsFramework.CommonProperties
{
	/// <summary>
	/// Holds information on the size of the object.
	/// </summary>
	public class ObjectSize : IExtensionProperty
	{
		/// <summary>The elliptical fit over the objects pixels.</summary>
		[PropertyDescription(true)]
		public SourceEllipse PixelEllipse;

		/// <summary>The intensity-weighted elliptical fit over the object's pixels.</summary>
		[PropertyDescription(true)]
		public SourceEllipse BarycentricEllipse;
	}
}
