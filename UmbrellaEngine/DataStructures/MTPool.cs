using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Umbrella2.Algorithms.Misc
{
	/// <summary>
	/// Multithreaded object pool.
	/// </summary>
	class MTPool<T>
	{
		/// <summary>
		/// Object status (whether the object is in the pool or held by other code.
		/// </summary>
		enum Status : byte
		{
			Held,
			InPool
		}

		Dictionary<int, T> Pool;
		Dictionary<int, Status> PoolStatus;
		public Func<T> Constructor = null;

		/// <summary>
		/// Creates a new object pool.
		/// </summary>
		public MTPool()
		{ Pool = new Dictionary<int, T>(); PoolStatus = new Dictionary<int, Status>(); }

		/// <summary>
		/// Acquires an object instance from the pool.
		/// </summary>
		/// <returns></returns>
		public T Acquire()
		{
			lock (this)
			{
				int TID = Thread.CurrentThread.ManagedThreadId;
				if (Pool.ContainsKey(TID))
				{
					if (PoolStatus[TID] == Status.InPool)
					{
						PoolStatus[TID] = Status.Held;
						return Pool[Thread.CurrentThread.ManagedThreadId];
					}
					else throw new InvalidOperationException("Cannot double-book");
				}
				else
				{
					Pool.Add(TID, Constructor());
					PoolStatus.Add(TID, Status.Held);
					return Pool[TID];
				}
			}
		}

		/// <summary>
		/// Releases an object instance back in the pool.
		/// </summary>
		public void Release()
		{
			lock (this)
			{
				try
				{ PoolStatus[Thread.CurrentThread.ManagedThreadId] = Status.InPool; }
				catch (KeyNotFoundException e)
				{ throw new InvalidOperationException("No resources held by given thread", e); }
			}
		}
	}
}
