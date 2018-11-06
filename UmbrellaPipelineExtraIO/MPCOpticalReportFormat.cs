using System;
using System.Text;
using static Umbrella2.Pipeline.ExtraIO.EquatorialPointStringFormatter;

namespace Umbrella2.Pipeline.ExtraIO
{
	/// <summary>
	/// Provides support for working with MPC Optical Reports.
	/// </summary>
	public class MPCOpticalReportFormat
	{
		/// <summary>ASCII space. Defined for convenience.</summary>
		const char MPCSpace = (char) 32;

		/// <summary>
		/// The Publishing Note entry of a record.
		/// </summary>
		public enum PublishingNote : byte
		{
			none = (byte) MPCSpace,
			a = (byte) 'a',
			A = (byte) 'A',
			EarlierApproximatePositionInferior = (byte) 'A',
			SenseOfMotionAmbiguous = (byte) 'a',
			BrightSkyblackOrDarkPlate = (byte) 'B',
			BadSeeing = (byte) 'b',
			CrowdedStarField = (byte) 'c',
			DeclinationUncertain = (byte) 'D',
			DiffuseImage = (byte) 'd',
			AtOrNearEdgeOfPlate = (byte) 'E',
			FaintImage = (byte) 'F',
			InvolvedWithEmulsionOrPlateFlaw = (byte) 'f',
			PoorGuiding = (byte) 'G',
			NoGuiding = (byte) 'g',
			HandMeasurementOfCCDImage = (byte) 'H',
			ObservedThroughCloudhaze = (byte) 'h',
			InvolvedWithStar = (byte) 'I',
			InkdotMeasured = (byte) 'i',
			J2000RereductionOfPreviouslyReportedPosition = (byte) 'J',
			StackedImage = (byte) 'K',
			StaremodeObservationByScanningSystem = (byte) 'k',
			MeasurementDifficult = (byte) 'M',
			ImageTrackedOnObjectMotion = (byte) 'm',
			NearEdgeOfPlateMeasurementUncertain = (byte) 'N',
			ImageOutOfFocus = (byte) 'O',
			PlateMeasuredInOneDirectionOnly = (byte) 'o',
			PositionUncertain = (byte) 'P',
			PoorImage = (byte) 'p',
			RightAscensionUncertain = (byte) 'R',
			PoorDistributionOfReferenceStars = (byte) 'r',
			PoorSky = (byte) 'S',
			StreakedImage = (byte) 's',
			TimeUncertain = (byte) 'T',
			TrailedImage = (byte) 't',
			UncertainImage = (byte) 'U',
			UnconfirmedImage = (byte) 'u',
			VeryFaintImage = (byte) 'V',
			WeakImage = (byte) 'W',
			WeakSolution = (byte) 'w'
		}

		/// <summary>
		/// The magnitude band in which the observations took place.
		/// </summary>
		public enum MagnitudeBand : byte
		{
			none = (byte) MPCSpace,
			R = (byte) 'R',
			V = (byte) 'V'
		}

		/// <summary>
		/// The Note2 entry of a record.
		/// </summary>
		public enum Note2 : byte
		{
			none = (byte) MPCSpace,
			Photographic = (byte) 'P',
			Encoder = (byte) 'e',
			CCD = (byte) 'C',
			MeridianOrTransitCircle = (byte) 'T',
			Micrometer = (byte) 'M',
			CorrectedWithoutRepublicationCCDObservation = (byte) 'c',
			OccultationDerivedObservations = (byte) 'E',
			OffsetObservations = (byte) 'O',
			HipparcosGeocentricObservations = (byte) 'H',
			NormalPlace = (byte) 'N',
			MiniNormalPlaceDerivedFromAveragingObservationsFromVideoFrames = (byte) 'n'
		}
		
		/// <summary>
		/// Instance of an observed object. Provides the object equivalent of a MPC record.
		/// </summary>
		public struct ObsInstance
		{
			public string ObjectDesignation;
			public bool DetectionAsterix;
			public PublishingNote PubNote;
			public Note2 N2;
			public DateTime ObsTime;
			public EquatorialPoint Coordinates;
			public double Mag;
			public MagnitudeBand MagBand;
			public string ObservatoryCode;
		}

		/// <summary>
		/// Creates a MPC record line from a given object observation.
		/// </summary>
		/// <param name="ObservedObject">The object observation instance for which to create the record.</param>
		/// <returns>A string containing the MPC report. Exactly 80 ASCII8 characters long.</returns>
		public static string GenerateLine(ObsInstance ObservedObject)
		{
			StringBuilder Line = new StringBuilder();
			Line.EnsureCapacity(100);
			Line.Length = 80;
			Line[0] = MPCSpace;
			Line[1] = MPCSpace;
			Line[2] = MPCSpace;
			Line[3] = MPCSpace;
			Line[4] = MPCSpace;
			if (ObservedObject.ObjectDesignation.Length != 7)
				throw new InvalidFieldException(InvalidFieldException.FieldType.ObjectDesignation);
			Line.Insert(5, ObservedObject.ObjectDesignation);
			Line[12] = ObservedObject.DetectionAsterix ? '*' : MPCSpace;
			Line[13] = (char) ((byte) ObservedObject.PubNote);
			Line[14] = (char) ((byte) ObservedObject.N2);
			string DateString = ObservedObject.ObsTime.ToString("yyyy MM dd");

			DateString += ObservedObject.ObsTime.TimeOfDay.TotalDays.ToString(".00000");
			Line.Insert(15, DateString);
			Line.Insert(31, MPCSpace);
			Line.Insert(32, ObservedObject.Coordinates.FormatToString(Format.MPC_RA));
			Line.Insert(44, ObservedObject.Coordinates.FormatToString(Format.MPC_Dec));
			int i;
			for (i = 56; i < 65; i++)
				Line[i] = MPCSpace;
			Line.Insert(65, ObservedObject.Mag.ToString("00.0 "));
			Line[70] = (char) ((byte) ObservedObject.MagBand);
			for (i = 71; i < 77; i++)
				Line[i] = MPCSpace;
			if (ObservedObject.ObservatoryCode.Length != 3)
				throw new InvalidFieldException(InvalidFieldException.FieldType.ObservatoryCode);
			Line.Insert(77, ObservedObject.ObservatoryCode);
			return Line.ToString().Substring(0, 80);
		}

		/// <summary>
		/// Represents an invalid ObsInstance field.
		/// </summary>
		public class InvalidFieldException : Exception
		{
			/// <summary>
			/// Represents the fields that could have failed.
			/// </summary>
			public enum FieldType
			{ PublishingNote, MagnitudeBand, Note2, ObjectDesignation, Time, RADEC, Magnitude, ObservatoryCode }

			/// <summary>
			/// The field that failed.
			/// </summary>
			public FieldType ExceptionType { get; }

			public InvalidFieldException(FieldType type) : base("Field " + type.ToString() + " is invalid.")
			{ ExceptionType = type; }
		}
	}
}
