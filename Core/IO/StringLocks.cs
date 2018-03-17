/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.IO {

    /// <summary>
    /// Simple locking mechanism based on a string - don't abuse.
    /// Used to lock resources by name - the name is arbitrary as long as they're consistent.
    /// This can only be used to lock a resource WITHIN one instance of YetaWF. To lock across processes, an operating system locking mechanism must be used.
    /// </summary>
    public static class StringLocks {

        class LockedObject {
            public string Name;
            public int UseCount;
            public AsyncLock Lock;
            public LockedObject() {
                Lock = new AsyncLock();
            }
        };

        private static readonly Dictionary<string, LockedObject> _locks = new Dictionary<string, LockedObject>();
        private static AsyncLock _lockObject = new AsyncLock();

        public static void DoAction(string s, Action action) {

            LockedObject obj = null;

            // Lock the resource by name, by adding an entry in the _locks dictionary or updating the use count
            using (_lockObject.Lock()) {
                if (!_locks.TryGetValue(s, out obj)) {
                    obj = new LockedObject { Name=s, UseCount=1 };
                    _locks.Add(s, obj);
                } else {
                    obj.UseCount++;
                }
            }
            using (obj.Lock.Lock()) {
                action();
            }
            // Unlock the resource by name, by removing the entry in the _locks dictionary or decrementing the use count
            using (_lockObject.Lock()) {
                obj = null;
                if (!_locks.TryGetValue(s, out obj))
                    throw new InternalError("An entry must be present - someone else removed it - due to a usecount mismatch?");
                --obj.UseCount;
                if (obj.UseCount <= 0) {
                    _locks.Remove(s);
                }
            }
        }

        public static async Task DoActionAsync(string s, Func<Task> action) {

            if (YetaWFManager.IsSync()) {
                DoAction(s, () => {
                    action().Wait(); // sync Wait because we in sync mode
                });
            } else {

                LockedObject obj = null;

                // Lock the resource by name, by adding an entry in the _locks dictionary or updating the use count
                using (await _lockObject.LockAsync()) {
                    if (!_locks.TryGetValue(s, out obj)) {
                        obj = new LockedObject { Name = s, UseCount = 1 };
                        _locks.Add(s, obj);
                    } else {
                        obj.UseCount++;
                    }
                }
                using (await obj.Lock.LockAsync()) {
                    await action();
                }
                // Unlock the resource by name, by removing the entry in the _locks dictionary or decrementing the use count
                using (await _lockObject.LockAsync()) {
                    obj = null;
                    if (!_locks.TryGetValue(s, out obj))
                        throw new InternalError("An entry must be present - someone else removed it - due to a usecount mismatch?");
                    --obj.UseCount;
                    if (obj.UseCount <= 0) {
                        _locks.Remove(s);
                    }
                }
            }
        }
    }
}
