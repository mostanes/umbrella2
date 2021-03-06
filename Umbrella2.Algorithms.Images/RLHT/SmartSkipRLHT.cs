﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.Algorithms.Geometry;
using static System.Math;

namespace Umbrella2.Algorithms.Images
{
	public static partial class RLHT
	{
		/// <summary>
		/// Runs the RLHT over the image, skipping lines around uninteresting areas.
		/// </summary>
		/// <param name="Input">Input image segment.</param>
		/// <param name="ImP">Image-specific detection parameters.</param>
		/// <param name="AGD">Algorithm arguments.</param>
		/// <returns></returns>
		internal static HTResult SmartSkipRLHT(double[,] Input, ImageParameters ImP, AlgorithmData AGD)
		{
			/* Compute algorithm parameters */
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(1, Height), Atan2(1, Width));
			double NTheta = 2 * PI / ThetaUnit;
			double RhoMax = Sqrt(Width * Width + Height * Height);
			lock (AGD.HTPool)
				if (AGD.HTPool.Constructor == null)
					AGD.HTPool.Constructor = () => new double[(int) Round(RhoMax), (int) Round(NTheta)];
			double[,] HTMatrix = AGD.HTPool.Acquire();
			int NRd = HTMatrix.GetLength(0);
			int NTh = HTMatrix.GetLength(1);
			int i, j;
			List<Vector> HoughPowerul = AGD.VPool.Acquire();

			double StrongHoughTh = AGD.StrongHoughThreshold;

			float[] FData = null;

			/* Initialize skip controlling variables */
			int StrongHoughReset = 2 * AGD.ScanSkip + 1; /* Counter reset value, minimum to search Skip around found value */
			int StrongHoughInnerCounter = 0; /* Inner loop counter */
			int StrongHoughOuterCounter = 0; /* Outer loop counter */
			bool StrongHough = false;

			/* For all distances */
			for (i = 0; i < NRd; i++)
			{
				/* Any relevant coordinates? */
				StrongHough = false;
				/* For all angles */
				for (j = 0; j < NTh; j++)
				{
					/* Compute angle and skip irrelevant angles */
					double Theta = j * ThetaUnit;
					if (Theta > PI / 2) if (Theta < PI) continue;

					/* Integrate along the line */
					double Length;
					if(AGD.SimpleLine) SimpleLineover(Input, Height, Width, i, Theta, ImP, out HTMatrix[i, j], out Length, ref FData, AGD.LineSkip);
					else Lineover(Input, Height, Width, i, Theta, ImP, out HTMatrix[i, j], out Length);

					/* If has a function dependent on the length */
					if (AGD.StrongValueFunction != null) StrongHoughTh = AGD.StrongValueFunction(Length);

					/* If relevant coordinates */
					if (Length != 0 && HTMatrix[i, j] > StrongHoughTh)
					{
						HoughPowerul.Add(new Vector() { X = i, Y = Theta });

						/* When new interesting coordinates, jump back and analyze */
						if (StrongHoughInnerCounter == 0) { if (j > AGD.ScanSkip) j -= AGD.ScanSkip; else j = 0; }
						/* Reset counter and notify outer loop */
						StrongHoughInnerCounter = StrongHoughReset;
						StrongHough = true;
					}
					/* If no interesting points, skip angles and decrement counter */
					else
					{
						if (StrongHoughInnerCounter == 0) j += AGD.ScanSkip - 1;
						else StrongHoughInnerCounter--;
					}
				}
				/* If no interesting points, skip radii and decrement counter */
				if (!StrongHough)
				{
					if (StrongHoughOuterCounter == 0) i += AGD.ScanSkip - 1;
					else StrongHoughOuterCounter--;
				}
				else
				{
					/* New interesting coordinates, jump back and analyze */
					if (StrongHoughOuterCounter == 0) { if (i > AGD.ScanSkip) i -= AGD.ScanSkip; else i = 0; }
					/* Reset counter */
					StrongHoughOuterCounter = StrongHoughReset;
				}
			}
			return new HTResult() { StrongPoints = HoughPowerul, HTMatrix = HTMatrix };
		}
	}
}
