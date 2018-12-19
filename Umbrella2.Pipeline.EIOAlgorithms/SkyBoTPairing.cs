using System;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.Algorithms.Misc;
using Umbrella2.PropertyModel.CommonProperties;
using static Umbrella2.Pipeline.ExtraIO.SkyBoTLookup;

namespace Umbrella2.Pipeline.ExtraIO
{
	/// <summary>
	/// Provides an algorithm for pairing SkyBoT objects with tracklets.
	/// </summary>
	public static class SkyBoTPairing
    {
		/// <summary>
		/// Finds the names of a set of objects.
		/// </summary>
		/// <param name="PairingObjects">Objects to be named.</param>
		/// <param name="NamesTree">List of nearby SkyBoT objects.</param>
		/// <param name="MaxArcsecNaming">Maximum distance between SkyBoT object and detection at which they are considered the same object.</param>
		public static void FindNamesFromTree(List<Tracklet> PairingObjects, QuadTree<SkybotObject> NamesTree, double MaxArcsecNaming)
		{
			double Separation = MaxArcsecNaming * Math.PI / 180 / 3600;
			PairingObjects.AsParallel().ForAll((x) => PairTracklet(x, NamesTree, Separation));
		}

		/// <summary>
		/// Creates a QuadTree of SkyBoT objects for quick lookup.
		/// </summary>
		/// <param name="NamesList">List of SkyBoT objects.</param>
		public static QuadTree<SkybotObject> CreateTreeFromList(List<SkybotObject> NamesList)
		{
			double T = double.MinValue, B = double.MaxValue, L = double.MaxValue, R = double.MinValue;
			foreach (SkybotObject obj in NamesList)
			{
				if (obj.Position.RA > T) T = obj.Position.RA;
				if (obj.Position.RA < B) B = obj.Position.RA;
				if (obj.Position.Dec > R) R = obj.Position.Dec;
				if (obj.Position.Dec < L) L = obj.Position.Dec;
			}
			QuadTree<SkybotObject> Tree = new QuadTree<SkybotObject>(8, T, B, L, R);
			foreach (SkybotObject obj in NamesList) Tree.Add(obj, obj.Position.RA, obj.Position.Dec);
			return Tree;
		}

		/// <summary>
		/// Finds the (if there is any) name of the object.
		/// </summary>
		/// <param name="t">Tracklet to process.</param>
		/// <param name="NamesTree">QuadTree of SkyBoT objects.</param>
		/// <param name="Separation">Maximum distance between SkyBoT object and detection at which they are considered the same object.</param>
		static void PairTracklet(Tracklet t, QuadTree<SkybotObject> NamesTree, double Separation)
		{
			foreach (ImageDetection m in t.Detections)
				if (m != null)
				{
					List<SkybotObject> Objects = NamesTree.Query(m.Barycenter.EP.RA, m.Barycenter.EP.Dec, Separation);
					List<SkybotObject> Selected = Objects.Where((x) => x.TimeCoordinate == m.Time.Time && (x.Position ^ m.Barycenter.EP) < Separation).ToList();
					if (Selected.Count == 1) m.SetResetProperty(new ObjectIdentity() { Name = Objects[0].Name });
				}
		}
    }
}
