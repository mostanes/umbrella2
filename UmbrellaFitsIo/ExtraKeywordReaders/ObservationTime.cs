using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.FITS.ElevatedRecord>;

namespace Umbrella2.IO.FITS.KnownKeywords
{
	public class ObservationTime : ImageProperties
	{
		public readonly DateTime Time;
		public readonly TimeSpan Exposure;

		public ObservationTime(FitsImage File) : base(File)
		{
			HeaderTable ht = File.Header;
			if (!ht.ContainsKey("DATE-OBS")) throw new FormatException("FITS image does not implement DATE-OBS header");
			string ObsDString;
			try
			{ ObsDString = ht["DATE-OBS"].GetFixedString; }
			catch { ObsDString = ht["DATE-OBS"].DataString; }
			string TrimmedDate = ObsDString.Trim().Split(' ')[0];
			string[] DatePieces = TrimmedDate.Split('-', 'T', ':');
			string Year = DatePieces[0];
			string Month = DatePieces[1];
			string Day = DatePieces[2];
			bool UTData = DatePieces.Length > 3;
			DateTime tm = new DateTime(int.Parse(Year), int.Parse(Month), int.Parse(Day));
			if (UTData)
			{
				string Hour = DatePieces[3];
				string Minutes = DatePieces[4];
				string Seconds = DatePieces[5];
				tm += ParseHMS(DatePieces[3], DatePieces[4], DatePieces[5]);
			}
			else
			{
				if (!ht.ContainsKey("UT")) throw new FormatException("FITS image does not implement UT header");
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
			if (!ht.ContainsKey("EXPTIME")) throw new FormatException("FITS image does not implement EXPTIME header");
			double SecLen = ht["EXPTIME"].FloatingPoint;
			Exposure = TimeSpan.FromSeconds(SecLen);
		}

		public override List<ElevatedRecord> GetRecords()
		{
			ElevatedRecord DATEOBS = new ElevatedRecord("DATE-OBS", " '" + Time.ToString("o") + "'");
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
