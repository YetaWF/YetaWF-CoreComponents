/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YetaWF.Core.Controllers {

    public static class PageLogging {

        private static object _lockObject = new object();

        private static List<Func<string, bool, Task>> Callbacks { get; set; }

        public static void RegisterCallback(Func<string, bool, Task> callback) {
            lock (_lockObject) { // used during startup to sync registrations
                if (Callbacks == null) Callbacks = new List<Func<string, bool, Task>>();
                if (!Callbacks.Contains(callback))
                    Callbacks.Add(callback);
            }
        }

        internal static async Task HandleCallbacksAsync(string url, bool full) {
            if (Callbacks != null) {
                foreach (Func<string, bool, Task> callback in Callbacks.ToList()) {
                    await callback(url, full);
                }
            }
        }
    }
}
