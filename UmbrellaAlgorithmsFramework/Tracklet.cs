using System;
using System.Collections.Generic;
using Umbrella2.PropertyModel;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2
{
	/// <summary>
	/// An object candidate.
	/// </summary>
	public class Tracklet
	{
		/// <summary>
		/// Object instances that form the tracklet.
		/// </summary>
		public readonly ImageDetection[] Detections;
		/// <summary>
		/// Object velocity.
		/// </summary>
		public readonly TrackletVelocity Velocity;
		/// <summary>
		/// Represents the tracklet's velocity regression parameters.
		/// </summary>
		public readonly TrackletVelocityRegression VelReg;

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
		/// Tries fetching a property of the ImageDetection.
		/// </summary>
		/// <typeparam name="T">Property type.</typeparam>
		/// <param name="Property">Property instance on the object.</param>
		/// <returns>True if property exists.</returns>
		public bool TryFetchProperty<T>(out T Property) where T : IExtensionProperty
		{
			bool b = ExtendedProperties.TryGetValue(typeof(T), out IExtensionProperty PropertyIEP);
			if (b)
				Property = (T) PropertyIEP;
			else Property = default(T);
			return b;
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
