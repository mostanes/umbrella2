using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.Algorithms.Geometry
{
	/// <summary>
	/// 2D vector.
	/// </summary>
	public struct Vector
	{
		public double X, Y;

		/// <summary>
		/// Increments the amount of the current vector by the given vector.
		/// </summary>
		/// <param name="v">Increment amount.</param>
		public void Increment(Vector v)
		{ X += v.X; Y += v.Y; }

		/// <summary>
		/// Multiplies the given vector by  scalar.
		/// </summary>
		public static Vector operator *(double Scalar, Vector Vect) => new Vector() { X = Scalar * Vect.X, Y = Scalar * Vect.Y };

		public override string ToString() { return "(" + X.ToString() + ";" + Y.ToString() + ")"; }
	}

	/// <summary>
	/// Class for computing intersections between lines.
	/// </summary>
	public static class LineIntersection
	{
		/// <summary>
		/// Intersects to lines given by directions D and a point on them X and returns the distance between Xs and the intersection in units of Ds.
		/// </summary>
		/// <param name="X1">Point on the first line.</param>
		/// <param name="X2">Point on the second line.</param>
		/// <param name="D1">Direction of the first line.</param>
		/// <param name="D2">Direction of the second line.</param>
		/// <returns>The distances (in units of direction vectors) on the lines from the point to the intersection.</returns>
		public static Vector GetLineIntersection(Vector X1, Vector X2, Vector D1, Vector D2)
		{
			/* Determinant of the 2 directions */
			double Det = (D2.X * D1.Y) - (D1.X * D2.Y);
			if (Det == 0)
			{
				/* The 2 lines are parallel */
				return new Vector() { X = double.NaN, Y = double.NaN };
			}
			Det = 1 / Det;
			/* System matrix */
			double C11 = -D2.Y * Det;
			double C12 = D2.X * Det;
			double C21 = -D1.Y * Det;
			double C22 = D1.X * Det;
			/* Coordinate differences */
			double X0 = X2.X - X1.X;
			double Y0 = X2.Y - X1.Y;
			/* Compute distances in terms of D1 and D2 */
			return new Vector() { X = C11 * X0 + C12 * Y0, Y = C21 * X0 + C22 * Y0 };
		}

		/// <summary>
		/// Computes the left-side intersection between a semiline and a rectangle with one corner at (0, 0).
		/// </summary>
		/// <param name="Origin">Origin point of the semiline.</param>
		/// <param name="Direction">Direction of the semiline.</param>
		/// <param name="Width">Width of the rectangle.</param>
		/// <param name="Height">Height of the rectangle.</param>
		/// <param name="Point">The resulting intersection point.</param>
		/// <param name="Distance">The distance between the line support and the intersection point.</param>
		/// <returns>A tuple containing the intersection point and the distance from Origin to it.</returns>
		public static bool IntersectLeft(Vector Origin, Vector Direction, int Width, int Height, out Vector Point, out double Distance)
		{
			/* Coordinate of the origin corner and the direction of the 2 axes */
			Vector X0 = new Vector { X = 0, Y = 0 };
			Vector Vert = new Vector { X = 0, Y = 1 };
			Vector Horz = new Vector { X = 1, Y = 0 };
			/* Intersect with the vertical axis */
			Vector v1 = GetLineIntersection(Origin, X0, Direction, Vert);
			/* Intersection happening on the Y-axis */
			if (v1.Y >= 0 && v1.Y < Height - 1)
			{
				Point = new Vector() { X = 0, Y = v1.Y };
				Distance = v1.X;
				return true;
			}
			v1 = GetLineIntersection(Origin, X0, Direction, Horz);
			/* Intersection happening on the X-axis */
			if (v1.Y >= 0 && v1.Y < Width - 1)
			{
				Point = new Vector() { X = v1.Y, Y = 0 };
				Distance = v1.X;
				return true;
			}
			Point = default(Vector);
			Distance = 0;
			return false;
		}

		/// <summary>
		/// Computes the right-side intersection between a semiline and a rectangle with one corner at (0, 0).
		/// </summary>
		/// <param name="Origin">Origin point of the semiline.</param>
		/// <param name="Direction">Direction of the semiline.</param>
		/// <param name="Width">Width of the rectangle.</param>
		/// <param name="Height">Height of the rectangle.</param>
		/// <param name="Point">The resulting intersection point.</param>
		/// <param name="Distance">The distance between the line support and the intersection point.</param>
		/// <returns>A tuple containing the intersection point and the distance from Origin to it.</returns>
		public static bool IntersectRight(Vector Origin, Vector Direction, int Width, int Height, out Vector Point, out double Distance)
		{
			/* Coordinate of the variable corner and the direction of the 2 axes */
			Vector X0 = new Vector { X = Width - 1, Y = Height - 1 };
			Vector Vert = new Vector { X = 0, Y = -1 };
			Vector Horz = new Vector { X = -1, Y = 0 };
			/* Intersect with the vertical axis */
			Vector v1 = GetLineIntersection(Origin, X0, Direction, Vert);
			if (v1.Y > 0 && v1.Y < Height - 1)
			{
				Point = new Vector() { X = Width - 1, Y = Height - 1 - v1.Y };
				Distance = v1.X;
				return true;
			}
			/* Intersect with the horizontal axis */
			v1 = GetLineIntersection(Origin, X0, Direction, Horz);
			if (v1.Y > 0 && v1.Y < Width - 1)
			{
				Point = new Vector() { X = Width - 1 - v1.Y, Y = Height - 1 };
				Distance = v1.X;
				return true;
			}
			Point = default(Vector);
			Distance = 0;
			return false;
		}
	}
}
