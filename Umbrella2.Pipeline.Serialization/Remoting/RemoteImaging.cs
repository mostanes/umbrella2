using System;
using System.Collections.Generic;
using Umbrella2.IO;

namespace Umbrella2.Pipeline.Serialization.Remoting
{
	/// <summary>Exposes the function that remotes a <see cref="Image"/>.</summary>
	public interface IFitsRemoteChannel
	{
		/// <summary>
		/// Performs the action specified in <paramref name="RD"/> and swaps the object's content with the response.
		/// </summary>
		/// <param name="RD">That that must be transferred to perform the remoting action.</param>
		void Swap(RemoteData RD);
	}

	/// <summary>
	/// The data that must be transferred accross domains to perform the remoted actions.
	/// </summary>
	public class RemoteData
	{
		/// <summary>Channel reference.</summary>
		public int Reference;
		/// <summary><see cref="ImageData"/> identification token.</summary>
		public byte[] Token;
		/// <summary>Remote function call arguments.</summary>
		public int[] Args;
		/// <summary>The data array for an <see cref="ImageData"/>.</summary>
		public double[,] Data;
		/// <summary>Command to execute on the remote image.</summary>
		public Command Command;

		/// <summary>Removes data that should not be returned.</summary>
		public void CleanForReturn() { Args = null; }
	}

	/// <summary>Command to be remoted.</summary>
	public enum Command
	{
		ExitLock,
		SwitchLock,
		LockData
	}

	/// <summary>
	/// Data that must be serialized and sent remotely to properly create a <see cref="RemoteImageSurrogate"/>.
	/// </summary>
	public class SerializationSurrogate
	{
		/// <summary>Channel reference.</summary>
		public int Reference;
		/// <summary>Original image properties.</summary>
		Dictionary<Type, ImageProperties> Properties;
		/// <summary>Original image headers.</summary>
		ICHV ICHV;

		/// <summary>Creates a <see cref="SerializationSurrogate"/> from the <paramref name="Img"/> on the <paramref name="Reference"/> channel.</summary>
		public SerializationSurrogate(int Reference, Image Img)
		{
			this.Reference = Reference;
			Properties = Img.GetAllProperties();
			ICHV = Img.GetICHV();
		}

		/// <summary>Unpacks the serialized data to an image surrogate.</summary>
		public RemoteImageSurrogate GetRemoteSurrogate(IFitsRemoteChannel RemotingChannel) =>
			new RemoteImageSurrogate(Reference, RemotingChannel, ICHV, Properties);
	}
}
