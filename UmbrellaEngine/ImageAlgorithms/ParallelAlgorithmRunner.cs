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
		public delegate void SimpleMap<T>(double[,] Input, double[,] Output, T Extra);

		/// <summary>
		/// Delegate for a transform that maps one input image to an output image with two extra arguments.
		/// </summary>
		/// <typeparam name="T">Type of the first argument passed.</typeparam>
		/// <typeparam name="U">Type of the second argument passed.</typeparam>
		/// <param name="Input">Input image data.</param>
		/// <param name="Output">Output image data.</param>
		/// <param name="Extra1">First passed-through argument.</param>
		/// <param name="Extra2">Second passed-through argument.</param>
		public delegate void SimpleMap<T, U>(double[,] Input, double[,] Output, T Extra1, U Extra2);

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
		public delegate void SimpleMap<T, U, V>(double[,] Input, double[,] Output, T Extra1, U Extra2, V Extra3);

		/// <summary>
		/// Delegate for a transform that maps one input image to an output image using pixel position information.
		/// </summary>
		/// <typeparam name="T">Type of the extra argument.</typeparam>
		/// <param name="Input">Input image data.</param>
		/// <param name="Output">Output image data.</param>
		/// <param name="InputPosition">Position of the input data w.r.t. the input image.</param>
		/// <param name="OutputPosition">Position of the output data w.r.t. the output image.</param>
		/// <param name="Extra"></param>
		public delegate void PositionDependentMap<T>(double[,] Input, double[,] Output, ImageSegmentPosition InputPosition, ImageSegmentPosition OutputPosition, T Extra);

		/// <summary>
		/// Delegate for a transform that maps multiple input images to an output image with one extra argument.
		/// </summary>
		/// <typeparam name="T">Type of the extra argument.</typeparam>
		/// <param name="Inputs">Input images data.</param>
		/// <param name="Output">Output image data.</param>
		/// <param name="InputPositions">Positions of the input data w.r.t. the input images.</param>
		/// <param name="OutputPosition">Position of the output data w.r.t. the output image.</param>
		/// <param name="Extra">Passed-through argument.</param>
		public delegate void Combiner<T>(double[][,] Inputs, double[,] Output, ImageSegmentPosition[] InputPositions, ImageSegmentPosition OutputPosition, T Extra);

		/// <summary>
		/// Delegate for a transform that reads data from an input image with one extra argument.
		/// </summary>
		/// <remarks>The extra argument typically collects the results.</remarks>
		/// <typeparam name="T">Type of the extra argument.</typeparam>
		/// <param name="Input">Input image data.</param>
		/// <param name="Extra">Passed-through argument.</param>
		public delegate void Extractor<T>(double[,] Input, T Extra);

		/// <summary>
		/// Delegate for a transform that reads data from an input image with one extra argument.
		/// </summary>
		/// <remarks>The extra argument typically collects the results.</remarks>
		/// <typeparam name="T">Type of the extra argument.</typeparam>
		/// <param name="Input">Input image data.</param>
		/// <param name="InputPosition">Position of the input data w.r.t. the input image.</param>
		/// <param name="Extra">Passed-through argument.</param>
		public delegate void PositionDependentExtractor<T>(double[,] Input, ImageSegmentPosition InputPosition, T Extra);

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
		/// Represents the position of a block of data w.r.t. the image.
		/// </summary>
		public struct ImageSegmentPosition
		{
			public PixelPoint Alignment;
			public WCS.WCSViaProjection WCS;
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
		public static void RunAlgorithm<T>(Extractor<T> Algorithm, T Argument, FitsImage Input, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new FitsImage[] { Input },
				OutputImage = null,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.Extractor,
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
		/// <param name="Parameters">Parameters of the algorithm run.</param>
		public static void RunAlgorithm<T>(PositionDependentExtractor<T> Algorithm, T Argument, FitsImage Input, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new FitsImage[] { Input },
				OutputImage = null,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.PositionExtractor,
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
		public static void RunAlgorithm<T>(SimpleMap<T> Algorithm, T Argument, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new FitsImage[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.SimpleMap_T,
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
		public static void RunAlgorithm<T, U>(SimpleMap<T, U> Algorithm, T Argument1, U Argument2, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new FitsImage[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument1, Argument2 },
				Type = AlgorithmType.SimpleMap_TU,
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
		public static void RunAlgorithm<T, U, V>(SimpleMap<T, U, V> Algorithm, T Argument1, U Argument2, V Argument3, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new FitsImage[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument1, Argument2, Argument3 },
				Type = AlgorithmType.SimpleMap_TUV,
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
		public static void RunAlgorithm<T>(PositionDependentMap<T> Algorithm, T Argument, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new FitsImage[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.PositionMap,
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
		public static void RunAlgorithm<T>(Combiner<T> Algorithm, T Argument, FitsImage[] Inputs, FitsImage Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = Inputs,
				OutputImage = Output,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.Combiner,
				OriginalP = Parameters
			};
			CommonRunAlg(details);
		}

		public static void Run<T>(this SimpleMap<T> Algorithm, T Argument, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Output, Parameters); }

		public static void Run<T, U>(this SimpleMap<T, U> Algorithm, T Argument1, U Argument2, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument1, Argument2, Input, Output, Parameters); }

		public static void Run<T, U, V>(this SimpleMap<T, U, V> Algorithm, T Argument1, U Argument2, V Argument3, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument1, Argument2, Argument3, Input, Output, Parameters); }

		public static void Run<T>(this PositionDependentMap<T> Algorithm, T Argument, FitsImage Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Output, Parameters); }

		public static void Run<T>(this Combiner<T> Algorithm, T Argument, FitsImage[] Input, FitsImage Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Output, Parameters); }

		public static void Run<T>(this Extractor<T> Algorithm, T Argument, FitsImage Input, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Parameters); }

		public static void Run<T>(this PositionDependentExtractor<T> Algorithm, T Argument, FitsImage Input, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Parameters); }
	}
}
