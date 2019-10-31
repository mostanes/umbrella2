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
		/// <summary>Pearson R correlation between Time and RA coordinate.</summary>
		public double R_TR;
		/// <summary>Pearson R correlation between Time and Dec coordinate.</summary>
		public double R_TD;
		/// <summary>Pearson R correlation between X and Y coordinates.</summary>
		public double R_RD;

		public double S_TR;
		public double S_TD;
	}
}
