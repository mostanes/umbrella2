using System;
using System.Collections.Generic;
using static System.Math;
using System.Threading.Tasks;
using Umbrella2.IO.FITS;

namespace Umbrella2.Algorithms.Images
{
	public class ImageStatistics
	{
		public double ZeroLevel;
		public double StDev;
		List<double> Means;
		List<double> Variances;

		public ImageStatistics(FitsImage Input)
		{
			const int ThreadStep = 250;
			const int LineStep = 50;
			Means = new List<double>();
			Variances = new List<double>();
			Parallel.For(0, Input.Height / ThreadStep, (x) => SingleImageBlock(Input, (int) x * ThreadStep, LineStep, (int) (x + 1) * ThreadStep));
			double[] M = Means.ToArray();
			double[] V = Variances.ToArray();
			Array.Sort(M);
			Array.Sort(V);
			ZeroLevel = M[M.Length / 2];
			StDev = Sqrt(V[M.Length / 2]);
		}


		void RunStats(double[,] Input)
		{
			int OW = Input.GetLength(1);
			int OH = Input.GetLength(0);
			int i, j, k, c;
			
			double Mean = 0, Var = 0;
			for (k = 0; k < OW - OH; k += OH)
			{
				Mean = 0; Var = 0;
				for (i = 0; i < OH; i++) for (j = 0; j < OH; j++)
					{ Mean += Input[i, j + k]; Var += Input[i, j + k] * Input[i, j + k]; }
				Mean /= (OH * OH);
				Var /= (OH * OH);
				Var -= Mean * Mean;
				lock (Means)
				{
					Means.Add(Mean);
					Variances.Add(Var);
				}
			}			
		}

		void SingleImageBlock(FitsImage Input, int StartLine, int LineStep, int LEnd)
		{
			ImageData InputData;
			System.Drawing.Rectangle Area = new System.Drawing.Rectangle(0, StartLine, (int) Input.Width, LineStep);
			InputData = Input.LockData(new System.Drawing.Rectangle(0, StartLine, (int) Input.Width, LineStep), true);
			int CLine = StartLine;
			for (CLine = StartLine; CLine < LEnd; CLine += LineStep)
			{
				InputData = Input.SwitchLockData(InputData, 0, CLine, true);

				RunStats(InputData.Data);
			}
			Input.ExitLock(InputData);
		}
	}
}
