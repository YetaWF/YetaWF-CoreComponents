/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Reflection;
using YetaWF.Core.Support;
using static YetaWF.Core.Log.Logging;

namespace YetaWF.Core.Log {

    public interface ILogging {
        void Clear();
        void Flush();
        void WriteToLogFile(LevelEnum level, int relStack, string text);
        void RegisterCallback(Action<string> callback);
        bool IsInstalled();
    }

    public static partial class Logging {

        // we can't use some clever scheme to get a logging provider because we need logging as soon as possible so
        // logging is set up using web.config.
        public static void SetupLogging() {

            TerminateLogging();

            string assembly = WebConfigHelper.GetValue<string>("Logging", "Assembly");
            string type = WebConfigHelper.GetValue<string>("Logging", "Type");
            int level = WebConfigHelper.GetValue<int>("Logging", "MinLevel", 0);

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
                    log.Clear();
                    Logging.AddLogMessage = log.WriteToLogFile;
                    Logging.ForceFlush = log.Flush;
                    Logging.RegisterCallback = log.RegisterCallback;
                    Logging.MinLevel = level;
                }
            }
        }

        public static void TerminateLogging() {
            Logging.AddLogMessage = null;
            Logging.ForceFlush = null;
            Logging.RegisterCallback = null;
            Logging.MinLevel = (int)LevelEnum.Info;
        }
    }
}
