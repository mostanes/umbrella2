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
			string ObsDString = ht["DATE-OBS"].DataString;
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
				int Milliseconds = 0;
				if (DatePieces.Length > 5) Milliseconds = int.Parse(DatePieces[6]);
				tm += new TimeSpan(0, int.Parse(Hour), int.Parse(Minutes), int.Parse(Seconds), Milliseconds);
			}
			else
			{
				if (!ht.ContainsKey("UT")) throw new FormatException("FITS image does not implement UT header");
				double ObsUT = ht["UT"].FloatingPoint;
				tm += TimeSpan.FromHours(ObsUT);
			}
			Time = tm;
			if (!ht.ContainsKey("EXPTIME")) throw new FormatException("FITS image does not implement EXPTIME header");
			double SecLen = ht["EXPTIME"].FloatingPoint;
			Exposure = TimeSpan.FromSeconds(SecLen);
		}

		public override List<ElevatedRecord> GetRecords()
		{
			throw new NotImplementedException();
		}
	}
}
