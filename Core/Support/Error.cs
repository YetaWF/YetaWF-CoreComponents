/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.Log;

namespace YetaWF.Core.Support {

    /// <summary>
    /// Represents errors that occur and are normal errors which are user facing. These typically occur due to user input which is invalid.
    /// These error messages should be localized.
    /// </summary>
    /// <remarks>Any errors that are instantiated with this class are logged.</remarks>
    public class Error : Exception {
        /// <summary>
        /// Initializes a new instance with the specified error message.
        /// </summary>
        /// <param name="message">Defines the error message using a composite format string.</param>
        /// <param name="parms">An array of objects to format.</param>
        public Error(string message, params object?[] parms) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message, parms))) { }
        /// <summary>
        /// Initializes a new instance with the specified error message.
        /// </summary>
        /// <param name="message">Defines the error message.</param>
        public Error(string message) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message))) { }
    }
    /// <summary>
    /// Represents errors that occur, but are considered internal errors. These may be shown to end-users also, but are intended mainly for development to find unusual conditions.
    /// These error messages do not need to be localized.
    /// </summary>
    /// <remarks>Any errors that are instantiated with this class are logged.</remarks>
    public class InternalError : Exception {
        private const string IntErr = "Internal Error: ";
        /// <summary>
        /// Initializes a new instance with the specified error message.
        /// </summary>
        /// <param name="message">Defines the error message using a composite format string.</param>
        /// <param name="parms">An array of objects to format.</param>
        public InternalError(string message, params object?[] parms) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message.StartsWith(IntErr) ? message : IntErr+message, parms))) { }
        /// <summary>
        /// Initializes a new instance with the specified error message.
        /// </summary>
        /// <param name="message">Defines the error message.</param>
        public InternalError(string message) : base(ErrorHandling.HandleCallbacks(Logging.AddErrorLog(message.StartsWith(IntErr) ? message : IntErr + message))) { }
    }

    /// <summary>
    /// Implements error handling helpers for applications.
    /// </summary>
    public static class ErrorHandling {

        private static object _lockObject = new object();

        private static List<Action<string>>? Callbacks { get; set; }

        /// <summary>
        /// Registers a callback that is called whenever an error occurs. This can be used by applications for logging purposes, for example.
        /// </summary>
        /// <param name="callback">The callback to call when an error occurs.</param>
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

        /// <summary>
        /// Returns a fully formatted error message, including text of any inner exceptions. It also handles any AggregateExceptions.
        /// </summary>
        /// <param name="exc">The exception for which the message is returned.</param>
        /// <returns>A fully formatted error message, including text of any inner exceptions. It also handles any AggregateExceptions.</returns>
        /// <remarks>The returned error message string may not be fully localized as inner exceptions or internal errors are not localized and will be shown in the operating system's default language.</remarks>
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
