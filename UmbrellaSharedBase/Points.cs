using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2
{
    public struct PixelPoint
    {
		public double X;
		public double Y;
    }

	public struct EquatorialPoint
	{
		public double RA;
		public double Dec;

		public static double operator ^(EquatorialPoint a, EquatorialPoint b) { return WCS.EquatorialDistance.GetDistance(a, b); }
		public static WCS.EquatorialDistance.GreatLine operator -(EquatorialPoint a, EquatorialPoint b) { return WCS.EquatorialDistance.GetSphericalVector(b, a); }
	}

	public struct ProjectionPoint
	{
		public double X;
		public double Y;
	}
}
