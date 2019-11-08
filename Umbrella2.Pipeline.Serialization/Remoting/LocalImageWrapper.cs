using System;
using System.Collections.Generic;
using Umbrella2.IO;

namespace Umbrella2.Pipeline.Serialization.Remoting
{
	/// <summary>
	/// Wrapping class for a <see cref="Image"/>. Exposes the swap function, which must be remoted.
	/// </summary>
	public class LocalImageWrapper : IFitsRemoteChannel
	{
		Image Local;
		Dictionary<Guid, ImageData> Held;

		public LocalImageWrapper(Image LocalImage)
		{ Local = LocalImage; Held = new Dictionary<Guid, ImageData>(); }

		public void Swap(RemoteData RD)
		{
			Guid Token;
			ImageData ImD;

			switch (RD.Command)
			{
				case Command.ExitLock:
					Token = new Guid(RD.Token);
					lock (Held)
						ImD = Held[Token];
					if (!ImD.ReadOnly)
						Buffer.BlockCopy(RD.Data, 0, ImD.Data, 0, RD.Data.Length * sizeof(double));
					Local.ExitLock(ImD);
					RD.Data = null;
					lock (Held)
						Held.Remove(Token);
					return;
				case Command.SwitchLock:
					Token = new Guid(RD.Token);
					lock (Held)
					{ ImD = Held[Token]; Held.Remove(Token); }
					if (!ImD.ReadOnly)
						Buffer.BlockCopy(RD.Data, 0, ImD.Data, 0, RD.Data.Length * sizeof(double));
					ImD = Local.SwitchLockData(ImD, RD.Args[0], RD.Args[1], (RD.Args[2] & 0x2) == 0x2, (RD.Args[2] & 0x1) == 0x1);
					RD.Data = ImD.Data;
					RD.Token = ImD.FDGuid.ToByteArray();
					lock (Held)
						Held.Add(ImD.FDGuid, ImD);
					return;
				case Command.LockData:
					ImD = Local.LockData(new System.Drawing.Rectangle(RD.Args[0], RD.Args[1], RD.Args[2], RD.Args[3]),
						(RD.Args[2] & 0x2) == 0x2, (RD.Args[2] & 0x1) == 0x1);
					lock (Held)
						Held.Add(ImD.FDGuid, ImD);
					RD.Token = ImD.FDGuid.ToByteArray();
					RD.Data = ImD.Data;
					return;
			}
		}
	}
}
