using System;
using Umbrella2.IO;
using HeaderTable = System.Collections.Generic.Dictionary<string, Umbrella2.IO.MetadataRecord>;
using PropDict = System.Collections.Generic.Dictionary<System.Type, Umbrella2.IO.ImageProperties>;

namespace Umbrella2.Pipeline.Serialization.Remoting
{
	/// <summary>
	/// Surrogate for an image on a remote location.
	/// </summary>
	public class RemoteImageSurrogate : IO.Image
	{
		/// <summary>Channel reference number. Used to separate between different images.</summary>
		int Reference;
		/// <summary>Communication channel to the remote location.</summary>
		IFitsRemoteChannel CommChannel;
		/// <summary>Synchronization for single-thread usage.</summary>
		bool HeldToken = false;

		/// <summary>Creates a <see cref="RemoteImageSurrogate"/> from a given set of headers, channel reference and communication channel.</summary>
		public RemoteImageSurrogate(int ChannelRef, IFitsRemoteChannel Channel, ICHV Header, PropDict Properties) : base(Header, Properties)
		{
			Reference = ChannelRef;
			CommChannel = Channel;
		}

		public override void ExitLock(ImageData Data)
		{
			lock (this)
				HeldToken = false;
			double[,] Dt = Data.ReadOnly ? null : Data.Data;
			RemoteData RD = new RemoteData() { Args = null, Command = Command.ExitLock, Data = Dt, Reference = Reference, Token = Data.FDGuid.ToByteArray() };
			CommChannel.Swap(RD);
		}

		public override ImageData LockData(System.Drawing.Rectangle Area, bool FillZero, bool RO = true)
		{
#warning Should do something about multi-threaded image remoting
			while(true)
			{
				lock (this)
					if (!HeldToken) { HeldToken = true; break; }
				System.Threading.Thread.Sleep(10);
			}

			double[,] Data = new double[Area.Height, Area.Width];
			int ArgBool = (FillZero ? 2 : 0) + (RO ? 1 : 0);
			int[] Args = new int[5] { Area.X, Area.Y, Area.Width, Area.Height, ArgBool };
			RemoteData RD = new RemoteData() { Args = Args, Command = Command.LockData, Data = Data, Reference = Reference, Token = null };

			CommChannel.Swap(RD);

			ImageData imd = new ImageData(Area, RD.Data, this, RO, new Guid(RD.Token));
			return imd;
		}

		public override ImageData SwitchLockData(ImageData Data, int NewX, int NewY, bool FillZero, bool RO = true)
		{
			int ArgBool = (FillZero ? 2 : 0) + (RO ? 1 : 0);
			int[] Args = new int[3] { NewX, NewY, ArgBool };
			RemoteData RD = new RemoteData() { Args = Args, Command = Command.SwitchLock, Data = Data.Data, Reference = Reference, Token = Data.FDGuid.ToByteArray() };

			CommChannel.Swap(RD);

			System.Drawing.Rectangle Area = new System.Drawing.Rectangle(NewX, NewY, Data.Position.Width, Data.Position.Height);
			return new ImageData(Area, RD.Data, this, RO, new Guid(RD.Token));
		}
	}
}
