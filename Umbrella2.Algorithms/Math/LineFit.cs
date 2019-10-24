using System;
using System.Collections.Generic;

namespace Umbrella2.Algorithms.Misc
{
	/// <summary>
	/// Fits a line to a set of points. Note that this assumes errors only in the Y-axis.
	/// </summary>
	public static class LineFit
	{
		/// <summary>
		/// Computes the sum of residuals' squares.
		/// </summary>
		/// <returns>The sum of residuals' squares..</returns>
		/// <param name="Parameters">Linear regression parameters.</param>
		/// <param name="X">Points' X coordinates.</param>
		/// <param name="Y">Points' Y coordinates.</param>
		[System.Diagnostics.Contracts.Pure]
		public static double ComputeResidualSqSum(LinearRegression.LinearRegressionParameters Parameters, double[] X, double[] Y)
		{
			System.Diagnostics.Contracts.Contract.Requires(X.Length == Y.Length);
			double Sum = 0;
			for (int i = 0; i < X.Length; i++)
			{
				double Est = Parameters.Slope * X[i] + Parameters.Intercept;
				Est -= Y[i];
				Sum += Est * Est;
			}
			return Sum;
		}

		/// <summary>
		/// Computes the sum of residuals' squares.
		/// </summary>
		/// <returns>The sum of residuals' squares..</returns>
		/// <param name="X">Points' X coordinates.</param>
		/// <param name="Y">Points' Y coordinates.</param>
		[System.Diagnostics.Contracts.Pure]
		public static double ComputeResidualSqSum(double[] X, double[] Y) => ComputeResidualSqSum(LinearRegression.ComputeLinearRegression(X, Y), X, Y);

		/// <summary>
		/// Computes the sum of residuals' squares.
		/// </summary>
		/// <returns>The sum of residuals' squares..</returns>
		/// <param name="Parameters">Linear regression parameters.</param>
		/// <param name="Points">Points to fit.</param>
		[System.Diagnostics.Contracts.Pure]
		public static double ComputeResidualSqSum(LinearRegression.LinearRegressionParameters Parameters, IEnumerable<PixelPoint> Points)
		{
			double Sum = 0;
			foreach (PixelPoint pp in Points)
			{
				double Est = Parameters.Slope * pp.X + Parameters.Intercept;
				Est -= pp.Y;
				Sum += Est * Est;
			}
			return Sum;
		}

		/// <summary>
		/// Computes the sum of residuals' squares.
		/// </summary>
		/// <returns>The sum of residuals' squares..</returns>
		/// <param name="Points">Points to fit.</param>
		[System.Diagnostics.Contracts.Pure]
		public static double ComputeResidualSqSum(IEnumerable<PixelPoint> Points) =>
			ComputeResidualSqSum(LinearRegression.ComputeLinearRegression(Points), Points);
	}
}
