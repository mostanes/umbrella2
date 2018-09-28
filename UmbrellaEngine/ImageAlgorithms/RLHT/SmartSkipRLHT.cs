using System;
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
		/// <param name="StrongHoughTh">Threshold for a strong response.</param>
		/// <param name="Skip">Number of steps to skip if no strong response has been observed.</param>
		/// <returns></returns>
		internal static HTResult SmartSkipRLHT(double[,] Input, ImageParameters ImP, double StrongHoughTh, int Skip)
		{
			/* Compute algorithm parameters */
			int Height = Input.GetLength(0);
			int Width = Input.GetLength(1);
			double ThetaUnit = Min(Atan2(1, Height), Atan2(1, Width));
			double NTheta = 2 * PI / ThetaUnit;
			double RhoMax = Sqrt(Width * Width + Height * Height);
			double[,] HTMatrix = new double[(int) Round(RhoMax), (int) Round(NTheta)];
			int NRd = HTMatrix.GetLength(0);
			int NTh = HTMatrix.GetLength(1);
			int i, j;
			List<Vector> HoughPowerul = new List<Vector>();

			/* Initialize skip controlling variables */
			int StrongHoughReset = 2 * Skip + 1; /* Counter reset value, minimum to search Skip around found value */
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
					Lineover(Input, Height, Width, i, Theta, ImP, out HTMatrix[i, j]);

					/* If relevant coordinates */
					if (HTMatrix[i, j] > StrongHoughTh)
					{
						HoughPowerul.Add(new Vector() { X = i, Y = Theta });

						/* When new interesting coordinates, jump back and analyze */
						if (StrongHoughInnerCounter == 0) { if (j > Skip) j -= Skip; else j = 0; }
						/* Reset counter and notify outer loop */
						StrongHoughInnerCounter = StrongHoughReset;
						StrongHough = true;
					}
					/* If no interesting points, skip angles and decrement counter */
					else
					{
						if (StrongHoughInnerCounter == 0) j += Skip - 1;
						else StrongHoughInnerCounter--;
					}
				}
				/* If no interesting points, skip radii and decrement counter */
				if (!StrongHough)
				{
					if (StrongHoughOuterCounter == 0) i += Skip - 1;
					else StrongHoughOuterCounter--;
				}
				else
				{
					/* New interesting coordinates, jump back and analyze */
					if (StrongHoughOuterCounter == 0) { if (i > Skip) i -= Skip; else i = 0; }
					/* Reset counter */
					StrongHoughOuterCounter = StrongHoughReset;
				}
			}
			return new HTResult() { StrongPoints = HoughPowerul, HTMatrix = HTMatrix };
		}
	}
}
