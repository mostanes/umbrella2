using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.IO.FITS;
using Umbrella2.IO.FITS.KnownKeywords;
using Umbrella2.PropertyModel;

namespace Umbrella2
{
	class ImageDetection
	{
		/// <summary>Position of the flux barycenter.</summary>
		[PropertyDescription(true)]
		public readonly Position Barycenter;
		/// <summary>Exposure information.</summary>
		[PropertyDescription(true)]
		public readonly ObservationTime Time;
		/// <summary>Image on which the detection was observed.</summary>
		[PropertyDescription(true)]
		public readonly FitsImage ParentImage;

		/// <summary>
		/// List of supplementary properties.
		/// </summary>
		/// <remarks>
		/// The held values should be reference types; otherwise boxing will make them read-only.
		/// </remarks>
		[PropertyList]
		public readonly Dictionary<Type, IExtensionProperty> ExtendedProperties;

		/// <summary>
		/// Fetches a property of the ImageDetection. Not thread-safe when also appending properties concurrently.
		/// </summary>
		/// <typeparam name="T">Property type.</typeparam>
		/// <returns>The property, casted to the appropriate type.</returns>
		public T FetchProperty<T>() where T : IExtensionProperty => (T) ExtendedProperties[typeof(T)];

		/// <summary>
		/// Appends a property to the object.
		/// </summary>
		/// <remarks>
		/// Note that this function sets the property type according to the generic type parameter.
		/// </remarks>
		/// <typeparam name="T">Property type.</typeparam>
		/// <param name="Property">Property instance.</param>
		public void AppendProperty<T>(T Property) where T : IExtensionProperty
		{ lock (ExtendedProperties) ExtendedProperties.Add(typeof(T), Property); }

		/// <summary>
		/// Appends or overwrites a property.
		/// </summary>
		/// <typeparam name="T">Property type.</typeparam>
		/// <param name="Property">Property instance.</param>
		public void SetResetProperty<T>(T Property) where T:IExtensionProperty
		{
			Type t = typeof(T);
			lock (ExtendedProperties)
			{
				if (ExtendedProperties.ContainsKey(t)) ExtendedProperties[t] = Property;
				else ExtendedProperties.Add(t, Property);
			}
		}
	}
}
