using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace Umbrella2.WCS
{
	/// <summary>
	/// Functions for computing distances on spherical coordinates.
	/// </summary>
	public static class EquatorialDistance
	{
		/// <summary>
		/// Returns the spherical distance between 2 points on the sphere.
		/// </summary>
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

		/// <summary>
		/// Represents a 3D vector.
		/// </summary>
		internal struct Vector3D
		{
			internal double X, Y, Z;

			/// <summary>
			/// Vector sum.
			/// </summary>
			public static Vector3D operator +(Vector3D A, Vector3D B) => new Vector3D() { X = A.X + B.X, Y = A.Y + B.Y, Z = A.Z + B.Z };

			/// <summary>
			/// Product with a scalar.
			/// </summary>
			public static Vector3D operator *(double M, Vector3D B) => new Vector3D() { X = M * B.X, Y = M * B.Y, Z = M * B.Z };
			
			/// <summary>
			/// Inner product.
			/// </summary>
			public static double operator *(Vector3D A, Vector3D B) => A.X * B.X + A.Y * B.Y + A.Z * B.Z;
		}

		/// <summary>
		/// Defines a spherical line segment / arc of a great circle.
		/// </summary>
		public struct GreatLine
		{
			internal Vector3D A, B;
			internal double AlphaAngle;

			/// <summary>
			/// Generates the great circle through the 2 given points.
			/// </summary>
			/// <param name="A">The positive direction of the great circle.</param>
			/// <param name="B">The origin of the great circle.</param>
			public GreatLine(EquatorialPoint A, EquatorialPoint B)
			{
				double sA = Cos(A.Dec), sB = Cos(B.Dec);
				this.A = new Vector3D() { X = sA * Cos(A.RA), Y = sA * Sin(A.RA), Z = Sin(A.Dec) };
				this.B = new Vector3D() { X = sB * Cos(B.RA), Y = sB * Sin(B.RA), Z = Sin(B.Dec) };
				AlphaAngle = GetDistance(A, B);
			}

			/// <summary>
			/// Provides great circle navigation. Equivalent of GetPointOnLine.
			/// </summary>
			public static EquatorialPoint operator +(GreatLine Vector, double Distance) => Vector.GetPointOnLine(Distance);
			
			/// <summary>
			/// Returns the line length / spherical distance between the points on the sphere.
			/// </summary>
			public static double operator ~(GreatLine Vector) => Vector.AlphaAngle;

			/// <summary>
			/// Returns the point a given distance away (from B) on the great circle.
			/// </summary>
			public EquatorialPoint GetPointOnLine(double Distance)
			{
				double mA = -Sin(Distance) / Sin(AlphaAngle);
				double mB = Sin(Distance + AlphaAngle) / Sin(AlphaAngle);
				Vector3D vCs = mA * A + mB * B;
				double RA = Atan2(vCs.Y, vCs.X);
				if (RA < 0) RA += 2 * PI;
				double Dec = Atan2(vCs.Z, Sqrt(vCs.X * vCs.X + vCs.Y * vCs.Y));
				return new EquatorialPoint() { RA = RA, Dec = Dec };
			}
		}

		/// <summary>
		/// Provides great circle navigation.
		/// </summary>
		/// <param name="A">Endpoint.</param>
		/// <param name="B">Startpoint.</param>
		/// <param name="Distance">Distance to navigate.</param>
		/// <returns>The point Distance away from B on the great circle defined by A and B.</returns>
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
	}
}
