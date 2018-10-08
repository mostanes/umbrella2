﻿using System;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.Algorithms.Geometry;
using Umbrella2.IO.FITS;

namespace Umbrella2.Algorithms.Images
{
	/// <summary>
	/// The new long trail detection mechanism, replacing SegmentDetector. Versatile and documented.
	/// </summary>
	public static class LongTrailDetector
	{
		/// <summary>
		/// The bag of algorithm parameters.
		/// </summary>
		public struct LongTrailData
		{
			/// <summary>
			/// Bag of RLHT parameters.
			/// </summary>
			internal RLHT.ImageParameters ImageParameters;
			/// <summary>
			/// Amount of lines to skip when skimming over image with SmartSkipRLHT.
			/// </summary>
			public int SkipSize;
			/// <summary>
			/// The width over which to scan high-scoring lines using LineAnalyzer.
			/// </summary>
			public int ScanWidth;
			/// <summary>
			/// RLHT score detection threshold.
			/// </summary>
			public double SigmaCount;
			/// <summary>
			/// Whether to use the simpler version of the lineover function (which computes the RLHT score for a given line). Given in units of standard deviations.
			/// </summary>
			public bool SimpleLine;
			/// <summary>
			/// Input image standard deviation.
			/// </summary>
			public double Sigma;
			/// <summary>
			/// Upper threshold for segment detection in LineAnalyzer. Given in units of standard deviations.
			/// </summary>
			public double SegmentSelectThreshold;
			/// <summary>
			/// Lower threshold for segment detection in LineAnalyzer. Given in units of standard deviations.
			/// </summary>
			public double SegmentDropThreshold;
			/// <summary>
			/// Maximum distance that can separate two segments on the same line that are considered part of the same MedianDetection.
			/// </summary>
			public int MaxInterblobDistance;
			/// <summary>
			/// Whether to skip analyzing crowded regions (as those are more likely to contain unwanted light sources and noise than actual asteroids).
			/// </summary>
			public bool DropCrowdedRegion;
			/// <summary>
			/// Currently processed image.
			/// </summary>
			public FitsImage RunningImage;
			/// <summary>
			/// The results of the algorithm run.
			/// </summary>
			public List<MedianDetection> Results;
		}

		/// <summary>
		/// The long trail detection algorithm.
		/// </summary>
		public static ParallelAlgorithmRunner.PositionDependentExtractor<LongTrailData> Algorithm = LTD_RLHT;

		/// <summary>
		/// Parameters for the ParallelAlgorithmRunner.
		/// </summary>
		public static ParallelAlgorithmRunner.AlgorithmRunParameters Parameters => new ParallelAlgorithmRunner.AlgorithmRunParameters()
		{
			FillZero = true,
			InputMargins = 50,
			Xstep = 200,
			Ystep = 200
		};

		/// <summary>
		/// Function for setting up algorithm parameters.
		/// </summary>
		/// <param name="PSFSize">Size of the PSF.</param>
		/// <param name="RLHTThreshold">RLHT line threshold. See documentation for more details.</param>
		/// <param name="SegmentSelectThreshold">Segment upper hysteresis threshold (for LineAnalyzer).</param>
		/// <param name="SegmentDropThreshold">Segment lower hysteresis threshold (for LineAnalyzer).</param>
		/// <param name="MaxInterblobDistance">Maximal distance between blobs of the same detection.</param>
		/// <param name="SimpleLine">Whether to use the simpler lineover function (requires well-smoothed input data).</param>
		/// <returns>The bag of algorithm parameters.</returns>
		public static LongTrailData GeneralAlgorithmSetup(int PSFSize, double RLHTThreshold, double SegmentSelectThreshold, double SegmentDropThreshold, int MaxInterblobDistance, bool SimpleLine)
		{
			LongTrailData Data = new LongTrailData()
			{
				ScanWidth = 2 * PSFSize,
				MaxInterblobDistance = MaxInterblobDistance,
				SegmentDropThreshold = SegmentDropThreshold,
				SegmentSelectThreshold = SegmentSelectThreshold,
				SigmaCount = RLHTThreshold,
				SkipSize = PSFSize - 1,
				SimpleLine = SimpleLine
			};
			Data.ImageParameters = new RLHT.ImageParameters()
			{
				LongAvgLength = MaxInterblobDistance,
				ShortAvgLength = PSFSize,
				MaxMultiplier = 10,
				DefaultRatio = Math.Pow(0.01, 1.0 / MaxInterblobDistance)
			};
			Data.ImageParameters.MaxRatio = Math.Pow(Data.ImageParameters.MaxMultiplier, 1.0 / MaxInterblobDistance);
			return Data;
		}

		/// <summary>
		/// Infers algorithm parameters from the input image.
		/// </summary>
		/// <param name="Image">Input image.</param>
		/// <param name="Stats">Input image statistical information.</param>
		/// <param name="Data">Algorithm data bag.</param>
		public static void PrepareAlgorithmForImage(FitsImage Image, ImageStatistics Stats, ref LongTrailData Data)
		{
			Data.Results = new List<MedianDetection>();
			Data.RunningImage = Image;
			Data.Sigma = Stats.StDev;
			Data.ImageParameters.IncreasingThreshold = Stats.StDev;
			Data.ImageParameters.ZeroLevel = Stats.ZeroLevel;
			if (Data.SimpleLine) Data.ImageParameters.IncreasingThreshold *= 1.5;
		}

		/// <summary>
		/// The segment detector function. It calls the RLHT scorer and if line segments are sensed, it calls the LineAnalyzer to find the source blobs.
		/// </summary>
		/// <param name="Input">Input data.</param>
		/// <param name="Position">Position of the input data array in the image.</param>
		/// <param name="Data">Bag of algorithm parameters and data.</param>
		static void LTD_RLHT(double[,] Input, ParallelAlgorithmRunner.ImageSegmentPosition Position, LongTrailData Data)
		{
			/* Extracts the size of the input data */
			int Height = Input.GetLength(0), Width = Input.GetLength(1);
			double Diagonal = Math.Sqrt(Width * Width + Height * Height);

			/* Applies the RLHT algorithm */
			var Result = RLHT.SmartSkipRLHT(Input, Data.ImageParameters, 0, Data.SkipSize, Data.SimpleLine, (x) => ThresholdComputer(x, Data, Diagonal));

			/* Prepare common data for the LineAnalyzer */
			bool[,] Mask = new bool[Height, Width];
			double SST = Data.SegmentSelectThreshold * Data.Sigma, SDT = Data.SegmentDropThreshold * Data.Sigma;
			int MIB = Data.MaxInterblobDistance, SW = Data.ScanWidth, pX = (int) Position.Alignment.X, pY = (int) Position.Alignment.Y;

			if (Data.DropCrowdedRegion) /* If the region is too crowded, it's very likely to be some luminous residue - for example star halos */
				if (Result.StrongPoints.Count > Diagonal) /* There is no deep meaning between this comparison; a reasonable Diagonal seems to correspond to a reasonable number of lines */
					return;

			/* Analyze each possible trail line and store the detections */
			foreach (Vector vx in Result.StrongPoints)
			{
				var z = LineAnalyzer.AnalyzeLine(Input, Mask, Height, Width, vx.X, vx.Y, SST, SDT, MIB, SW, pX, pY);
				Data.Results.AddRange(z.Select((x) => new MedianDetection(Position.WCS, Data.RunningImage, x.Points, x.PointValues)));
			}
		}

		/// <summary>
		/// Computes the RLHT score detection thresholds as a function of the line length. This particular implementation compensates for short line lengths by increasing their threshold.
		/// </summary>
		/// <param name="LineLength">Length of the line for which to compute the threshold.</param>
		/// <param name="Data">Bag of parameters for the LongTrailDetector.</param>
		/// <param name="Diagonal">Length of the image diagonal.</param>
		/// <returns>The RLHT score above which to scan the line for detection's blobs.</returns>
		static double ThresholdComputer(double LineLength, LongTrailData Data, double Diagonal)
		{
			if (LineLength < 0.5 * Diagonal) LineLength = LineLength * 0.5 + Diagonal * 0.25;
			return Data.SigmaCount * Data.Sigma * LineLength;
		}
	}
}
