using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.Algorithms.Geometry;
using Umbrella2.IO.FITS;

namespace Umbrella2.Algorithms.Images
{
	public class SegmentDetector
	{
		double IncTh;
		double SegOnTh;
		double SegDropTh;
		double StrongHT;
		double PSFSize = 5;
		double MinFlux = 50;
		double MinIntensity = 10;
		List<Vector> StrongLines;
		List<RLHT.Segment> Segments;
		List<LineAnalyzer.LineDetection> DetectedFasts;

		public SegmentDetector(double IncrementThreshold, double SegmentCreateThreshold, double SegmentDropThreshold)
		{ IncTh = IncrementThreshold; SegOnTh = SegmentCreateThreshold; SegDropTh = SegmentDropThreshold; StrongHT = 1000; DetectedFasts = new List<LineAnalyzer.LineDetection>(); }

		public void GetSegments(FitsImage Input)
		{
			const int ThreadStep = 450;

			StrongLines = new List<Vector>();
			Segments = new List<RLHT.Segment>();

			Parallel.For(0, (Input.Height - 100) / ThreadStep, (x) => SingleImageBlock(Input, (int) x * ThreadStep + 50, (int) (x + 1) * ThreadStep + 50));
			if ((Input.Height - 50) % ThreadStep > 50) SingleImageBlock(Input, (int) ((Input.Height - 100) / ThreadStep * ThreadStep), (int) Input.Height - 50);

			ConsolidateSegments();
		}

		void SingleImageBlock(FitsImage Input, int StartLine, int LEnd)
		{
			ImageData InputData;
			InputData = Input.LockData(new System.Drawing.Rectangle(0, StartLine - 50, 200, 200), true);
			int CLine = StartLine;
			for (CLine = StartLine; CLine < LEnd; CLine += 150)
			{
				for (int j = 50; j + 250 < Input.Width; j+=150)
				{
					if (CLine < 1800 && CLine > 1700 - 150 && j < 350 && j > 250-150)
						;
					InputData = Input.SwitchLockData(InputData, j, CLine - 50, true);
					var w = RLHT.RunRLHT(InputData.Data, IncTh, PSFSize, MinFlux, StrongHT);
					bool[,] Mask = new bool[200, 200];
					
					if (true)
					{
						foreach (Vector vx in w.StrongPoints)
						{
							//var z = RLHT.RefineRLHT(InputData.Data, IncTh, PSFSize, MinFlux, StrongHT, vx.X, vx.Y);
							var z = LineAnalyzer.AnalyzeLine(InputData.Data, Mask, 200, 200, vx.X, vx.Y, SegOnTh, SegDropTh, 20, 5);
							DetectedFasts.AddRange(z);
						}
					}
					/*lock (StrongLines)
					{
						StrongLines.AddRange(w.StrongPoints);
						for (int i = 0; i < w.Segments.Count; i++)
						{
							if (w.Segments[i].Intensity < MinIntensity) continue;
							RLHT.Segment seg = w.Segments[i]; seg.Start.X += j; seg.End.X += j; seg.Start.Y += CLine - 50; seg.End.Y += CLine - 50;
							seg.Radius += CLine * Math.Cos(seg.Angle) - j * Math.Sin(seg.Angle);
							Segments.Add(seg);
						}
					}*/
					;
				}
			}
			Input.ExitLock(InputData);
		}

		void ConsolidateSegments()
		{
			int i, j;
			Comparison<RLHT.Segment> Cpx = (x, y) => x.Angle < y.Angle ? -1 : x.Angle == y.Angle ? 0 : 1;
			Segments.Sort(Cpx);
			;
		}

		double LSDistance(RLHT.Segment A, RLHT.Segment B)
		{
			double ScaleFactor = (200 / 2 * Math.PI) / (2 * PSFSize);
			return (A.Angle - B.Angle) * (PSFSize + (A.Radius - B.Radius)) * ScaleFactor;
		}
	}
}
