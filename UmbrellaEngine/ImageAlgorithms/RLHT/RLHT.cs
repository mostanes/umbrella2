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
		}

	}
}
