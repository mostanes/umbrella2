using System;
using System.Threading.Tasks;
using System.Linq;
using Umbrella2.IO.FITS;

namespace Umbrella2.Algorithms.Images
{
	public static partial class ParallelAlgorithmRunner
	{
		/// <summary>
		/// Which delegate the algorithm corresponds to.
		/// </summary>
		enum AlgorithmType
		{
			A1t1_T,
			A1t1_TU,
			A1t1_TUV,
			ANt1,
			A1t0_T
		}

		/// <summary>
		/// Bag of data for the algorithm run.
		/// </summary>
		struct RunDetails
		{
			internal Delegate Algorithm;
			internal object[] Parameters;
			internal AlgorithmType Type;
			internal FitsImage[] InputImages;
			internal FitsImage OutputImage;
			internal int InputMargins;
			internal int Ystep;
			internal int Xstep;
			internal int DataWidth;
			internal bool FillZero;
			internal AlgorithmRunParameters OriginalP;
		}

		/// <summary>
		/// Thread-specific parameter bag.
		/// </summary>
		struct ThreadDetails
		{
			internal int StartPosition;
			internal int EndPosition;
			internal int CurrentPositionX;
			internal int CurrentPositionY;
		}

		/// <summary>
		/// Common code for running the algorithms in parallel.
		/// </summary>
		/// <param name="Details">Algorithm run parameters.</param>
		static void CommonRunAlg(RunDetails Details)
		{
			/* Copies common parameters */
			Details.FillZero = Details.OriginalP.FillZero;
			Details.Xstep = Details.OriginalP.Xstep;
			if (Details.Xstep == 0)
				Details.Xstep = (int) Details.OutputImage.Width;
			Details.Ystep = Details.OriginalP.Ystep;
			Details.InputMargins = Details.OriginalP.InputMargins;

			int Parallelism = Environment.ProcessorCount;

			ThreadDetails[] thDetails = new ThreadDetails[Parallelism];

			/* Compute block sizes */
			int ImLength, StepSize;
			if (Details.OutputImage != null) { ImLength = (int) Details.OutputImage.Height; Details.DataWidth = (int) Details.OutputImage.Width; }
			else { ImLength = (int) Details.InputImages[0].Height; Details.DataWidth = (int) Details.InputImages[0].Width; }
			StepSize = (ImLength + Parallelism - 1) / Parallelism;

			/* Update blocks */
			for (int i = 0; i < Parallelism; i++)
				thDetails[i] = new ThreadDetails() { CurrentPositionX = 0, CurrentPositionY = i * StepSize, StartPosition = i * StepSize, EndPosition = (i + 1) * StepSize };
			thDetails[Parallelism - 1].EndPosition = ImLength;

			/* Run in parallel */
			Parallel.For(0, Parallelism, (i) => ProcessBlock(Details, ref thDetails[i]));
		}

		/// <summary>
		/// Process a block of data.
		/// </summary>
		/// <param name="RunDetails">Thread-common run parameters.</param>
		/// <param name="ThDetails">Thread-specific run parameters.</param>
		static void ProcessBlock(RunDetails RunDetails, ref ThreadDetails ThDetails)
		{
			/* Initialized inputs and outputs */
			ImageData[] Dataset = new ImageData[RunDetails.InputImages.Length];
			ImageData OutputData = default(ImageData);
			/* While there is data to process */
			while (ThDetails.CurrentPositionY < ThDetails.EndPosition)
			{
				/* Read data and initialize output */
				for (int i = 0; i < Dataset.Length; i++)
					ReadImageBlock(RunDetails, RunDetails.InputImages[i], ref Dataset[i], ref ThDetails);
				if (RunDetails.OutputImage != null)
					ProcessOutput(RunDetails, ThDetails, ref OutputData);

				CallAlgorithm(RunDetails, Dataset, OutputData);

				ThDetails.CurrentPositionX += RunDetails.Xstep;
				if (ThDetails.CurrentPositionX >= RunDetails.DataWidth) { ThDetails.CurrentPositionX = 0; ThDetails.CurrentPositionY += RunDetails.Ystep; }
			}

			/* Release inputs and outputs */
			for (int i = 0; i < Dataset.Length; i++)
				RunDetails.InputImages[i].ExitLock(Dataset[i]);

			if (RunDetails.OutputImage != null) RunDetails.OutputImage.ExitLock(OutputData);
		}

		/// <summary>
		/// Reads a block of data from the input images.
		/// </summary>
		static void ReadImageBlock(RunDetails RD, FitsImage Selected, ref ImageData Data, ref ThreadDetails TD)
		{
			if (TD.CurrentPositionY == TD.StartPosition && TD.CurrentPositionX == 0)
				Data = Selected.LockData(new System.Drawing.Rectangle(
					TD.CurrentPositionX - RD.InputMargins, TD.CurrentPositionY - RD.InputMargins,
					RD.Xstep + 2 * RD.InputMargins, RD.Ystep + 2 * RD.InputMargins),
					RD.FillZero);
			else
				Data = Selected.SwitchLockData(Data, TD.CurrentPositionX - RD.InputMargins, TD.CurrentPositionY - RD.InputMargins, RD.FillZero);
		}

		/// <summary>
		/// Initializes and writes data to output image.
		/// </summary>
		static void ProcessOutput(RunDetails RunDetails, ThreadDetails ThDetails, ref ImageData OutputData)
		{
			if (ThDetails.CurrentPositionY == ThDetails.StartPosition && ThDetails.CurrentPositionX == 0)
				OutputData = RunDetails.OutputImage.LockData(new System.Drawing.Rectangle(ThDetails.CurrentPositionX, ThDetails.CurrentPositionY, RunDetails.Xstep, RunDetails.Ystep), false, false);
			else if (ThDetails.CurrentPositionX + RunDetails.Xstep > RunDetails.OutputImage.Width)
				throw new NotImplementedException();
			else if (ThDetails.CurrentPositionY + RunDetails.Ystep > ThDetails.EndPosition)
			{
				RunDetails.OutputImage.ExitLock(OutputData);
				OutputData = RunDetails.OutputImage.LockData(new System.Drawing.Rectangle(ThDetails.CurrentPositionX, ThDetails.CurrentPositionY, RunDetails.Xstep, (int) ThDetails.EndPosition - ThDetails.CurrentPositionY), false, false);
			}
			else
				OutputData = RunDetails.OutputImage.SwitchLockData(OutputData, ThDetails.CurrentPositionX, ThDetails.CurrentPositionY, false, false);
		}

		/// <summary>
		/// Calls the algorithm code (depending on its type).
		/// </summary>
		static void CallAlgorithm(RunDetails Details, ImageData[] Inputs, ImageData Output)
		{
			switch (Details.Type)
			{
				case AlgorithmType.A1t1_T: Details.Algorithm.DynamicInvoke(Inputs[0].Data, Output.Data, Details.Parameters[0]); break;
				case AlgorithmType.A1t1_TU: Details.Algorithm.DynamicInvoke(Inputs[0].Data, Output.Data, Details.Parameters[0], Details.Parameters[1]); break;
				case AlgorithmType.A1t1_TUV: Details.Algorithm.DynamicInvoke(Inputs[0].Data, Output.Data, Details.Parameters[0], Details.Parameters[1], Details.Parameters[2]); break;
				case AlgorithmType.ANt1:
					PixelPoint[] IA = Inputs.Select((x) => new PixelPoint() { X = x.Position.Location.X, Y = x.Position.Location.Y }).ToArray();
					PixelPoint OA = new PixelPoint() { X = Output.Position.X, Y = Output.Position.Y };
					Details.Algorithm.DynamicInvoke(Inputs.Select((x) => x.Data).ToArray(), Output.Data, IA, OA, Details.Parameters[0]); break;
				case AlgorithmType.A1t0_T: Details.Algorithm.DynamicInvoke(Inputs[0].Data, Details.Parameters[0]); break;
			}
		}
	}
}
