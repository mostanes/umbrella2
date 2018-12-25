using System;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.IO.FITS;
using Umbrella2.IO.FITS.KnownKeywords;
using Umbrella2.WCS;
using static System.Math;

namespace Umbrella2
{
	/// <summary>
	/// Represents a detection on a processed image. Volatile interface.
	/// </summary>
	[Obsolete]
	public class MedianDetection
	{
		public List<EquatorialPoint> EquatorialPoints;
		public List<PixelPoint> PixelPoints;
		public List<double> PixelValues;
		public double Flux;
		public SourceEllipse PixelEllipse;
		public SourceEllipse BarycentricEllipse;
		public PixelPoint BarycenterPP;
		public EquatorialPoint BarycenterEP;
		public ObservationTime Time;
		public double LargestDistance;
		public FitsImage ParentImage;
		public bool StarPolluted;
		public bool IsPaired;
		public bool IsDotDetection;
		public double PearsonR;
		public string Name;

		public MedianDetection(WCSViaProjection Transform, FitsImage Image, List<PixelPoint> Points, List<double> Values)
		{
			this.Time = Image.GetProperty<ObservationTime>();
			ParentImage = Image;
			PixelPoints = Points;
			PixelValues = Values;
			EquatorialPoints = Transform.GetEquatorialPoints(PixelPoints);
			double RAmin = double.MaxValue, RAmax = double.MinValue, Decmin = double.MaxValue, Decmax = double.MinValue;
			foreach (EquatorialPoint eqp in EquatorialPoints)
			{
				if (eqp.RA < RAmin) RAmin = eqp.RA;
				if (eqp.RA > RAmax) RAmax = eqp.RA;
				if (eqp.Dec < Decmin) Decmin = eqp.Dec;
				if (eqp.Dec > Decmax) Decmax = eqp.Dec;
			}

			EquatorialPoint min = new EquatorialPoint() { RA = RAmin, Dec = Decmin };
			EquatorialPoint max = new EquatorialPoint() { RA = RAmax, Dec = Decmax };
			LargestDistance = min ^ max;

			double Xmean = 0, Ymean = 0;
			double XXP = 0, XYP = 0, YYP = 0;
			double XXB = 0, XYB = 0, YYB = 0;
			Flux = 0;
			double XBmean = 0, YBmean = 0;
			for (int i = 0; i < Points.Count; i++)
			{
				PixelPoint pt = Points[i];
				double Val = Values[i];
				Xmean += pt.X; Ymean += pt.Y;
				XBmean += Val * pt.X; YBmean += Val * pt.Y;
				XXB += pt.X * pt.X * Val; XYB += pt.X * pt.Y * Val; YYB += pt.Y * pt.Y * Val;
				XXP += pt.X * pt.X; XYP += pt.X * pt.Y; YYP += pt.Y * pt.Y;
				Flux += Val;
			}
			Xmean /= Points.Count;
			Ymean /= Points.Count;
			XBmean /= Flux;
			YBmean /= Flux;
			XXB /= Flux;
			XYB /= Flux;
			YYB /= Flux;
			XXP /= Points.Count;
			XYP /= Points.Count;
			YYP /= Points.Count;
			XXB -= XBmean * XBmean;
			XYB -= XBmean * YBmean;
			YYB -= YBmean * YBmean;
			XXP -= Xmean * Xmean;
			XYP -= Xmean * Ymean;
			YYP -= Ymean * Ymean;

			BarycenterPP = new PixelPoint() { X = XBmean, Y = YBmean };
			BarycenterEP = Transform.GetEquatorialPoint(BarycenterPP);
			BarycentricEllipse = new SourceEllipse(XXB, XYB, YYB);
			PixelEllipse = new SourceEllipse(XXP, XYP, YYP);
		}

		public override string ToString()
		{
			return "[" + BarycenterPP.ToString() + "]:{" + "Cnt=" + PixelPoints.Count + ", a=" + PixelEllipse.SemiaxisMajor.ToString("G6") + ", b=" + PixelEllipse.SemiaxisMinor.ToString("G6") +
				", uX=" + Cos(PixelEllipse.SemiaxisMajorAngle).ToString("G6") + ", uY=" + Sin(PixelEllipse.SemiaxisMajorAngle).ToString("G6") + "}";
		}
	}

	/// <summary>
	/// Represents an elliptical fit of a source's pixels.
	/// </summary>
	public struct SourceEllipse
	{
		public double SemiaxisMajorAngle;
		public double SemiaxisMajor;
		public double SemiaxisMinor;

		public SourceEllipse(double XX, double XY, double YY)
		{
			double Msq = Sqrt(XX * XX + 4 * XY * XY - 2 * XX * YY + YY * YY);
			double L1 = 1.0 / 2 * (XX + YY - Msq);
			double L2 = 1.0 / 2 * (XX + YY + Msq);
			double A1 = Atan2(2 * XY, -(-XX + YY + Msq));
			double A2 = Atan2(2 * XY, -(-XX + YY - Msq));
			if (L1 > L2) { SemiaxisMajorAngle = A1; SemiaxisMajor = 2 * Sqrt(L1); SemiaxisMinor = 2 * Sqrt(L2); }
			else { SemiaxisMajorAngle = A2; SemiaxisMajor = 2 * Sqrt(L2); SemiaxisMinor = 2 * Sqrt(L1); }
		}

		public override string ToString()
		{
			return "a = " + SemiaxisMajor.ToString("G6") + "; b = " + SemiaxisMinor.ToString("G6");
		}
	}

	/// <summary>
	/// Represents a serializable reference to a MedianDetection.
	/// </summary>
	[Obsolete]
	[Serializable]
	public struct MedianReference
	{
		internal FitsImageReference Image;
		internal PixelPoint[] Pixels;
		internal double[] Values;
		internal bool IsDotDetection;

		public MedianReference(MedianDetection Detection)
		{
			Image = new FitsImageReference(Detection.ParentImage);
			Pixels = Detection.PixelPoints.ToArray();
			Values = Detection.PixelValues.ToArray();
			IsDotDetection = Detection.IsDotDetection;
		}

		public MedianDetection Acquire()
		{
			FitsImage Img = Image.AcquireImage();
			MedianDetection m = new MedianDetection(Img.Transform, Img, Pixels.ToList(), Values.ToList());
			m.IsDotDetection = IsDotDetection;
			return m;
		}
	}
}
