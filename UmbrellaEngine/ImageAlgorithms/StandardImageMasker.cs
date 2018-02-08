using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.IO.FITS;
using Umbrella2.WCS;

namespace Umbrella2.Algorithms.Images
{
	public class StandardImageMasker
	{
		double UpperThreshold;
		double LowerThreshold;
		double Floor;
		FitsImage Mask;
		WCSViaProjection MaskTransform;

		public StandardImageMasker(FitsImage Mask, double UpperThreshold, double LowerThreshold, double Floor)
		{
			this.Mask = Mask;
			MaskTransform = Mask.Transform;
			this.UpperThreshold = UpperThreshold;
			this.LowerThreshold = LowerThreshold;
			this.Floor = Floor;
		}

		public void MaskImage(FitsImage Input, FitsImage Output)
		{
			const int ThreadStep = 250;
			const int LineStep = 50;
			Parallel.For(0, Input.Height / ThreadStep, (x) => SingleImageBlock(Input, Output, (int) x * ThreadStep, LineStep, (int) (x + 1) * ThreadStep));
			if (Input.Height % ThreadStep != 0) SingleImageBlock(Input, Output, (int) (Input.Height - Input.Height % ThreadStep), LineStep, (int) Input.Height);
		}


		void RunSingleMask(double[,] Input, double[,] Output, double[,] Mask, int MaskX, int MaskY, int OX, int OY, WCSViaProjection InputTransform)
		{
			int OW = Output.GetLength(1);
			int OH = Output.GetLength(0);
			int i, j, k, c;
			bool[,] SmartMask = new bool[OH, OW];
			PixelPoint pxp = new PixelPoint();
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					if (SmartMask[i, j]) continue;
					pxp.X = j + OX; pxp.Y = i + OY;
					EquatorialPoint ep = InputTransform.GetEquatorialPoint(pxp);
					PixelPoint mpt = MaskTransform.GetPixelPoint(ep);
					mpt.X = Math.Round(mpt.X - MaskX); mpt.Y = Math.Round(mpt.Y - MaskY);
					if (mpt.X < 0 || mpt.X >= Mask.GetLength(1)) continue;
					if (mpt.Y < 0 || mpt.Y >= Mask.GetLength(0)) continue;
					if (Mask[(int) mpt.Y, (int) mpt.X] > UpperThreshold)
						BitmapFill(Mask, Input, pxp, MaskX, MaskY, OX, OY, InputTransform, SmartMask);
				}
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					if (SmartMask[i, j] || Input[i, j] < Floor) Output[i, j] = 0;
					else Output[i, j] = Input[i, j] - Floor;
				}
		}

		void BitmapFill(double[,] Mask, double[,] Input, PixelPoint DPoint, int MaskX, int MaskY, int OX, int OY, WCSViaProjection InputTransform, bool[,] SmartMask)
		{
			Queue<PixelPoint> PointQ = new Queue<PixelPoint>();
			PointQ.Enqueue(DPoint);
			double XLow = (LowerThreshold + Floor) / 2;
			while (PointQ.Count > 0)
			{
				PixelPoint pt = PointQ.Dequeue();
				if (pt.X < OX || pt.X >= OX + SmartMask.GetLength(1)) continue;
				if (pt.Y < OY || pt.Y >= OY + SmartMask.GetLength(0)) continue;
				if (SmartMask[(int) pt.Y - OY, (int) pt.X - OX]) continue;
				EquatorialPoint ep = InputTransform.GetEquatorialPoint(pt);
				PixelPoint mpt = MaskTransform.GetPixelPoint(ep);
				mpt.X = Math.Round(mpt.X - MaskX); mpt.Y = Math.Round(mpt.Y - MaskY);
				if (mpt.X < 0 || mpt.X >= Mask.GetLength(1)) continue;
				if (mpt.Y < 0 || mpt.Y >= Mask.GetLength(0)) continue;
				bool Do = false;
				if (Mask[(int) mpt.Y, (int) mpt.X] > LowerThreshold) Do = true;
				if (!Do && Mask[(int) mpt.Y, (int) mpt.X] > XLow) if (Input[(int) pt.Y - OY, (int) pt.X - OX] > LowerThreshold) Do = true;
				if (Do)
				{
					SmartMask[(int) pt.Y - OY, (int) pt.X - OX] = true;
					PointQ.Enqueue(new PixelPoint() { X = pt.X - 1, Y = pt.Y });
					PointQ.Enqueue(new PixelPoint() { X = pt.X + 1, Y = pt.Y });
					PointQ.Enqueue(new PixelPoint() { X = pt.X, Y = pt.Y - 1 });
					PointQ.Enqueue(new PixelPoint() { X = pt.X, Y = pt.Y + 1 });
				}
			}
		}

		void SingleImageBlock(FitsImage Input, FitsImage Output, int StartLine, int LineStep, int LEnd)
		{
			ImageData InputData;
			ImageData MaskData;
			ImageData OutputData;
			System.Drawing.Rectangle Area = new System.Drawing.Rectangle(0, StartLine, (int) Output.Width, LineStep);
			if (StartLine + LineStep < Output.Height) OutputData = Output.LockData(new System.Drawing.Rectangle(0, StartLine, (int) Output.Width, LineStep), false, false);
			else OutputData = Output.LockData(new System.Drawing.Rectangle(0, StartLine, (int) Output.Width, (int) Output.Height - StartLine), false, false);
			InputData = Input.LockData(new System.Drawing.Rectangle(0, StartLine, (int) Output.Width, LineStep), true);
			MaskData = Mask.LockData(new System.Drawing.Rectangle(-50, StartLine - 50, (int) Input.Width + 100, LineStep + 100), true);
			int CLine = StartLine;
			for (CLine = StartLine; CLine < LEnd; CLine += LineStep)
			{
				if (CLine + LineStep < Output.Height)
					OutputData = Output.SwitchLockData(OutputData, 0, CLine, false, false);
				else { Output.ExitLock(OutputData); OutputData = Output.LockData(new System.Drawing.Rectangle(0, CLine, (int) Output.Width, (int) Output.Height - CLine), false, false); }
				InputData = Input.SwitchLockData(InputData, 0, CLine, true);
				MaskData = Mask.SwitchLockData(MaskData, -50, CLine - 50, true);

				RunSingleMask(InputData.Data, OutputData.Data, MaskData.Data, -50, CLine - 50, 0, CLine, Input.Transform);
			}
			Input.ExitLock(InputData);
			Output.ExitLock(OutputData);
			Mask.ExitLock(MaskData);
		}
	}
}
