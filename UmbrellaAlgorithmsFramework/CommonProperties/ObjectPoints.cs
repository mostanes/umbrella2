using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2;
using Umbrella2.PropertyModel;

namespace UmbrellaAlgorithmsFramework.CommonProperties
{
	/// <summary>
	/// The collection of points covered by an object.
	/// </summary>
	public class ObjectPoints : IExtensionProperty
	{
		/// <summary>List of pixels covered by an object in equatorial coordinates.</summary>
		public List<EquatorialPoint> EquatorialPoints;
		/// <summary>List of pixels covered by an object in image coordinates.</summary>
		public List<PixelPoint> PixelPoints;
		/// <summary>List of values of the comprising pixels.</summary>
		public List<double> PixelValues;
	}
}
