using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.IO.FITS;

namespace Umbrella2.Algorithms.Images
{
	public class WeightedMedianFilter
	{
		/// <summary>
		/// Runs a semi-median filtering using a weighted method on a single image.
		/// </summary>
		/// <param name="Input">Input image.</param>
		/// <param name="Output">Output image.</param>
		/// <param name="PSF">PSF weights.</param>
		public static void RunMedian(FitsImage Input, FitsImage Output, double[,] PSF)
		{
			double[] PSFLinear = new double[PSF.Length];
			Buffer.BlockCopy(PSF, 0, PSFLinear, 0, PSFLinear.Length * 8);
			const int ThreadStep = 250;
			const int LineStep = 50;
			Parallel.For(0, Input.Height / ThreadStep, (x) => SingleImageBlock(Input, Output, PSFLinear, (PSF.GetLength(0) / 2), (int) x * ThreadStep, LineStep, (int) (x + 1) * ThreadStep));
			if (Input.Height % ThreadStep != 0) SingleImageBlock(Input, Output, PSFLinear, (PSF.GetLength(0) / 2), (int) (Input.Height - Input.Height % ThreadStep), LineStep, (int) Input.Height);
			//SingleImageBlock(Input, Output, PSFLinear, (PSF.GetLength(0) - 1) / 2, 0, 50, (int) Input.Height);
		}

		static void RunSingleMedian(double[,] Input, double[,] Output, double[] PSF, int Size)
		{
			int OW = Output.GetLength(1);
			int OH = Output.GetLength(0);
			int i, j, k, l;
			double[] MedValues = new double[PSF.Length];
			double[] DPSF = new double[PSF.Length];
			int cnt;
			double s;
			int SzD = Size / 2;
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					cnt = 0;
					for (k = 0; k < Size; k++) for (l = 0; l < Size; l++)
						{ MedValues[cnt] = Input[i + k, j + l]; cnt++; }
					Buffer.BlockCopy(PSF, 0, DPSF, 0, 8 * PSF.Length);
					Array.Sort(MedValues, DPSF);
					for (k = 0, s = 0; s < 0.5; k++) s += DPSF[k];
					Output[i, j] = 0.5 * (MedValues[k - 1] + MedValues[k + 1]) + MedValues[k];
					Output[i, j] /= 2;
				}
		}

		static void SingleImageBlock(FitsImage Input, FitsImage Output, double[] PSFLinearized, int Size, int StartLine, int LineStep, int LEnd)
		{
			ImageData InputData;
			ImageData OutputData;
			if (StartLine + LineStep < Output.Height) OutputData = Output.LockData(new System.Drawing.Rectangle(0, StartLine, (int) Output.Width, LineStep), false, false);
			else OutputData = Output.LockData(new System.Drawing.Rectangle(0, StartLine, (int) Output.Width, (int) Output.Height - StartLine), false, false);
			InputData = Input.LockData(new System.Drawing.Rectangle(-Size, StartLine - Size, (int) Input.Width + Size + Size, LineStep + Size + Size), true);
			int CLine = StartLine;
			for (CLine = StartLine; CLine < LEnd; CLine += LineStep)
			{
				if (CLine + LineStep < Output.Height)
					OutputData = Output.SwitchLockData(OutputData, 0, CLine, false, false);
				else { Output.ExitLock(OutputData); OutputData = Output.LockData(new System.Drawing.Rectangle(0, CLine, (int) Output.Width, (int) Output.Height - CLine), false, false); }
				InputData = Input.SwitchLockData(InputData, -Size, CLine - Size, true);

				RunSingleMedian(InputData.Data, OutputData.Data, PSFLinearized, Size * 2 + 1);
			}
			Input.ExitLock(InputData);
			Output.ExitLock(OutputData);
		}
	}
}
