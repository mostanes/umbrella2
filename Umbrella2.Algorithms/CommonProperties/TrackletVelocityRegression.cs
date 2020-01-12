using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.Algorithms.Misc;
using Umbrella2.PropertyModel;

namespace Umbrella2.PropertyModel.CommonProperties
{
	/// <summary>
	/// Represents the correlation coefficienct on the regression of tracklet velocity.
	/// </summary>
	public class TrackletVelocityRegression : IExtensionProperty
	{
		/// <summary>Pearson R correlation between Time and RA coordinate.</summary>
		[PropertyDescription(true)]
		public double R_TR;
		/// <summary>Pearson R correlation between Time and Dec coordinate.</summary>
		[PropertyDescription(true)]
		public double R_TD;
		/// <summary>Pearson R correlation between X and Y coordinates.</summary>
		[PropertyDescription(true)]
		public double R_RD;

		/// <summary>Sum of residuals' squares on Time - RA regression.</summary>
		[PropertyDescription(true)]
		public double S_TR;

		/// <summary>Sum of residuals' squares on Time - Dec regression.</summary>
		[PropertyDescription(true)]
		public double S_TD;

		/// <summary>Regression parameters Time - RA.</summary>
		[PropertyDescription(true)]
		public LinearRegression.LinearRegressionParameters P_TR;

		/// <summary>Regression parameters Time - Dec.</summary>
		[PropertyDescription(true)]
		public LinearRegression.LinearRegressionParameters P_TD;

		/// <summary>Time at regression intercept.</summary>
		[PropertyDescription(true)]
		public DateTime ZeroTime;
	}
}
