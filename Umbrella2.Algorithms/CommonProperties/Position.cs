using Umbrella2.PropertyModel;

namespace Umbrella2
{
	/// <summary>
	/// Represents an object position.
	/// </summary>
	public struct Position : IExtensionProperty
	{
		/// <summary>The position in equatorial coordinates.</summary>
		[PropertyDescription(true)]
		public readonly EquatorialPoint EP;
		/// <summary>The position in image coordinates.</summary>
		[PropertyDescription(true)]
		public readonly PixelPoint PP;

		/// <summary>Creates a new instance of given equatorial coordinates and image coordinates.</summary>
		/// <param name="EqPoint">Equatorial coordinates.</param>
		/// <param name="PixPoint">Image coordinates.</param>
		public Position(EquatorialPoint EqPoint, PixelPoint PixPoint) { EP = EqPoint; PP = PixPoint; }

		/// <summary>Extracts the equatorial coordinates of a given position.</summary>
		public static implicit operator EquatorialPoint(Position p) => p.EP;

		/// <summary>Extracts the image coordinates of a given position.</summary>
		public static implicit operator PixelPoint(Position p) => p.PP;
	}
}
