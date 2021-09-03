using System;
using System.Collections.Generic;

namespace Umbrella2.Algorithms.DataStructures
{
	/// <summary>
	/// Data structure for fast retrieval of objects by their coordinates in a spherical coordinate system.
	/// </summary>
	public class SphericalQuadTree<T>
	{
		/// <summary>
		/// Tree depth.
		/// </summary>
		readonly int Depth;

		/// <summary>
		/// Tree root for -pi/4 &lt; Delta &lt; pi/4.
		/// </summary>
		readonly QuadTreeNode CylinderRoot;

		/// <summary>
		/// Tree root for Delta &lt; -pi/4.
		/// </summary>
		readonly QuadTreeNode BottomRoot;

		/// <summary>
		/// Tree root for Delta &gt; pi/4.
		/// </summary>
		readonly QuadTreeNode TopRoot;

		/// <summary>
		/// Creates a new SphericalQuadTree of given depth.
		/// </summary>
		/// <param name="Depth">Tree depth. Number of branches to the lowest object bucket.</param>
		public SphericalQuadTree(int Depth)
		{
			this.Depth = Depth;
			CylinderRoot = new QuadTreeNode(Math.PI / 4, -Math.PI / 4, 0, 2 * Math.PI);
			BottomRoot = new QuadTreeNode(-1, 1, -1, 1);
			TopRoot = new QuadTreeNode(-1, 1, -1, 1);
		}

		/// <summary>
		/// Adds a new object to the tree.
		/// </summary>
		/// <param name="Object">Object to be added.</param>
		/// <param name="Longitude">The longitude of the object.</param>
		/// <param name="Latitude">The latitude of the object.</param>
		public void Add(T Object, double Longitude, double Latitude)
		{
			QuadTreeNode[] QTNList = new QuadTreeNode[Depth];
			double X, Y;

			/* Check which tree to add the node to, and compute the X-Y coordinates for the QuadTree */
			if (Latitude > Math.PI / 4)
			{
				QTNList[0] = TopRoot;
				double radius = Math.Cos(Latitude) / Math.Sin(Latitude);
				X = radius * Math.Cos(Longitude);
				Y = radius * Math.Sin(Longitude);
			}
			else if (Latitude < -Math.PI / 4)
			{
				QTNList[0] = BottomRoot;
				double radius = -Math.Cos(Latitude) / Math.Sin(Latitude);
				X = radius * Math.Cos(Longitude);
				Y = radius * Math.Sin(Longitude);
			}
			else
			{
				QTNList[0] = CylinderRoot;
				X = Longitude;
				Y = Latitude;
			}

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
		/// Queries the tree for objects around a given point.
		/// </summary>
		/// <param name="Longitude">Longitude of the center of the square.</param>
		/// <param name="Latitude">Latitude of the center of the square.</param>
		/// <param name="SquareSemiside">Distance from the center to the edges of the square.</param>
		/// <returns></returns>
		public List<T> Query(double Longitude, double Latitude, double SquareSemiside)
		{
			List<T> Result = new List<T>();
			if (Latitude + SquareSemiside > Math.PI / 4)
			{
				double radius = Math.Cos(Latitude) / Math.Sin(Latitude);
				double X = radius * Math.Cos(Longitude);
				double Y = radius * Math.Sin(Longitude);
				double Size = SquareSemiside / Math.Pow(Math.Sin(Latitude), 2);
				TopRoot.Query(Y - Size, Y + Size, X - Size, X + Size, Result);
			}
			if (Latitude - SquareSemiside < -Math.PI / 4)
			{
				double radius = -Math.Cos(Latitude) / Math.Sin(Latitude);
				double X = radius * Math.Cos(Longitude);
				double Y = radius * Math.Sin(Longitude);
				double Size = SquareSemiside / Math.Pow(Math.Sin(Latitude), 2);
				BottomRoot.Query(Y - Size, Y + Size, X - Size, X + Size, Result);
			}
			if (Latitude - SquareSemiside < Math.PI / 4 | Latitude + SquareSemiside > -Math.PI / 4)
				CylinderRoot.Query(Latitude - SquareSemiside, Latitude + SquareSemiside,
					Longitude - SquareSemiside / Math.Cos(Latitude), Longitude + SquareSemiside / Math.Cos(Latitude), Result);

			return Result;
		}

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
