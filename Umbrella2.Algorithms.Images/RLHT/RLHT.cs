using System;
using System.Collections.Generic;
using Umbrella2.Algorithms.Geometry;

namespace Umbrella2.Algorithms.Images
{
	public static partial class RLHT
	{
		/// <summary>
		/// Result of running a Hough Transform.
		/// </summary>
		public struct HTResult
		{
			internal double[,] HTMatrix;
			internal List<Vector> StrongPoints;
		}

		/// <summary>
		/// Bag of data containing thresholds and detection algorithm image-specific parameters.
		/// </summary>
		internal struct ImageParameters
		{
			internal double ZeroLevel;
			internal double IncreasingThreshold;
			internal double MaxMultiplier;
			internal double MaxRatio;
			internal double DefaultRatio;

			internal int ShortAvgLength;
			internal int LongAvgLength;
		}

		/// <summary>
		/// Bag of data containing runtime RLHT values.
		/// </summary>
		internal struct AlgorithmData
		{
			internal bool SimpleLine;
			internal double StrongHoughThreshold;
			internal Func<double, double> StrongValueFunction;
			internal int ScanSkip;
			internal int LineSkip;

			internal Misc.MTPool<double[,]> HTPool;
			internal Misc.MTPool<List<Vector>> VPool;
		}
	}
}
