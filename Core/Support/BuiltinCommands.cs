/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Identity;
using YetaWF.Core.Log;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Implements management of built-in commands for URLs that don't map to a page.
    /// Packages typically add built-in commands during application startup and commands should be reserved for internal use.
    /// </summary>
    public static class BuiltinCommands {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private class BuiltinCommandEntry {
            public string Command { get; set; } = null!;
            public string Resource { get; set; } = null!;
            public Func<QueryHelper, Task> Callback { get; set; } = null!;
        }
        private class BuiltinCommandDictionary : Dictionary<string, BuiltinCommandEntry> { }

        private static BuiltinCommandDictionary Commands = new BuiltinCommandDictionary { };

        /// <summary>
        /// Adds a built-in command.
        /// </summary>
        /// <param name="url">Defines the URL used to invoke the command being added. E.g., "/$this-is-a-test".</param>
        /// <param name="resourceName">The resource name used to protect the built-in command. Only users/roles with the specified resource name enabled have access to this command.
        /// In demo mode, the command is always protected and cannot be used.</param>
        /// <param name="func">The callback invoked when the specified command is used.</param>
        public static void Add(string url, string resourceName, Func<QueryHelper, Task> func) {
            Commands.Add(url, new BuiltinCommandEntry { Command = url.ToLower(), Resource = resourceName, Callback = func });
        }

        /// <summary>
        /// Used to locate a built-in command based on its URL.
        /// </summary>
        /// <param name="url">The URL of the built-in command.</param>
        /// <param name="checkAuthorization">true to check for authorization, be checking whether the use/role has the required resource name defined. Or false otherwise, which means no authorization checking is performed.</param>
        /// <returns>The callback to run the command. null is returned if the URL doesn't map to a built-in command.</returns>
        /// <remarks></remarks>
        public static async Task<Func<QueryHelper, Task>?> FindAsync(string url, bool checkAuthorization = true) {
            // find the built-in command
            if (!Commands.TryGetValue(url.ToLower(), out BuiltinCommandEntry? entry)) return null;
            // verify authorization
            if (checkAuthorization && (!await Resource.ResourceAccess.IsResourceAuthorizedAsync(entry.Resource) || (YetaWFManager.IsDemo || Manager.IsDemoUser))) {
                Logging.AddErrorLog("403 Not Authorized - not authorized for built-in command");
                return null;// pretend it doesn't exist
            }
            // return the action
            return entry.Callback;
        }
    }
}
