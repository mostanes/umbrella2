using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace Umbrella2.WCS
{
	public class EquatorialDistance
	{
		public static double GetDistance(EquatorialPoint A, EquatorialPoint B)
		{
			double DeltaPhi = A.Dec - B.Dec;
			double DeltaLambda = A.RA - B.RA;
			double HvPhi = Sin(DeltaPhi / 2);
			double HvLbd = Sin(DeltaLambda / 2);
			double HaverRoot = HvPhi * HvPhi + Cos(A.Dec) * Cos(B.Dec) * HvLbd * HvLbd;
			double Distance = 2 * Asin(Sqrt(HaverRoot));
			return Distance;
		}

		internal struct Vector3D
		{
			internal double X, Y, Z;

			public static Vector3D operator +(Vector3D A, Vector3D B) => new Vector3D() { X = A.X + B.X, Y = A.Y + B.Y, Z = A.Z + B.Z };
			public static Vector3D operator *(double M, Vector3D B) => new Vector3D() { X = M * B.X, Y = M * B.Y, Z = M * B.Z };
			public static double operator *(Vector3D A, Vector3D B) => A.X * B.X + A.Y * B.Y + A.Z * B.Z;
		}

		public struct GreatLine
		{
			internal Vector3D A, B;
			internal double AlphaAngle;

			public static EquatorialPoint operator +(GreatLine Vector, double Distance) => GetPointOnLine(Vector, Distance);
			public static double operator ~(GreatLine Vector) => Vector.AlphaAngle;
		}

		public static EquatorialPoint GetGreatCircleWaypoint(EquatorialPoint A, EquatorialPoint B, double Distance)
		{
			double sA = Cos(A.Dec), sB = Cos(B.Dec);
			Vector3D vA = new Vector3D() { X = sA * Cos(A.RA), Y = sA * Sin(A.RA), Z = Sin(A.Dec) };
			Vector3D vB = new Vector3D() { X = sB * Cos(B.RA), Y = sB * Sin(B.RA), Z = Sin(B.Dec) };
			double Alpha = GetDistance(A, B);
			double mA = -Sin(Distance) / Sin(Alpha);
			double mB = Sin(Distance + Alpha) / Sin(Alpha);
			Vector3D vCs = mA * vA + mB * vB;
			double RA = Atan2(vCs.Y, vCs.X);
			double Dec = Atan2(vCs.Z, Sqrt(vCs.X * vCs.X + vCs.Y * vCs.Y));
			return new EquatorialPoint() { RA = RA, Dec = Dec };
		}

		public static GreatLine GetSphericalVector(EquatorialPoint A, EquatorialPoint B)
		{
			double sA = Cos(A.Dec), sB = Cos(B.Dec);
			Vector3D vA = new Vector3D() { X = sA * Cos(A.RA), Y = sA * Sin(A.RA), Z = Sin(A.Dec) };
			Vector3D vB = new Vector3D() { X = sB * Cos(B.RA), Y = sB * Sin(B.RA), Z = Sin(B.Dec) };
			double Alpha = GetDistance(A, B);
			return new GreatLine() { A = vA, B = vB, AlphaAngle = Alpha };
		}

		public static EquatorialPoint GetPointOnLine(GreatLine Vector, double Distance)
		{
			double mA = -Sin(Distance) / Sin(Vector.AlphaAngle);
			double mB = Sin(Distance + Vector.AlphaAngle) / Sin(Vector.AlphaAngle);
			Vector3D vCs = mA * Vector.A + mB * Vector.B;
			double RA = Atan2(vCs.Y, vCs.X);
			if (RA < 0) RA += 2 * PI;
			double Dec = Atan2(vCs.Z, Sqrt(vCs.X * vCs.X + vCs.Y * vCs.Y));
			return new EquatorialPoint() { RA = RA, Dec = Dec };
		}
	}
}
