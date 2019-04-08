using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.FITS.ElevatedRecord>;

namespace Umbrella2.IO.FITS.KnownKeywords
{
	/// <summary>
	/// Records for specifying the observation time of the frame.
	/// </summary>
	public class ObservationTime : ImageProperties
	{
		/// <summary>Observation time of the image. As given in image fields, but assumed to be since the start of the observation.</summary>
		/// <remarks>There is no standard way of determining whether the Time field refers to the start of the observation.</remarks>
		public readonly DateTime Time;
		/// <summary>Exposure of the image, as given by EXPTIME.</summary>
		public readonly TimeSpan Exposure;

		public ObservationTime(FitsImage File) : base(File)
		{
			HeaderTable ht = File.Header;
			ht.CheckRecord("DATE-OBS");
			string ObsDString;
			try
			{ ObsDString = ht["DATE-OBS"].GetFixedString; }
			catch { ObsDString = ht["DATE-OBS"].DataString; }
			string TrimmedDate = ObsDString.Trim().Split(' ')[0];
			string[] DatePieces = TrimmedDate.Split('-', 'T', ':');
			string Year = DatePieces[0];
			string Month = DatePieces[1];
			string Day = DatePieces[2];
			/* Whether the DATE-OBS includes the time or not */
			bool UTData = DatePieces.Length > 3;
			DateTime tm = new DateTime(int.Parse(Year), int.Parse(Month), int.Parse(Day), 0, 0, 0, DateTimeKind.Utc);
			if (UTData)
			{
				string Hour = DatePieces[3];
				string Minutes = DatePieces[4];
				string Seconds = DatePieces[5];
				tm += ParseHMS(DatePieces[3], DatePieces[4], DatePieces[5]);
			}
			else
			{
				/* No time in DATE-OBS, hence the rest should be in the UT tag. */
				ht.CheckRecord("UT");
				TimeSpan ts;
				try
				{
					double ObsUT = ht["UT"].FloatingPoint;
					ts = TimeSpan.FromHours(ObsUT);
				}
				catch
				{
					string UTVal = ht["UT"].GetFixedString; string[] Vals = UTVal.Trim().Split(':');
					ts = ParseHMS(Vals[0], Vals[1], Vals[2]);
				}
				tm += ts;
			}
			Time = tm;
			ht.CheckRecord("EXPTIME");
			double SecLen = ht["EXPTIME"].FloatingPoint;
			Exposure = TimeSpan.FromSeconds(SecLen);
		}

		public override List<ElevatedRecord> GetRecords()
		{
			ElevatedRecord DATEOBS = new ElevatedRecord("DATE-OBS", " '" + Time.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + "'");
			ElevatedRecord expTime = new ElevatedRecord("EXPTIME", "  " + Exposure.TotalSeconds.ToString("E"));
			return new List<ElevatedRecord>() { DATEOBS, expTime };
		}

		TimeSpan ParseHMS(string Hour, string Minutes, string Seconds)
		{
			double SecondsD = double.Parse(Seconds);
			int SecondsI = (int) SecondsD;
			int Msec = (int) ((SecondsD - SecondsI) * 1000);
			return new TimeSpan(0, int.Parse(Hour), int.Parse(Minutes), SecondsI, Msec);
		}
	}
}
