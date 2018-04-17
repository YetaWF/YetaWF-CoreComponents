/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using YetaWF.Core.Log;

namespace YetaWF2.Logger {
    public static class LoggerExtensions {
        public static ILoggingBuilder AddYetaWFLogger(this ILoggingBuilder builder) {
            builder.Services.AddSingleton<ILoggerProvider, YetaWFLoggerProvider>();
            return builder;
        }
    }

    public class YetaWFLoggerProvider : ILoggerProvider {
        public ILogger CreateLogger(string categoryName) {
            return new YetaWFLogger(this, categoryName);
        }
        public void Dispose() { }
    }

    internal class YetaWFLogger : ILogger {

        private YetaWFLoggerProvider YetaWFLoggerProvider;
        private string CategoryName;

        public YetaWFLogger(YetaWFLoggerProvider yetaWFLoggerProvider, string categoryName) {
            YetaWFLoggerProvider = yetaWFLoggerProvider;
            CategoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
            Logging.LevelEnum level;
            switch (logLevel) {
                case LogLevel.None: return;
                default:
                case LogLevel.Trace: level = Logging.LevelEnum.Trace; break;
                case LogLevel.Debug: level = Logging.LevelEnum.Trace; break;
                case LogLevel.Information: level = Logging.LevelEnum.Trace; break;// information is very spammy, make it trace instead
                case LogLevel.Warning: level = Logging.LevelEnum.Warning; break;
                case LogLevel.Error: level = Logging.LevelEnum.Error; break;
                case LogLevel.Critical: level = Logging.LevelEnum.Error; break;
            }
            string msg = formatter(state, exception);
            Logging.WriteToAllLogFiles(CategoryName, level, 0, msg);
        }
    }
}
