/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.Log {

    /// <summary>
    /// Defines the interface implemented by logging data providers.
    /// </summary>
    public interface ILogging {
        /// <summary>
        /// Initializes the logging data provider.
        /// </summary>
        Task InitAsync();
        /// <summary>
        /// Clears all log records.
        /// </summary>
        Task ClearAsync();
        /// <summary>
        /// Flushes all pending log records to permanent storage.
        /// </summary>
        Task FlushAsync();
        /// <summary>
        /// Adds a record using the logging data provider.
        /// </summary>
        /// <param name="category">The event name or category.</param>
        /// <param name="level">The severity level of the message.</param>
        /// <param name="relStack">The number of call stack entries that should be skipped when generating a call stack.</param>
        /// <param name="text">The log message.</param>
        void WriteToLogFile(string category, Logging.LevelEnum level, int relStack, string text);
        /// <summary>
        /// Returns the minimum severity level that is logged by the logging data provider.
        /// </summary>
        /// <returns>Returns the minimum severity level that is logged by the logging data provider.</returns>
        Logging.LevelEnum GetLevel();
        /// <summary>
        /// Returns whether the logging data provider is installed and available.
        /// </summary>
        /// <returns>Returns whether the logging data provider is installed and available.</returns>
        Task<bool> IsInstalledAsync();
        /// <summary>
        /// Defines whether the logging data provider is already logging an event.
        /// </summary>
        bool IsProcessing { get; set; }
    }

    public static partial class Logging {

        /// <summary>
        /// Defines the minimum severity level that is logged by any of the installed logging data providers.
        /// </summary>
        public static LevelEnum MinLevel { get; private set; }
        /// <summary>
        /// A collection of installed logging data providers.
        /// </summary>
        private static List<ILogging>? Loggers { get; set; }
        /// <summary>
        /// The installed default logging data providers.
        /// </summary>
        private static ILogging? DefaultLogger { get; set; }
        /// <summary>
        /// The type of the default logging data provider.
        /// </summary>
        public static Type? DefaultLoggerType { get; private set; }
        /// <summary>
        /// The type of the defined logging data provider.
        /// </summary>
        public static Type? DefinedLoggerType { get; private set; }

        private static object _lockObject = new object();

        private static List<ILogging> GetLoggers() {
            lock (_lockObject) { // short-term lock to sync loggers
                if (Loggers == null) return new List<ILogging>();
                return Loggers.ToList();
            }
        }
        private static void SetLoggers(List<ILogging> loggers) {
            lock (_lockObject) { // short-term lock to sync loggers
                Loggers = loggers;
            }
        }

        // we can't use some clever scheme to get a logging provider because we need logging as soon as possible so
        // logging is set up using appsettings.json.
        /// <summary>
        /// Defines a default logging data provider based on appsettings.json settings. This is called during site startup.
        /// </summary>
        public static async Task SetupLoggingAsync() {

            TerminateLogging();

            string? assembly = WebConfigHelper.GetValue<string>("Logging", "Assembly");
            string? type = WebConfigHelper.GetValue<string>("Logging", "Type");

            if (!string.IsNullOrWhiteSpace(assembly) && !string.IsNullOrWhiteSpace(type)) {
                // load the assembly/type implementing logging
                Type? tp = null;
                try {
                    Assembly? asm = Assemblies.Load(assembly);
                    tp = asm!.GetType(type);
                    DefinedLoggerType = tp;
                } catch (Exception) { }

                // create an instance of the class implementing logging
                ILogging? log = null;
                try {
                    log = (ILogging?)Activator.CreateInstance(tp!);
                } catch (Exception) { }

                if (log != null) {
                    if (await log.IsInstalledAsync()) {
                        DefaultLogger = log;
                        DefaultLoggerType = tp;
                        await RegisterLoggingAsync(log);
                        await log.ClearAsync();
                    }
                }
            }
        }
        /// <summary>
        /// Terminates the default logging data provider.
        /// </summary>
        public static void TerminateLogging() {
            if (DefaultLogger != null) {
                UnregisterLogging(DefaultLogger);
                DefaultLogger = null;
            }
        }
        /// <summary>
        /// Registers a new logging data provider.
        /// </summary>
        /// <param name="logger">An ILogging interface for the logging data provider's implementation.</param>
        public static async Task RegisterLoggingAsync(ILogging logger) {
            await logger.InitAsync();
            lock (_lockObject) { // short-term lock to sync loggers
                if (Loggers == null)
                    Loggers = new List<ILogging>();
                Loggers.Add(logger);
            }
            MinLevel = GetLowestLogLevel();
        }
        private static LevelEnum GetLowestLogLevel() {
            LevelEnum level = LevelEnum.Error;
            if (Loggers == null) return LevelEnum.Trace;
            foreach (ILogging log in GetLoggers()) {
                if (log.GetLevel() < level)
                    level = log.GetLevel();
            }
            return level;
        }
        /// <summary>
        /// Unregisters an existing logging data provider.
        /// </summary>
        public static void UnregisterLogging(ILogging logger) {
            lock (_lockObject) { // short-term lock to sync loggers
                if (Loggers != null) {
                    if (Loggers.Contains(logger))
                        Loggers.Remove(logger);
                }
            }
            MinLevel = GetLowestLogLevel();
        }
        private static void WriteToAllLogFiles(LevelEnum level, int relStack, string message) {
            WriteToAllLogFiles(YetaWFEvent, level, relStack, message);
        }
        /// <summary>
        /// Writes a message to all logging data providers.
        /// </summary>
        /// <param name="level">The severity level of the message.</param>
        /// <param name="relStack">The number of call stack entries that should be skipped when generating a call stack.</param>
        /// <param name="message">The log message.</param>
        public static void WriteToAllLogFiles(string category, LevelEnum level, int relStack, string message) {
            foreach (ILogging log in GetLoggers()) {
                if (log.GetLevel() <= level) {
                    if (!log.IsProcessing) {
                        try {
                            log.IsProcessing = true;
                            log.WriteToLogFile(category, level, relStack, message);
                        } catch (Exception) {
                            throw;
                        } finally {
                            log.IsProcessing = false;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Flushes all pending log records to permanent storage by calling all logging data providers.
        /// </summary>
        public static void ForceFlush() {
            foreach (ILogging log in GetLoggers()) {
                log.FlushAsync();
            }
        }
    }
}
