using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbrella2.IO;
using static Umbrella2.Algorithms.Images.SchedCore;
using static Umbrella2.Algorithms.Images.Schedulers.SchedUtil;

namespace Umbrella2.Algorithms.Images.Schedulers
{
	class CPUParallel
	{
		public static void Scheduler(RunDetails Details)
		{
			int Parallelism = Environment.ProcessorCount;
			ThreadDetails[] thDetails = new ThreadDetails[Parallelism];

			/* Compute block sizes. Make sure block sizes are multiples of Ystep (except for last block). */
			int StepSize = (Details.DataHeight + Parallelism - 1) / Parallelism;
			if (Details.Ystep != 0)
				StepSize = (StepSize + Details.Ystep - 1) / Details.Ystep * Details.Ystep;

			/* Update blocks */
			for (int i = 0; i < Parallelism; i++)
				thDetails[i] = new ThreadDetails() { CurrentPositionX = 0, CurrentPositionY = i * StepSize, StartPosition = i * StepSize, EndPosition = (i + 1) * StepSize };
			thDetails[Parallelism - 1].EndPosition = Details.DataHeight;

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
	}
}
