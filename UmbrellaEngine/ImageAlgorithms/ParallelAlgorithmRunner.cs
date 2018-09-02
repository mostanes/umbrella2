using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UmbrellaEngine.ImageAlgorithms
{
	class ParallelAlgorithmRunner
	{
		delegate void Algorithm1to1<T>(double[,] Input, double[,] Output, T Extra);
		delegate void Algorithm1to1<T, U>(double[,] Input, double[,] Output, T Extra1, U Extra2);
		delegate void Algorithm1to1<T, U, V>(double[,] Input, double[,] Output, T Extra1, U Extra2, V Extra3);
		delegate void AlgorithmNto1<T>(double[,][] Inputs, double[] Output, T Extra);
	}
}
