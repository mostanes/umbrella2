using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UmbrellaFitsIo.FrameworkSupport
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
		List<Thread> WaitingThreads;

		public RWLockArea()
        {
            Areas = new List<Tuple<Rectangle, bool, Thread, Guid>>();
			WaitingThreads = new List<Thread>();
        }

		/// <summary>
		/// Acquire a read or write lock.
		/// </summary>
		/// <param name="zone">Image area over which to get the lock.</param>
		/// <returns>A lock token.</returns>
        public Guid EnterLock(Rectangle zone, bool WriteLock)
        {
			/* Retry until lock is acquired. */
        retry:
			/* Thread-local lock sampling variable */
            bool islocked = false;
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
                    Guid g = Guid.NewGuid();
                    Areas.Add(new Tuple<Rectangle, bool, Thread, Guid>(zone, false, Thread.CurrentThread, g));
                    return g;
                }
            }
			/* We cannot lock the data, so we wait. */
            if (islocked)
            {
				/* Wait for lock state to change and try again. */
				lock (WaitingThreads)
					if (!WaitingThreads.Contains(Thread.CurrentThread))
						WaitingThreads.Add(Thread.CurrentThread);

				try { Thread.Sleep(Timeout.Infinite); }
				catch (ThreadInterruptedException) { }

				goto retry;
            }

			/* Remove ourselves from the waiting list. */
			lock (WaitingThreads)
				if (WaitingThreads.Contains(Thread.CurrentThread))
					WaitingThreads.Remove(Thread.CurrentThread);
			throw new Exception();
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

		public void ExitLock(Guid Lock, bool Force)
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
			foreach (Thread t in WaitingThreads)
			{
				t.Interrupt();
			}
		}
	}
}
