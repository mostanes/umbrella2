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
		/*
		public static HTResult RefineRLHT(double[,] Input, double IncTh, double MinLength, double MinFlux, double StrongHoughTh, double Rho, double Theta)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(1, Height), Atan2(1, Width));
			return RunRLHT(Input, IncTh, MinLength, MinFlux, StrongHoughTh, 1, 1, Rho - 10, Rho + 10, Theta - 10 * ThetaUnit, Theta + 10 * ThetaUnit);
		}

		public static List<Vector>[] RefinedRLHT(double[,] Input, double IncTh, double MinLength, double MinFlux, double StrongHoughTh, List<Vector> StrongPoints)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(1, Height), Atan2(1, Width));
			List<Vector> Directions = new List<Vector>();
			HTResult res = RunRLHT(Input, IncTh, MinLength, MinFlux, StrongHoughTh, 1, 1, Rho - 10, Rho + 10, Theta - 10 * ThetaUnit, Theta + 10 * ThetaUnit);
			Directions.AddRange(res.StrongPoints);
			Algorithms.Misc.ConnectedComponentGraph<Vector> ccg = new Misc.ConnectedComponentGraph<Vector>(res.StrongPoints, (x, y) => (Abs(x.X - y.X) + Abs(x.Y - y.Y) / ThetaUnit) < 2.1);
			List<Vector>[] lv = ccg.GetConnectedComponents();
			return lv;
		}
		*/
	}
}
