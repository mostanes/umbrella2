using System;
using System.Diagnostics;

namespace Umbrella2
{
#pragma warning disable 1591
	/// <summary>
	/// Velocity in pixel coordinates. Values in units per second.
	/// </summary>
	[Serializable]
	public struct PixelVelocity
	{
		/// <summary>X velocity in units per second.</summary>
		public double Xvel;
		/// <summary>Y velocity in units per second.</summary>
		public double Yvel;

		public double Magnitude => Math.Sqrt(Xvel * Xvel + Yvel * Yvel);
		public double Angle => Math.Atan2(Yvel, Xvel);

		public override string ToString()
		{
			return "Xvel=" + Xvel.ToString("G6") + ", Yvel=" + Yvel.ToString("G6");
		}

		public static explicit operator double(PixelVelocity pv) => pv.Magnitude;
	}

	/// <summary>
	/// Velocity in the equatorial coordinate system.
	/// </summary>
	[Serializable]
	public struct EquatorialVelocity
	{
		/// <summary>RA velocity in radians per second.</summary>
		public double RAvel;
		/// <summary>Dec velocity in radians per second.</summary>
		public double Decvel;

		public override string ToString()
		{
			return "RAvel=" + (RAvel * 38880000 / Math.PI).ToString("G6") + "\"/min, Decvel=" + (Decvel * 38880000 / Math.PI).ToString("G6") + "\"/min";
		}
	}

	/// <summary>
	/// Velocity in projection plane coordinates.
	/// </summary>
	/// <remarks>
	/// The standard units of <see cref="ProjectionVelocity"/> are radians per second.
	/// </remarks>
	[Serializable]
	public struct ProjectionVelocity
	{
		public double X;
		public double Y;
	}
#pragma warning restore 1591
}
