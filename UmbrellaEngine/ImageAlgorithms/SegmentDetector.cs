using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.Algorithms.Geometry;
using Umbrella2.IO.FITS;
using Umbrella2.IO.FITS.KnownKeywords;

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
		double LowSeparation = 12;
		double AngleEqual = 0.3;
		List<Vector> StrongLines;
		List<LineAnalyzer.LineDetection> DetectedFasts;
		List<List<PixelPoint>> DetectedPP;
		List<List<double>> DetectedPV;

		public SegmentDetector(double IncrementThreshold, double SegmentCreateThreshold, double SegmentDropThreshold)
		{
			IncTh = IncrementThreshold; SegOnTh = SegmentCreateThreshold; SegDropTh = SegmentDropThreshold; StrongHT = 1200; DetectedFasts = new List<LineAnalyzer.LineDetection>();
			
		}

		public List<MedianDetection> GetLongTrails(FitsImage Input)
		{
			const int ThreadStep = 450;

			StrongLines = new List<Vector>();
			DetectedPP = new List<List<PixelPoint>>(); DetectedPV = new List<List<double>>();

			Parallel.For(0, (Input.Height - 100) / ThreadStep, (x) => SingleImageBlock(Input, (int) x * ThreadStep + 50, (int) (x + 1) * ThreadStep + 50));
			if ((Input.Height - 50) % ThreadStep > 50) SingleImageBlock(Input, (int) ((Input.Height - 100) / ThreadStep * ThreadStep), (int) Input.Height - 50);

			return GetMedetect(Input, Input);
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
					//var w = RLHT.SkimRLHT(InputData.Data, IncTh, StrongHT, 4);
					var w = RLHT.SmartSkipRLHT(InputData.Data, IncTh, StrongHT, 4);
					bool[,] Mask = new bool[200, 200];

					if (true)
					{
						
						List<LineAnalyzer.LineDetection> IntermediateList = new List<LineAnalyzer.LineDetection>();
						
						foreach (Vector vx in w.StrongPoints)
						{
							//var RefinedSP = RLHT.RefinedRLHT(InputData.Data, IncTh, PSFSize, MinFlux, StrongHT, vx.X, vx.Y);
							if (CLine < 1800 && CLine > 1700 - 150 && j < 350 && j > 250 - 150)
								;
							var z = LineAnalyzer.AnalyzeLine(InputData.Data, Mask, 200, 200, vx.X, vx.Y, SegOnTh, SegDropTh, 40, 10, j, CLine - 50);
							IntermediateList.AddRange(z);
						}
						if (CLine < 1800 && CLine > 1700 - 150 && j < 350 && j > 250 - 150)
							;
						int[] Tags = new int[IntermediateList.Count];
						for (int k = 0; k < IntermediateList.Count; k++) Tags[k] = k;
						bool TagsMod = true;
						while (TagsMod)
						{
							TagsMod = false;
							for (int k = 0; k < IntermediateList.Count; k++)
								for (int l = 0; l < IntermediateList.Count; l++)
								{
									if (k == l) continue;
									double dangle1 = IntermediateList[k].EigenAngle1 - IntermediateList[l].EigenAngle1;
									double dangle2 = Math.IEEERemainder(dangle1, Math.PI);
									double dangle = Math.Abs(dangle2);
									double EDiMult = AngleEqual / (AngleEqual + dangle);
									double Mx1 = Math.Max(IntermediateList[k].EigenValue1, IntermediateList[k].EigenValue2);
									double Mx2 = Math.Max(IntermediateList[l].EigenValue1, IntermediateList[l].EigenValue2);
									double ExtraDistance = 2 * Math.Sqrt(Mx1 + Mx2);
									if (LowSeparation + ExtraDistance > (IntermediateList[k].Barycenter ^ IntermediateList[l].Barycenter))
									{
										if (Tags[l] > Tags[k]) { Tags[l] = Tags[k]; TagsMod = true; }
										else if (Tags[l] < Tags[k]) { Tags[k] = Tags[l]; TagsMod = true; }
									}
								}
						}
						if (CLine < 1800 && CLine > 1700 - 150 && j < 350 && j > 250 - 150)
							;
						Dictionary<int, List<PixelPoint>> Pixels = new Dictionary<int, List<PixelPoint>>();
						Dictionary<int, List<double>> Values = new Dictionary<int, List<double>>();
						for(int k = 0; k < IntermediateList.Count; k++)
						{
							if (Pixels.ContainsKey(Tags[k])) { Pixels[Tags[k]].AddRange(IntermediateList[k].Points); Values[Tags[k]].AddRange(IntermediateList[k].PointValues); }
							else { Pixels.Add(Tags[k], IntermediateList[k].Points); Values.Add(Tags[k], IntermediateList[k].PointValues); }
						}
						lock (DetectedPP)
						{
							foreach (List<PixelPoint> px in Pixels.Values) DetectedPP.Add(px);
							foreach (List<double> pv in Values.Values) DetectedPV.Add(pv);
						}
						
						
						//lock (DetectedFasts)
						//	DetectedFasts.AddRange(IntermediateList);
					}
					;
				}
			}
			Input.ExitLock(InputData);
		}

		List<MedianDetection> GetMedetect(FitsImage InputImage, FitsImage Image)
		{
			//return DetectedFasts.Select((x) => new MedianDetection(InputImage.Transform, ObservationTime, x.Points, x.PointValues)).ToList();
			var x1 = DetectedPP.Zip(DetectedPV, (x, y) => new MedianDetection(InputImage.Transform, Image, x, y));
			return x1.Where((x) => x.PixelEllipse.SemiaxisMinor < 2.5 * PSFSize && x.PixelPoints.Count > 2 * PSFSize * PSFSize).ToList();
		}
	}
}
