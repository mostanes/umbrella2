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

#pragma warning disable 1591
		/// <summary>
		/// The Publishing Note entry of a record. See the <a href="https://www.minorplanetcenter.net/iau/info/ObsNote.html">MPC publishing note entry</a>
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
		/// The magnitude band in which the observations took place. See <a href="https://www.minorplanetcenter.net/iau/info/OpticalObs.html">MPC optical report format</a>.
		/// </summary>
		/// <remarks>
		/// This list of magnitude bands is not complete.
		/// </remarks>
		public enum MagnitudeBand : byte
		{
			none = (byte) MPCSpace,
			R = (byte) 'R',
			V = (byte) 'V'
		}

		/// <summary>
		/// The Note2 entry of a record. See <a href="https://www.minorplanetcenter.net/iau/info/OpticalObs.html">MPC optical report format</a>.
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
		/// Instance of an observed object. Provides the object equivalent of a <a href="https://www.minorplanetcenter.net/iau/info/OpticalObs.html">MPC optical report format</a> record.
		/// </summary>
		public struct ObsInstance
		{
			public string ObjectDesignation;
			public bool DetectionAsterisk;
			public PublishingNote PubNote;
			public Note2 N2;
			public DateTime? ObsTime;
			public EquatorialPoint? Coordinates;
			public double? Mag;
			public MagnitudeBand MagBand;
			public string ObservatoryCode;
		}
#pragma warning restore 1591

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

			if (ObservedObject.ObjectDesignation == null)
				ObservedObject.ObjectDesignation = new string(' ', 7);
			if (ObservedObject.ObjectDesignation.Length != 7)
				throw new InvalidFieldException(InvalidFieldException.FieldType.ObjectDesignation);
			Line.Insert(5, ObservedObject.ObjectDesignation);

			Line[12] = ObservedObject.DetectionAsterisk ? '*' : MPCSpace;
			Line[13] = (char) ((byte) ObservedObject.PubNote);
			Line[14] = (char) ((byte) ObservedObject.N2);

			string DateString = new string(' ', 16);
			if (ObservedObject.ObsTime.HasValue)
			{
				DateString = ObservedObject.ObsTime.Value.ToString("yyyy MM dd");
				DateString += ObservedObject.ObsTime.Value.TimeOfDay.TotalDays.ToString(".00000");
			}

			Line.Insert(15, DateString);
			Line.Insert(31, MPCSpace);
			if (ObservedObject.Coordinates.HasValue)
			{
				Line.Insert(32, ObservedObject.Coordinates.Value.FormatToString(Format.MPC_RA));
				Line.Insert(44, ObservedObject.Coordinates.Value.FormatToString(Format.MPC_Dec));
			}
			else Line.Insert(32, new string(' ', 24));

			int i;
			for (i = 56; i < 65; i++)
				Line[i] = MPCSpace;

			if (ObservedObject.Mag.HasValue)
				Line.Insert(65, ObservedObject.Mag.Value.ToString("00.0 "));
			else Line.Insert(65, new string(' ', 5));

			Line[70] = (char) ((byte) ObservedObject.MagBand);
			for (i = 71; i < 77; i++)
				Line[i] = MPCSpace;

			if (ObservedObject.ObservatoryCode == null) ObservedObject.ObservatoryCode = new string(' ', 3);
			if (ObservedObject.ObservatoryCode.Length != 3)
				throw new InvalidFieldException(InvalidFieldException.FieldType.ObservatoryCode);
			Line.Insert(77, ObservedObject.ObservatoryCode);

			return Line.ToString().Substring(0, 80);
		}

		/// <summary>
		/// Parses a MPC record line.
		/// </summary>
		/// <param name="Line">A string containing the MPC report. Must be exactly 80 ASCII8 characters long.</param>
		/// <returns>An object observation instance.</returns>
		public static ObsInstance ParseLine(string Line)
		{
			if (Line.Length != 80)
				throw new ArgumentException("The line is too short. Expecting 80-character lines.", nameof(Line));

			ObsInstance instance = new ObsInstance();
			string Designation = Line.Substring(5, 7);
			if (string.IsNullOrWhiteSpace(Designation))
				instance.ObjectDesignation = null;
			else
				instance.ObjectDesignation = Designation;

			switch(Line[12])
			{
				case '*': instance.DetectionAsterisk = true; break;
				case MPCSpace: instance.DetectionAsterisk = false; break;
				default: throw new InvalidFieldException(InvalidFieldException.FieldType.DetectionAsterisk);
			}

			byte PubNote = (byte)Line[13];
			byte N2 = (byte)Line[14];
			instance.PubNote = (PublishingNote)PubNote;
			instance.N2 = (Note2)N2;

			string Date = Line.Substring(15, 10);
			string Time = Line.Substring(25, 6);
			var IC = System.Globalization.CultureInfo.InvariantCulture;
			if (string.IsNullOrWhiteSpace(Date) | string.IsNullOrWhiteSpace(Time))
				instance.ObsTime = null;
			else
			{
				try
				{
					DateTime dt = DateTime.ParseExact(Date, "yyyy MM dd", IC);
					double tval = double.Parse(Time, System.Globalization.NumberStyles.AllowDecimalPoint);
					instance.ObsTime = dt.AddDays(tval);
				}
				catch (Exception ex) { throw new InvalidFieldException(InvalidFieldException.FieldType.ObsTime); }
			}

			string RA = Line.Substring(32, 12);
			string Dec = Line.Substring(44, 12);
			if (string.IsNullOrWhiteSpace(RA) | string.IsNullOrWhiteSpace(Dec))
				instance.Coordinates = null;
			else
			{
				try
				{ EquatorialPoint eqp = ParseFromMPCString(RA + " " + Dec); instance.Coordinates = eqp; }
				catch (Exception ex) { throw new InvalidFieldException(InvalidFieldException.FieldType.Coordinates); }
			}

			string Mag = Line.Substring(65, 4);
			if (string.IsNullOrWhiteSpace(Mag))
				instance.Mag = null;
			else
			{
				if (double.TryParse(Mag, out double M))
					instance.Mag = M;
				else throw new InvalidFieldException(InvalidFieldException.FieldType.Magnitude);
			}

			byte MagBand = (byte)Line[70];
			instance.MagBand = (MagnitudeBand)MagBand;

			string ObsCode = Line.Substring(77, 3);
			if (string.IsNullOrWhiteSpace(ObsCode))
				instance.ObservatoryCode = null;
			else instance.ObservatoryCode = ObsCode;

			return instance;
		}

#pragma warning disable 1591
		/// <summary>
		/// Represents an invalid ObsInstance field.
		/// </summary>
		public class InvalidFieldException : Exception
		{
			/// <summary>
			/// Represents the fields that could have failed.
			/// </summary>
			public enum FieldType
			{ PublishingNote, MagnitudeBand, Note2, ObjectDesignation, Time, RADEC, Magnitude, ObservatoryCode, DetectionAsterisk, ObsTime, Coordinates }

			/// <summary>
			/// The field that failed.
			/// </summary>
			public FieldType ExceptionType { get; }

			public InvalidFieldException(FieldType type) : base("Field " + type.ToString() + " is invalid.")
			{ ExceptionType = type; }
		}
#pragma warning restore 1591
	}
}
