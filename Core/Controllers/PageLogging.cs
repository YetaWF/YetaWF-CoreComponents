/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Implements page access logging.
    /// </summary>
    /// <remarks>
    /// Applications can register a callback which is called whenever a page is requested.
    ///
    /// The callback is called whenever a page is requested, for both full page requests and partial "Single Page Application" requests.
    /// </remarks>
    public static class PageLogging {

        private static object _lockObject = new object();

        private static List<Func<string, bool, Task>> Callbacks { get; set; }

        /// <summary>
        /// Register an application-specific callback which is called whenever a page is requested.
        /// </summary>
        /// <param name="callback">A callback which is called whenever a page is requested.</param>
        /// <remarks>
        /// The callback is called whenever a page is requested, for both full page requests and partial "Single Page Application" requests.
        ///
        /// The callback receives the URL requested (string) and whether the request is for a full page (bool).
        ///
        /// Callbacks cannot be unregistered.
        /// </remarks>
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
