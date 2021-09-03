using System;
using System.Collections.Generic;
using Umbrella2.IO;
using static System.Math;

namespace Umbrella2.Algorithms.Images
{
	/// <summary>
	/// Simple solver for producing <see cref="ImageStatistics"/>.
	/// </summary>
	public class BasicImstatSolver
	{
		List<double> Means;
		List<double> Variances;

		/// <summary>
		/// Accessible form of the computation function.
		/// </summary>
		static SchedCore.Extractor<BasicImstatSolver> StatAlgorithm = RunStatistics;

		/// <summary>
		/// Solver function conforming to <see cref="ImageStatistics.StatisticsSolver"/>.
		/// </summary>
		public static void BasicSolver(Image Image, out double ZeroLevel, out double StDev)
		{
			BasicImstatSolver imsolver = new BasicImstatSolver()
			{
				Means = new List<double>(),
				Variances = new List<double>()
			};

			StatAlgorithm.Run(imsolver, Image, new SchedCore.AlgorithmRunParameters() { FillZero = false, InputMargins = 0, Xstep = 0, Ystep = 50 });

			double[] M = imsolver.Means.ToArray();
			double[] V = imsolver.Variances.ToArray();
			Array.Sort(M);
			Array.Sort(V);
			ZeroLevel = M[M.Length / 2];
			StDev = Sqrt(V[M.Length / 2]);
		}

		/// <summary>
		/// Computation function.
		/// </summary>
		/// <param name="Input">Input data.</param>
		/// <param name="Stats">Result collector.</param>
		static void RunStatistics(double[,] Input, BasicImstatSolver Stats)
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

	}
}
