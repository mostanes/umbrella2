using System;
using System.Collections;
using System.Collections.Generic;
using Umbrella2.IO.FITS;
using Umbrella2.PropertyModel.CommonProperties;
using Umbrella2.WCS;
using static Umbrella2.Algorithms.Images.SchedCore;

namespace Umbrella2.Algorithms.Images
{
	/// <summary>
	/// Class for filtering out static light sources by means of a mask obtained from the median image.
	/// </summary>
	public static class MaskByMedian
	{
		/// <summary>
		/// Masking map.
		/// </summary>
		public static PositionDependentMap<MaskProperties> Masker = MaskImage; 

		/// <summary>
		/// Mask generator.
		/// </summary>
		public static PositionDependentExtractor<MaskProperties> MaskGenerator = GenerateMask;

		/// <summary>
		/// Default parameters for the algorithms. Do switch FillZero to false when creating masks.
		/// </summary>
		public static AlgorithmRunParameters Parameters => new AlgorithmRunParameters()
		{
			FillZero = true,
			InputMargins = 0,
			Xstep = 0,
			Ystep = 50
		};

		/// <summary>
		/// Image masking properties.
		/// </summary>
		public class MaskProperties
		{
			/// <summary>
			/// Upper hysteresis threshold in standard deviations.
			/// </summary>
			public double UTM;
			/// <summary>
			/// Lower hysteresis threshold in standard deviations.
			/// </summary>
			public double LTM;
			/// <summary>
			/// Image standard deviation. Relevant only for median generation.
			/// </summary>
			public double StDev;
			/// <summary>
			/// Image base level. Taken to be mean since most of the pixels in an image are background.
			/// </summary>
			public double Mean;
			/// <summary>
			/// WCS Transform of the mask.
			/// </summary>
			public WCSViaProjection MaskTransform;
			/// <summary>
			/// Mask for the image.
			/// </summary>
			public BitArray[] MaskData;
			/// <summary>
			/// Ratio between light source radius and extra masking circle.
			/// </summary>
			public double MaskRadiusMultiplier;
			/// <summary>
			/// Extra radius (in pixels) to be added to the masking circle.
			/// </summary>
			public double ExtraMaskRadius;
			/// <summary>
			/// Optional list of stars; will be populated by the algorithm if present.
			/// </summary>
			public Umbrella2.Algorithms.Filtering.StarData StarList;
		}

		/// <summary>
		/// Creates a mask from an image. All light sources are detected via a hysteresis algorithm and flagged in the mask.
		/// </summary>
		/// <param name="Input">Input image data.</param>
		/// <param name="Position">Data position in the image.</param>
		/// <param name="Properties">Bag of mask data.</param>
		static void GenerateMask(double[,] Input, ImageSegmentPosition Position, MaskProperties Properties)
		{
			int Width = Input.GetLength(1);
			int Height = Input.GetLength(0);
			int i, j;
			PixelPoint pxp = new PixelPoint();

			/* Compute masking thresholds */
			double UpperThreshold = Properties.UTM * Properties.StDev + Properties.Mean;
			double LowerThreshold = Properties.LTM * Properties.StDev + Properties.Mean;

			for (i = 0; i < Height; i++) for (j = 0; j < Width; j++)
				{
					pxp.X = j + Position.Alignment.X;
					pxp.Y = i + Position.Alignment.Y;

					if (Properties.MaskData[(int) pxp.Y][(int) pxp.X]) continue;

					if (Input[i, j] > UpperThreshold)
					{
						BitmapFill(Properties.MaskData, Input, Position.Alignment, pxp, LowerThreshold, Properties.MaskRadiusMultiplier, Properties.ExtraMaskRadius, out Filtering.Star? Star);
						if (Star != null) { Filtering.Star S = Star.Value; S.EqCenter = Position.WCS.GetEquatorialPoint(S.PixCenter); lock (Properties.StarList) Properties.StarList.FixedStarList.Add(S); }
					}
				}
		}

		/// <summary>
		/// Runs the hysteresis connected component detection algorithm for light sources. At the end also applies the extra circular masking.
		/// </summary>
		/// <param name="Mask">Mask array.</param>
		/// <param name="MaskData">Masking image data.</param>
		/// <param name="Alignment">Position of data in the image.</param>
		/// <param name="DPoint">Starting point for the connected component algorithm.</param>
		/// <param name="LowerThreshold">Lower hysteresis threshold.</param>
		/// <param name="RadiusMultiplier">Ratio between extra masking circle radius and light source radius.</param>
		/// <param name="ExtraRadius">Extra radius for the masking circle.</param>
		/// <param name="Star">The potential output star.</param>
		static void BitmapFill(BitArray[] Mask, double[,] MaskData, PixelPoint Alignment, PixelPoint DPoint, double LowerThreshold, double RadiusMultiplier, double ExtraRadius, out Filtering.Star? Star)
		{
			Queue<PixelPoint> PointQ = new Queue<PixelPoint>();
			PointQ.Enqueue(DPoint);

			double XMean = 0, YMean = 0, XSquare = 0, YSquare = 0, XY = 0;
			int PCount = 0;
			double Flux = 0;

			while (PointQ.Count > 0)
			{
				PixelPoint pt = PointQ.Dequeue();
				if (pt.X < 0 || pt.X >= Mask[0].Length) continue;
				if (pt.Y < 0 || pt.Y >= Mask.Length) continue;

				if (Mask[(int) pt.Y][(int) pt.X]) continue;

				double dX = pt.X - Alignment.X;
				double dY = pt.Y - Alignment.Y;
				dX = Math.Round(dX); dY = Math.Round(dY);
				if (dX < 0 || dX >= MaskData.GetLength(1)) continue;
				if (dY < 0 || dY >= MaskData.GetLength(0)) continue;

				if (MaskData[(int) dY, (int) dX] > LowerThreshold)
				{
					Mask[(int) pt.Y][(int) pt.X] = true;
					PointQ.Enqueue(new PixelPoint() { X = pt.X - 1, Y = pt.Y });
					PointQ.Enqueue(new PixelPoint() { X = pt.X + 1, Y = pt.Y });
					PointQ.Enqueue(new PixelPoint() { X = pt.X, Y = pt.Y - 1 });
					PointQ.Enqueue(new PixelPoint() { X = pt.X, Y = pt.Y + 1 });
					XMean += pt.X; YMean += pt.Y;
					XSquare += pt.X * pt.X; YSquare += pt.Y * pt.Y;
					XY += pt.X * pt.Y;
					PCount++;
					Flux += MaskData[(int) dY, (int) dX];
				}
			}

			/* Computes size of and shape of the light source */
			XMean /= PCount;
			YMean /= PCount;
			XSquare /= PCount;
			YSquare /= PCount;
			XY /= PCount;
			XSquare -= XMean * XMean;
			YSquare -= YMean * YMean;
			XY -= XMean * YMean;

			double Radius = Math.Sqrt(XSquare + YSquare);
			SourceEllipse Shape = new SourceEllipse(XSquare, XY, YSquare);

			/* If not to irregular, suppose it is a star and apply extra masking. */
			if (Shape.SemiaxisMajor < 3 * Shape.SemiaxisMinor)
			{
				FillMarginsExtra(Mask, new PixelPoint() { X = XMean, Y = YMean }, Radius * RadiusMultiplier + ExtraRadius);
				Star = new Filtering.Star() { Shape = Shape, PixCenter = new PixelPoint() { X = XMean, Y = YMean }, PixRadius = Radius };
			}
			else Star = null;
		}

		/// <summary>
		/// Appends circular mask to the mask data.
		/// </summary>
		/// <param name="Mask">Mask data.</param>
		/// <param name="Center">Disk center.</param>
		/// <param name="Radius">Disk radius.</param>
		static void FillMarginsExtra(BitArray[] Mask, PixelPoint Center, double Radius)
		{
			int i, j;
			int StartX = (int) Math.Round(Center.X - Radius), EndX = (int) Math.Round(Center.X + Radius);
			int StartY = (int) Math.Round(Center.Y - Radius), EndY = (int) Math.Round(Center.Y + Radius);
			if (StartX < 0) StartX = 0; if (StartY < 0) StartY = 0;
			if (EndX >= Mask[0].Length) EndX = Mask[0].Length - 1;
			if (EndY >= Mask.Length) EndY = Mask.Length - 1;
			for (i = StartY; i <= EndY; i++) for (j = StartX; j <= EndX; j++)
					if ((i - Center.Y) * (i - Center.Y) + (j - Center.X) * (j - Center.X) < Radius * Radius)
						Mask[i][j] = true;
		}

		/// <summary>
		/// Masks the input image with a given mask. Masked pixels are set to -1 standard deviation.
		/// </summary>
		/// <param name="Input">Input image data.</param>
		/// <param name="Output">Output image data.</param>
		/// <param name="InputPosition">Input data position.</param>
		/// <param name="OutputPosition">Output data position.</param>
		/// <param name="Properties">Mask data.</param>
		static void MaskImage(double[,] Input, double[,] Output, ImageSegmentPosition InputPosition, ImageSegmentPosition OutputPosition, MaskProperties Properties)
		{
			int Width = Output.GetLength(1);
			int Height = Output.GetLength(0);
			int i, j;

			PixelPoint pxp = new PixelPoint();

			for (i = 0; i < Height; i++) for (j = 0; j < Width; j++)
				{
					pxp.X = j + OutputPosition.Alignment.X; pxp.Y = i + OutputPosition.Alignment.Y;
					EquatorialPoint ep = OutputPosition.WCS.GetEquatorialPoint(pxp);
					PixelPoint mpt = Properties.MaskTransform.GetPixelPoint(ep);
					mpt.X = Math.Round(mpt.X); mpt.Y = Math.Round(mpt.Y);
					if (mpt.X < 0 || mpt.X >= Properties.MaskData[0].Length) continue;
					if (mpt.Y < 0 || mpt.Y >= Properties.MaskData.Length) continue;
					PixelPoint ipt = InputPosition.WCS.GetPixelPoint(ep);
					ipt.X = Math.Round(ipt.X - InputPosition.Alignment.X); ipt.Y = Math.Round(ipt.Y - InputPosition.Alignment.Y);

					if (Properties.MaskData[(int) mpt.Y][(int) mpt.X]) Output[i, j] = -Properties.StDev;
					else Output[i, j] = Input[(int) ipt.Y, (int) ipt.X] - Properties.Mean;
				}
		}

		/// <summary>
		/// Masks an image.
		/// </summary>
		/// <param name="Input">Input image.</param>
		/// <param name="Output">Output image.</param>
		/// <param name="Properties">Masking data.</param>
		public static void MaskImage(FitsImage Input, FitsImage Output, MaskProperties Properties)
		{ Masker.Run(Properties, Input, Output, Parameters); }

		/// <summary>
		/// Creates a mask from a given image.
		/// The image is scanned for light sources which are then converted to a mask.
		/// </summary>
		/// <param name="Input">Input image.</param>
		/// <param name="Properties">Masking data and parameters. The masking thresholds should be set; the other parameters are automatically filled in.</param>
		/// <param name="Stats">Image statistical information.</param>
		public static void CreateMasker(FitsImage Input, MaskProperties Properties, ImageStatistics Stats)
		{
			Properties.Mean = Stats.ZeroLevel; Properties.StDev = Stats.StDev;
			Properties.MaskData = new BitArray[Input.Height]; Properties.MaskTransform = Input.Transform;
			for (int i = 0; i < Input.Height; i++) Properties.MaskData[i] = new BitArray((int) Input.Width);
			var p = Parameters; p.FillZero = false;
			MaskGenerator.Run(Properties, Input, p);
		}
	}
}
