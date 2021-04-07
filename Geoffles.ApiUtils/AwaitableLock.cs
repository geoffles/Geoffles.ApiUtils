using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Geoffles.ApiUtils
{
    public class LockUnavailableException : Exception
    {
        public LockUnavailableException() : base("The lock is not currently available") { }
    }

    public class AwaitableLock
    {
       

        public interface ILockRelease : IDisposable { }


        /// <summary>
        /// This class represents a thread independent lock instance. Call Dispose to release the lock.
        /// </summary>
        private sealed class LockRelease : ILockRelease
        {
            public AwaitableLock Lock { get; private set; }

            public LockRelease(AwaitableLock @lock)
            {
                Lock = @lock;
            }

            private void Release()
            {
                if (Lock != null)
                {
                    Lock.Release();
                    Lock = null;
                }
            }

            public void Dispose()
            {
                Release();
            }
        }

        /// <summary>
        /// Encapsulation of the lock release task
        /// </summary>
        private class ReleaseTask : Task<ILockRelease>
        {
            public ReleaseTask(AwaitableLock @lock, CancellationToken token) : base(() => new LockRelease(@lock), token) {}
        }

        private object _lockedStateAccessLock = new object();
        private Queue<ReleaseTask> _waitingLocks = new Queue<ReleaseTask>();

        /// <summary>
        /// Unsynchronised read state. Do not use for synchronisation
        /// </summary>
        public bool IsLocked { get; private set; }

        /// <summary>
        /// The number of waiting locks
        /// </summary>
        public int QueueLength
        {
            get { return _waitingLocks.Count; }
        }

        /// <summary>
        /// Gets a task that will start once lock is acquired. Call Dispose on the result to release the lock.
        /// </summary>
        /// <returns></returns>

        public ILockRelease Acquire()
        {
            ILockRelease result;
            if (TryAcquire(out result))
            {
                return result;
            }
            else
            {
                throw new LockUnavailableException();
            }
        }

        public bool TryAcquire(out ILockRelease lockRelease)
        {
            lock (_lockedStateAccessLock)
            {
                if (!IsLocked)
                {
                    lockRelease = new LockRelease(this);
                    return true;
                }
                else
                {
                    lockRelease = null;
                    return false;
                }
            }
        }

        public Task<ILockRelease> WaitOneAsync(CancellationToken token = default(CancellationToken))
        {
            lock (_lockedStateAccessLock)
            {
                if (!IsLocked)
                {
                    IsLocked = true;
                    var waitingTask = new ReleaseTask(this, token);
                    waitingTask.Start();
                    return waitingTask;
                }
                else
                {
                    var waitingTask = new ReleaseTask(this, token);
                    _waitingLocks.Enqueue(waitingTask);
                    return waitingTask;
                }
            }
        }

        private void Release()
        {
            lock (_lockedStateAccessLock)
            {
                if (!IsLocked)
                {
                    throw new Exception("Release called on unlocked lock");
                }

                if (_waitingLocks.Any())
                {
                    while (_waitingLocks.Any())
                    {
                        var waitingTask = _waitingLocks.Dequeue();
                        if (!waitingTask.IsCanceled)
                        {
                            waitingTask.Start();
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    IsLocked = false;
                }
                
            }
        }
    }
}
