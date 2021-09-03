using System;
using System.Collections.Generic;
using Umbrella2.IO;
using Umbrella2.IO.FITS.KnownKeywords;
using Umbrella2.PropertyModel;

namespace Umbrella2
{
	/// <summary>
	/// The detection on an image of an object.
	/// </summary>
	public class ImageDetection : IExtendable
	{
		/// <summary>Position of the flux barycenter.</summary>
		[PropertyDescription(true)]
		public readonly Position Barycenter;
		/// <summary>Exposure information.</summary>
		[PropertyDescription(true)]
		public readonly ObservationTime Time;
		/// <summary>Image on which the detection was observed.</summary>
		[PropertyDescription(true)]
		public readonly Image ParentImage;

		/// <summary>
		/// Creates a ImageDetection from the given arguments. This constructor is internally called by the ImageDetection factories.
		/// </summary>
		public ImageDetection(Position Barycenter, ObservationTime Time, Image ParentImage)
		{ this.Barycenter = Barycenter; this.Time = Time; this.ParentImage = ParentImage; ExtendedProperties = new Dictionary<Type, IExtensionProperty>(); }

		/// <summary>
		/// Empty constructor, for easier use with reflection.
		/// </summary>
		public ImageDetection() { }

		/// <summary>
		/// List of supplementary properties.
		/// </summary>
		/// <remarks>
		/// The held values should be reference types; otherwise boxing will make them read-only.
		/// </remarks>
		[PropertyList]
		public Dictionary<Type, IExtensionProperty> ExtendedProperties { get; }

		/// <summary>
		/// Fetches a property of the ImageDetection. Not thread-safe when also appending properties concurrently.
		/// </summary>
		/// <typeparam name="T">Property type.</typeparam>
		/// <returns>The property, casted to the appropriate type.</returns>
		public T FetchProperty<T>() where T : IExtensionProperty => (T) ExtendedProperties[typeof(T)];

		/// <summary>
		/// Tries fetching a property of the ImageDetection.
		/// </summary>
		/// <typeparam name="T">Property type.</typeparam>
		/// <param name="Property">Property instance on the object.</param>
		/// <returns>True if property exists.</returns>
		public bool TryFetchProperty<T>(out T Property) where T : IExtensionProperty, new()
		{
			bool b = ExtendedProperties.TryGetValue(typeof(T), out IExtensionProperty PropertyIEP);
			if (b)
				Property = (T) PropertyIEP;
			else Property = default(T);
			if (Property == null) Property = new T();
			return b;
		}

		/// <summary>
		/// Tries fetching a property of the ImageDetection or creates a new one.
		/// </summary>
		/// <typeparam name="T">Property type.</typeparam>
		/// <returns>Property instance on the object or the default value of the type.</returns>
		public T FetchOrCreate<T>() where T : IExtensionProperty, new()
		{
			bool b = ExtendedProperties.TryGetValue(typeof(T), out IExtensionProperty PropertyIEP);
			if (b)
				return (T) PropertyIEP;
			T Value = default(T);
			if (Value == null) Value = new T();
			ExtendedProperties.Add(typeof(T), Value);
			return Value;
		}

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
		public void SetResetProperty<T>(T Property) where T : IExtensionProperty
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
