/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.Log;

namespace YetaWF.Core.Support {

    public class StartupLogging : ILogging {

        private string LogFile { get; set; }

        public StartupLogging() { }
        public Logging.LevelEnum GetLevel() { return Logging.LevelEnum.Trace; }
        public Task InitAsync() {
            string rootFolder;
#if MVC6
            rootFolder = YetaWFManager.RootFolderWebProject;
#else
            rootFolder = YetaWFManager.RootFolder;
#endif
            string folder = Path.Combine(rootFolder, Globals.DataFolder);
            Directory.CreateDirectory(folder);
            LogFile = Path.Combine(rootFolder, Globals.DataFolder, Globals.StartupLogFile);
            File.Delete(LogFile);
            return Task.CompletedTask;
        }
        public Task ClearAsync() { return Task.CompletedTask; }
        public Task FlushAsync() { return Task.CompletedTask; }
        public Task<bool> IsInstalledAsync() { return Task.FromResult(true); }

        public void WriteToLogFile(string category, Logging.LevelEnum level, int relStack, string text) {
            File.AppendAllText(LogFile, $"{DateTime.Now} {text}\r\n");
        }
    }
}
