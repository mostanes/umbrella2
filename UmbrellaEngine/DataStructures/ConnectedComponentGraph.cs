using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.Algorithms.Misc
{
	class ConnectedComponentGraph<T>
	{
		T[] Objects;
		Func<T, T, bool> EdgeGenerator;
		GNode[] Nodes;

		public ConnectedComponentGraph(List<T> ObjectList, Func<T, T, bool> EdgeGeneratingFunction)
		{
			Objects = ObjectList.ToArray();
			EdgeGenerator = EdgeGeneratingFunction;
			int Index = 0;
			Nodes = Objects.Select((x) => new GNode() { Object = x, Index = Index++, ConnectedNodes = new List<GNode>() }).ToArray();
			int i, j;
			for (i = 0; i < Nodes.Length; i++) for (j = i + 1; j < Nodes.Length; j++)
					if (EdgeGenerator(Objects[i], Objects[j])) { Nodes[i].ConnectedNodes.Add(Nodes[j]); Nodes[j].ConnectedNodes.Add(Nodes[i]); }
		}

		class GNode
		{
			internal T Object;
			internal int Index;
			internal List<GNode> ConnectedNodes;
		}

		public List<T>[] GetConnectedComponents()
		{
			int[] ComponentsNumber = new int[Nodes.Length];
			int i, CNum = 0;
			for (i = 0; i < Nodes.Length; i++) if (ComponentsNumber[i] == 0)
				{ ComponentsNumber[i] = ++CNum; FollowConnectedComponent(i, ComponentsNumber[i], ComponentsNumber); }
			List<T>[] Components = new List<T>[CNum];
			for (i = 0; i < CNum; i++) Components[i] = new List<T>();
			foreach (GNode nd in Nodes) Components[ComponentsNumber[nd.Index] - 1].Add(nd.Object);
			return Components;
		}

		void FollowConnectedComponent(int ZeroIndex, int ComponentNumber, int[] Components)
		{
			foreach (GNode nd in Nodes[ZeroIndex].ConnectedNodes)
			{
				if (Components[nd.Index] == ComponentNumber) continue;
				System.Diagnostics.Debug.Assert(Components[nd.Index] == 0);

				Components[nd.Index] = ComponentNumber;
				FollowConnectedComponent(nd.Index, ComponentNumber, Components);
			}
		}
	}
}
