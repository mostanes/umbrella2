using System;
using Umbrella2.PropertyModel;

namespace Umbrella2
{
	/// <summary>
	/// Represents the velocity of a tracklet.
	/// </summary>
	public class TrackletVelocity : IExtensionProperty
	{
		/// <summary>
		/// Represents the velocity in pixel coordinates.
		/// </summary>
		public PixelVelocity PixelVelocity;

		/* NOTE: The following code should be implemented at a later time. */
		/*
		/// <summary>
		/// Represents the velocity in equatorial coordinates.
		/// </summary>
		EquatorialVelocity EquatorialVelocity;
		*/
		/// <summary>
		/// Represents the velocity in equatorial coordinates.
		/// </summary>
		public double EquatorialVelocity;
	}

	/// <summary>
	/// Represents a velocity in pixel coordinates.
	/// </summary>
	public struct PixelVelocity
	{
		public double Xvel;
		public double Yvel;

		public static explicit operator double(PixelVelocity Velocity) => Math.Sqrt(Velocity.Xvel * Velocity.Xvel + Velocity.Yvel * Velocity.Yvel);
	}

	/*
	/// <summary>
	/// Represents a velocity in equatorial coordinates.
	/// </summary>
	struct EquatorialVelocity
	{
		/// <summary>Velocity on right ascension.</summary>
		public readonly double RAvel;
		/// <summary>Velocity on declination.</summary>
		public readonly double Decvel;
		/// <summary>Equatorial point at which the velocity is given.</summary>
		public readonly EquatorialPoint EqP;
		/// <summary>Angular velocity.</summary>
		public readonly double TotalVelocity;

		public static explicit operator double(EquatorialVelocity Velocity) => Velocity.TotalVelocity;
	}
	*/
}
