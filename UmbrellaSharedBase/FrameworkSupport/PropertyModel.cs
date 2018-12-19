using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Umbrella2.PropertyModel
{
	/// <summary>
	/// Attribute marks a user-visible property or field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class PropertyDescriptionAttribute : Attribute
	{
		/// <summary>Property name.</summary>
		public readonly string Name;
		/// <summary>Description of the property to show on demand.</summary>
		public readonly string Description;
		/// <summary>Whether the Name and Description should be populated from the XML code documentation.</summary>
		public readonly bool ParseDocumentation;

		public PropertyDescriptionAttribute(bool ParseDocumentation) { this.ParseDocumentation = ParseDocumentation; }
		public PropertyDescriptionAttribute(string Name) { this.ParseDocumentation = false; this.Name = Name; }
		public PropertyDescriptionAttribute(string Name = "", string Description = "") { this.ParseDocumentation = false; this.Name = Name; this.Description = Description; }
	}

	/// <summary>
	/// Indicates a field is in fact a list of properties (of the original object).
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class PropertyListAttribute : Attribute
	{ public PropertyListAttribute() { } }

	/// <summary>
	/// Interface for components that allow the user to view and modify object properties.
	/// </summary>
	/// <typeparam name="T">Type of the object that can be viewed.</typeparam>
	public interface IObjectViewer<T>
	{
		/// <summary>On call, display object.</summary>
		/// <param name="obj">Object to be shown.</param>
		void ViewObject(T obj);
		/// <summary>Register a callback for replacing the object.</summary>
		/// <param name="Callback">Callback on object change.</param>
		void RegisterModificationCallback(Action<T> Callback);
	}

	/// <summary>
	/// Interface for components that allow the user to view and modify a certain object property.
	/// </summary>
	/// <seealso cref="IObjectViewer{T}"/>
	/// <typeparam name="T">Type of the object whose property can be viewed.</typeparam>
	/// <typeparam name="U">Type of the property.</typeparam>
	public interface IObjectPropertyViewer<T, U> where U : IExtensionProperty
	{
		void ViewObject(T obj);
		void RegisterModificationCallback(Action<U> Callback);
	}

	/// <summary>
	/// Used to denote a property that can be attached to an object.
	/// </summary>
	public interface IExtensionProperty
	{ }

	/// <summary>
	/// Represents a method that can compute an extension property of a given object from its other properties.
	/// </summary>
	/// <typeparam name="T">Type of the property to compute.</typeparam>
	/// <typeparam name="U">Type of the object to compute the properties on.</typeparam>
	public interface IPropertyCalculator<T, U>
	{
		/// <summary>Retrieves a list of lists of properties necessary to compute the new property; each list being sufficient to compute the property.</summary>
		/// <returns>An array of types that are required for computing the object.</returns>
		Type[][] GetRequiredProperties();
		/// <summary>Computes the property <see cref="T"/> of the specified object. If the required types are not available, it should throw <see cref="InsufficientInformationException"/>.</summary>
		/// <param name="Object">Object on which to compute the property.</param>
		/// <returns>The computed property.</returns>
		T ComputeProperty(U Object);
	}

	/// <summary>
	/// Thrown when not enough types are available to compute a given property.
	/// </summary>
	public class InsufficientInformationException : Exception
	{ }
}
