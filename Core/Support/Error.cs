/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Log;

namespace YetaWF.Core.Support {

    public class Error : Exception {
        public Error(string message, params object?[] parms) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message, parms))) { }
        public Error(string message) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message))) { }
    }
    public class InternalError : Exception {
        private const string IntErr = "Internal Error: ";
        public InternalError(string message, params object?[] parms) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message.StartsWith(IntErr) ? message : IntErr+message, parms))) { }
        public InternalError(string message) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message.StartsWith(IntErr) ? message : IntErr + message))) { }
    }

    public static class ErrorHandling {

        private static object _lockObject = new object();

        private static List<Action<string>>? Callbacks { get; set; }

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

        public static string FormatExceptionMessage(Exception? exc) {
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
