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
			SimpleMap_T,
			SimpleMap_TU,
			SimpleMap_TUV,
			PositionMap,
			Combiner,
			Extractor,
			PositionExtractor
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
			{
				if (Details.OutputImage != null)
					Details.Xstep = (int) Details.OutputImage.Width;
				else Details.Xstep = (int) Details.InputImages[0].Width;
			}
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
			if (!RD.FillZero)
			{ LockDataNofill(RD, TD, Selected, ref Data, true); return; }

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
			LockDataNofill(RunDetails, ThDetails, RunDetails.OutputImage, ref OutputData, false);
		}

		static void LockDataNofill(RunDetails RD, ThreadDetails TD, FitsImage Image, ref ImageData Data, bool Readonly)
		{
			/* Not initialized */
			if (TD.CurrentPositionY == TD.StartPosition && TD.CurrentPositionX == 0)
				Data = Image.LockData(new System.Drawing.Rectangle(TD.CurrentPositionX, TD.CurrentPositionY, RD.Xstep, RD.Ystep), false, Readonly);

			/* Compute required height and width */
			int NWidth = RD.Xstep;
			int NHeight = RD.Ystep;
			if (TD.CurrentPositionX + RD.Xstep > Image.Width)
				NWidth = (int) Image.Width - TD.CurrentPositionX;
			if (TD.CurrentPositionY + RD.Ystep > TD.EndPosition)
				NHeight = TD.EndPosition - TD.CurrentPositionY;

			/* If window size must be changed */
			if (NWidth != Data.Position.Width || NHeight != Data.Position.Height)
			{
				Image.ExitLock(Data);
				Data = Image.LockData(new System.Drawing.Rectangle(TD.CurrentPositionX, TD.CurrentPositionY, NWidth, NHeight), false, Readonly);
			}
			/* Just swap otherwise */
			else
				Data = Image.SwitchLockData(Data, TD.CurrentPositionX, TD.CurrentPositionY, false, Readonly);
		}

		/// <summary>
		/// Calls the algorithm code (depending on its type).
		/// </summary>
		static void CallAlgorithm(RunDetails Details, ImageData[] Inputs, ImageData Output)
		{
			switch (Details.Type)
			{
				case AlgorithmType.SimpleMap_T: Details.Algorithm.DynamicInvoke(Inputs[0].Data, Output.Data, Details.Parameters[0]); break;
				case AlgorithmType.SimpleMap_TU: Details.Algorithm.DynamicInvoke(Inputs[0].Data, Output.Data, Details.Parameters[0], Details.Parameters[1]); break;
				case AlgorithmType.SimpleMap_TUV: Details.Algorithm.DynamicInvoke(Inputs[0].Data, Output.Data, Details.Parameters[0], Details.Parameters[1], Details.Parameters[2]); break;
				case AlgorithmType.PositionMap:
					ImageSegmentPosition pmIP = GetPosition(Inputs[0]);
					ImageSegmentPosition pmOP = GetPosition(Output);
					Details.Algorithm.DynamicInvoke(Inputs[0].Data, Output.Data, pmIP, pmOP, Details.Parameters[0]);
					break;
				case AlgorithmType.Combiner:
					ImageSegmentPosition[] cIP = Inputs.Select(GetPosition).ToArray();
					ImageSegmentPosition cOP = GetPosition(Output);
					Details.Algorithm.DynamicInvoke(Inputs.Select((x) => x.Data).ToArray(), Output.Data, cIP, cOP, Details.Parameters[0]); break;
				case AlgorithmType.Extractor: Details.Algorithm.DynamicInvoke(Inputs[0].Data, Details.Parameters[0]); break;
				case AlgorithmType.PositionExtractor: Details.Algorithm.DynamicInvoke(Inputs[0].Data, GetPosition(Inputs[0]), Details.Parameters[0]); break;
			}
		}

		static ImageSegmentPosition GetPosition(ImageData Data) =>
			new ImageSegmentPosition() { WCS = Data.Parent.Transform, Alignment = new PixelPoint() { X = Data.Position.X, Y = Data.Position.Y } };
	}
}
