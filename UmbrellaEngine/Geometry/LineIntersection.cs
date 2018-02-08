using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.Algorithms.Geometry
{
	struct Vector
	{
		public double X, Y;
		public void Increment(Vector v)
		{ X += v.X; Y += v.Y; }

		public override string ToString() { return "(" + X.ToString() + ";" + Y.ToString() + ")"; }
	}

	static class LineIntersection
	{
		internal static Vector GetLineIntersection(Vector X1, Vector X2, Vector D1, Vector D2)
		{
			double Det = (D2.X * D1.Y) - (D1.X * D2.Y);
			if (Det == 0)
			{
				if (X1.X == X2.X && X1.Y == X2.Y) return X1;
				return new Vector() { X = double.NaN, Y = double.NaN };
			}
			Det = 1 / Det;
			double C11 = -D2.Y * Det;
			double C12 = D2.X * Det;
			double C21 = -D1.Y * Det;
			double C22 = D1.X * Det;
			double X0 = X2.X - X1.X;
			double Y0 = X2.Y - X1.Y;
			return new Vector() { X = C11 * X0 + C12 * Y0, Y = C21 * X0 + C22 * Y0 };
		}

		internal static Tuple<Vector, double> IntersectLeft(Vector Origin, Vector Direction, int Width, int Height)
		{
			Vector X0 = new Vector { X = 0, Y = 0 };
			Vector Vert = new Vector { X = 0, Y = 1 };
			Vector Horz = new Vector { X = 1, Y = 0 };
			Vector v1 = GetLineIntersection(Origin, X0, Direction, Vert);
			if (v1.Y >= 0 && v1.Y < Height - 1) return new Tuple<Vector, double>(new Vector() { X = 0, Y = v1.Y }, v1.X);
			v1 = GetLineIntersection(Origin, X0, Direction, Horz);
			if (v1.Y >= 0 && v1.Y < Width - 1) return new Tuple<Vector, double>(new Vector() { X = v1.Y, Y = 0 }, v1.X);
			return null;
		}

		internal static Tuple<Vector, double> IntersectRight(Vector Origin, Vector Direction, int Width, int Height)
		{
			Vector X0 = new Vector { X = Width - 1, Y = Height - 1 };
			Vector Vert = new Vector { X = 0, Y = -1 };
			Vector Horz = new Vector { X = -1, Y = 0 };
			Vector v1 = GetLineIntersection(Origin, X0, Direction, Vert);
			if (v1.Y > 0 && v1.Y < Height - 1) return new Tuple<Vector, double>(new Vector() { X = Width - 1, Y = Height - 1 - v1.Y }, v1.X);
			v1 = GetLineIntersection(Origin, X0, Direction, Horz);
			if (v1.Y > 0 && v1.Y < Width - 1) return new Tuple<Vector, double>(new Vector() { X = Width - 1 - v1.Y, Y = Height - 1 }, v1.X);
			return null;
		}
	}
}
