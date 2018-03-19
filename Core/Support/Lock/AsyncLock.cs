using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// Based on https://github.com/neosmart/AsyncLock

namespace YetaWF.Core.Support {

    public class AsyncLock {

        private object _reentrancy = new object();
        private int _reentrances = 0;
        //We are using this SemaphoreSlim like a posix condition variable
        //we only want to wake waiters, one or more of whom will try to obtain a different lock to do their thing
        //so long as we can guarantee no wakes are missed, the number of awakees is not important
        //ideally, this would be "friend" for access only from InnerLock, but whatever.
        internal SemaphoreSlim _retry = new SemaphoreSlim(0, 1);
        //We do not have System.Threading.Thread.* on .NET Standard without additional dependencies
        //Work around is easy: create a new ThreadLocal<T> with a random value and this is our thread id :)
        //private static readonly long UnlockedThreadId = 0; //"owning" thread id when unlocked
        //internal long _owningId = UnlockedThreadId;
        private static readonly YetaWFManager UnlockedOwner = null; //"owning" thread id when unlocked
        internal YetaWFManager _owningManager = UnlockedOwner;
        //private static int _globalThreadCounter;
        //private static readonly ThreadLocal<int> _threadId = new ThreadLocal<int>(() => Interlocked.Increment(ref _globalThreadCounter));
        //We generate a unique id from the thread ID combined with the task ID, if any
        //public static long ThreadId => (long)(((ulong)_threadId.Value) << 32) | ((uint)(Task.CurrentId ?? 0));
        public static YetaWFManager ThreadOwner => YetaWFManager.Manager;

        struct InnerLock : IDisposable {
            private readonly AsyncLock _parent;
#if DEBUG
            private bool _disposed;
#endif

            internal InnerLock(AsyncLock parent) {
                _parent = parent;
#if DEBUG
                _disposed = false;
#endif                
            }

            internal async Task ObtainLockAsync() {
                while (!TryEnter()) {
                    //we need to wait for someone to leave the lock before trying again
                    await _parent._retry.WaitAsync();
                }
            }

            internal async Task ObtainLockAsync(CancellationToken ct) {
                while (!TryEnter()) {
                    //we need to wait for someone to leave the lock before trying again
                    await _parent._retry.WaitAsync(ct);
                }
            }

            internal void ObtainLock() {
                while (!TryEnter()) {
                    //we need to wait for someone to leave the lock before trying again
                    _parent._retry.Wait(); // Sync wait because a sync Lock was requested
                }
            }

            private bool TryEnter() {
                lock (_parent._reentrancy) {
                    Debug.Assert((_parent._owningManager == UnlockedOwner) == (_parent._reentrances == 0));
                    if (_parent._owningManager != UnlockedOwner && _parent._owningManager != AsyncLock.ThreadOwner) {
                        //another thread currently owns the lock
                        return false;
                    }
                    //we can go in
                    Interlocked.Increment(ref _parent._reentrances);
                    _parent._owningManager = AsyncLock.ThreadOwner;
                    return true;
                }
            }

            public void Dispose() {
#if DEBUG
                Debug.Assert(!_disposed);
                _disposed = true;
#endif
                lock (_parent._reentrancy) {
                    Interlocked.Decrement(ref _parent._reentrances);
                    if (_parent._reentrances == 0) {
                        //the owning thread is always the same so long as we are in a nested stack call
                        //we reset the owning id to null only when the lock is fully unlocked
                        _parent._owningManager = UnlockedOwner;
                        if (_parent._retry.CurrentCount == 0) {
                            _parent._retry.Release();
                        }
                    }
                }
            }
        }

        private IDisposable IntLock() {
            var @lock = new InnerLock(this);
            @lock.ObtainLock();
            return @lock;
        }

        private async Task<IDisposable> IntLockAsync() {
            var @lock = new InnerLock(this);
            await @lock.ObtainLockAsync();
            return @lock;
        }

        //private async Task<IDisposable> LockAsync(CancellationToken ct) {
        //    var @lock = new InnerLock(this);
        //    await @lock.ObtainLockAsync(ct);
        //    return @lock;
        //}

        public async Task<IDisposable> LockAsync() {
            if (YetaWFManager.IsSync()) {
                return IntLock();
            } else {
                return await IntLockAsync();
            }
        }
        public IDisposable Lock() {
            return IntLock();
        }
    }
}
