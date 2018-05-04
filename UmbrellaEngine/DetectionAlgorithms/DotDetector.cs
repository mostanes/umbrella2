using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.IO.FITS;
using Umbrella2.IO.FITS.KnownKeywords;

namespace Umbrella2.Algorithms.Detection
{
	public class DotDetector
	{
		public double HighThresholdMultiplier;
		public double LowThresholdMultiplier;
		public double MinPix;
		List<DotDetection> Detections;

		public List<MedianDetection> DetectDots(FitsImage Input, ObservationTime ObservationTime)
		{
			const int ThreadStep = 250;
			const int LineStep = 50;
			Detections = new List<DotDetection>();
			Parallel.For(0, Input.Height / ThreadStep, (x) => SingleImageBlock(Input, (int) x * ThreadStep, LineStep, (int) (x + 1) * ThreadStep));
			if (Input.Height % ThreadStep != 0) SingleImageBlock(Input, (int) (Input.Height - Input.Height % ThreadStep), LineStep, (int) Input.Height);

			List<MedianDetection> Mdect = Detections.Select((x) => new MedianDetection(Input.Transform, Input, x.Pixels, x.PixelValues)).ToList();
			return Mdect;
		}

		void DotDetect(double[,] Input, int OX, int OY)
		{
			int OW = Input.GetLength(1);
			int OH = Input.GetLength(0);
			int i, j, k, l;
			bool[,] Mask = new bool[OH, OW];
			List<DotDetection> ldot = new List<DotDetection>();

			double Mean = 0, Var = 0;
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{ Mean += Input[i, j]; Var += Input[i, j] * Input[i, j]; }
			Mean /= Input.Length;
			Var /= Input.Length;
			Var -= Mean;
			double StDev = Math.Sqrt(Var);
			double HighThreshold = HighThresholdMultiplier * StDev + Mean;
			double LowThreshold = LowThresholdMultiplier * StDev + Mean;

			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					if (Mask[i, j]) continue;
					if (Input[i, j] > HighThreshold)
						ldot.Add(BitmapFill(Input, new IntPoint() { X = j, Y = i }, Mask, LowThreshold, OX, OY));
				}
			ldot.RemoveAll((x) => x.Pixels.Count < MinPix);
			lock (Detections)
				Detections.AddRange(ldot);
		}

		void SingleImageBlock(FitsImage Input, int StartLine, int LineStep, int LEnd)
		{
			ImageData InputData;
			InputData = Input.LockData(new System.Drawing.Rectangle(0, StartLine, (int) Input.Width, LineStep), true);
			int CLine = StartLine;
			for (CLine = StartLine; CLine < LEnd; CLine += LineStep)
			{
				if (CLine != StartLine)
					InputData = Input.SwitchLockData(InputData, 0, CLine, true);

				DotDetect(InputData.Data, 0, CLine);
			}
			Input.ExitLock(InputData);
		}

		internal struct IntPoint { internal int X, Y; }

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
