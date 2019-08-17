using System;

namespace Umbrella2.Pipeline.ExtraIO
{
	/// <summary>
	/// Converts EquatorialPoints to strings and back.
	/// </summary>
	public static class EquatorialPointStringFormatter
	{
		/// <summary>
		/// Format to output string in.
		/// </summary>
		public enum Format
		{
			/// <summary>
			/// MPC standard format.
			/// </summary>
			MPC,
			/// <summary>
			/// MPC Right Ascension only.
			/// </summary>
			MPC_RA,
			/// <summary>
			/// MPC Declination only.
			/// </summary>
			MPC_Dec,
			/// <summary>
			/// Radians, space separated.
			/// </summary>
			RadSpace,
			/// <summary>
			/// Radians, explicit RA/Dec.
			/// </summary>
			RadExplicit,
			/// <summary>
			/// MPC, but tab-separated.
			/// </summary>
			MPC_Tab
		}

		/// <summary>
		/// Formats point to string.
		/// </summary>
		/// <param name="Point">Point to apply to.</param>
		/// <param name="OutputFormat">String output format.</param>
		/// <returns>A formatted string containing the coordinates of the input point.</returns>
		public static string FormatToString(this EquatorialPoint Point, Format OutputFormat)
		{
			switch (OutputFormat)
			{
				case Format.MPC:
					return RASexa(Point.RA) + " " + DecSexa(Point.Dec);
				case Format.MPC_RA:
					return RASexa(Point.RA);
				case Format.MPC_Dec:
					return DecSexa(Point.Dec);
				case Format.MPC_Tab:
					return RASexa(Point.RA) + "\t" + DecSexa(Point.Dec);
				case Format.RadSpace:
					return Point.RA.ToString() + " " + Point.Dec.ToString();
				case Format.RadExplicit:
					return "RA=" + Point.RA.ToString() + " " + "Dec=" + Point.Dec.ToString();
				default:
					throw new ArgumentOutOfRangeException("OutputFormat");
			}
		}

		/// <summary>
		/// Parses a MPC string into an EquatorialPoint.
		/// </summary>
		/// <param name="Point">MPC-style coordinate string.</param>
		/// <returns>EquatorialPoint with specified coordinates.</returns>
		public static EquatorialPoint ParseFromMPCString(string Point)
		{
			EquatorialPoint eqp = new EquatorialPoint();
			string[] Components = Point.Split(' ', ':');
			if (Components.Length != 6)
				throw new FormatException();
			eqp.RA = Math.PI * ((double.Parse(Components[2]) / 60 + double.Parse(Components[1])) / 60 + double.Parse(Components[0])) / 12;
			if (Components[3][0] != '-') eqp.Dec = Math.PI * ((double.Parse(Components[5]) / 60 + double.Parse(Components[4])) / 60 + double.Parse(Components[3])) / 180;
			else eqp.Dec = Math.PI * (double.Parse(Components[3]) - (double.Parse(Components[5]) / 60 + double.Parse(Components[4])) / 60) / 180;
			return eqp;
		}

		/// <summary>
		/// Computes the 24h sexagesimal format of the Right Ascension.
		/// </summary>
		/// <param name="a">Right Ascension (in radians).</param>
		/// <returns>A string containing the 24h format of the input.</returns>
		static string RASexa(double a)
		{
			double raU, raS;
			int raH, raM;
			raU = (a * 12 / Math.PI);
			raH = (int) (Math.Floor(raU));
			raU = (raU - raH) * 60;
			raM = (int) (Math.Floor(raU));
			raS = (raU - raM) * 60;
			return raH.ToString("00 ") + raM.ToString("00 ") + raS.ToString("00.00 ");
		}

		/// <summary>
		/// Computes the 180 degrees sexagesimal format of the Declination.
		/// </summary>
		/// <param name="a">Declination (in radians).</param>
		/// <returns>A string containing the 180 degree format of the input.</returns>
		static string DecSexa(double a)
		{
			double decU, decS;
			int decD, decM;
			decU = (Math.Abs(a) * 180 / Math.PI);
			decD = (int) (Math.Floor(decU));
			decU = (decU - decD) * 60;
			decM = (int) (Math.Floor(decU));
			decS = (decU - decM) * 60;
			return ((a > 0 ? "+" : "-") + decD.ToString("00 ") + decM.ToString("00 ") + decS.ToString("00.0 "));
		}
	}
}
