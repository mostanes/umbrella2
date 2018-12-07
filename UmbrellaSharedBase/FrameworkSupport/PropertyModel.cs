using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Umbrella2.PropertyModel
{
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class PropertyDescriptionAttribute : Attribute
	{
		public readonly string Name;
		public readonly string Description;
		public readonly bool ParseDocumentation;

		public PropertyDescriptionAttribute(bool ParseDocumentation) { this.ParseDocumentation = ParseDocumentation; }
		public PropertyDescriptionAttribute(string Name) { this.ParseDocumentation = false; this.Name = Name; }
		public PropertyDescriptionAttribute(string Name = "", string Description = "") { this.ParseDocumentation = false; this.Name = Name; this.Description = Description; }
	}

	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class PropertyListAttribute : Attribute
	{ public PropertyListAttribute() { } }

	public interface IObjectViewer<T>
	{
		void ViewObject(T obj);
		void RegisterModificationCallback(Action<T> Callback);
	}

	public interface IObjectPropertyViewer<T, U> where U : IExtensionProperty
	{
		void ViewObject(T obj);
		void RegisterModificationCallback(Action<U> Callback);
	}

	public interface IPropertyViewer<T> where T : IExtensionProperty
	{
		void ViewProperty(T obj);
		void RegisterModificationCallback(Action<T> Callback);
	}

	public interface IExtensionProperty
	{ }
}
