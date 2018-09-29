using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.Plugins
{
	/// <summary>
	/// Represents an Umbrella2 plugin holder element which can load plugin elements.
	/// </summary>
	public interface IPluggableElementLoader
	{
		/// <summary>
		/// Scans and loads plugins from given types.
		/// </summary>
		/// <param name="TypeArray">List of types to scan.</param>
		void LoadFromTypeList(Type[] TypeArray);
	}

	/// <summary>
	/// Holds references to Umbrella2 plugin holder elements. On loading new types, informs plugin holders that new types are available.
	/// </summary>
	public static class LoadableTypes
	{
		/// <summary>
		/// List of plugin holders.
		/// </summary>
		public static readonly Dictionary<string, IPluggableElementLoader> Loaders = new Dictionary<string, IPluggableElementLoader>();

		/// <summary>
		/// Contains types already discovered. The list is kept for re-announcing types to new plugin holders.
		/// </summary>
		static List<Type> TypeCache = new List<Type>();

		/// <summary>
		/// Registers a new plugin holder. Also informs the plugin holder of the already known types.
		/// </summary>
		/// <param name="Name">Name of the plugin holder.</param>
		/// <param name="Loader">The plugin holder.</param>
		public static void RegisterLoader(string Name, IPluggableElementLoader Loader)
		{ Loaders.Add(Name, Loader); Loader.LoadFromTypeList(TypeCache.ToArray()); }

		/// <summary>
		/// Informs the plugin holders of the new available types.
		/// </summary>
		/// <param name="Types">The new available types.</param>
		public static void RegisterNewTypes(Type[] Types)
		{
			TypeCache.AddRange(Types);
			foreach (IPluggableElementLoader ipel in Loaders.Values) ipel.LoadFromTypeList(Types);
		}
	}
}
