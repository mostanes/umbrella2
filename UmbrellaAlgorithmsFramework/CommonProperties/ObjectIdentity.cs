using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2;
using Umbrella2.PropertyModel;

namespace Umbrella2.PropertyModel.CommonProperties
{
	/// <summary>
	/// Contains information on the identity of the object observed (i.e. which celestial body it is).
	/// </summary>
	public class ObjectIdentity : IExtensionProperty
	{
		/// <summary>Name of the observed object.</summary>
		[PropertyDescription(true)]
		public string Name;
	}
}
