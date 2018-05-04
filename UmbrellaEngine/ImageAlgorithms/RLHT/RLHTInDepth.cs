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
		public static HTResult RefineResult(double[,] Input, double IncTh, double StrongHoughTh, HTResult Result)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(1, Height), Atan2(1, Width));
			double NTheta = 2 * PI / ThetaUnit;
			double RhoMax = Sqrt(Width * Width + Height * Height);
			double DeltaTheta = 10 * ThetaUnit;
			double DeltaRho = 10;
			double[,] HTMatrix = new double[(int) Round(RhoMax), (int) Round(NTheta)];
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
					if (Result.StrongPoints.Any((x) => (Abs(x.X - i)) < DeltaRho && (Abs(x.Y - Theta) < DeltaTheta))) continue;
					Lineover(Input, Height, Width, i, Theta, IncTh, out HTMatrix[i, j]);
					if (HTMatrix[i, j] > StrongHoughTh) HoughPowerul.Add(new Vector() { X = i, Y = Theta });
				}
			}
			return new HTResult() { StrongPoints = HoughPowerul };
		}
	}
}
