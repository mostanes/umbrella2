using System;
using System.Collections.Generic;
using System.IO;
using Umbrella2.Algorithms.Images;
using Umbrella2.IO.FITS;
using Umbrella2.PropertyModel.CommonProperties;

namespace UmbrellaAlgorithmsFramework.Pipeline.Utils
{
	/// <summary>
	/// Provides shortcuts for common pipeline image mapping tasks.
	/// </summary>
	public static class AutoscheduleExtensions
	{
		/// <summary>Default BITPIX value for output images.</summary>
		public static int DefaultBitPix = -32;

		/// <summary>
		/// Ensures that a certain image is present.
		/// </summary>
		/// <param name="Algorithm">Algorithm to generate the image if it is not present.</param>
		/// <param name="Model">Image to copy width, height and transform from.</param>
		/// <param name="OutputName">Path of the output image.</param>
		/// <param name="BitPix">BITPIX value of the output image.</param>
		/// <param name="ExtraProperties">List of properties to be passed to the image constructor.</param>
		/// <returns>The desired image.</returns>
		public static FitsImage EnsureImage(Action<FitsImage> Algorithm, FitsImage Model, string OutputName, int BitPix = 0, List<ImageProperties> ExtraProperties = null)
		{
			if (File.Exists(OutputName)) return new FitsImage(new FitsFile(OutputName, false));
			if (BitPix == 0) BitPix = DefaultBitPix;
			FitsImage Image = new FitsImage(new FitsFile(OutputName, true), Model.Width, Model.Height, Model.Transform, BitPix, ExtraProperties);
			Algorithm(Image);
			return Image;
		}

		/// <summary>
		/// Ensures that a certain image is present.
		/// </summary>
		/// <param name="Algorithm">Algorithm to generate the image if it is not present.</param>
		/// <param name="Model">Image to copy width, height and transform from.</param>
		/// <param name="RunDir">Working directory of the pipeline.</param>
		/// <param name="Name">Name of the output file.</param>
		/// <param name="Number">Output file's sequence number.</param>
		/// <param name="BitPix">BITPIX value of the output image.</param>
		/// <param name="ExtraProperties">List of properties to be passed to the image constructor.</param>
		/// <returns>The desired image.</returns>
		public static FitsImage EnsureImage(Action<FitsImage> Algorithm, FitsImage Model, string RunDir, string Name, int Number, int BitPix = 0, List<ImageProperties> ExtraProperties = null)
			=> EnsureImage(Algorithm, Model, Path.Combine(RunDir, Name + Number.ToString() + ".fits"), BitPix, ExtraProperties);

		/// <summary>
		/// Ensures that a certain image is present.
		/// </summary>
		/// <param name="Algorithm">Algorithm to generate the image if it is not present.</param>
		/// <param name="Input">Image assumed to be the input of the algorithm.</param>
		/// <param name="RunDir">Working directory of the pipeline.</param>
		/// <param name="Name">Name of the output file.</param>
		/// <param name="Number">Output file's sequence number.</param>
		/// <param name="DisplayName">Name as added to the ImageSource list.</param>
		/// <param name="BitPix">BITPIX value of the output image.</param>
		/// <param name="ExtraProperties">List of properties to be passed to the image constructor.</param>
		/// <returns>The desired image.</returns>
		public static FitsImage EnsureImage(Action<FitsImage> Algorithm, FitsImage Input, string RunDir, string Name, int Number, string DisplayName, int BitPix = 0, List<ImageProperties> ExtraProperties = null)
		{
			FitsImage img = EnsureImage(Algorithm, Input, RunDir, Name, Number, BitPix, ExtraProperties);
			img.GetProperty<ImageSource>().AddToSet(Input, DisplayName);
			return img;
		}

		/// <summary>
		/// Ensures that a certain image is present.
		/// </summary>
		/// <param name="Algorithm">Algorithm to generate the image if it is not present.</param>
		/// <param name="Parameters">Scheduler arguments for the algorithm.</param>
		/// <param name="Argument">Argument to be passed to the algorithm.</param>
		/// <param name="Input">Input image to the algorithm.</param>
		/// <param name="RunDir">Working directory of the pipeline.</param>
		/// <param name="Name">Name of the output file.</param>
		/// <param name="Number">Output file's sequence number.</param>
		/// <param name="BitPix">BITPIX value of the output image.</param>
		/// <param name="ExtraProperties">List of properties to be passed to the image constructor.</param>
		/// <returns>The desired image.</returns>
		public static FitsImage SchedEnsure<T>(SchedCore.SimpleMap<T> Algorithm, SchedCore.AlgorithmRunParameters Parameters, T Argument,
			FitsImage Input, string RunDir, string Name, int Number, int BitPix = 0, List<ImageProperties> ExtraProperties = null)
			=> EnsureImage((FitsImage f) => Algorithm.Run(Argument, Input, f, Parameters), Input, RunDir, Name, Number, BitPix, ExtraProperties);

		/// <summary>
		/// Ensures that a certain image is present.
		/// </summary>
		/// <param name="Algorithm">Algorithm to generate the image if it is not present.</param>
		/// <param name="Parameters">Scheduler arguments for the algorithm.</param>
		/// <param name="Argument">Argument to be passed to the algorithm.</param>
		/// <param name="Input">Input image to the algorithm.</param>
		/// <param name="RunDir">Working directory of the pipeline.</param>
		/// <param name="Name">Name of the output file.</param>
		/// <param name="Number">Output file's sequence number.</param>
		/// <param name="BitPix">BITPIX value of the output image.</param>
		/// <param name="ExtraProperties">List of properties to be passed to the image constructor.</param>
		/// <returns>The desired image.</returns>
		public static FitsImage SchedEnsure<T>(SchedCore.PositionDependentMap<T> Algorithm, SchedCore.AlgorithmRunParameters Parameters, T Argument,
			FitsImage Input, string RunDir, string Name, int Number, int BitPix = 0, List<ImageProperties> ExtraProperties = null)
			=> EnsureImage((FitsImage f) => Algorithm.Run(Argument, Input, f, Parameters), Input, RunDir, Name, Number, BitPix, ExtraProperties);

		/// <summary>
		/// Ensures that a certain image is present.
		/// </summary>
		/// <param name="Algorithm">Algorithm to generate the image if it is not present.</param>
		/// <param name="Parameters">Scheduler arguments for the algorithm.</param>
		/// <param name="Argument">Argument to be passed to the algorithm.</param>
		/// <param name="Input">Input image to the algorithm.</param>
		/// <param name="RunDir">Working directory of the pipeline.</param>
		/// <param name="Name">Name of the output file.</param>
		/// <param name="Number">Output file's sequence number.</param>
		/// <param name="DisplayName">Name as added to the ImageSource list.</param>
		/// <param name="BitPix">BITPIX value of the output image.</param>
		/// <param name="ExtraProperties">List of properties to be passed to the image constructor.</param>
		/// <returns>The desired image.</returns>
		public static FitsImage SchedEnsure<T>(SchedCore.SimpleMap<T> Algorithm, SchedCore.AlgorithmRunParameters Parameters, T Argument,
			FitsImage Input, string RunDir, string Name, int Number, string DisplayName, int BitPix = 0, List<ImageProperties> ExtraProperties = null)
			=> EnsureImage((FitsImage f) => Algorithm.Run(Argument, Input, f, Parameters), Input, RunDir, Name, Number, DisplayName, BitPix, ExtraProperties);

		/// <summary>
		/// Ensures that a certain image is present.
		/// </summary>
		/// <param name="Algorithm">Algorithm to generate the image if it is not present.</param>
		/// <param name="Parameters">Scheduler arguments for the algorithm.</param>
		/// <param name="Argument">Argument to be passed to the algorithm.</param>
		/// <param name="Input">Input image to the algorithm.</param>
		/// <param name="RunDir">Working directory of the pipeline.</param>
		/// <param name="Name">Name of the output file.</param>
		/// <param name="Number">Output file's sequence number.</param>
		/// <param name="DisplayName">Name as added to the ImageSource list.</param>
		/// <param name="BitPix">BITPIX value of the output image.</param>
		/// <param name="ExtraProperties">List of properties to be passed to the image constructor.</param>
		/// <returns>The desired image.</returns>
		public static FitsImage SchedEnsure<T>(SchedCore.PositionDependentMap<T> Algorithm, SchedCore.AlgorithmRunParameters Parameters, T Argument,
			FitsImage Input, string RunDir, string Name, int Number, string DisplayName, int BitPix = 0, List<ImageProperties> ExtraProperties = null)
			=> EnsureImage((FitsImage f) => Algorithm.Run(Argument, Input, f, Parameters), Input, RunDir, Name, Number, DisplayName, BitPix, ExtraProperties);
	}
}
