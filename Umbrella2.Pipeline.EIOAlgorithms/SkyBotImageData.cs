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
		QuadTree<SkybotObject> ObjTree;
		/// <summary>SkyBoT results.</summary>
		SkybotObject[] ObjList;
		/// <summary>Which objects have not been paired yet.</summary>
		HashSet<SkybotObject> Unpaired;
		/// <summary>The center of the image.</summary>
		readonly EquatorialPoint ImageCenter;
		/// <summary>Image radius.</summary>
		readonly double Radius;
		/// <summary>Image associated to this property.</summary>
		readonly Image AssociatedImage;

		/// <summary>Creates the property.</summary>
		public SkyBotImageData(Image Image) : base(Image)
		{
			PixelPoint CPP = new PixelPoint() { X = Image.Width / 2, Y = Image.Height / 2 };
			PixelPoint Corner1 = new PixelPoint() { X = 0, Y = 0 };
			PixelPoint Corner2 = new PixelPoint() { X = Image.Width, Y = Image.Height };
			ImageCenter = Image.Transform.GetEquatorialPoint(CPP);
			Radius = Image.Transform.GetEquatorialPoint(Corner1) ^ Image.Transform.GetEquatorialPoint(Corner2);
			Radius *= 0.55;

			ShotTime = Image.GetProperty<ObservationTime>().Time;
			Exposure = Image.GetProperty<ObservationTime>().Exposure;
			AssociatedImage = Image;
		}

		/// <summary>
		/// Performs the retrieval of objects.
		/// </summary>
		/// <param name="ObservatoryCode">Observatory code. If null, uses the SCS interface, without it.</param>
		public void RetrieveObjects(string ObservatoryCode = null)
		{
			DateTime Time = ShotTime + TimeSpan.FromSeconds(Exposure.TotalSeconds * 0.5);
			var ObjURL = ObservatoryCode != null ? GenerateNSUrl(ImageCenter, Radius, Time, ObservatoryCode) : GenerateSCSUrl(ImageCenter, Radius, Time);
			if (!GetObjects(ObjURL, Time, out List<SkybotObject> OList)) return;

			List<SkybotObject> Clean = new List<SkybotObject>();
			foreach (SkybotObject o in OList)
			{
				PixelPoint PixP = AssociatedImage.Transform.GetPixelPoint(o.Position);
				if (PixP.X > 0 & PixP.Y > 0 & PixP.X < AssociatedImage.Width & PixP.Y < AssociatedImage.Height)
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
			if (!t.TryFetchProperty(out ObjectIdentity obid)) obid = new ObjectIdentity();

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
		/// <returns>The SkyBoT objects not found among the tracklets so far.</returns>
		public List<SkybotObject> GetUnpaired()
		{
			List<SkybotObject> NP = new List<SkybotObject>();
			foreach (SkybotObject so in Unpaired)
				NP.Add(so);
			return NP;
		}

		public override List<MetadataRecord> GetRecords()
		{ throw new InvalidOperationException(); }
	}
}
