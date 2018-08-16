using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.Plugins;

namespace Umbrella2.WCS.Projections
{
	/// <summary>
	/// Functions for dealing with WCS projection algorithms.
	/// </summary>
	class WCSProjections : IPluggableElementLoader
	{
		Dictionary<string, Type> ProjectionTypes = new Dictionary<string, Type>();

		/// <summary>
		/// The singleton instance.
		/// </summary>
		static WCSProjections Instance = new WCSProjections().Register();

		/// <summary>
		/// Loads projection algorithms from a list of dotNET types.
		/// </summary>
		/// <param name="TypeArray">List of types.</param>
		public void LoadFromTypeList(Type[] TypeArray)
		{
			var Results = TypeArray.Select((x) => new Tuple<Type, object>(x, x.GetCustomAttribute(typeof(ProjectionAttribute)))).Where((x) => x.Item2 != null);
			foreach (var z in Results) if (!ProjectionTypes.ContainsKey(((ProjectionAttribute) z.Item2).Name)) ProjectionTypes.Add(((ProjectionAttribute) z.Item2).Name, z.Item1);
		}

		/// <summary>
		/// Singleton registration.
		/// </summary>
		/// <returns>The instance.</returns>
		protected WCSProjections Register()
		{ LoadableTypes.RegisterLoader(nameof(WCSProjections), this); return this; }

		/// <summary>
		/// Retrieves the projection algorithm from the list of known projection algorithm.
		/// </summary>
		/// <param name="Algorithm">Name of the algorithm</param>
		/// <param name="RA">Reference point Right Ascension.</param>
		/// <param name="Dec">Reference point Declination.</param>
		/// <returns>An instance of the projection algorithm at given reference point.</returns>
		public static WCSProjectionTransform GetProjectionTransform(string Algorithm, double RA, double Dec)
		{
			return (WCSProjectionTransform) Activator.CreateInstance(Instance.ProjectionTypes[Algorithm], RA, Dec);
		}
	}
}
