using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Umbrella2.Algorithms.Misc
{
	/// <summary>
	/// Provides linear regression functions.
	/// </summary>
	class LinearRegression
	{
		/// <summary>
		/// The parameters obtained from a linear regression.
		/// </summary>
		public struct LinearRegressionParameters
		{
			/// <summary>
			/// The Pearson R coefficient.
			/// </summary>
			public double PearsonR;
			/// <summary>
			/// Slope of the regression line.
			/// </summary>
			public double Slope;
			/// <summary>
			/// Intercept of the regression line.
			/// </summary>
			public double Intercept;
		}

		/// <summary>
		/// Fits a line to a collection of points.
		/// </summary>
		/// <param name="X">The x-axis values.</param>
		/// <param name="Y">The y-axis values.</param>
		/// <returns>Regression parameters.</returns>
		[System.Diagnostics.Contracts.Pure]
		public static LinearRegressionParameters ComputeLinearRegression(double[] X, double[] Y)
		{
			System.Diagnostics.Contracts.Contract.Requires(X.Length == Y.Length);
			double sumX = 0, sumY = 0, sumXSq = 0, sumYSq = 0, ssX = 0, ssY = 0, sumCodev = 0, sCo = 0;
			int i;
			for (i = 0; i < X.Length; i++)
			{
				double x = X[i], y = Y[i];
				sumCodev += x * y;
				sumX += x; sumY += y;
				sumXSq += x * x; sumYSq += y * y;
			}
			return LinearRegressionCore(sumX, sumY, sumCodev, sumXSq, sumYSq, X.Length);
		}

		/// <summary>
		/// Fits a line to a collection of points.
		/// </summary>
		/// <param name="Points">Input data.</param>
		/// <returns>Regression parameters.</returns>
		[System.Diagnostics.Contracts.Pure]
		public static LinearRegressionParameters ComputeLinearRegression(IEnumerable<PixelPoint> Points)
		{
			double sumX = 0, sumY = 0, sumXSq = 0, sumYSq = 0, sumCodev = 0;
			int Count = 0;
			foreach (PixelPoint px in Points)
			{
				double x = px.X, y = px.Y;
				sumCodev += x * y; Count++;
				sumX += x; sumY += y;
				sumXSq += x * x; sumYSq += y * y;
			}
			return LinearRegressionCore(sumX, sumY, sumCodev, sumXSq, sumYSq, Count);
		}

		/// <summary>
		/// Common core for computing the linear regression of a set of points.
		/// </summary>
		[System.Diagnostics.Contracts.Pure]
		static LinearRegressionParameters LinearRegressionCore(double sumX, double sumY, double sumCodev, double sumXSq, double sumYSq, int Length)
		{
			double ssX = 0, ssY = 0, sCo = 0;
			ssX = sumXSq - ((sumX * sumX) / Length);
			ssY = sumYSq - ((sumY * sumY) / Length);
			double Rup = (Length * sumCodev) - (sumX * sumY);
			double Rdown = (Length * sumXSq - (sumX * sumX)) * (Length * sumYSq - (sumY * sumY));
			sCo = sumCodev - ((sumX * sumY) / Length);

			double meanX = sumX / Length;
			double meanY = sumY / Length;
			LinearRegressionParameters lrp;
			lrp.PearsonR = Rup / Math.Sqrt(Rdown);
			lrp.Intercept = meanY - ((sCo / ssX) * meanX);
			lrp.Slope = sCo / ssX;
			return lrp;
		}
	}
}
