using System;

namespace Umbrella2
{
	/// <summary>
	/// Point representing a pixel coordinate.
	/// </summary>
	[Serializable]
    public struct PixelPoint
    {
		public double X;
		public double Y;
		public override string ToString()
		{
			return "X=" + X.ToString("G6") + ", Y=" + Y.ToString("G6");
		}
		public static double operator ^(PixelPoint a, PixelPoint b) { return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y)); }
	}

	/// <summary>
	/// Point representing a point on the equatorial coordinate system.
	/// </summary>
	[Serializable]
	public struct EquatorialPoint
	{
		public double RA;
		public double Dec;

		public static double operator ^(EquatorialPoint a, EquatorialPoint b) { return WCS.EquatorialDistance.GetDistance(a, b); }
		public static WCS.EquatorialDistance.GreatLine operator -(EquatorialPoint a, EquatorialPoint b) { return new WCS.EquatorialDistance.GreatLine(b, a); }
	}

	/// <summary>
	/// Point representing a projection plane coordinate.
	/// </summary>
	/// <remarks>
	/// The standard units of ProjectionPoint are radians.
	/// </remarks>
	[Serializable]
	public struct ProjectionPoint
	{
		public double X;
		public double Y;
	}
}
