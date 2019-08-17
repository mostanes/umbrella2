using Umbrella2.IO;

namespace Umbrella2.Algorithms.Images
{
	/// <summary>
	/// Algorithm scheduling core interface.
	/// </summary>
	public static partial class SchedCore
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
			/// <summary>Amount of data to read around the current working window.</summary>
			public int InputMargins;
			/// <summary>
			/// Amount of X-coordinate data to be fed at once in the function. When set to 0, it is implicitly set to the image width.
			/// </summary>
			public int Xstep;
			/// <summary>Amount of Y-coordinate data to be fed at once in the function.</summary>
			public int Ystep;
			/// <summary>
			/// Whether to ignore the image margins and fill regions outside the image with zeros. Must be set to true if <see cref="InputMargins"/> or <see cref="Xstep"/> is non-zero.
			/// </summary>
			public bool FillZero;
		}

		/// <summary>
		/// Represents the position of a block of data w.r.t. the image.
		/// </summary>
		public struct ImageSegmentPosition
		{
			/// <summary>Position of the data block in the image.</summary>
			public PixelPoint Alignment;
			/// <summary>WCS coordinates of the image.</summary>
			public WCS.IWCSProjection WCS;
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
		public static void RunAlgorithm<T>(Extractor<T> Algorithm, T Argument, Image Input, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new Image[] { Input },
				OutputImage = null,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.Extractor,
			};
			PrepareGeometry(ref details, Parameters);
			DefaultScheduler(details);
		}

		/// <summary>
		/// Runs the given algorithm on the input data.
		/// </summary>
		/// <typeparam name="T">Extra parameter type.</typeparam>
		/// <param name="Algorithm">Parallel algorithm.</param>
		/// <param name="Argument">Argument to be passed to all invocations.</param>
		/// <param name="Input">Input image.</param>
		/// <param name="Parameters">Parameters of the algorithm run.</param>
		public static void RunAlgorithm<T>(PositionDependentExtractor<T> Algorithm, T Argument, Image Input, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new Image[] { Input },
				OutputImage = null,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.PositionExtractor,
			};
			PrepareGeometry(ref details, Parameters);
			DefaultScheduler(details);
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
		public static void RunAlgorithm<T>(SimpleMap<T> Algorithm, T Argument, Image Input, Image Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new Image[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.SimpleMap_T,
			};
			PrepareGeometry(ref details, Parameters);
			DefaultScheduler(details);
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
		public static void RunAlgorithm<T, U>(SimpleMap<T, U> Algorithm, T Argument1, U Argument2, Image Input, Image Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new Image[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument1, Argument2 },
				Type = AlgorithmType.SimpleMap_TU,
			};
			PrepareGeometry(ref details, Parameters);
			DefaultScheduler(details);
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
		public static void RunAlgorithm<T, U, V>(SimpleMap<T, U, V> Algorithm, T Argument1, U Argument2, V Argument3, Image Input, Image Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new Image[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument1, Argument2, Argument3 },
				Type = AlgorithmType.SimpleMap_TUV,
			};
			PrepareGeometry(ref details, Parameters);
			DefaultScheduler(details);
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
		public static void RunAlgorithm<T>(PositionDependentMap<T> Algorithm, T Argument, Image Input, Image Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = new Image[] { Input },
				OutputImage = Output,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.PositionMap,
			};
			PrepareGeometry(ref details, Parameters);
			DefaultScheduler(details);
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
		public static void RunAlgorithm<T>(Combiner<T> Algorithm, T Argument, Image[] Inputs, Image Output, AlgorithmRunParameters Parameters)
		{
			RunDetails details = new RunDetails()
			{
				Algorithm = Algorithm,
				InputImages = Inputs,
				OutputImage = Output,
				Parameters = new object[] { Argument },
				Type = AlgorithmType.Combiner,
			};
			PrepareGeometry(ref details, Parameters);
			DefaultScheduler(details);
		}

#pragma warning disable CS1591
		/* Extension method for running the delegates with the default scheduler */
		public static void Run<T>(this SimpleMap<T> Algorithm, T Argument, Image Input, Image Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Output, Parameters); }

		public static void Run<T, U>(this SimpleMap<T, U> Algorithm, T Argument1, U Argument2, Image Input, Image Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument1, Argument2, Input, Output, Parameters); }

		public static void Run<T, U, V>(this SimpleMap<T, U, V> Algorithm, T Argument1, U Argument2, V Argument3, Image Input, Image Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument1, Argument2, Argument3, Input, Output, Parameters); }

		public static void Run<T>(this PositionDependentMap<T> Algorithm, T Argument, Image Input, Image Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Output, Parameters); }

		public static void Run<T>(this Combiner<T> Algorithm, T Argument, Image[] Input, Image Output, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Output, Parameters); }

		public static void Run<T>(this Extractor<T> Algorithm, T Argument, Image Input, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Parameters); }

		public static void Run<T>(this PositionDependentExtractor<T> Algorithm, T Argument, Image Input, AlgorithmRunParameters Parameters)
		{ RunAlgorithm(Algorithm, Argument, Input, Parameters); }

		/// <summary>
		/// Which delegate the algorithm corresponds to.
		/// </summary>
		public enum AlgorithmType
		{
			SimpleMap_T,
			SimpleMap_TU,
			SimpleMap_TUV,
			PositionMap,
			Combiner,
			Extractor,
			PositionExtractor
		}
#pragma warning restore CS1591

		/// <summary>
		/// Bag of data for the algorithm run.
		/// </summary>
		public struct RunDetails
		{
			/// <summary>Function to be run.</summary>
			public System.Delegate Algorithm;
			/// <summary>Arguments passed through from the caller.</summary>
			public object[] Parameters;
			/// <summary>Delegate type.</summary>
			public AlgorithmType Type;
			/// <summary>Images read by the scheduler and fed into the target algorithm.</summary>
			public Image[] InputImages;
			/// <summary>Image to be written by the algorithm.</summary>
			public Image OutputImage;
			/// <summary>Same as <see cref="AlgorithmRunParameters.InputMargins"/>.</summary>
			public int InputMargins;
			/// <summary>Same as <see cref="AlgorithmRunParameters.Ystep"/>.</summary>
			public int Ystep;
			/// <summary>Same as <see cref="AlgorithmRunParameters.Xstep"/>.</summary>
			public int Xstep;
			/// <summary>Width of the working image.</summary>
			public int DataWidth;
			/// <summary>Height of the working image.</summary>
			public int DataHeight;
			/// <summary>Same as <see cref="AlgorithmRunParameters.FillZero"/>.</summary>
			public bool FillZero;
		}

		/// <summary>
		/// Represents a scheduler for image processing functions. It must run the functions of a given algorithm over the image, according to the given parameters.
		/// </summary>
		/// <param name="RunParameters">Parameters that specify the context to be prepared for the called function.</param>
		public delegate void Scheduler(RunDetails RunParameters);

		/// <summary>
		/// The scheduler used when calling RunAlgorithm or Run on a the delegate of the algorithm.
		/// </summary>
		public static Scheduler DefaultScheduler = Schedulers.CPUParallel.Scheduler;

		/// <summary>
		/// Prepares algorithm geometry.
		/// </summary>
		/// <param name="Details">Parameters to prepare.</param>
		/// <param name="Parameters">Input parameters.</param>
		internal static void PrepareGeometry(ref RunDetails Details, AlgorithmRunParameters Parameters)
		{
			/* Copies common parameters */
			Details.FillZero = Parameters.FillZero;
			Details.Xstep = Parameters.Xstep;
			if (Details.Xstep == 0)
			{
				if (Details.OutputImage != null)
					Details.Xstep = (int) Details.OutputImage.Width;
				else Details.Xstep = (int) Details.InputImages[0].Width;
			}
			Details.Ystep = Parameters.Ystep;
			Details.InputMargins = Parameters.InputMargins;

			/* Compute block sizes */
			if (Details.OutputImage != null) { Details.DataHeight = (int) Details.OutputImage.Height; Details.DataWidth = (int) Details.OutputImage.Width; }
			else { Details.DataHeight = (int) Details.InputImages[0].Height; Details.DataWidth = (int) Details.InputImages[0].Width; }
		}
	}
}
