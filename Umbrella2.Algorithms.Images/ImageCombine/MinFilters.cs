using System;
using System.Linq;
using Umbrella2.WCS;

namespace Umbrella2.Algorithms.Images.ImageCombine
{
	/// <summary>
	/// Class for minimum-value filtering of multiple images.
	/// </summary>
	public static class MinFilters
	{
		/// <summary>
		/// Computes the minimum image of multiple input images. WCS information must be passed to the algorithm.
		/// </summary>
		public static SchedCore.Combiner<object> MinFilter => MiniFilter;

		/// <summary>
		/// Computes the second-minimum image of multiple input images. WCS information must be passed to the algorithm.
		/// </summary>
		public static SchedCore.Combiner<object> SemiMinFilter => SeMinFilter;

		/// <summary>
		/// Computes the minimum value of multiple images.
		/// </summary>
		/// <param name="Inputs">Input data.</param>
		/// <param name="Output">Output data.</param>
		/// <param name="InputPositions">Input alignments.</param>
		/// <param name="OutputPosition">Output alignment.</param>
		/// <param name="empty">Dummy argument.</param>
		static void MiniFilter(double[][,] Inputs, double[,] Output, SchedCore.ImageSegmentPosition[] InputPositions, SchedCore.ImageSegmentPosition OutputPosition, object empty)
		{
			PixelPoint[] InputAlignments = InputPositions.Select((x) => x.Alignment).ToArray();
			PixelPoint OutputAlignment = OutputPosition.Alignment;
			IWCSProjection[] InputImagesTransforms = InputPositions.Select((x) => x.WCS).ToArray();
			IWCSProjection OutputImageTransform = OutputPosition.WCS;

			int OW = Output.GetLength(1);
			int OH = Output.GetLength(0);
			int i, j, k;
			PixelPoint pxp = new PixelPoint();
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					double Min = double.MaxValue;
					pxp.X = j + OutputAlignment.X; pxp.Y = i + OutputAlignment.Y;
					EquatorialPoint eqp = OutputImageTransform.GetEquatorialPoint(pxp);
					for (k = 0; k < Inputs.Length; k++)
					{
						PixelPoint pyp = InputImagesTransforms[k].GetPixelPoint(eqp);
						pyp.X = Math.Round(pyp.X - InputAlignments[k].X); pyp.Y = Math.Round(pyp.Y - InputAlignments[k].Y);
						if (pyp.X < 0 || pyp.X >= Inputs[k].GetLength(1)) continue;
						if (pyp.Y < 0 || pyp.Y >= Inputs[k].GetLength(0)) continue;
						double dex = Inputs[k][(int)pyp.Y, (int)pyp.X];
						Min = dex < Min ? dex : Min;
					}
					Output[i, j] = Min;
				}
		}

		/// <summary>
		/// Computes the second minimum value of multiple images.
		/// </summary>
		/// <param name="Inputs">Input data.</param>
		/// <param name="Output">Output data.</param>
		/// <param name="InputPositions">Input alignments.</param>
		/// <param name="OutputPosition">Output alignment.</param>
		/// <param name="empty">Dummy argument.</param>
		static void SeMinFilter(double[][,] Inputs, double[,] Output, SchedCore.ImageSegmentPosition[] InputPositions, SchedCore.ImageSegmentPosition OutputPosition, object empty)
		{
			PixelPoint[] InputAlignments = InputPositions.Select((x) => x.Alignment).ToArray();
			PixelPoint OutputAlignment = OutputPosition.Alignment;
			IWCSProjection[] InputImagesTransforms = InputPositions.Select((x) => x.WCS).ToArray();
			IWCSProjection OutputImageTransform = OutputPosition.WCS;

			int OW = Output.GetLength(1);
			int OH = Output.GetLength(0);
			int i, j, k;
			PixelPoint pxp = new PixelPoint();
			for (i = 0; i < OH; i++) for (j = 0; j < OW; j++)
				{
					double Min = double.MaxValue, Min2 = double.MaxValue;
					pxp.X = j + OutputAlignment.X; pxp.Y = i + OutputAlignment.Y;
					EquatorialPoint eqp = OutputImageTransform.GetEquatorialPoint(pxp);
					for (k = 0; k < Inputs.Length; k++)
					{
						PixelPoint pyp = InputImagesTransforms[k].GetPixelPoint(eqp);
						pyp.X = Math.Round(pyp.X - InputAlignments[k].X); pyp.Y = Math.Round(pyp.Y - InputAlignments[k].Y);
						if (pyp.X < 0 || pyp.X >= Inputs[k].GetLength(1)) continue;
						if (pyp.Y < 0 || pyp.Y >= Inputs[k].GetLength(0)) continue;
						double dex = Inputs[k][(int)pyp.Y, (int)pyp.X];
						if (dex < Min) { Min2 = Min; Min = dex; continue; }
						if (dex < Min2) Min2 = dex;
					}
					Output[i, j] = Min2;
				}
		}
	}
}
