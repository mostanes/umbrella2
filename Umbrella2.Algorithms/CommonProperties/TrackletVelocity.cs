using System;
using System.Diagnostics;
using Umbrella2.PropertyModel;

namespace Umbrella2
{
	/// <summary>
	/// Represents the velocity of a tracklet.
	/// </summary>
	[DebuggerDisplay("{ArcSecMin} \"/min ")]
	public class TrackletVelocity : IExtensionProperty
	{
		/// <summary>
		/// Represents the velocity in pixel coordinates.
		/// </summary>
		[PropertyDescription(true)]
		public PixelVelocity PixelVelocity;

		/// <summary>
		/// Represents the velocity in equatorial coordinates.
		/// </summary>
		[PropertyDescription(true)]
		public EquatorialVelocity EquatorialVelocity;

		/// <summary>
		/// Represents the velocity in radians per second on the sphere.
		/// </summary>
		[PropertyDescription(true)]
		public double SphericalVelocity;

		/// <summary>
		/// Equatorial velocity in arcsec per minute.
		/// </summary>
		public double ArcSecMin => SphericalVelocity * 3600 * 60 * 180 / Math.PI;
	}
}
