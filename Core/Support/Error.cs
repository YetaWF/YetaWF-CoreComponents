/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
#if MVC6
using Microsoft.AspNetCore.Mvc.ModelBinding;
#else
using System.Web.WebPages.Html;
#endif
using YetaWF.Core.Log;

namespace YetaWF.Core.Support {

    [Serializable]
    public class Error : Exception {
        public Error(string message, params object[] parms) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message, parms))) { }
        public Error(string message) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message))) { }
    }
    [Serializable]
    public class InternalError : Exception {
        private const string IntErr = "Internal Error: ";
        public InternalError(string message, params object[] parms) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message.StartsWith(IntErr) ? message : IntErr+message, parms))) { }
        public InternalError(string message) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message.StartsWith(IntErr) ? message : IntErr + message))) { }
    }

#if MVC6
#else
    public static class ModelStateDictionaryExtender {
        public static void AddModelError(this ModelStateDictionary dict, string key, string errorMessage, params object[] parms) {
            dict.AddError(key, string.Format(errorMessage, parms));
        }
    }
#endif
    public static class ErrorHandling {

        private static object _lockObject = new object();

        private static List<Action<string>> Callbacks { get; set; }

        public static void RegisterCallback(Action<string> callback) {
            lock (_lockObject) { // short-term lock to sync registration during startup
                if (Callbacks == null) Callbacks = new List<Action<string>>();
                if (!Callbacks.Contains(callback))
                    Callbacks.Add(callback);
            }
        }

        internal static string HandleCallbacks(string message) {
            if (Callbacks != null) {
                foreach (Action<string> callback in Callbacks.ToList()) {
                    callback(message);// no await, as in fire and forget
                }
            }
            return message;
        }

        public static string FormatExceptionMessage(Exception exc) {
            if (exc == null) return "";
            string message = "(none)";
            if (exc.Message != null && !string.IsNullOrWhiteSpace(exc.Message))
                message = exc.Message;
            if (exc is AggregateException) {
                AggregateException aggrExc = (AggregateException)exc;
                foreach (Exception innerExc in aggrExc.InnerExceptions) {
                    string s = FormatExceptionMessage(innerExc);
                    if (s != null)
                        message += " - " + s;
                }
            } else {
                while (exc.InnerException != null) {
                    exc = exc.InnerException;
                    string s = FormatExceptionMessage(exc);
                    if (s != null)
                        message += " - " + s;
                }
            }
            return message;
        }
    }
}
