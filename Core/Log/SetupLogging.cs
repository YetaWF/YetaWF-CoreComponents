/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Reflection;
using YetaWF.Core.Support;

namespace YetaWF.Core.Log {

    public interface ILogging {
        void Clear();
        void Flush();
        void WriteToLogFile(Logging.LevelEnum level, int relStack, string text);
        Logging.LevelEnum GetLevel();
        bool IsInstalled();
    }

    public static partial class Logging {

        public static LevelEnum MinLevel { get; private set; }
        private static List<ILogging> Loggers { get; set; }
        private static ILogging DefaultLogger { get; set; }

        // we can't use some clever scheme to get a logging provider because we need logging as soon as possible so
        // logging is set up using web.config/appsettings.json.
        /// <summary>
        /// Set up default log provider.
        /// </summary>
        public static void SetupLogging() {

            TerminateLogging();

            string assembly = WebConfigHelper.GetValue<string>("Logging", "Assembly");
            string type = WebConfigHelper.GetValue<string>("Logging", "Type");

            // load the assembly/type implementing logging
            Type tp = null;
            try {
                Assembly asm = Assemblies.Load(assembly);
                tp = asm.GetType(type);
            } catch (Exception) { }

            // create an instance of the class implementing logging
            ILogging log = null;
            try {
                log = (ILogging)Activator.CreateInstance(tp);
            } catch (Exception) { }

            if (log != null) {
                if (log.IsInstalled()) {
                    DefaultLogger = log;
                    RegisterLogging(log);
                    log.Clear();
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
        public static void RegisterLogging(ILogging logger) {
            if (Loggers == null) Loggers = new List<ILogging>();
            Loggers.Add(logger);
            MinLevel = GetLowestLogLevel();
        }
        private static LevelEnum GetLowestLogLevel() {
            LevelEnum level = LevelEnum.Error;
            if (Loggers == null) return LevelEnum.Info;
            foreach (ILogging log in Loggers) {
                if (log.GetLevel() < level)
                    level = log.GetLevel();
            }
            return level;
        }
        /// <summary>
        /// Unregister an existing logger.
        /// </summary>
        public static void UnregisterLogging(ILogging logger) {
            if (Loggers != null) {
                if (Loggers.Contains(logger))
                    Loggers.Remove(logger);
            }
            MinLevel = GetLowestLogLevel();
        }
        /// <summary>
        /// Write a message to all loggers.
        /// </summary>
        public static void WriteToAllLogFiles(LevelEnum level, int relStack, string message) {
            if (Loggers != null) {
                string text = string.Format("{0} - {1}", DateTime.Now/*Local Time*/, message);
                foreach (ILogging log in Loggers) {
                    log.WriteToLogFile(level, relStack, text);
                }
            }
        }
        /// <summary>
        /// Flush all loggers.
        /// </summary>
        public static void ForceFlush() {
            if (Loggers != null) {
                foreach (ILogging log in Loggers) {
                    log.Flush();
                }
            }
        }
    }
}
