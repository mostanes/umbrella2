using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Umbrella2.Algorithms.Images.SchedCore;

namespace Umbrella2.Algorithms.Images
{
	public static class CoreFilter
	{
		public class CoreFilterParameters
		{
			internal double[] PSF;
			internal BitArray[] Mask;

			public CoreFilterParameters(double[] Weights, BitArray[] BadpixelMask)
			{ PSF = Weights; Mask = BadpixelMask; }
		}

		public static AlgorithmRunParameters Parameters(int PSFRadius) => new AlgorithmRunParameters()
		{
			FillZero = true,
			InputMargins = PSFRadius,
			Xstep = 0,
			Ystep = 50
		};

		public static readonly PositionDependentMap<CoreFilterParameters> Filter = CoreFilterAlgorithm;

		static void CoreFilterAlgorithm(double[,] Input, double[,] Output, ImageSegmentPosition InputPosition, ImageSegmentPosition OutPos, CoreFilterParameters FilterParameters)
		{
			int OW = Output.GetLength(1);
			int OH = Output.GetLength(0);
			int i, j, k, l;
			int Size = (int) Math.Round(Math.Sqrt(FilterParameters.PSF.Length));
			int XSz = Size / 2;
			double[] MedValues = new double[FilterParameters.PSF.Length];
			double[] DPSF = new double[FilterParameters.PSF.Length];
			int cnt;
			double s;
			int SzD = Size / 2;
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					cnt = 0;
					for (k = 0; k < Size; k++) for (l = 0; l < Size; l++)
						{
							int Y = i + k + ((int) Math.Round(InputPosition.Alignment.Y));
							int X = j + l + ((int) Math.Round(InputPosition.Alignment.X));
							if (X < 0 || X >= FilterParameters.Mask[0].Length || Y < 0 || Y >= FilterParameters.Mask.Length) continue;
							DPSF[cnt] = FilterParameters.PSF[k * Size + l];
							if (!FilterParameters.Mask[Y][X]) { MedValues[cnt] = Input[i + k, j + l]; cnt++; }
						}
					if (cnt <= 2 * Size + 1) Output[i, j] = 0;
					else
					{
						Array.Sort(MedValues, DPSF, 0, cnt);
						double w;
						for (s = 0, w = 0, k = Size; k <= cnt - Size; k++)
						{ s += MedValues[k] * DPSF[k]; w += DPSF[k]; }
						Output[i, j] = s / w;
					}
				}
		}
	}
}
