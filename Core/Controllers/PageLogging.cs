/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;

namespace YetaWF.Core.Controllers {

    public static class PageLogging {

        private static object _lockObject = new object();

        private static List<Action<string, bool>> Callbacks { get; set; }

        public static void RegisterCallback(Action<string, bool> callback) {
            lock (_lockObject) {
                if (Callbacks == null) Callbacks = new List<Action<string, bool>>();
                if (!Callbacks.Contains(callback))
                    Callbacks.Add(callback);
            }
        }

        internal static void HandleCallbacks(string url, bool full) {
            if (Callbacks != null) {
                foreach (Action<string, bool> callback in Callbacks.ToList()) {
                    callback(url, full);
                }
            }
        }
    }
}
