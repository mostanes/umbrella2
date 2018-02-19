using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.Algorithms.Misc
{
	class QuadTree<T>
	{
		readonly int Depth;
		readonly QuadTreeNode Root;

		public QuadTree(int Depth, double Top, double Bottom, double Left, double Right)
		{
			this.Depth = Depth;
			Root = new QuadTreeNode(Top, Bottom, Left, Right);
		}

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

		public List<T> Query(double Top, double Bottom, double Left, double Right)
		{
			List<T> Result = new List<T>();
			Root.Query(Top, Bottom, Left, Right, Result);
			return Result;
		}

		public List<T> Query(double X, double Y, double SquareSemiside)
		{ return Query(X - SquareSemiside, X + SquareSemiside, Y - SquareSemiside, Y + SquareSemiside); }



		private class QuadTreeNode
		{
			internal readonly double Tp, Bt, Lf, Rg;
			internal QuadTreeNode nTL, nTR, nBL, nBR;
			internal List<T> Bucket;

			public QuadTreeNode(double Top, double Bottom, double Left, double Right)
			{
				Tp = Top;
				Bt = Bottom;
				Lf = Left;
				Rg = Right;
			}

			public void Query(double Top, double Bottom, double Left, double Right, List<T> Accumulator)
			{
				bool YOK = (Tp > Top && Tp < Bottom) || (Bt > Top && Bt < Bottom) || (Tp < Top && Bt > Bottom);
				bool XOK = (Lf > Left && Lf < Right) || (Rg > Left && Rg < Right) || (Lf < Left && Rg > Right);
				if (!XOK || !YOK) return;
				if (Bucket != null)
					Accumulator.AddRange(Bucket);
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
