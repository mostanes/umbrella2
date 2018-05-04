using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.Algorithms.Geometry;
using static System.Math;

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
		/// Runs a Run-length Hough Transform skimming over an input image.
		/// </summary>
		/// <param name="Input">Input image.</param>
		/// <param name="IncTh">Increasing Threshold.</param>
		/// <param name="StrongHoughTh">Threshold for interesting points.</param>
		/// <param name="SkimSize">Amount of pixels to skip.</param>
		/// <returns>A HTResult structure that contains the results of running the RLHT.</returns>
		public static HTResult SkimRLHT(double[,] Input, double IncTh, double StrongHoughTh, double SkimSize)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double RhoMax = Sqrt(Width * Width + Height * Height);
			double ThetaUnit = Min(Atan2(SkimSize, Height), Atan2(SkimSize, Width));
			double NTheta = 2 * PI / ThetaUnit;
			double[,] HTMatrix = new double[(int) Round(RhoMax / SkimSize), (int) Round(NTheta)];
			int NRd = HTMatrix.GetLength(0);
			int NTh = HTMatrix.GetLength(1);
			int i, j;
			List<Vector> HoughPowerul = new List<Vector>();
			for (i = 0; i < NRd; i++)
			{
				for (j = 0; j < NTh; j++)
				{
					double Theta = j * ThetaUnit;
					if (Theta > PI / 2) if (Theta < PI) continue;
					Lineover(Input, Height, Width, SkimSize * i, Theta, IncTh, out HTMatrix[i, j]);
					if (HTMatrix[i, j] > StrongHoughTh) HoughPowerul.Add(new Vector() { X = SkimSize * i, Y = Theta });
				}
			}
			return new HTResult() { HTMatrix = HTMatrix, StrongPoints = HoughPowerul };
		}
	}
}
