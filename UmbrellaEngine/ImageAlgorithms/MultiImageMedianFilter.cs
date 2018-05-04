using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.IO.FITS;
using Umbrella2.WCS;

namespace Umbrella2.Algorithms.Images
{
	public class MultiImageMedianFilter
	{
		public static void RunMedian(FitsImage[] Input, FitsImage Output)
		{
			const int ThreadStep = 50;
			const int LineStep = 50;
			ParallelOptions popt = new ParallelOptions();// { MaxDegreeOfParallelism = 1 };
			Parallel.For(0, Output.Height / ThreadStep, popt, (x) => SingleImageBlock(Input, Output, (int) x * ThreadStep, LineStep, (int) (x + 1) * ThreadStep));
			if (Output.Height % ThreadStep != 0) SingleImageBlock(Input, Output, (int) (Output.Height - Output.Height % ThreadStep), LineStep, (int) Output.Height);
			//SingleImageBlock(Input, Output, PSFLinear, (PSF.GetLength(0) - 1) / 2, 0, 50, (int) Input.Height);
		}

		static void RunSingleMedian(double[][,] Inputs, double[,] Output, int StartX, int StartY, int OX, int OY, WCSViaProjection[] InputsTransforms, WCSViaProjection OutputTransform)
		{
			int OW = Output.GetLength(1);
			int OH = Output.GetLength(0);
			int i, j, k, c;
			double[] MedValues = new double[Inputs.Length];
			PixelPoint pxp = new PixelPoint();
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					pxp.X = j + OX; pxp.Y = i + OY;
					EquatorialPoint eqp = OutputTransform.GetEquatorialPoint(pxp);
					c = 0;
					for (k = 0; k < Inputs.Length; k++)
					{
						PixelPoint pyp = InputsTransforms[k].GetPixelPoint(eqp);
						pyp.X = Math.Round(pyp.X-StartX); pyp.Y = Math.Round(pyp.Y - StartY);
						if (pyp.X < 0 || pyp.X >= Inputs[k].GetLength(1)) continue;
						if (pyp.Y < 0 || pyp.Y >= Inputs[k].GetLength(0)) continue;
						double dex = Inputs[k][(int) pyp.Y, (int) pyp.X];
						MedValues[c] = dex;
						c++;
					}
					if (c == 0)
						continue;
					
					Array.Sort(MedValues);
					Output[i, j] = MedValues[c / 2];
				}
		}

		static void SingleImageBlock(FitsImage[] Input, FitsImage Output, int StartLine, int LineStep, int LEnd)
		{
			ImageData[] InputsData = new ImageData[Input.Length];
			ImageData OutputData;
			WCSViaProjection[] InputsTransforms = Input.Select((x) => x.Transform).ToArray();
			if (StartLine + LineStep < Output.Height) OutputData = Output.LockData(new System.Drawing.Rectangle(0, StartLine, (int) Output.Width, LineStep), false, false);
			else OutputData = Output.LockData(new System.Drawing.Rectangle(0, StartLine, (int) Output.Width, (int) Output.Height - StartLine), false, false);
			for (int i = 0; i < Input.Length; i++) InputsData[i] = Input[i].LockData(new System.Drawing.Rectangle(-50, StartLine - 50, (int) Input[i].Width + 100, LineStep + 100), true);
			int CLine = StartLine;
			for (CLine = StartLine; CLine < LEnd; CLine += LineStep)
			{
				if (CLine + LineStep < Output.Height)
					OutputData = Output.SwitchLockData(OutputData, 0, CLine, false, false);
				else { Output.ExitLock(OutputData); OutputData = Output.LockData(new System.Drawing.Rectangle(0, CLine, (int) Output.Width, (int) Output.Height - CLine), false, false); }
				for (int i = 0; i < Input.Length; i++) InputsData[i] = Input[i].SwitchLockData(InputsData[i], -50, CLine - 50, true);

				RunSingleMedian(InputsData.Select((x) => x.Data).ToArray(), OutputData.Data, -50, CLine - 50, 0, CLine, InputsTransforms, Output.Transform);
			}
			for (int i = 0; i < Input.Length; i++) Input[i].ExitLock(InputsData[i]);
			Output.ExitLock(OutputData);
		}
	}
}
