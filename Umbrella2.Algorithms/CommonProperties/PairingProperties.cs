using System;
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

		/// <summary>Which algorithm(s) produced the detection.</summary>
		[PropertyDescription(true)]
		public DetectionAlgorithm Algorithm;

		/// <summary>If <see langword="true"/>, detection has multiple detections with no <see cref="ObjectPoints"/>.</summary>
		[PropertyDescription(true)]
		public bool MultiNoPoints;
	}

	/// <summary>
	/// Detection algorithms used.
	/// </summary>
	[Flags]
	public enum DetectionAlgorithm : uint
	{
		/// <summary>Unknown status.</summary>
		Unknown = 0,
		/// <summary>Umbrella-builtin blob algorithm.</summary>
		Blob = 1,
		/// <summary>Umbrella-builtin long trails algorithm.</summary>
		Trail = 2,
		/// <summary>Imported from AstrOmatic Source Extractor.</summary>
		SourceExtractor = 4
	}
}
