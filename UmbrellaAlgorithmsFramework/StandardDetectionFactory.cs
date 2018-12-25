using System;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.IO.FITS;
using Umbrella2.IO.FITS.KnownKeywords;
using Umbrella2.PropertyModel.CommonProperties;
using Umbrella2.WCS;

namespace Umbrella2
{
	/// <summary>
	/// A set of standard methods of creating ImageDetections.
	/// </summary>
	public static class StandardDetectionFactory
	{
		/// <summary>
		/// Creates a new ImageDetection from a given image, set of points and values. It also populates it with <see cref="ObjectPhotometry"/>, <see cref="ObjectPoints"/>,
		/// and <see cref="ObjectSize"/> properties.
		/// </summary>
		/// <param name="Image">Image on which the object was detected.</param>
		/// <param name="Points">The set of points on the image where it has been detected.</param>
		/// <param name="Values">The set of pixel intensitities.</param>
		/// <returns>A new instance of ImageDetection with the specified extension properties.</returns>
		public static ImageDetection CreateDetection(FitsImage Image, IEnumerable<PixelPoint> Points, IEnumerable<double> Values)
		{
			WCSViaProjection Transform = Image.Transform;

			PixelPoint[] PixPoints = Points.ToArray();
			double[] PixValues = Values.ToArray();
			EquatorialPoint[] EquatorialPoints = Transform.GetEquatorialPoints(PixPoints);

			double Xmean = 0, Ymean = 0;
			double XXP = 0, XYP = 0, YYP = 0;
			double XXB = 0, XYB = 0, YYB = 0;
			double Flux = 0;
			double XBmean = 0, YBmean = 0;
			for (int i = 0; i < PixPoints.Length; i++)
			{
				PixelPoint pt = PixPoints[i];
				double Val = PixValues[i];
				Xmean += pt.X; Ymean += pt.Y;
				XBmean += Val * pt.X; YBmean += Val * pt.Y;
				XXB += pt.X * pt.X * Val; XYB += pt.X * pt.Y * Val; YYB += pt.Y * pt.Y * Val;
				XXP += pt.X * pt.X; XYP += pt.X * pt.Y; YYP += pt.Y * pt.Y;
				Flux += Val;
			}
			Xmean /= PixPoints.Length; Ymean /= PixPoints.Length;
			XBmean /= Flux; YBmean /= Flux; XXB /= Flux; XYB /= Flux; YYB /= Flux;
			XXP /= PixPoints.Length; XYP /= PixPoints.Length; YYP /= PixPoints.Length;
			XXB -= XBmean * XBmean; XYB -= XBmean * YBmean; YYB -= YBmean * YBmean;
			XXP -= Xmean * Xmean; XYP -= Xmean * Ymean;	YYP -= Ymean * Ymean;

			PixelPoint BarycenterPP = new PixelPoint() { X = XBmean, Y = YBmean };
			EquatorialPoint BarycenterEP = Transform.GetEquatorialPoint(BarycenterPP);
			Position Pos = new Position(BarycenterEP, BarycenterPP);

			SourceEllipse BarycentricEllipse = new SourceEllipse(XXB, XYB, YYB);
			SourceEllipse PixelEllipse = new SourceEllipse(XXP, XYP, YYP);

			ObjectSize Shape = new ObjectSize() { BarycentricEllipse = BarycentricEllipse, PixelEllipse = PixelEllipse };

			ImageDetection Detection = new ImageDetection(Pos, Image.GetProperty<ObservationTime>(), Image);
			Detection.AppendProperty(Shape);
			Detection.AppendProperty(new ObjectPhotometry() { Flux = Flux });
			Detection.AppendProperty(new ObjectPoints() { PixelPoints = PixPoints, PixelValues = PixValues, EquatorialPoints = EquatorialPoints });
			return Detection;
		}

		/// <summary>
		/// Wrapper for the original ImageDetection constructor.
		/// </summary>
		/// <param name="Barycenter">Object barycenter.</param>
		/// <param name="Time">Time at which the object was observed.</param>
		/// <param name="ParentImage">Image on which the object was detected.</param>
		/// <returns></returns>
		public static ImageDetection CreateDetection(Position Barycenter, ObservationTime Time, FitsImage ParentImage) => new ImageDetection(Barycenter, Time, ParentImage);

		/// <summary>
		/// Creates a new ImageDetection at a specified position in an image.
		/// Internally, fetches the intensities from the image at the given position and calls <see cref="CreateDetection(FitsImage, IEnumerable{PixelPoint}, IEnumerable{double})"/>.
		/// </summary>
		/// <param name="Image">Image on which the object was detected.</param>
		/// <param name="Points">The set of pixels at which it has been detected.</param>
		/// <returns>The ImageDetection as from the expanded form.</returns>
		public static ImageDetection CreateDetection(FitsImage Image, IEnumerable<PixelPoint> Points)
		{
			double MinX = double.MaxValue, MaxX = double.MinValue, MinY = double.MaxValue, MaxY = double.MinValue;
			PixelPoint[] PixPoints = Points.ToArray();
			foreach(PixelPoint Point in PixPoints)
			{
				if (Point.X < MinX) MinX = Point.X;
				if (Point.X > MaxX) MaxX = Point.X;
				if (Point.Y < MinY) MinY = Point.Y;
				if (Point.Y > MaxY) MaxY = Point.Y;
			}

			int X = (int) Math.Round(MinX), Y = (int) Math.Round(MinY), W = (int) Math.Round(MaxX - MinX) + 1, H = (int) Math.Round(MaxY - MinY) + 1;
			ImageData Data = Image.LockData(new System.Drawing.Rectangle(X, Y, W, H), false);
			double[] PixValues = new double[PixPoints.Length];
			for (int i = 0; i < PixPoints.Length; i++)
			{
				PixelPoint Point = PixPoints[i];
				int Px = (int) Math.Round(Point.X - X);
				int Py = (int) Math.Round(Point.Y - Y);
				try
				{
					PixValues[i] = Data.Data[Py, Px];
				}
				catch { PixValues[i] = 0; }
			}
			Image.ExitLock(Data);

			return CreateDetection(Image, PixPoints, PixValues);
		}
	}
}
