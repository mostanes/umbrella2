using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace Umbrella2.WCS.Projections
{
	/// <summary>
	/// Gnomonic projection algorithm for image WCS.
	/// </summary>
	[Projection(AlgorithmName, AlgorithmDescription)]
	class TAN : WCSProjectionTransform
	{
		public const string AlgorithmName = "TAN";
		public const string AlgorithmDescription = "Gnomonic projection.";

		public TAN(double RA, double Dec) : base(RA, Dec)
		{ }

		public override string Name => AlgorithmName;

		public override string Description => AlgorithmDescription;

		public override EquatorialPoint GetEquatorialPoint(ProjectionPoint Point)
		{
			double Rho = Sqrt(Point.X * Point.X + Point.Y * Point.Y);
			double C = Atan(Rho);
			double RAn = RA + Atan2(Point.X * Sin(C), (Rho * Cos(Dec) * Cos(C) - Point.Y * Sin(Dec) * Sin(C)));
			double Decn = Asin((Point.Y * Cos(Dec) + Sin(Dec)) / Sqrt(1 + Point.X * Point.X + Point.Y * Point.Y));
			return new EquatorialPoint() { RA = RAn, Dec = Decn };
		}

		public override EquatorialPoint[] GetEquatorialPoints(ProjectionPoint[] Points)
		{
			EquatorialPoint[] EqP = new EquatorialPoint[Points.Length];
			for (int i = 0; i < Points.Length; i++)
			{
				double X = Points[i].X;
				double Y = Points[i].Y;
				double Rho = Sqrt(X * X + Y * Y);
				double C = Atan(Rho);
				EqP[i].RA = RA + Atan2(X * Sin(C), (Rho * Cos(Dec) * Cos(C) - Y * Sin(Dec) * Sin(C)));
				EqP[i].Dec = Asin((Y * Cos(Dec) + Sin(Dec)) / Sqrt(1 + X * X + Y * Y));
			}
			return EqP;
		}

		public override List<EquatorialPoint> GetEquatorialPoints(IEnumerable<ProjectionPoint> Points)
		{
			List<EquatorialPoint> EqP = new List<EquatorialPoint>();
			foreach (ProjectionPoint Point in Points)
			{
				double Rho = Sqrt(Point.X * Point.X + Point.Y * Point.Y);
				double C = Atan(Rho);
				double RAn = RA + Atan2(Point.X * Sin(C), (Rho * Cos(Dec) * Cos(C) - Point.Y * Sin(Dec) * Sin(C)));
				double Decn = Asin((Point.Y * Cos(Dec) + Sin(Dec)) / Sqrt(1 + Point.X * Point.X + Point.Y * Point.Y));
				EqP.Add(new EquatorialPoint() { RA = RAn, Dec = Decn });
			}
			return EqP;
		}

		public override double GetEstimatedWCSChainDerivative()
		{
			return 1;
		}

		public override ProjectionPoint GetProjectionPoint(EquatorialPoint Point)
		{
			double CC = Sin(Point.Dec) * Sin(Dec) + Cos(Point.Dec) * Cos(Dec) * Cos(Point.RA - RA);
			double Xn = Cos(Point.Dec) * Sin(Point.RA - RA) / CC;
			double Yn = (Cos(Dec) * Sin(Point.Dec) - Sin(Dec) * Cos(Point.Dec) * Cos(Point.RA - RA)) / CC;
			return new ProjectionPoint() { X = Xn, Y = Yn };
		}

		public override ProjectionPoint[] GetProjectionPoints(EquatorialPoint[] Points)
		{
			ProjectionPoint[] EqP = new ProjectionPoint[Points.Length];
			for (int i = 0; i < Points.Length; i++)
			{
				double DDec = Points[i].Dec;
				double RRa = Points[i].RA;
				double CC = Sin(DDec) * Sin(Dec) + Cos(DDec) * Cos(Dec) * Cos(RRa - RA);
				EqP[i].X = Cos(DDec) * Sin(RRa - RA) / CC;
				EqP[i].Y = (Cos(Dec) * Sin(DDec) - Sin(Dec) * Cos(DDec) * Cos(RRa - RA)) / CC;
			}
			return EqP;
		}

		public override List<ProjectionPoint> GetProjectionPoints(IEnumerable<EquatorialPoint> Points)
		{
			List<ProjectionPoint> EqP = new List<ProjectionPoint>();
			foreach (EquatorialPoint Point in Points)
			{
				double CC = Sin(Point.Dec) * Sin(Dec) + Cos(Point.Dec) * Cos(Dec) * Cos(Point.RA - RA);
				double Xn = Cos(Point.Dec) * Sin(Point.RA - RA) / CC;
				double Yn = (Cos(Dec) * Sin(Point.Dec) - Sin(Dec) * Cos(Point.Dec) * Cos(Point.RA - RA)) / CC;
				EqP.Add(new ProjectionPoint() { X = Xn, Y = Yn });
			}
			return EqP;
		}

		public override void GetReferencePoints(out double RA, out double Dec) { RA = this.RA; Dec = this.Dec; }
	}
}
