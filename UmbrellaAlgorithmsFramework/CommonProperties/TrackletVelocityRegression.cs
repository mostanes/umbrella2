using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.PropertyModel;

namespace Umbrella2.PropertyModel.CommonProperties
{
	/// <summary>
	/// Represents the correlation coefficienct on the regression of tracklet velocity.
	/// </summary>
	public class TrackletVelocityRegression : IExtensionProperty
	{
		/// <summary>Pearson R correlation between Time and X coordinate.</summary>
		public readonly double R_TX;
		/// <summary>Pearson R correlation between Time and Y coordinate.</summary>
		public readonly double R_TY;
		/// <summary>Pearson R correlation between X and Y coordinates.</summary>
		public readonly double R_XY;
	}
}
