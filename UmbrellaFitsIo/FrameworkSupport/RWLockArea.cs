using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Umbrella2.Framework
{
	/// <summary>
	/// Provides readers-writers lock for images.
	/// </summary>
    class RWLockArea
    {
		/// <summary>
		/// List of areas locked. Should usually be small enough to be kept in a list (rather than some other more complex data structure).
		/// Tuple order: Area, Writer lock, Locking thread, Locking token.
		/// </summary>
		/// <remarks>Thread safety in reading internal lock state is obtained by locking this list.</remarks>
		List<Tuple<Rectangle, bool, Thread, Guid>> Areas;
		ManualResetEvent WaitingMechanism;

		public RWLockArea()
        {
            Areas = new List<Tuple<Rectangle, bool, Thread, Guid>>();
			WaitingMechanism = new ManualResetEvent(false);
        }

		/// <summary>
		/// Acquire a read or write lock.
		/// </summary>
		/// <param name="zone">Image area over which to get the lock.</param>
		/// <param name="WriteLock">True if the lock is a writer lock, false if it is a reader lock.</param>
		/// <returns>A lock token.</returns>
		public Guid EnterLock(Rectangle zone, bool WriteLock)
		{
			/* Thread-local lock sampling variable */
			bool islocked = false;

			/* The output Guid */
			Guid g = Guid.NewGuid();

			/* Retry until lock is acquired. */
			do
			{
				lock (Areas)
				{
					/* Check if our area intersects with any other area and (if read lock) whether the intersecting areas are writable */
					foreach (var rs in Areas)
					{
						/* Cannot acquire new lock if another lock is held by the same thread. Release previous lock first. */
						if (rs.Item3 == Thread.CurrentThread)
							throw new LockRecursionException("Attempted to acquire an area lock when another area lock is already held");

						/* Check if our area is modified by someone else */
						islocked = zone.IntersectsWith(rs.Item1) & (rs.Item2 | WriteLock);
						if (islocked) break;
					}
					if (!islocked)
					{
						/* Clear to acquire lock, so acquiring. */
						Areas.Add(new Tuple<Rectangle, bool, Thread, Guid>(zone, false, Thread.CurrentThread, g));
						return g;
					}
				}
				/* We cannot lock the data, so we wait for lock state to change and try again. */
				WaitingMechanism.WaitOne();
			} while (islocked);
			throw new Exception("Invalid program state.");
		}

		/// <summary>
		/// Exits an acquired lock.
		/// </summary>
		/// <param name="Lock">The lock token.</param>
        public void ExitLock(Guid Lock)
        {
			ExitLock(Lock, false);
        }

		/// <summary>
		/// Force exits locks held by other threads.
		/// </summary>
		/// <param name="Lock">The lock token.</param>
		public void ForceExitLock(Guid Lock)
		{
			ExitLock(Lock, true);
		}

		void ExitLock(Guid Lock, bool Force)
		{
			bool lockExists = false;
			lock (Areas)
			{
				Areas.RemoveAll(delegate (Tuple<Rectangle, bool, Thread, Guid> obj)
				{
					if (obj.Item4 == Lock)
					{
						lockExists = true;
						if (obj.Item3 != Thread.CurrentThread)
						{
							if (Force) System.Diagnostics.Debug.WriteLine("Exited a lock held by a thread other than the acquiring thread");
							else throw new SynchronizationLockException("Tried to exit a lock held by a different thread.");
						}
						return true;
					}
					return false;
				});
			}
			if (!lockExists)
				throw new KeyNotFoundException("No lock held");

			/* Wake up waiting threads */
			WaitingMechanism.Set();
		}
	}
}
