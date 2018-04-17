/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.Log {

    public interface ILogging {
        Task InitAsync();
        Task ClearAsync();
        Task FlushAsync();
        void WriteToLogFile(string category, Logging.LevelEnum level, int relStack, string text);
        Logging.LevelEnum GetLevel();
        Task<bool> IsInstalledAsync();
    }

    public static partial class Logging {

        public static LevelEnum MinLevel { get; private set; }
        private static List<ILogging> Loggers { get; set; }
        private static ILogging DefaultLogger { get; set; }
        public static Type DefaultLoggerType { get; private set; }
        public static Type DefinedLoggerType { get; private set; }

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
        /// Set up default log provider.
        /// </summary>
        public static async Task SetupLoggingAsync() {

            TerminateLogging();

            string assembly = WebConfigHelper.GetValue<string>("Logging", "Assembly");
            string type = WebConfigHelper.GetValue<string>("Logging", "Type");

            // load the assembly/type implementing logging
            Type tp = null;
            try {
                Assembly asm = Assemblies.Load(assembly);
                tp = asm.GetType(type);
                DefinedLoggerType = tp;
            } catch (Exception) { }

            // create an instance of the class implementing logging
            ILogging log = null;
            try {
                log = (ILogging)Activator.CreateInstance(tp);
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
        /// <summary>
        /// Terminate default log provider.
        /// </summary>
        public static void TerminateLogging() {
            if (DefaultLogger != null) {
                UnregisterLogging(DefaultLogger);
                DefaultLogger = null;
            }
        }
        /// <summary>
        /// Register a new logger.
        /// </summary>
        /// <param name="logger"></param>
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
        /// Unregister an existing logger.
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
        /// <summary>
        /// Write a message to all loggers.
        /// </summary>
        private static void WriteToAllLogFiles(LevelEnum level, int relStack, string message) {
            WriteToAllLogFiles(YetaWFEvent, level, relStack, message);
        }
        /// <summary>
        /// Write a message to all loggers.
        /// </summary>
        public static void WriteToAllLogFiles(string category, LevelEnum level, int relStack, string message) {
            foreach (ILogging log in GetLoggers()) {
                if (log.GetLevel() <= level)
                    log.WriteToLogFile(category, level, relStack, message);
            }
        }
        /// <summary>
        /// Flush all loggers.
        /// </summary>
        public static void ForceFlush() {
            foreach (ILogging log in GetLoggers()) {
                log.FlushAsync();
            }
        }
    }
}
