using System.Collections.Generic;
using System.Linq;
using Umbrella2.IO.FITS;
using Umbrella2.IO.FITS.KnownKeywords;
using Umbrella2.PropertyModel.CommonProperties;
using static System.Math;
using static Umbrella2.Algorithms.Images.SchedCore;

namespace Umbrella2.Algorithms.Detection
{
	/// <summary>
	/// Connected component hysteresis algorithm for light source detection.
	/// </summary>
	public class DotDetector
	{
		/// <summary>
		/// Upper hysteresis in (local) standard deviations.
		/// </summary>
		public double HighThresholdMultiplier;
		/// <summary>
		/// Lower hysteresis in (local) standard deviations.
		/// </summary>
		public double LowThresholdMultiplier;
		/// <summary>
		/// Minimum number of pixels to consider a detection.
		/// </summary>
		public double MinPix;
		/// <summary>
		/// The maximum value a pixel can take before being excluded from the local mean.
		/// </summary>
		public double NonrepresentativeThreshold;
		/// <summary>
		/// List of unprocessed detections.
		/// </summary>
		List<DotDetection> Detections;
		/// <summary>
		/// ParallelAlgorithm options.
		/// </summary>
		public AlgorithmRunParameters Parameters = new AlgorithmRunParameters()
		{
			FillZero = true,
			InputMargins = 0,
			Xstep = 0,
			Ystep = 50
		};

		/// <summary>
		/// Detects trailless light sources on the input image.
		/// </summary>
		/// <param name="Input">Input image.</param>
		/// <param name="ObservationTime">Input image time of observation.</param>
		/// <returns>A list of detections.</returns>
		public List<ImageDetection> DetectDots(FitsImage Input, ObservationTime ObservationTime)
		{
			Detections = new List<DotDetection>();
			PositionDependentExtractor<DotDetector> Extractor = DetectSources;

			Extractor.Run(this, Input, Parameters);

			List<ImageDetection> Mdect = Detections.Select((x) => StandardDetectionFactory.CreateDetection(Input, x.Pixels, x.PixelValues)).ToList();
			foreach (ImageDetection m in Mdect) m.SetResetProperty(new PairingProperties() { IsDotDetection = true });
			return Mdect;
		}

		/// <summary>
		/// Actual detection function for trailless light sources.
		/// </summary>
		/// <param name="Input">Input image data.</param>
		/// <param name="Position">Data position in the image.</param>
		/// <param name="Instance">DotDetector instance in which this is called.</param>
		static void DetectSources(double[,] Input, ImageSegmentPosition Position, DotDetector Instance)
		{
			int OW = Input.GetLength(1);
			int OH = Input.GetLength(0);
			int i, j;
			bool[,] Mask = new bool[OH, OW];
			List<DotDetection> ldot = new List<DotDetection>();

			/* Local mean & variance computation */
			double Mean = 0, Var = 0;
			double NThSq = Instance.NonrepresentativeThreshold * Instance.NonrepresentativeThreshold;
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
					if (Input[i, j] * Input[i, j] < NThSq)
					{ Mean += Input[i, j]; Var += Input[i, j] * Input[i, j]; }
			Mean /= Input.Length;
			Var /= Input.Length;
			Var -= Mean;
			double StDev = Sqrt(Var);
			double HighThreshold = Instance.HighThresholdMultiplier * StDev + Mean;
			double LowThreshold = Instance.LowThresholdMultiplier * StDev + Mean;

			/* Hysteresis-based detection */
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					if (Mask[i, j]) continue;
					if (Input[i, j] > HighThreshold)
						ldot.Add(BitmapFill(Input, new IntPoint() { X = j, Y = i }, Mask, LowThreshold, (int) Position.Alignment.X, (int) Position.Alignment.Y));
				}
			ldot.RemoveAll((x) => x.Pixels.Count < Instance.MinPix);
			lock (Instance.Detections)
				Instance.Detections.AddRange(ldot);
		}

		/// <summary>
		/// Integer lattice point.
		/// </summary>
		internal struct IntPoint { internal int X, Y; }

		/// <summary>
		/// Holds the data of a light source.
		/// </summary>
		internal struct DotDetection
		{
			internal PixelPoint Barycenter;
			internal List<PixelPoint> Pixels;
			internal List<double> PixelValues;
			internal PixelPoint PixelCenter;
			internal double Flux;

			public override string ToString()
			{
				return "[DD]{" + Barycenter.ToString() + "}";
			}
		}

		/// <summary>
		/// Connected component / bitmap fill function.
		/// </summary>
		/// <param name="Input">Image input</param>
		/// <param name="StartPoint">Starting coordinates.</param>
		/// <param name="Mask">Mask of visited coordinates.</param>
		/// <param name="LowThreshold">Lower hysteresis threshold.</param>
		/// <param name="OX">X origin delta.</param>
		/// <param name="OY">Y origin delta.</param>
		/// <returns></returns>
		static DotDetection BitmapFill(double[,] Input, IntPoint StartPoint, bool[,] Mask, double LowThreshold, int OX, int OY)
		{
			Queue<IntPoint> PointQ = new Queue<IntPoint>();
			PointQ.Enqueue(StartPoint);
			List<PixelPoint> DiscoveredPoints = new List<PixelPoint>();
			List<double> PValues = new List<double>();
			double Xmean = 0, Ymean = 0, XBmean = 0, YBmean = 0, Flux = 0;

			while (PointQ.Count > 0)
			{
				IntPoint pt = PointQ.Dequeue();
				if (pt.X < 0 || pt.X >= Mask.GetLength(1)) continue;
				if (pt.Y < 0 || pt.Y >= Mask.GetLength(0)) continue;
				if (Mask[pt.Y, pt.X]) continue;
				double Val = Input[pt.Y, pt.X];
				if (Val > LowThreshold)
				{
					Mask[pt.Y, pt.X] = true;
					Xmean += pt.X; Ymean += pt.Y; XBmean += Val * pt.X; YBmean += Val * pt.Y;
					Flux += Val;
					DiscoveredPoints.Add(new PixelPoint() { X = pt.X + OX, Y = pt.Y + OY });
					PValues.Add(Val);
					PointQ.Enqueue(new IntPoint() { X = pt.X - 1, Y = pt.Y });
					PointQ.Enqueue(new IntPoint() { X = pt.X + 1, Y = pt.Y });
					PointQ.Enqueue(new IntPoint() { X = pt.X, Y = pt.Y - 1 });
					PointQ.Enqueue(new IntPoint() { X = pt.X, Y = pt.Y + 1 });
				}
			}
			Xmean = Xmean / DiscoveredPoints.Count + OX;
			Ymean = Ymean / DiscoveredPoints.Count + OY;
			XBmean = XBmean / Flux + OX;
			YBmean = YBmean / Flux + OY;

			return new DotDetection()
			{
				Barycenter = new PixelPoint() { X = XBmean, Y = YBmean },
				Flux = Flux,
				PixelCenter = new PixelPoint() { X = Xmean, Y = Ymean },
				Pixels = DiscoveredPoints,
				PixelValues = PValues
			};
		}
	}
}
