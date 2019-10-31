using System;
using Umbrella2.Algorithms.Misc;
using Umbrella2.IO;
using static System.Math;
using static Umbrella2.Algorithms.Images.SchedCore;

namespace Umbrella2.Algorithms.Images.Normalization
{
	/// <summary>
	/// An image brightness uniformization algorithm that interpolates background intensity by the distance to the points in a mesh of medians.
	/// </summary>
	public class Point4Distance
	{
		/// <summary>Size of the median mesh.</summary>
		public int MeshSize;
		/// <summary>Mesh of medians.</summary>
		double[,] MedianPoints;
		private MTPool<double[]> Cached;
		Image Input;
		Image Output;
		ImageStatistics InputStat;

		PositionDependentExtractor<Point4Distance> MedianMesh = RunMesh;
		PositionDependentMap<Point4Distance> Normalizer = Normalize;

		public Point4Distance(Image Input, Image Output, int MeshSize)
		{
			this.Input = Input;
			this.Output = Output;
			this.MeshSize = MeshSize;
			InputStat = Input.GetProperty<ImageStatistics>();
			MedianPoints = new double[Input.Height / MeshSize + 1, Input.Width / MeshSize + 1];
			Cached = new MTPool<double[]>() { Constructor = () => new double[MeshSize * MeshSize] };
			MedianMesh.Run(this, Input, new AlgorithmRunParameters() { FillZero = true, InputMargins = 0, Xstep = MeshSize, Ystep = MeshSize });
			Normalizer.Run(this, Input, Output, new AlgorithmRunParameters() { FillZero = false, InputMargins = 0, Xstep = 0, Ystep = MeshSize });
		}

		/// <summary>Mesh generation function.</summary>
		static void RunMesh(double[,] Input, ImageSegmentPosition Position, Point4Distance Mesh)
		{
			/* Find median of input block */
			double[] V = new double[Input.Length];
			Buffer.BlockCopy(Input, 0, V, 0, V.Length * sizeof(double));
			Array.Sort(V);
			/* In very bright regions, keep image's zero level as the median, thus avoiding background stretching by stars */
			if (Abs(V[V.Length / 2] - Mesh.InputStat.ZeroLevel) > 10 * Mesh.InputStat.StDev) V[V.Length / 2] = Mesh.InputStat.ZeroLevel;
			/* Load the median as a mesh point */
			Mesh.MedianPoints[(int) Round(Position.Alignment.Y) / Mesh.MeshSize, (int) Round(Position.Alignment.X) / Mesh.MeshSize] = V[V.Length / 2];
		}

		/// <summary>
		/// Output normalization function.
		/// </summary>
		static void Normalize(double[,] Input, double[,] Output, ImageSegmentPosition InputPosition, ImageSegmentPosition OutputPosition, Point4Distance Mesh)
		{
			int OH = Input.GetLength(0), OW = Input.GetLength(1);
			int i, j;

			/* Deal with image edges */
			if (InputPosition.Alignment.Y <= Mesh.MeshSize)
			{
				for (i = 0; i < OH; i++)
				{
					for (j = 0; j < Mesh.MeshSize; j++) Output[i, j] = Input[i, j] - Mesh.MedianPoints[2, 2];
					for (; j < OW - Mesh.MeshSize; j++)
						Output[i, j] = Input[i, j] - Mesh.MedianPoints[2, j / Mesh.MeshSize];
					for (; j < OW; j++) Output[i, j] = Input[i, j] - Mesh.MedianPoints[2, Mesh.MedianPoints.GetLength(1) - 3];
				}
				return;
			}
			if (InputPosition.Alignment.Y + 2 * Mesh.MeshSize >= Mesh.Input.Height)
			{
				for (i = 0; i < OH; i++)
				{
					for (j = 0; j < Mesh.MeshSize; j++) Output[i, j] = Input[i, j] - Mesh.MedianPoints[Mesh.MedianPoints.GetLength(0) - 2, 2];
					for (; j < OW - Mesh.MeshSize; j++)
						Output[i, j] = Input[i, j] - Mesh.MedianPoints[Mesh.MedianPoints.GetLength(0) - 2, j / Mesh.MeshSize];
					for (j = 0; j < Mesh.MeshSize; j++) Output[i, j] = Input[i, j] - Mesh.MedianPoints[Mesh.MedianPoints.GetLength(0) - 2, Mesh.MedianPoints.GetLength(1) - 3];
				}
				return;
			}

			for (i = 0; i < OH; i++)
			{
				for (j = 0; j < 3 * Mesh.MeshSize / 2; j++) Output[i, j] = Input[i, j] - Mesh.MedianPoints[(i + (int) InputPosition.Alignment.Y) / Mesh.MeshSize, 2];
				/* Perform main loop */
				for (; j < OW - 3 * Mesh.MeshSize / 2; j++)
				{
					int PX = (int) InputPosition.Alignment.X + j;
					int PY = (int) InputPosition.Alignment.Y + i;
					double[] Distances = new double[4];
					double DistSum, ValSum;

					/* Find nearest mesh points and the distances to them */
					int dPX = PX % Mesh.MeshSize;
					int dPY = PY % Mesh.MeshSize;
					int kPX = PX / Mesh.MeshSize;
					int kPY = PY / Mesh.MeshSize;
					Distances[0] = Sqrt(dPX * dPX + dPY * dPY);
					Distances[1] = Sqrt((Mesh.MeshSize - dPX) * (Mesh.MeshSize - dPX) + dPY * dPY);
					Distances[2] = Sqrt((Mesh.MeshSize - dPY) * (Mesh.MeshSize - dPY) + dPX * dPX);
					Distances[3] = Sqrt((Mesh.MeshSize - dPX) * (Mesh.MeshSize - dPX) + (Mesh.MeshSize - dPY) * (Mesh.MeshSize - dPY));
					DistSum = Distances[0] + Distances[1] + Distances[2] + Distances[3];
					/* Interpolate based on the distance to each mesh point */
					ValSum = (DistSum - Distances[0]) * Mesh.MedianPoints[kPY, kPX] + (DistSum - Distances[1]) * Mesh.MedianPoints[kPY, kPX + 1];
					ValSum += (DistSum - Distances[2]) * Mesh.MedianPoints[kPY + 1, kPX] + (DistSum - Distances[3]) * Mesh.MedianPoints[kPY + 1, kPX + 1];
					double Interpolated = ValSum / DistSum / 3;
					/* Remove background */
					Output[i, j] = Input[i, j] - Interpolated;

				}
				for (; j < OW; j++) Output[i, j] = Input[i, j] - Mesh.MedianPoints[(i + (int) InputPosition.Alignment.Y) / Mesh.MeshSize, Mesh.MedianPoints.GetLength(1) - 3];
			}
		}
	}
}
