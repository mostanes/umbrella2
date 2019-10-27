using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbrella2.WCS
{
    public interface IWCSProjection
    {
        EquatorialPoint GetEquatorialPoint(PixelPoint Point);
        EquatorialPoint[] GetEquatorialPoints(PixelPoint[] Points);
        List<EquatorialPoint> GetEquatorialPoints(IEnumerable<PixelPoint> Points);
		[Obsolete]
        double GetEstimatedWCSChainDerivative();
        PixelPoint GetPixelPoint(EquatorialPoint Point);
        PixelPoint[] GetPixelPoints(EquatorialPoint[] Points);
        List<PixelPoint> GetPixelPoints(IEnumerable<EquatorialPoint> Points);
		EquatorialVelocity GetEquatorialVelocity(PixelVelocity PV);
		PixelVelocity GetPixelVelocity(EquatorialVelocity EV);
    }
#pragma warning disable 1591
    /// <summary>
    /// Represents a transform of FITS image coordinates to WCS via a linear map and a spherical projection.
    /// </summary>
    public class WCSViaProjection : IWCSProjection
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

		public EquatorialVelocity GetEquatorialVelocity(PixelVelocity PV)
		{ return ProjectionTransform.GetEquatorialVelocity(LinearTransform.GetProjectionVelocity(PV)); }

		public PixelVelocity GetPixelVelocity(EquatorialVelocity EV)
		{ return LinearTransform.GetPixelVelocity(ProjectionTransform.GetProjectionVelocity(EV)); }
	}

	public abstract class WCSProjectionTransform
	{
		/// <summary>
		/// Reference point Right Ascension.
		/// </summary>
		protected readonly double RA;

		/// <summary>
		/// Reference point Declination.
		/// </summary>
		protected readonly double Dec;
		public WCSProjectionTransform(double RA, double Dec)
		{ this.RA = RA; this.Dec = Dec; }

		public abstract EquatorialPoint GetEquatorialPoint(ProjectionPoint Point);
		public abstract EquatorialPoint[] GetEquatorialPoints(ProjectionPoint[] Points);
		public abstract List<EquatorialPoint> GetEquatorialPoints(IEnumerable<ProjectionPoint> Points);
		public abstract ProjectionPoint GetProjectionPoint(EquatorialPoint Point);
		public abstract ProjectionPoint[] GetProjectionPoints(EquatorialPoint[] Points);
		public abstract List<ProjectionPoint> GetProjectionPoints(IEnumerable<EquatorialPoint> Points);
		public abstract EquatorialVelocity GetEquatorialVelocity(ProjectionVelocity PV);
		public abstract ProjectionVelocity GetProjectionVelocity(EquatorialVelocity EV);
		
		/// <summary>
		/// Estimated linear distance derivative for quick computation of image distances and velocities.
		/// </summary>
		/// <returns></returns>
		[Obsolete]
		public abstract double GetEstimatedWCSChainDerivative();

		/// <summary>
		/// Retrieves the coordinates of the reference point of the projection.
		/// </summary>
		/// <param name="RA">Reference point Right Ascension.</param>
		/// <param name="Dec">Reference point Declination.</param>
		public abstract void GetReferencePoints(out double RA, out double Dec);
		
		/// <summary>
		/// Name of the projection algorithm (tag).
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Description of the projection algorithm (ex. full algorithm name).
		/// </summary>
		public abstract string Description { get; }
	}
#pragma warning restore 1591

	/// <summary>
	/// Attribute for recognizing WCS projection algorithms.
	/// </summary>
	public class ProjectionAttribute : Attribute
	{
		/// <summary>
		/// Name tag of the projection.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Description text of the projection.
		/// </summary>
		public readonly string Description;

		public ProjectionAttribute(string ProjectionTag, string ProjectionDescription)
		{
			Name = ProjectionTag;
			Description = ProjectionDescription;
		}
	}
}
