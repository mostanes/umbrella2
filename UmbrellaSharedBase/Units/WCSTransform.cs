using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.WCS
{
	public class WCSViaProjection
	{
		public readonly WCSProjectionTransform ProjectionTransform;
		public readonly WCSLinPart LinearTransform;

		public WCSViaProjection(WCSProjectionTransform Projection, WCSLinPart Matrix)
		{ ProjectionTransform = Projection; LinearTransform = Matrix; }

		public EquatorialPoint GetEquatorialPoint(PixelPoint Point)
		{ return ProjectionTransform.GetEquatorialPoint(LinearTransform.GetProjectionPoint(Point)); }

		public EquatorialPoint[] GetEquatorialPoints(PixelPoint[] Points)
		{ return ProjectionTransform.GetEquatorialPoints(LinearTransform.GetProjectionPoints(Points)); }

		public List<EquatorialPoint> GetEquatorialPoints(IEnumerable<PixelPoint> Points)
		{ return ProjectionTransform.GetEquatorialPoints(LinearTransform.GetProjectionPoints(Points)); }

		public PixelPoint GetPixelPoint(EquatorialPoint Point)
		{ return LinearTransform.GetPixelPoint(ProjectionTransform.GetProjectionPoint(Point)); }

		public PixelPoint[] GetPixelPoints(EquatorialPoint[] Points)
		{ return LinearTransform.GetPixelPoints(ProjectionTransform.GetProjectionPoints(Points)); }

		public List<PixelPoint> GetPixelPoints(IEnumerable<EquatorialPoint> Points)
		{ return LinearTransform.GetPixelPoints(ProjectionTransform.GetProjectionPoints(Points)); }

		public double GetEstimatedWCSChainDerivative()
		{ return LinearTransform.WCSChainDerivative * ProjectionTransform.GetEstimatedWCSChainDerivative(); }
	}

	public abstract class WCSProjectionTransform
	{
		protected readonly double RA, Dec;
		public WCSProjectionTransform(double RA, double Dec)
		{ this.RA = RA; this.Dec = Dec; }

		public abstract EquatorialPoint GetEquatorialPoint(ProjectionPoint Point);
		public abstract EquatorialPoint[] GetEquatorialPoints(ProjectionPoint[] Points);
		public abstract List<EquatorialPoint> GetEquatorialPoints(IEnumerable<ProjectionPoint> Points);
		public abstract ProjectionPoint GetProjectionPoint(EquatorialPoint Point);
		public abstract ProjectionPoint[] GetProjectionPoints(EquatorialPoint[] Points);
		public abstract List<ProjectionPoint> GetProjectionPoints(IEnumerable<EquatorialPoint> Points);
		public abstract double GetEstimatedWCSChainDerivative();
		public abstract void GetReferencePoints(out double RA, out double Dec);
		public abstract string Name { get; }
		public abstract string Description { get; }
	}
}
