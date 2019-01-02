﻿/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Identity;
using YetaWF.Core.Log;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Implements management of built-in commands (urls that don't match a page).
    /// Classes typically add built-in command during application startup.
    /// </summary>
    public static class BuiltinCommands {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private class BuiltinCommandEntry {
            public string Command { get; set; }
            public string Resource { get; set; }
            public Func<QueryHelper, Task> Callback { get; set; }
        }
        private class BuiltinCommandDictionary : Dictionary<string, BuiltinCommandEntry> { }

        private static BuiltinCommandDictionary Commands = new BuiltinCommandDictionary { };

        public static void Add(string url, string resourceName, Func<QueryHelper, Task> func) {
            Commands.Add(url, new BuiltinCommandEntry { Command = url.ToLower(), Resource = resourceName, Callback = func });
        }

        public static async Task<Func<QueryHelper, Task>> FindAsync(string url, bool checkAuthorization = true) {
            BuiltinCommandEntry entry;
            // find the built-in command
            if (!Commands.TryGetValue(url.ToLower(), out entry)) return null;
            // verify authorization
            if (checkAuthorization && (!await Resource.ResourceAccess.IsResourceAuthorizedAsync(entry.Resource) || Manager.IsDemo)) {
                Logging.AddErrorLog("403 Not Authorized - not authorized for builtin command");
                return null;// pretend it doesn't exist
            }
            // return the action
            return entry.Callback;
        }
    }
}
