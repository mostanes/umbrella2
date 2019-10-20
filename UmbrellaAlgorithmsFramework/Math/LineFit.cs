using System;
using System.Collections.Generic;

namespace Umbrella2.Algorithms.Misc
{
	public static class LineFit
	{
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

		[System.Diagnostics.Contracts.Pure]
		public static double ComputeResidualSqSum(double[] X, double[] Y) => ComputeResidualSqSum(LinearRegression.ComputeLinearRegression(X, Y), X, Y);

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

		[System.Diagnostics.Contracts.Pure]
		public static double ComputeResidualSqSum(IEnumerable<PixelPoint> Points) =>
			ComputeResidualSqSum(LinearRegression.ComputeLinearRegression(Points), Points);
	}
}
