using System;
using System.Collections.Generic;
using System.Linq;
using static Umbrella2.Algorithms.Filtering.Helper;

namespace Umbrella2.Algorithms.Filtering
{
	static class Helper
	{
		/// <summary>Creates a vector from a 2 points.</summary>
		public static BadzoneFilter.Vector Sub(PixelPoint a, PixelPoint b) => new BadzoneFilter.Vector() { X = b.X - a.X, Y = b.Y - a.Y };
	}

	/// <summary>
	/// Filters detections in bad image areas.
	/// </summary>
	public class BadzoneFilter : IImageDetectionFilter
	{
		/// <summary>Bad areas on the input images.</summary>
		List<ConvexPolygon> BadAreas;

		/// <summary>Creates a new instance using the given points as vertices of convex polygons.</summary>
		/// <param name="Badzones">A list of convex polygons in the form of lists of vertices.</param>
		public BadzoneFilter(List<List<PixelPoint>> Badzones)
		{
			BadAreas = Badzones.Select((x) => new ConvexPolygon() { Vertices = x.ToArray() }).ToList();
		}

		public bool Filter(ImageDetection Input)
		{
			PixelPoint pp = Input.Barycenter.PP;

			return !BadAreas.Any((x) => x.IsInside(pp));
		}

		internal struct Vector
		{
			internal double X, Y;


			public static double operator |(Vector a, Vector b) => a.X * b.Y - a.Y * b.X;
		}

		struct ConvexPolygon
		{
			internal PixelPoint[] Vertices;

			public bool IsInside(PixelPoint Point)
			{
				/* By computing the sign of the area/cross product and checking it's always the same */
				double Prx = Sub(Vertices[Vertices.Length - 1], Vertices[0]) | Sub(Vertices[Vertices.Length - 1], Point);

				for (int i = 1; i < Vertices.Length; i++)
				{
					double r = Sub(Vertices[i - 1], Vertices[i]) | Sub(Vertices[i - 1], Point);
					if (r * Prx < 0)
						return false;
				}

				return true;
			}
		}
	}
}
