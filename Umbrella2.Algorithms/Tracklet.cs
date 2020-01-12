using System;
using System.Collections.Generic;
using Umbrella2.PropertyModel;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2
{
	/// <summary>
	/// An object candidate.
	/// </summary>
	public class Tracklet : IExtendable
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
		public Dictionary<Type, IExtensionProperty> ExtendedProperties { get; }

		/// <summary>
		/// Creates a Tracklet from the given arguments. This constructor is internally called by the Tracklet factories.
		/// </summary>
		public Tracklet(ImageDetection[] Detections, TrackletVelocity Velocity, TrackletVelocityRegression Regression)
		{
			this.Detections = Detections;
			this.Velocity = Velocity;
			VelReg = Regression;
			ExtendedProperties = new Dictionary<Type, IExtensionProperty>();
		}

		/// <summary>
		/// Empty constructor, for easier use with reflection.
		/// </summary>
		public Tracklet() { }

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
				Property = (T)PropertyIEP;
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
				return (T)PropertyIEP;
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
