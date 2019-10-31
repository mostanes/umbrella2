using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Umbrella2;
using Umbrella2.PropertyModel;
using static System.Math;

namespace Umbrella2.PropertyModel.CommonProperties
{
	/// <summary>
	/// Holds information on the size of the object.
	/// </summary>
	public class ObjectSize : IExtensionProperty
	{
		/// <summary>The elliptical fit over the objects pixels.</summary>
		[PropertyDescription(true)]
		public SourceEllipse PixelEllipse;

		/// <summary>The intensity-weighted elliptical fit over the object's pixels.</summary>
		[PropertyDescription(true)]
		public SourceEllipse BarycentricEllipse;
	}

	/// <summary>
	/// Represents an elliptical fit of a source's pixels.
	/// </summary>
	public struct SourceEllipse
	{
		/// <summary>Trigonometric angle of the major semiaxis.</summary>
		public double SemiaxisMajorAngle;
		/// <summary>Major semiaxis of the elliptical fit.</summary>
		public double SemiaxisMajor;
		/// <summary>Minor semiaxis of the elliptical fit.</summary>
		public double SemiaxisMinor;

		/// <summary>
		/// Creates a new elliptical fit over a set of data.
		/// </summary>
		/// <param name="XX">Mean X*X.</param>
		/// <param name="XY">Mean X*Y.</param>
		/// <param name="YY">Mean Y*Y.</param>
		public SourceEllipse(double XX, double XY, double YY)
		{
			/* Discriminant of 2nd order eigenvalue polynomial */
			double Msq = Sqrt(XX * XX + 4 * XY * XY - 2 * XX * YY + YY * YY);
			/* Eigenvalues */
			double L1 = 1.0 / 2 * (XX + YY - Msq);
			double L2 = 1.0 / 2 * (XX + YY + Msq);
			/* Trig angle of eigenvectors */
			double A1 = Atan2(2 * XY, -(-XX + YY + Msq));
			double A2 = Atan2(2 * XY, -(-XX + YY - Msq));
			if (L1 > L2) { SemiaxisMajorAngle = A1; SemiaxisMajor = 2 * Sqrt(L1); SemiaxisMinor = 2 * Sqrt(L2); }
			else { SemiaxisMajorAngle = A2; SemiaxisMajor = 2 * Sqrt(L2); SemiaxisMinor = 2 * Sqrt(L1); }
		}

		public override string ToString()
		{
			return "a = " + SemiaxisMajor.ToString("G6") + "; b = " + SemiaxisMinor.ToString("G6");
		}
	}
}
