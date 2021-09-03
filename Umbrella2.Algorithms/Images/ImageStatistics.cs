using System;
using System.Collections.Generic;
using static System.Math;
using System.Threading.Tasks;
using Umbrella2.IO;

namespace Umbrella2.Algorithms.Images
{
	/// <summary>
	/// Contains a set of information about the image.
	/// </summary>
	public class ImageStatistics : ImageProperties
	{
		/// <summary>
		/// Signature of the solver algorithm used by <see cref="ImageStatistics"/> to obtain the background and noise levels.
		/// </summary>
		public delegate void StatisticsSolver(Image Image, out double ZeroLevel, out double StDev);

		/// <summary>
		/// Background level.
		/// </summary>
		public readonly double ZeroLevel;
		/// <summary>
		/// Noise standard deviations.
		/// </summary>
		public readonly double StDev;

		/// <summary>
		/// Computes the ImageStatistics for a given image.
		/// </summary>
		/// <param name="Image">Input image.</param>
		public ImageStatistics(Image Image) : base(Image)
		{
			Solver(Image, out ZeroLevel, out StDev);
		}

		/// <summary>
		/// Creates an artificial set of statistics for an image.
		/// </summary>
		/// <param name="Image">Image.</param>
		/// <param name="ZeroLevel">Background level.</param>
		/// <param name="StDev">Standard deviation.</param>
		public ImageStatistics(Image Image, double ZeroLevel, double StDev) : base(Image) { this.ZeroLevel = ZeroLevel; this.StDev = StDev; }

		/// <summary>
		/// The computation function for solving image statistics.
		/// </summary>
		static StatisticsSolver Solver = BasicImstatSolver.BasicSolver;

		/// <inheritdoc/>
		public override List<MetadataRecord> GetRecords()
		{
			throw new NotImplementedException();
		}
	}
}
