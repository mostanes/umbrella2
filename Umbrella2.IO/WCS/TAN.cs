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
		/// <summary>This epsilon constant is used to avoid a 0/0 division in computing Decn in the code below.</summary>
		const double ADDC = 144.0 / double.MaxValue;

		public TAN(double RA, double Dec) : base(RA, Dec)
		{ }

		public override string Name => AlgorithmName;

		public override string Description => AlgorithmDescription;

		public override EquatorialPoint GetEquatorialPoint(ProjectionPoint Point)
		{
			double Rho = Sqrt(Point.X * Point.X + Point.Y * Point.Y + ADDC);
			double C = Atan(Rho);
			double RAn = RA + Atan2(Point.X, Cos(Dec) - Point.Y * Sin(Dec));
			double Decn = Asin((Point.Y * Cos(Dec) + Sin(Dec)) * Cos(C));
			return new EquatorialPoint() { RA = RAn, Dec = Decn };
		}

		public override EquatorialPoint[] GetEquatorialPoints(ProjectionPoint[] Points)
		{
			EquatorialPoint[] EqP = new EquatorialPoint[Points.Length];
			for (int i = 0; i < Points.Length; i++)
			{
				double X = Points[i].X;
				double Y = Points[i].Y;
				double Rho = Sqrt(X * X + Y * Y + ADDC);
				double C = Atan(Rho);
				EqP[i].RA = RA + Atan2(X, Cos(Dec) - Y * Sin(Dec));
				EqP[i].Dec = Asin((Y * Cos(Dec) + Sin(Dec)) * Cos(C));
			}
			return EqP;
		}

		public override List<EquatorialPoint> GetEquatorialPoints(IEnumerable<ProjectionPoint> Points)
		{
			List<EquatorialPoint> EqP = new List<EquatorialPoint>();
			foreach (ProjectionPoint Point in Points)
			{
				double Rho = Sqrt(Point.X * Point.X + Point.Y * Point.Y + ADDC);
				double C = Atan(Rho);
				double RAn = RA + Atan2(Point.X, Cos(Dec) - Point.Y * Sin(Dec));
				double Decn = Asin((Point.Y * Cos(Dec) + Sin(Dec)) * Cos(C));
				EqP.Add(new EquatorialPoint() { RA = RAn, Dec = Decn });
			}
			return EqP;
		}

		public override EquatorialVelocity GetEquatorialVelocity(ProjectionVelocity PV)
		{ return new EquatorialVelocity() { RAvel = PV.X / Cos(Dec), Decvel = PV.Y * Cos(Dec) }; }

		public override double GetEstimatedWCSChainDerivative()
		{ return 1; }

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

		public override ProjectionVelocity GetProjectionVelocity(EquatorialVelocity EV)
		{ return new ProjectionVelocity() { X = EV.RAvel * Cos(Dec), Y = EV.Decvel / Cos(Dec) }; }

		public override void GetReferencePoints(out double RA, out double Dec) { RA = this.RA; Dec = this.Dec; }
	}
}
