/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Log {

    /// <summary>
    /// This static class implements all YetaWF logging.
    /// </summary>
    public static partial class Logging {

        /// <summary>
        /// The name of an event logged by YetaWF. All events logged by YetaWF have this name.
        /// Applications can use their own event names.
        /// </summary>
        public const string YetaWFEvent = "YetaWF";

        /// <summary>
        /// Defines the logging event severity level.
        /// </summary>
        public enum LevelEnum {
            /// <summary>
            /// Defines a tracing/debug event.
            /// </summary>
            [EnumDescription("Trace", "Tracing/Debug Information")]
            Trace = 0,
            /// <summary>
            /// Defines an informational event.
            /// </summary>
            [EnumDescription("Info", "Informational")]
            Info = 25,
            /// <summary>
            /// Defines an warning event.
            /// </summary>
            [EnumDescription("Warning", "Warning")]
            Warning = 50,
            /// <summary>
            /// Defines an error event.
            /// </summary>
            [EnumDescription("Error", "Error")]
            Error = 99,

            /// <summary>
            /// Defines an event that is always logged (not necessarily an error).
            /// </summary>
            [EnumDescription("Always", "Always logged")]
            Always = 200,
        }

        /// <summary>
        /// Records a message to the YetaWF log.
        /// </summary>
        /// <returns>Returns the message text.</returns>
        public static string AddLog(LevelEnum logLevel, string text) {
            if (MinLevel <= logLevel)
                WriteToAllLogFiles(logLevel, 0, text);
            return text;
        }

        /// <summary>
        /// Records an informational message to the YetaWF log.
        /// </summary>
        /// <returns>Returns the message text.</returns>
        public static string AddLog(string text) {
            if (MinLevel <= LevelEnum.Info)
                WriteToAllLogFiles(LevelEnum.Info, 0, text);
            return text;
        }
        /// <summary>
        /// Records an informational message to the YetaWF log with formatted parameters.
        /// </summary>
        /// <param name="text">The message with formatting information for the parameters <paramref name="parms"/>.</param>
        /// <param name="parms">A list of parameters that are formatted using the provided <paramref name="text"/> parameter.</param>
        /// <returns>Returns the fully formatted message text.</returns>
        public static string AddLog(string text, params object?[] parms) {
            text = FormatMessage(text, parms);
            if (MinLevel <= LevelEnum.Info)
                WriteToAllLogFiles(LevelEnum.Info, 0, text);
            return text;
        }

        /// <summary>
        /// Records an trace message to the YetaWF log.
        /// </summary>
        /// <param name="text">The message.</param>
        /// <returns>Returns the message text.</returns>
        public static string AddTraceLog(string text) {
            if (MinLevel <= LevelEnum.Trace)
                WriteToAllLogFiles(LevelEnum.Trace, 0, text);
            return text;
        }
        /// <summary>
        /// Records an trace message to the YetaWF log with formatted parameters.
        /// </summary>
        /// <param name="text">The message with formatting information for the parameters <paramref name="parms"/>.</param>
        /// <param name="parms">A list of parameters that are formatted using the provided <paramref name="text"/> parameter.</param>
        /// <returns>Returns the fully formatted message text.</returns>
        public static string AddTraceLog(string text, params object?[] parms) {
            text = FormatMessage(text, parms);
            if (MinLevel <= LevelEnum.Trace)
                WriteToAllLogFiles(LevelEnum.Trace, 0, text);
            return text;
        }

        /// <summary>
        /// Records an warning message to the YetaWF log.
        /// </summary>
        /// <returns>Returns the message text.</returns>
        public static string AddWarningLog(string text) {
            if (MinLevel <= LevelEnum.Warning)
                WriteToAllLogFiles(LevelEnum.Warning, 0, text);
            return text;
        }
        /// <summary>
        /// Records an warning message to the YetaWF log with formatted parameters.
        /// </summary>
        /// <param name="text">The message with formatting information for the parameters <paramref name="parms"/>.</param>
        /// <param name="parms">A list of parameters that are formatted using the provided <paramref name="text"/> parameter.</param>
        /// <returns>Returns the fully formatted message text.</returns>
        public static string AddWarningLog(string text, params object?[] parms) {
            text = FormatMessage(text, parms);
            if (MinLevel <= LevelEnum.Warning)
                WriteToAllLogFiles(LevelEnum.Warning, 0, text);
            return text;
        }

        /// <summary>
        /// Records an error message to the YetaWF log.
        /// </summary>
        /// <returns>Returns the message text.</returns>
        public static string AddErrorLog(string text) {
            if (MinLevel <= LevelEnum.Error)
                WriteToAllLogFiles(LevelEnum.Error, 0, text);
            return text;
        }

        /// <summary>
        /// Records an error message to the YetaWF log with formatted parameters.
        /// </summary>
        /// <param name="text">The message with formatting information for the parameters <paramref name="parms"/>.</param>
        /// <param name="parms">A list of parameters that are formatted using the provided <paramref name="text"/> parameter.</param>
        /// <returns>Returns the fully formatted message text.</returns>
        public static string AddErrorLog(string text, params object?[] parms) {
            text = FormatMessage(text, parms);
            if (MinLevel <= LevelEnum.Error)
                WriteToAllLogFiles(LevelEnum.Error, 0, text);
            return text;
        }
        //public static string AddErrorLogAdjustStack(int relStack, string text) {
        //    if (MinLevel <= LevelEnum.Error)
        //        WriteToAllLogFiles(LevelEnum.Error, relStack, text);
        //    return text;
        //}
        //public static string AddErrorLogAdjustStack(int relStack, string text, params object?[] parms) {
        //    text = FormatMessage(text, parms);
        //    if (MinLevel <= LevelEnum.Error)
        //        WriteToAllLogFiles(LevelEnum.Error, relStack, text);
        //    return text;
        //}

        /// <summary>
        /// Records an error message to the YetaWF log.
        /// </summary>
        /// <returns>Returns the message text.</returns>
        public static string AddAlwaysLog(string text) {
            WriteToAllLogFiles(LevelEnum.Always, 0, text);
            return text;
        }

        private static string FormatMessage(string text, params object?[] parms) {

            SqlCommand? sqlCmd = null;
            Exception? exc = null;
            List<string>? errors = null;

            StringBuilder bld = new StringBuilder();
            if (parms != null) {
                foreach (var obj in parms) {
                    if (obj is SqlCommand)
                        sqlCmd = (SqlCommand) obj;
                    else if (obj is Exception)
                        exc = (Exception) obj;
                    else if (obj is List<string>)
                        errors = (List<string>) obj;
                }
            }
            if (!string.IsNullOrEmpty(text)) {
                if (parms == null || parms.Length == 0)
                    bld.Append(text);
                else
                    bld.AppendFormat(text, parms);
                bld.Append("\n");
            }
            if (exc != null) {
                while (exc != null) {
                    bld.AppendFormat("{0}\n", ErrorHandling.FormatExceptionMessage(exc));
                    //System.Data.Entity.Validation.DbEntityValidationException excDb = exc as System.Data.Entity.Validation.DbEntityValidationException;
                    //if (excDb != null) {
                    //    foreach (System.Data.Entity.Validation.DbEntityValidationResult res in excDb.EntityValidationErrors) {
                    //        foreach (System.Data.Entity.Validation.DbValidationError err in res.ValidationErrors) {
                    //            bld.AppendFormat("{0} - {1}\n", err.PropertyName, err.ErrorMessage);
                    //        }
                    //    }
                    //}
                    exc = exc.InnerException;
                }
            }
            if (errors != null) {
                foreach (var error in errors) {
                    bld.AppendFormat("{0}\n", error);
                }
            }
            if (sqlCmd != null) {
                bld.AppendFormat("Using SqlCommand {0} on database {2}:\n{1}\n", sqlCmd.CommandType.ToString(), sqlCmd.CommandText, sqlCmd.Connection.Database);
                foreach (SqlParameter parm in sqlCmd.Parameters) {
                    bld.AppendFormat("{0}: {1}\n", parm.ParameterName, parm.Value != null ? parm.Value.ToString() : "(null)");
                }
            }
            if (bld.Length > 0) {
                if (bld[bld.Length - 1] == '\n')
                    bld.Remove(bld.Length - 1, 1);
            }
            return bld.ToString();
        }
    }
}
