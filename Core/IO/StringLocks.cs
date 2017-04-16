/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
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
        };

        private static readonly Dictionary<string, LockedObject> _locks = new Dictionary<string, LockedObject>();

        public static void DoAction(string s, Action action) {
            LockedObject obj = null;
            // Lock the resource by name, by adding an entry in the _locks dictionary or updating the use count
            lock (_locks) {
                if (!_locks.TryGetValue(s, out obj)) {
                    obj = new LockedObject { Name=s, UseCount=1 };
                    _locks.Add(s, obj);
                } else {
                    obj.UseCount++;
                }
            }
            lock (obj) {
                action();
            }
            // Unlock the resource by name, by removing the entry in the _locks dictionary or decrementing the use count
            lock (_locks) {
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
