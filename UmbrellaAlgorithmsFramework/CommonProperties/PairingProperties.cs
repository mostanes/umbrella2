﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2.PropertyModel;

namespace Umbrella2.PropertyModel.CommonProperties
{
	/// <summary>
	/// Holds information relevant to object pairing.
	/// </summary>
	public class PairingProperties : IExtensionProperty
	{
		/// <summary>Marks an object that is close enough to a star that its flux could have been influenced.</summary>
		[PropertyDescription(true)]
		public bool StarPolluted;

		/// <summary>Marks an object that has already been paired into a tracklet.</summary>
		[PropertyDescription(true)]
		public bool IsPaired;

		/// <summary>Marks a trailless detection.</summary>
		[PropertyDescription(true)]
		public bool IsDotDetection;

		/// <summary>The Pearson R correlation coefficient of the object's pixels.</summary>
		[PropertyDescription(true)]
		public double PearsonR;
	}
}
