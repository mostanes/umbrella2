using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.Plugins
{
	public interface IPluggableElementLoader
	{
		void LoadFromTypeList(Type[] TypeArray);
	}

	public static class LoadableTypes
	{
		public static readonly Dictionary<string, IPluggableElementLoader> Loaders = new Dictionary<string, IPluggableElementLoader>();
		static List<Type> TypeCache = new List<Type>();
		public static void RegisterLoader(string Name, IPluggableElementLoader Loader)
		{ Loaders.Add(Name, Loader); Loader.LoadFromTypeList(TypeCache.ToArray()); }

		public static void RegisterNewTypes(Type[] Types)
		{
			TypeCache.AddRange(Types);
			foreach (IPluggableElementLoader ipel in Loaders.Values) ipel.LoadFromTypeList(Types);
		}
	}
}
