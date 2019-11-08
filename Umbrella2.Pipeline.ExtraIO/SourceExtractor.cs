using System;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.IO.FITS;
using Umbrella2.IO.FITS.KnownKeywords;
using Umbrella2.PropertyModel.CommonProperties;

namespace Umbrella2.Pipeline.ExtraIO
{
	public static class SourceExtractor
	{
		struct ObsEntry
		{
			public double? Flux;
			public double? Mag;
			public double X;
			public double Y;
			public double RA;
			public double Dec;
			public double? FWHM;
			public double? Ellipticity;
			public double? A;
			public double? B;
			public double? EllipseTheta;
		}

		/// <summary>
		/// Parses a Source Extractor catalog file.
		/// </summary>
		/// <returns>The detections in the catalog.</returns>
		/// <param name="Lines">Catalog file lines.</param>
		/// <param name="AssociatedImage">Image to which the catalog is associated to.</param>
		public static List<ImageDetection> ParseSEFile(IEnumerable<string> Lines, FitsImage AssociatedImage)
		{
			List<string> ColList = new List<string>();
			Dictionary<string, int> Columns = new Dictionary<string, int>();
			List<ObsEntry> Entries = new List<ObsEntry>();
			foreach(string Line in Lines)
			{
				if(Line[0] == '#')
				{
					string[] Hdata = Line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
					if (Hdata[0] != "#")
						throw new FormatException("Cannot understand SE file");
					int CNum = int.Parse(Hdata[1]) - 1;
					string CName = Hdata[2];
					ColList.Add(CName);
					Columns.Add(CName, CNum);
				}
				else
				{
					string[] Ldata = Line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
					ObsEntry Entry = new ObsEntry();
					Entry.Flux = Parse(Columns, Ldata, "FLUX_AUTO");
					Entry.Mag = Parse(Columns, Ldata, "MAG_AUTO");
					Entry.X = Parse(Columns, Ldata, "X_IMAGE").Value;
					Entry.Y = Parse(Columns, Ldata, "Y_IMAGE").Value;
					Entry.RA = Parse(Columns, Ldata, "ALPHA_J2000").Value;
					Entry.Dec = Parse(Columns, Ldata, "DELTA_J2000").Value;
					Entry.FWHM = Parse(Columns, Ldata, "FWHM_IMAGE");
					Entry.Ellipticity = Parse(Columns, Ldata, "ELLIPTICITY");
					Entry.A = Parse(Columns, Ldata, "A_IMAGE");
					Entry.B = Parse(Columns, Ldata, "B_IMAGE");

					Entries.Add(Entry);
				}
			}
			List<ImageDetection> Detections = Entries.Select((x) => Transform(x, AssociatedImage)).ToList();
			return Detections;
		}

		static double? Parse(Dictionary<string, int> Columns, string[] Data, string Value)
		{
			if (!Columns.ContainsKey(Value)) return null;
			string V = Data[Columns[Value]];
			return double.Parse(V);
		}

		static ImageDetection Transform(ObsEntry Entry, FitsImage AssociatedImage)
		{
			EquatorialPoint eqp = new EquatorialPoint() { RA = Entry.RA / 180 * Math.PI, Dec = Entry.Dec / 180 * Math.PI };
			PixelPoint pp = new PixelPoint() { X = Entry.X, Y = Entry.Y };
			Position p = new Position(eqp, pp);
			ImageDetection det = new ImageDetection(p, AssociatedImage.GetProperty<ObservationTime>(), AssociatedImage);

			bool Ellipse = false;
			ObjectSize sz = new ObjectSize();
			if(Entry.A.HasValue && Entry.B.HasValue)
			{
				sz.PixelEllipse = new SourceEllipse() { SemiaxisMajor = Entry.A.Value, SemiaxisMinor = Entry.B.Value };
				Ellipse = true;
			}
			else if(Entry.FWHM.HasValue && Entry.Ellipticity.HasValue)
			{
				sz.PixelEllipse = new SourceEllipse()
				{
					SemiaxisMajor = Entry.FWHM.Value / Math.Sqrt(Entry.Ellipticity.Value),
					SemiaxisMinor = Entry.FWHM.Value * Math.Sqrt(Entry.Ellipticity.Value)
				};
				Ellipse = true;
			}
			if(Ellipse)
			{
				if (Entry.EllipseTheta.HasValue)
					sz.PixelEllipse.SemiaxisMajorAngle = Entry.EllipseTheta.Value / 180 * Math.PI;
				det.AppendProperty(sz);
			}

			PairingProperties pprop = new PairingProperties()
			{
				IsDotDetection = Ellipse && (sz.PixelEllipse.SemiaxisMajor < 2 * sz.PixelEllipse.SemiaxisMinor),
				IsPaired = false,
				PearsonR = 0,
				StarPolluted = false,
				Algorithm = DetectionAlgorithm.SourceExtractor
			};
			det.AppendProperty(pprop);
			if (Entry.Flux.HasValue)
			{
				ObjectPhotometry oph = new ObjectPhotometry() { Flux = Entry.Flux.Value, Magnitude = Entry.Mag.Value };
				det.AppendProperty(oph);
			}

			return det;
		}
	}
}
