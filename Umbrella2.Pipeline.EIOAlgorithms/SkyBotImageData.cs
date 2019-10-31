using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Umbrella2.Algorithms.Misc;
using Umbrella2.IO;
using Umbrella2.IO.FITS.KnownKeywords;
using Umbrella2.PropertyModel.CommonProperties;
using static Umbrella2.Pipeline.ExtraIO.SkyBoTLookup;

namespace Umbrella2.Pipeline.EIOAlgorithms
{
	/// <summary>
	/// SkyBoT pairing data for a given image.
	/// </summary>
	public class SkyBotImageData : ImageProperties
	{
		/// <summary>Time at which the data is valid.</summary>
		readonly DateTime ShotTime;
		/// <summary>Interval for which the data is valid.</summary>
		readonly TimeSpan Exposure;
		/// <summary>Object search structure.</summary>
		readonly QuadTree<SkybotObject> ObjTree;
		/// <summary>SkyBoT results.</summary>
		readonly SkybotObject[] ObjList;
		/// <summary>Which objects have not been paired yet.</summary>
		readonly HashSet<SkybotObject> Unpaired;

		/// <summary>Retrieves SkyBoT objects in a given image.</summary>
		public SkyBotImageData(Image Image) : base(Image)
		{
			PixelPoint CPP = new PixelPoint() { X = Image.Width / 2, Y = Image.Height / 2 };
			PixelPoint Corner1 = new PixelPoint() { X = 0, Y = 0 };
			PixelPoint Corner2 = new PixelPoint() { X = Image.Width, Y = Image.Height };
			EquatorialPoint CEP = Image.Transform.GetEquatorialPoint(CPP);
			double Radius = Image.Transform.GetEquatorialPoint(Corner1) ^ Image.Transform.GetEquatorialPoint(Corner2);

			ShotTime = Image.GetProperty<ObservationTime>().Time;
			Exposure = Image.GetProperty<ObservationTime>().Exposure;
			var Obj = GetObjects(CEP, Radius, ShotTime).ToArray();
			List<SkybotObject> Clean = new List<SkybotObject>();
			foreach (SkybotObject o in Obj)
			{
				PixelPoint PixP = Image.Transform.GetPixelPoint(o.Position);
				if (PixP.X > 0 & PixP.Y > 0 & PixP.X < Image.Width & PixP.Y < Image.Height)
					Clean.Add(o);
			}

			ObjList = Clean.ToArray();
			Unpaired = new HashSet<SkybotObject>(Clean);
			ObjTree = SkyBoTPairing.CreateTreeFromList(ObjList);
		}

		/// <summary>
		/// Tries pairing a tracklet (if it contains a detection in the time range of the image).
		/// </summary>
		/// <param name="t">Tracklet.</param>
		/// <param name="Separation">Maximum distance to consider, in arcseconds.</param>
		public void TryPair(Tracklet t, double Separation)
		{
			Separation *= Math.PI / 180 / 3600;
			ObjectIdentity obid;
			if (!t.TryFetchProperty(out obid)) obid = new ObjectIdentity();

			bool NamesPresent = false;

			foreach (ImageDetection imd in t.Detections)
			{
				if (Math.Abs((imd.Time.Time - ShotTime).TotalSeconds) < Exposure.TotalSeconds)
				{
					List<SkybotObject> Objects = ObjTree.Query(imd.Barycenter.EP.RA, imd.Barycenter.EP.Dec, Separation);
					foreach (SkybotObject so in Objects)
					{ obid.AddName(so.Name, so.PermanentDesignation, so.Position ^ imd.Barycenter.EP); Unpaired.Remove(so); NamesPresent = true; }
				}
			}
			if (NamesPresent)
				t.SetResetProperty(obid);
		}

		/// <summary>Fetch objects that should be in the image but have not been detected.</summary>
		/// <returns>The estimated positions of the objects not found.</returns>
		public List<EquatorialPoint> GetUnpaired()
		{
			List<EquatorialPoint> NP = new List<EquatorialPoint>();
			foreach (SkybotObject so in Unpaired)
				NP.Add(so.Position);
			return NP;
		}

		public override List<MetadataRecord> GetRecords()
		{ throw new InvalidOperationException(); }
	}
}
