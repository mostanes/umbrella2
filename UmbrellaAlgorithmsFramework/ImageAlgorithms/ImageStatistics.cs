using System;
using System.Collections.Generic;
using static System.Math;
using System.Threading.Tasks;
using Umbrella2.IO.FITS;

namespace Umbrella2.Algorithms.Images
{
	/// <summary>
	/// Contains a set of information about the image.
	/// </summary>
	public class ImageStatistics : ImageProperties
	{
		/// <summary>
		/// Background level.
		/// </summary>
		public readonly double ZeroLevel;
		/// <summary>
		/// Noise standard deviations.
		/// </summary>
		public readonly double StDev;

		List<double> Means;
		List<double> Variances;

		/// <summary>
		/// Computes the ImageStatistics for a given image.
		/// </summary>
		/// <param name="Image">Input image.</param>
		public ImageStatistics(FitsImage Image) : base(Image)
		{
			Means = new List<double>();
			Variances = new List<double>();

			StatAlgorithm.Run(this, Image, new ParallelAlgorithmRunner.AlgorithmRunParameters() { FillZero = false, InputMargins = 0, Xstep = 0, Ystep = 50 });

			double[] M = Means.ToArray();
			double[] V = Variances.ToArray();
			Array.Sort(M);
			Array.Sort(V);
			ZeroLevel = M[M.Length / 2];
			StDev = Sqrt(V[M.Length / 2]);
		}

		/// <summary>
		/// Accessible form of the computation function.
		/// </summary>
		static ParallelAlgorithmRunner.Extractor<ImageStatistics> StatAlgorithm = RunStatistics;

		/// <summary>
		/// Computation function.
		/// </summary>
		/// <param name="Input">Input data.</param>
		/// <param name="Stats">Result collector.</param>
		static void RunStatistics(double[,] Input, ImageStatistics Stats)
		{ 
			int OW = Input.GetLength(1);
			int OH = Input.GetLength(0);
			int i, j, k;
			
			double Mean = 0, Var = 0;
			/* Scan the image on block-by-block basis */
			for (k = 0; k < OW - OH; k += OH)
			{
				/* Scan block */
				Mean = 0; Var = 0;
				for (i = 0; i < OH; i++) for (j = 0; j < OH; j++)
					{ Mean += Input[i, j + k]; Var += Input[i, j + k] * Input[i, j + k]; }
				
				/* Compute mean and variance */
				Mean /= (OH * OH);
				Var /= (OH * OH);
				Var -= Mean * Mean;

				/* Update results */
				lock (Stats.Means)
				{
					Stats.Means.Add(Mean);
					Stats.Variances.Add(Var);
				}
			}			
		}

		public override List<ElevatedRecord> GetRecords()
		{
			throw new NotImplementedException();
		}
	}
}
