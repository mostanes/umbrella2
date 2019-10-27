﻿using System;
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
		public PixelVelocity PixelVelocity;

		/// <summary>
		/// Represents the velocity in equatorial coordinates.
		/// </summary>
		public EquatorialVelocity EquatorialVelocity;

		/// <summary>
		/// Equatorial velocity in arcsec per minute.
		/// </summary>
		public double ArcSecMin => ((double)EquatorialVelocity) * 3600 * 60 * 180 / Math.PI;
	}
}
