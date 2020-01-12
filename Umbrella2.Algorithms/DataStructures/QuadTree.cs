using System.Collections.Generic;

namespace Umbrella2.Algorithms.Misc
{
	/// <summary>
	/// A QuadTree (2-d tree) for quickly identifying objects in a given neighborhood.
	/// </summary>
	/// <typeparam name="T">The objects held by the QuadTree.</typeparam>
	public class QuadTree<T>
	{
		/// <summary>
		/// Tree depth.
		/// </summary>
		readonly int Depth;

		/// <summary>
		/// Tree root.
		/// </summary>
		readonly QuadTreeNode Root;

		/// <summary>
		/// Creates a new QuadTree of given position, size and depth.
		/// </summary>
		/// <param name="Depth">Tree depth. Number of branches to the lowest object bucket.</param>
		/// <param name="Top">Topmost Y coordinate of tree.</param>
		/// <param name="Bottom">Bottommost Y coordinate of tree.</param>
		/// <param name="Left">Leftmost X coordinate of the tree.</param>
		/// <param name="Right">Rightmost X coordinate of the tree.</param>
		public QuadTree(int Depth, double Top, double Bottom, double Left, double Right)
		{
			this.Depth = Depth;
			Root = new QuadTreeNode(Top, Bottom, Left, Right);
		}

		/// <summary>
		/// Adds a new object to the tree.
		/// </summary>
		/// <param name="Object">Object to be added.</param>
		/// <param name="X">The X coordinate of the object.</param>
		/// <param name="Y">The Y coordinate of the object.</param>
		public void Add(T Object, double X, double Y)
		{
			QuadTreeNode[] QTNList = new QuadTreeNode[Depth];
			QTNList[0] = Root;
			for (int i = 1; i < Depth; i++)
			{
				QuadTreeNode CNode = QTNList[i - 1];
				bool Bottom = Y > (CNode.Tp + CNode.Bt) / 2;
				bool Right = X > (CNode.Lf + CNode.Rg) / 2;
				if (Bottom)
				{
					if (Right)
					{
						if (CNode.nBR == null) CNode.nBR = new QuadTreeNode((CNode.Tp + CNode.Bt) / 2, CNode.Bt, (CNode.Lf + CNode.Rg) / 2, CNode.Rg);
						QTNList[i] = CNode.nBR;
					}
					else
					{
						if (CNode.nBL == null) CNode.nBL = new QuadTreeNode((CNode.Tp + CNode.Bt) / 2, CNode.Bt, CNode.Lf, (CNode.Lf + CNode.Rg) / 2);
						QTNList[i] = CNode.nBL;
					}
				}
				else
				{
					if (Right)
					{
						if (CNode.nTR == null) CNode.nTR = new QuadTreeNode(CNode.Tp, (CNode.Tp + CNode.Bt) / 2, (CNode.Lf + CNode.Rg) / 2, CNode.Rg);
						QTNList[i] = CNode.nTR;
					}
					else
					{
						if (CNode.nTL == null) CNode.nTL = new QuadTreeNode(CNode.Tp, (CNode.Tp + CNode.Bt) / 2, CNode.Lf, (CNode.Lf + CNode.Rg) / 2);
						QTNList[i] = CNode.nTL;
					}
				}
			}
			if (QTNList[Depth - 1].Bucket == null) QTNList[Depth - 1].Bucket = new List<T>();
			QTNList[Depth - 1].Bucket.Add(Object);
		}

		/// <summary>
		/// Queries the tree for objects in a given area.
		/// </summary>
		/// <param name="Top">Top (smallest) Y coordinate.</param>
		/// <param name="Bottom">Bottom (largest) Y coordinate.</param>
		/// <param name="Left">Left X coordinate.</param>
		/// <param name="Right">Right X coordinate.</param>
		/// <returns></returns>
		public List<T> Query(double Top, double Bottom, double Left, double Right)
		{
			List<T> Result = new List<T>();
			Root.Query(Top, Bottom, Left, Right, Result);
			return Result;
		}

		/// <summary>
		/// Queries the tree for objects around a given point.
		/// </summary>
		/// <param name="X">X coordinate of the center of the square.</param>
		/// <param name="Y">Y coordinate of the center of the square.</param>
		/// <param name="SquareSemiside">Distance from the center to the edges of the square.</param>
		/// <returns></returns>
		public List<T> Query(double X, double Y, double SquareSemiside)
		{ return Query(Y - SquareSemiside, Y + SquareSemiside, X - SquareSemiside, X + SquareSemiside); }

		/// <summary>
		/// Node of the QuadTree
		/// </summary>
		private class QuadTreeNode
		{
			internal readonly double Tp, Bt, Lf, Rg;
			internal QuadTreeNode nTL, nTR, nBL, nBR;
			/// <summary>
			/// Object bucket.
			/// </summary>
			internal List<T> Bucket;

			/// <param name="Top">Top Y coordinate of node area.</param>
			/// <param name="Bottom">Bottom Y coordinate of the node area.</param>
			/// <param name="Left">Left X coordinate of the node area.</param>
			/// <param name="Right">Right X coordinate of the node area.</param>
			public QuadTreeNode(double Top, double Bottom, double Left, double Right)
			{
				Tp = Top;
				Bt = Bottom;
				Lf = Left;
				Rg = Right;
			}

			/// <summary>
			/// Recursively queries the tree for objects.
			/// </summary>
			/// <param name="Top">Top Y coordinate of the search area.</param>
			/// <param name="Bottom">Bottom Y coordinate of the search area.</param>
			/// <param name="Left">Left X coordinate of the search area.</param>
			/// <param name="Right">Right X coordinate of the search area.</param>
			/// <param name="Accumulator"></param>
			public void Query(double Top, double Bottom, double Left, double Right, List<T> Accumulator)
			{
				/* Check if in range */
				bool YOK = (Tp > Top && Tp < Bottom) || (Bt > Top && Bt < Bottom) || (Tp < Top && Bt > Bottom);
				bool XOK = (Lf > Left && Lf < Right) || (Rg > Left && Rg < Right) || (Lf < Left && Rg > Right);
				if (!XOK || !YOK) return;
				/* If leaf node */
				if (Bucket != null)
					Accumulator.AddRange(Bucket);
				/* Recurse otherwise */
				else
				{
					if (nTL != null) nTL.Query(Top, Bottom, Left, Right, Accumulator);
					if (nTR != null) nTR.Query(Top, Bottom, Left, Right, Accumulator);
					if (nBL != null) nBL.Query(Top, Bottom, Left, Right, Accumulator);
					if (nBR != null) nBR.Query(Top, Bottom, Left, Right, Accumulator);
				}
			}
		}
	}
}
