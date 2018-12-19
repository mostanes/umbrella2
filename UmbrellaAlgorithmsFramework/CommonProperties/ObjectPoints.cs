using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2;
using Umbrella2.PropertyModel;

namespace Umbrella2.PropertyModel.CommonProperties
{
	/// <summary>
	/// The collection of points covered by an object.
	/// </summary>
	public class ObjectPoints : IExtensionProperty
	{
		/// <summary>List of pixels covered by an object in equatorial coordinates.</summary>
		public EquatorialPoint[] EquatorialPoints;
		/// <summary>List of pixels covered by an object in image coordinates.</summary>
		public PixelPoint[] PixelPoints;
		/// <summary>List of values of the comprising pixels.</summary>
		public double[] PixelValues;
	}
}
