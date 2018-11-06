using System;
using System.Collections.Generic;

namespace Umbrella2.WCS
{
	/// <summary>
	/// Computes linear the linear part of the WCS transforms.
	/// </summary>
	public class WCSLinPart
	{
		readonly double C11, C12, C21, C22;
		readonly double R11, R12, R21, R22;
		readonly double Ref1, Ref2;
		public readonly double WCSChainDerivative;

		/// <summary>
		/// Creates a new instance of WCSLinPart.
		/// </summary>
		/// <param name="CDRA_X">X to RA component, in degrees.</param>
		/// <param name="CDRA_Y">Y to RA component, in degrees.</param>
		/// <param name="CDDec_X">X to Dec component, in degrees.</param>
		/// <param name="CDDec_Y">Y to Dec component, in degrees.</param>
		/// <param name="RefX">Reference X point, 1-based.</param>
		/// <param name="RefY">Reference Y point, 1-based.</param>
		public WCSLinPart(double CDRA_X, double CDRA_Y, double CDDec_X, double CDDec_Y, double RefX, double RefY)
		{
			C11 = CDRA_X; C12 = CDRA_Y; C21 = CDDec_X; C22 = CDDec_Y;
			C11 *= Math.PI / 180; C12 *= Math.PI / 180; C21 *= Math.PI / 180; C22 *= Math.PI / 180;
			double Det = C11 * C22 - C12 * C21;
			R11 = C22 / Det; R12 = -C12 / Det; R21 = -C21 / Det; R22 = C11 / Det;
			Ref1 = RefX - 1;
			Ref2 = RefY - 1;
			WCSChainDerivative = Math.Sqrt(Math.Abs(Det));
		}

		public ProjectionPoint GetProjectionPoint(PixelPoint Point)
		{ return new ProjectionPoint() { X = C11 * (Point.X - Ref1) + C12 * (Point.Y - Ref2), Y = C21 * (Point.X - Ref1) + C22 * (Point.Y - Ref2) }; }

		public ProjectionPoint[] GetProjectionPoints(PixelPoint[] Points)
		{
			ProjectionPoint[] pps = new ProjectionPoint[Points.Length];
			for (int i = 0; i < Points.Length; i++) pps[i] = new ProjectionPoint() { X = C11 * (Points[i].X + Ref1) + C12 * (Points[i].Y + Ref2), Y = C21 * (Points[i].X + Ref1) + C22 * (Points[i].Y + Ref2) };
			return pps;
		}

		public List<ProjectionPoint> GetProjectionPoints(IEnumerable<PixelPoint> Points)
		{
			List<ProjectionPoint> pps = new List<ProjectionPoint>();
			foreach (PixelPoint pp in Points) pps.Add(new ProjectionPoint() { X = C11 * (pp.X + Ref1) + C12 * (pp.Y + Ref2), Y = C21 * (pp.X + Ref1) + C22 * (pp.Y + Ref2) });
			return pps;
		}

		public PixelPoint GetPixelPoint(ProjectionPoint Point)
		{ return new PixelPoint() { X = R11 * Point.X + R12 * Point.Y + Ref1, Y = R21 * Point.X + R22 * Point.Y + Ref2 }; }

		public PixelPoint[] GetPixelPoints(ProjectionPoint[] Points)
		{
			PixelPoint[] pps = new PixelPoint[Points.Length];
			for (int i = 0; i < Points.Length; i++) pps[i] = new PixelPoint() { X = R11 * Points[i].X + R12 * Points[i].Y + Ref1, Y = R21 * Points[i].X + R22 * Points[i].Y + Ref2 };
			return pps;
		}

		public List<PixelPoint> GetPixelPoints(IEnumerable<ProjectionPoint> Points)
		{
			List<PixelPoint> pps = new List<PixelPoint>();
			foreach (ProjectionPoint pp in Points) pps.Add(new PixelPoint() { X = R11 * pp.X + R12 * pp.Y + Ref1, Y = R21 * pp.X + R22 * pp.Y + Ref2 });
			return pps;
		}

		public double[] Matrix { get => new double[] { C11, C12, C21, C22, Ref1, Ref2 }; }
	}
}
