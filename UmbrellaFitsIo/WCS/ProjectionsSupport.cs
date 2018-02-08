using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.Plugins;

namespace Umbrella2.WCS.Projections
{
	class ProjectionAttribute : Attribute
	{
		public readonly string Name;
		public readonly string Description;

		public ProjectionAttribute(string ProjectionTag, string ProjectionDescription)
		{
			Name = ProjectionTag;
			Description = ProjectionDescription;
		}
	}

	class WCSProjections : IPluggableElementLoader
	{
		Dictionary<string, Type> ProjectionTypes = new Dictionary<string, Type>();

		static WCSProjections Instance = new WCSProjections().Register("WCSProjections");

		public void LoadFromTypeList(Type[] TypeArray)
		{
			var Results = TypeArray.Select((x) => new Tuple<Type, object>(x, x.GetCustomAttribute(typeof(ProjectionAttribute)))).Where((x) => x.Item2 != null);
			foreach (var z in Results) if (!ProjectionTypes.ContainsKey(((ProjectionAttribute) z.Item2).Name)) ProjectionTypes.Add(((ProjectionAttribute) z.Item2).Name, z.Item1);
		}

		protected WCSProjections Register(string Name)
		{ LoadableTypes.RegisterLoader(Name, this); return this; }

		public static WCSProjectionTransform GetProjectionTransform(string Algorithm, double RA, double Dec)
		{
			return (WCSProjectionTransform) Activator.CreateInstance(Instance.ProjectionTypes[Algorithm], RA, Dec);
		}
	}
}
