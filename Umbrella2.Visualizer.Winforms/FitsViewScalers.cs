namespace Umbrella2.Visualizer.Winforms
{
	/// <summary>
	/// Represents an image scaling algorithm, for compressing the double precision floating point input to an 8-bit pixel value.
	/// </summary>
	public interface IFitsViewScaler
	{
		/// <summary>
		/// Scales the input data to an appropriate image value.
		/// </summary>
		/// <param name="Input">Input value.</param>
		/// <returns>An 8-bit pixel intensity.</returns>
		byte GetValue(double Input);
	}

	/// <summary>
	/// Algorithm for scaling input images linearly.
	/// </summary>
	public class LinearScaler : IFitsViewScaler
	{
		double Black;
		double White;
		double Slope;

		/// <summary>
		/// Creates a new LinearScaler from a pair of pixel values that should be considered black and white respectively.
		/// </summary>
		/// <param name="Black">The threshold under which a pixel is 0-black.</param>
		/// <param name="White">The threshold over which a pixel is 255-white.</param>
		public LinearScaler(double Black, double White)
		{
			this.Black = Black;
			this.White = White;
			Slope = 255 / (White - Black);
		}

		public byte GetValue(double Input)
		{
			if (Input < Black) return 0;
			else if (Input > White) return 255;
			else return (byte) (Slope * (Input - Black));
		}
	}
}
