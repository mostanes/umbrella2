using Umbrella2.IO.FITS;

namespace Umbrella2.Algorithms.Images
{
	public static partial class ParallelAlgorithmRunner
	{
		/// <summary>
		/// Delegate for a transform that maps one input image to an output image with one extra argument.
		/// </summary>
		/// <typeparam name="T">Type of the argument passed.</typeparam>
		/// <param name="Input">Input image data.</param>
		/// <param name="Output">Output image data.</param>
		/// <param name="Extra">Passed-through argument.</param>
		public delegate void Algorithm1to1<T>(double[,] Input, double[,] Output, T Extra);

		/// <summary>
		/// Delegate for a transform that maps one input image to an output image with two extra arguments.
		/// </summary>
		/// <typeparam name="T">Type of the first argument passed.</typeparam>
		/// <typeparam name="U">Type of the second argument passed.</typeparam>
		/// <param name="Input">Input image data.</param>
		/// <param name="Output">Output image data.</param>
		/// <param name="Extra1">First passed-through argument.</param>
		/// <param name="Extra2">Second passed-through argument.</param>
		public delegate void Algorithm1to1<T, U>(double[,] Input, double[,] Output, T Extra1, U Extra2);

		/// <summary>
		/// Delegate for a transform that maps one input image to an output image with three extra arguments.
		/// </summary>
		/// <typeparam name="T">Type of the first argument passed.</typeparam>
		/// <typeparam name="U">Type of the second argument passed.</typeparam>
		/// <typeparam name="V">Type of the third argument passed.</typeparam>
		/// <param name="Input">Input image data.</param>
		/// <param name="Output">Output image data.</param>
		/// <param name="Extra1">First passed-through argument.</param>
		/// <param name="Extra2">Second passed-through argument.</param>
		/// <param name="Extra3">>Third passed-through argument.</param>
		public delegate void Algorithm1to1<T, U, V>(double[,] Input, double[,] Output, T Extra1, U Extra2, V Extra3);

		/// <summary>
		/// Delegate for a transform that maps multiple input images to an output image with one extra argument.
		/// </summary>
		/// <typeparam name="T">Type of the extra argument.</typeparam>
		/// <param name="Inputs">Input images data.</param>
		/// <param name="Output">Output image data.</param>
		/// <param name="Extra">Passed-through argument.</param>
		public delegate void AlgorithmNto1<T>(double[][,] Inputs, double[,] Output, PixelPoint[] InputAlignments, PixelPoint OutputAlignment, T Extra);

		/// <summary>
		/// Delegate for a transform that reads data from an input image with one extra argument.
		/// </summary>
		/// <remarks>The extra argument typically collects the results.</remarks>
		/// <typeparam name="T">Type of the extra argument.</typeparam>
		/// <param name="Input">Input image data.</param>
		/// <param name="Extra">Passed-through argument.</param>
		public delegate void Algorithm1to0<T>(double[,] Input, T Extra);

		/// <summary>
		/// Common algorithm parameters. Usually specified by algorithm type.
		/// </summary>
		public struct AlgorithmRunParameters
		{
			public int InputMargins;
			public int Xstep;
			public int Ystep;
			public bool FillZero;
		}

		/// <summary>
		/// Force the code to run single-threaded.
		/// </summary>
		public static bool ForceSerial = false;

		/// <summary>
		/// Runs the given algorithm on the input data.
		/// </summary>
		/// <typeparam name="T">Extra parameter type.</typeparam>
		/// <param name="Algorithm">Parallel algorithm.</param>
		/// <param name="Argument">Argument to be passed to all invocations.</param>
		/// <param name="Input">Input image.</param>
		/// <param name="Parameters">Parameters of the algorithm run.</param>
		public static void RunAlgorithm<T>(Algorithm1to0<T> Algorithm, T Argument, FitsImage Input, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new FitsImage[] { Input },
				OutputImage = null,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.A1t0_T,
				OriginalP = Parameters
			};
			CommonRunAlg(details);
		}

		/// <summary>
		/// Runs the given algorithm on the input data.
		/// </summary>
		/// <typeparam name="T">Extra parameter type.</typeparam>
		/// <param name="Algorithm">Parallel algorithm.</param>
		/// <param name="Argument">Argument to be passed to all invocations.</param>
		/// <param name="Input">Input image.</param>
		/// <param name="Output">Output image.</param>
		/// <param name="Parameters">Parameters of the algorithm run.</param>
		public static void RunAlgorithm<T>(Algorithm1to1<T> Algorithm, T Argument, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new FitsImage[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.A1t1_T,
				OriginalP = Parameters
			};
			CommonRunAlg(details);
		}

		/// <summary>
		/// Runs the given algorithm on the input data.
		/// </summary>
		/// <typeparam name="T">First extra parameter type.</typeparam>
		/// <typeparam name="U">Second extra parameter type.</typeparam>
		/// <param name="Algorithm">Parallel algorithm.</param>
		/// <param name="Argument1">First argument to be passed to all invocations.</param>
		/// <param name="Argument2">Second argument to be passed to all invocations.</param>
		/// <param name="Input">Input image.</param>
		/// <param name="Output">Output image.</param>
		/// <param name="Parameters">Parameters of the algorithm run.</param>
		public static void RunAlgorithm<T, U>(Algorithm1to1<T, U> Algorithm, T Argument1, U Argument2, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new FitsImage[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument1, Argument2 },
				Type = AlgorithmType.A1t1_TU,
				OriginalP = Parameters
			};
			CommonRunAlg(details);
		}

		/// <summary>
		/// Runs the given algorithm on the input data.
		/// </summary>
		/// <typeparam name="T">First extra parameter type.</typeparam>
		/// <typeparam name="U">Second extra parameter type.</typeparam>
		/// <typeparam name="V">Third extra parameter type.</typeparam>
		/// <param name="Algorithm">Parallel algorithm.</param>
		/// <param name="Argument1">First argument to be passed to all invocations.</param>
		/// <param name="Argument2">Second argument to be passed to all invocations.</param>
		/// <param name="Argument3">Third argument to be passed to all invocations.</param>
		/// <param name="Input">Input image.</param>
		/// <param name="Output">Output image.</param>
		/// <param name="Parameters">Parameters of the algorithm run.</param>
		public static void RunAlgorithm<T, U, V>(Algorithm1to1<T, U, V> Algorithm, T Argument1, U Argument2, V Argument3, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new FitsImage[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument1, Argument2, Argument3 },
				Type = AlgorithmType.A1t1_TUV,
				OriginalP = Parameters
			};
			CommonRunAlg(details);
		}

		/// <summary>
		/// Runs the given algorithm on the input data.
		/// </summary>
		/// <typeparam name="T">Extra parameter type.</typeparam>
		/// <param name="Algorithm">Parallel algorithm.</param>
		/// <param name="Argument">Argument to be passed to all invocations.</param>
		/// <param name="Inputs">Input images.</param>
		/// <param name="Output">Output image.</param>
		/// <param name="Parameters">Parameters of the algorithm run.</param>
		public static void RunAlgorithm<T>(AlgorithmNto1<T> Algorithm, T Argument, FitsImage[] Inputs, FitsImage Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = Inputs,
				OutputImage = Output,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.ANt1,
				OriginalP = Parameters
			};
			CommonRunAlg(details);
		}

		public static void Run<T>(this Algorithm1to1<T> Algorithm, T Argument, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Output, Parameters); }

		public static void Run<T, U>(this Algorithm1to1<T, U> Algorithm, T Argument1, U Argument2, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument1, Argument2, Input, Output, Parameters); }

		public static void Run<T, U, V>(this Algorithm1to1<T, U, V> Algorithm, T Argument1, U Argument2, V Argument3, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument1, Argument2, Argument3, Input, Output, Parameters); }

		public static void Run<T>(this AlgorithmNto1<T> Algorithm, T Argument, FitsImage[] Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Output, Parameters); }

		public static void Run<T>(this Algorithm1to0<T> Algorithm, T Argument, FitsImage Input, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Parameters); }
	}
}
