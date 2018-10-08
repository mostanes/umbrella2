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
	/// <summary>
	/// Detects the linear asteroid trails.
	/// Obsolete. Please use newer LongTrailDetector.
	/// </summary>
	/// <remarks>
	/// This code is highly sensitive to changes. This is also why several parameters are hardcoded.
	/// </remarks>
	[Obsolete]
	public class SegmentDetector
	{
		double SegOnM;
		double SegDropM;
		double OnThreshold;
		double DropThreshold;
		double StrongHT;
		double PSFSize = 5;

		double LowSeparation = 12;
		double AngleEqual = 0.3;

		const int WorkingSize = 200;
		const int AreaOverlap = 50;
		int Skip = 4;
		List<Vector> StrongLines;
		List<LineAnalyzer.LineDetection> DetectedFasts;
		List<List<PixelPoint>> DetectedPP;
		List<List<double>> DetectedPV;

		/// <summary>
		/// Instantiates a new SegmentDetector with given hysteresis thresholds.
		/// </summary>
		/// <param name="IncrementThreshold"></param>
		/// <param name="SegmentCreateThreshold"></param>
		/// <param name="SegmentDropThreshold"></param>
		public SegmentDetector(double IncrementThreshold, double SegmentCreateThreshold, double SegmentDropThreshold)
		{
			SegOnM = SegmentCreateThreshold; SegDropM = SegmentDropThreshold;
			DetectedFasts = new List<LineAnalyzer.LineDetection>();
		}

		/// <summary>
		/// Scans the input image for long trails.
		/// </summary>
		/// <returns>The list of detected long trails.</returns>
		public List<MedianDetection> GetLongTrails(FitsImage Input, ImageStatistics ImStat)
		{
			const int ThreadStep = 450;
			StrongHT = WorkingSize * ImStat.StDev * Math.Log10(WorkingSize) * 1.2;
			OnThreshold = SegOnM * ImStat.StDev;
			DropThreshold = SegDropM * ImStat.StDev;

			StrongLines = new List<Vector>();
			DetectedPP = new List<List<PixelPoint>>(); DetectedPV = new List<List<double>>();

			Parallel.For(0, (Input.Height - 2 * AreaOverlap) / ThreadStep, (x) => SingleImageBlock(Input, (int) x * ThreadStep + AreaOverlap,
				  (int) (x + 1) * ThreadStep + AreaOverlap, ImStat));
			if ((Input.Height - AreaOverlap) % ThreadStep > AreaOverlap) SingleImageBlock(Input,
				(int) ((Input.Height - 2 * AreaOverlap) / ThreadStep * ThreadStep), (int) Input.Height - AreaOverlap, ImStat);

			return GetMedetect(Input, Input);
		}

		/// <summary>
		/// Processes the image blockwise in parallel.
		/// </summary>
		void SingleImageBlock(FitsImage Input, int StartLine, int LEnd, ImageStatistics ImStats)
		{
			RLHT.ImageParameters imp = new RLHT.ImageParameters()
			{
				DefaultRatio = 0.9,
				MaxRatio = 1.08,
				IncreasingThreshold = 0.75 * ImStats.StDev,
				MaxMultiplier = 10,
				ZeroLevel = ImStats.ZeroLevel + 0.75 * ImStats.StDev,
				ShortAvgLength = 5,
				LongAvgLength = 35
			};

			ImageData InputData;
			InputData = Input.LockData(new System.Drawing.Rectangle(0, StartLine - AreaOverlap, WorkingSize, WorkingSize), true);
			int CLine = StartLine;
			for (CLine = StartLine; CLine < LEnd; CLine += WorkingSize-AreaOverlap)
			{
				for (int j = AreaOverlap; j + WorkingSize + AreaOverlap < Input.Width; j += WorkingSize - AreaOverlap)
				{
					/* Scan lines for possible trails */
					InputData = Input.SwitchLockData(InputData, j, CLine - AreaOverlap, true);
					var w = RLHT.SmartSkipRLHT(InputData.Data, imp, StrongHT, Skip, true);
					bool[,] Mask = new bool[WorkingSize, WorkingSize];


					List<LineAnalyzer.LineDetection> IntermediateList = new List<LineAnalyzer.LineDetection>();

					/* Analyze each possible trail line */
					foreach (Vector vx in w.StrongPoints)
					{
						var z = LineAnalyzer.AnalyzeLine(InputData.Data, Mask, WorkingSize, WorkingSize, vx.X, vx.Y, OnThreshold,
							DropThreshold, 40, 10, j, CLine - AreaOverlap);
						IntermediateList.AddRange(z);
					}

					/* Pair the segments */
					/* This is a complicated, quick hack, not a real algorithm. FIXME */
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

					/* Load detection data */
					Dictionary<int, List<PixelPoint>> Pixels = new Dictionary<int, List<PixelPoint>>();
					Dictionary<int, List<double>> Values = new Dictionary<int, List<double>>();
					for (int k = 0; k < IntermediateList.Count; k++)
					{
						if (Pixels.ContainsKey(Tags[k])) { Pixels[Tags[k]].AddRange(IntermediateList[k].Points); Values[Tags[k]].AddRange(IntermediateList[k].PointValues); }
						else { Pixels.Add(Tags[k], IntermediateList[k].Points); Values.Add(Tags[k], IntermediateList[k].PointValues); }
					}
					lock (DetectedPP)
					{
						foreach (List<PixelPoint> px in Pixels.Values) DetectedPP.Add(px);
						foreach (List<double> pv in Values.Values) DetectedPV.Add(pv);
					}

				}
			}
			Input.ExitLock(InputData);
		}

		/// <summary>
		/// Transforms the results into MedianDetections and runs a small filter for unnatural lines.
		/// </summary>
		List<MedianDetection> GetMedetect(FitsImage InputImage, FitsImage Image)
		{
			var x1 = DetectedPP.Zip(DetectedPV, (x, y) => new MedianDetection(InputImage.Transform, Image, x, y));
			return x1.Where((x) => x.PixelEllipse.SemiaxisMinor < 2.5 * Math.Sqrt(x.PixelEllipse.SemiaxisMajor*PSFSize) && x.PixelPoints.Count > 2 * PSFSize * PSFSize).ToList();
		}
	}
}
