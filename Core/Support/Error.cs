/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.WebPages.Html;
using YetaWF.Core.Log;

namespace YetaWF.Core.Support {

    [Serializable]
    public class Error : Exception {
        public Error(string message, params object[] parms) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message, parms))) { }
        public Error(string message) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message))) { }
    }
    [Serializable]
    public class InternalError : Exception {
        public InternalError(string message, params object[] parms) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog("Internal Error: " + message, parms))) { }
        public InternalError(string message) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog("Internal Error: " + message))) { }
    }

    public static class ModelStateDictionaryExtender {
        public static void AddModelError(this ModelStateDictionary dict, string key, string errorMessage, params object[] parms) {
            dict.AddError(key, string.Format(errorMessage, parms));
        }
    }
    public static class ErrorHandling {

        private static object _lockObject = new object();

        private static List<Action<string>> Callbacks { get; set; }

        public static void RegisterCallback(Action<string> callback) {
            lock (_lockObject) {
                if (Callbacks == null) Callbacks = new List<Action<string>>();
                if (!Callbacks.Contains(callback))
                    Callbacks.Add(callback);
            }
        }

        internal static string HandleCallbacks(string message) {
            if (Callbacks != null) {
                foreach (Action<string> callback in Callbacks.ToList()) {
                    callback(message);
                }
            }
            return message;
        }
    }
}
