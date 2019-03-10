using Umbrella2.IO.FITS;
using static Umbrella2.Algorithms.Images.SchedCore;

namespace Umbrella2.Algorithms.Images.Schedulers
{
	/// <summary>
	/// Useful functions for implementing schedulers.
	/// </summary>
	public static class SchedUtil
	{
		/// <summary>
		/// Thread-specific parameter bag. Can be used for implementing threaded schedulers.
		/// </summary>
		public struct ThreadDetails
		{
			/// <summary>Y coordinate at which the thread should start processing.</summary>
			public int StartPosition;
			/// <summary>Y coordinate at which the thread should end processing.</summary>
			public int EndPosition;
			/// <summary>Current Y coordinate.</summary>
			public int CurrentPositionX;
			/// <summary>Current X coordinate.</summary>
			public int CurrentPositionY;
		}
		
		/// <summary>
		/// Reads a block of data from the input images.
		/// </summary>
		public static void ReadImageBlock(RunDetails RD, FitsImage Selected, ref ImageData Data, ref ThreadDetails TD)
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
		public static void ProcessOutput(RunDetails RunDetails, ThreadDetails ThDetails, ref ImageData OutputData)
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

		/// <summary>Computes an <see cref="ImageSegmentPosition"/> from an <see cref="ImageData"/>.</summary>
		public static ImageSegmentPosition GetPosition(ImageData Data) =>
			new ImageSegmentPosition() { WCS = Data.Parent.Transform, Alignment = new PixelPoint() { X = Data.Position.X, Y = Data.Position.Y } };
	}
}
