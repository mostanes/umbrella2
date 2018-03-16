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
		public static HTResult RunRLHT(double[,] Input, double IncTh, double MinLength, double MinFlux, double StrongHoughTh)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double Rhomax = Sqrt(Height * Height + Width * Width);
			return RunRLHT(Input, IncTh, MinLength, MinFlux, StrongHoughTh, 4, 4, 0, Sqrt(Height * Height + Width * Width), 0, 2 * PI);
		}

		public static HTResult RefineRLHT(double[,] Input, double IncTh, double MinLength, double MinFlux, double StrongHoughTh, double Rho, double Theta)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(1, Height), Atan2(1, Width));
			return RunRLHT(Input, IncTh, MinLength, MinFlux, StrongHoughTh, 1, 1, Rho - 10, Rho + 10, Theta - 10 * ThetaUnit, Theta + 10 * ThetaUnit);
		}

		public static HTSpan RefinedRLHT(double[,] Input, double IncTh, double MinLength, double MinFlux, double StrongHoughTh, List<Vector> StrongPoints)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(1, Height), Atan2(1, Width));
			List<Vector> Directions = new List<Vector>();
			HTResult res = RunRLHT(Input, IncTh, MinLength, MinFlux, StrongHoughTh, 1, 1, Rho - 10, Rho + 10, Theta - 10 * ThetaUnit, Theta + 10 * ThetaUnit);
			Directions.AddRange(res.StrongPoints);
			Algorithms.Misc.ConnectedComponentGraph<Vector> ccg = new Misc.ConnectedComponentGraph<Vector>(res.StrongPoints, (x, y) => (Abs(x.X - y.X) + Abs(x.Y - y.Y) / ThetaUnit) < 2.1);
			List<Vector>[] lv = ccg.GetConnectedComponents();
			HTSpan hspan = new HTSpan() { StrongPointSpans = lv };
			return hspan;
		}

		public static HTResult RunRLHT(double[,] Input, double IncTh,double MinLen, double MinFx, double SHTh, double SkA, double SkR, double StRad, double EndRad, double StAng, double EndAng)
		{
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(SkA, Height), Atan2(SkA, Width));
			double NTheta = (EndAng - StAng) / ThetaUnit;
			double[,] HTMatrix = new double[(int) Round((EndRad - StRad) / SkR), (int) Round(NTheta)];
			int NRd = HTMatrix.GetLength(0);
			int NTh = HTMatrix.GetLength(1);
			int i, j;
			List<Vector> HoughPowerul = new List<Vector>();
			for (i = 0; i < NRd; i++)
			{
				for (j = 0; j < NTh; j++)
				{
					double Theta = j * ThetaUnit + StAng;
					if (Theta > PI / 2) if (Theta < PI) continue;
					Lineover(Input, Height, Width, SkR * i + StRad, Theta, IncTh, out HTMatrix[i, j]);
					if (HTMatrix[i, j] > SHTh) HoughPowerul.Add(new Vector() { X = SkR * i + StRad, Y = Theta });
				}
			}
			return new HTResult() { HTMatrix = HTMatrix, StrongPoints = HoughPowerul };
		}

		public struct HTResult
		{
			internal double[,] HTMatrix;
			//internal List<Segment> Segments;
			internal List<Vector> StrongPoints;
		}

		public struct HTSpan
		{
			internal List<Vector>[] StrongPointSpans;
		}

		public struct Segment
		{
			internal Vector Start, End;
			internal double Intensity;
			internal double Flux;
			internal double Angle;
			internal double Radius;

			internal double Length { get => Sqrt((Start.X - End.X) * (Start.X - End.X) + (Start.Y - End.Y) * (Start.Y - End.Y)); }

			public override string ToString() { return Start.ToString() + "  ---->  " + End.ToString() + "    |  " + Intensity.ToString("E5"); }
		}

		
	}
}
