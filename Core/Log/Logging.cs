/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Log {

    public static partial class Logging {

        public enum LevelEnum {
            [EnumDescription("Trace", "Tracing/Debug Information")]
            Trace = 0,
            [EnumDescription("Info", "Informational")]
            Info = 25,
            [EnumDescription("Warning", "Warning")]
            Warning = 50,
            [EnumDescription("Error", "Error")]
            Error = 99,
        }

        /// <summary>
        /// Logging routine to record a log message, typically registered during application startup.
        /// </summary>
        public static string AddLog(string text) {
            if (MinLevel <= LevelEnum.Info)
                WriteToAllLogFiles(LevelEnum.Info, 0, text);
            return text;
        }
        public static string AddLog(string text, params object[] parms) {
            text = FormatMessage(text, parms);
            if (MinLevel <= LevelEnum.Info)
                WriteToAllLogFiles(LevelEnum.Info, 0, text);
            return text;
        }

        public static string AddTraceLog(string text) {
            if (MinLevel <= LevelEnum.Trace)
                WriteToAllLogFiles(LevelEnum.Trace, 0, text);
            return text;
        }
        public static string AddTraceLog(string text, params object[] parms) {
            text = FormatMessage(text, parms);
            if (MinLevel <= LevelEnum.Trace)
                WriteToAllLogFiles(LevelEnum.Trace, 0, text);
            return text;
        }

        public static string AddWarningLog(string text) {
            WriteToAllLogFiles(LevelEnum.Warning, 0, text);
            return text;
        }
        public static string AddWarningLog(string text, params object[] parms) {
            text = FormatMessage(text, parms);
            WriteToAllLogFiles(LevelEnum.Warning, 0, text);
            return text;
        }

        public static string AddErrorLog(string text) {
            WriteToAllLogFiles(LevelEnum.Error, 0, text);
            return text;
        }
        public static string AddErrorLog(string text, params object[] parms) {
            text = FormatMessage(text, parms);
            WriteToAllLogFiles(LevelEnum.Error, 0, text);
            return text;
        }
        public static string AddErrorLogAdjustStack(int relStack, string text) {
            WriteToAllLogFiles(LevelEnum.Error, relStack, text);
            return text;
        }
        public static string AddErrorLogAdjustStack(int relStack, string text, params object[] parms) {
            text = FormatMessage(text, parms);
            WriteToAllLogFiles(LevelEnum.Error, relStack, text);
            return text;
        }

        private static string FormatMessage(string text, params object[] parms) {

            SqlCommand sqlCmd = null;
            Exception exc = null;
            List<string> errors = null;

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
                    bld.AppendFormat("{0}\n", exc.Message);
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
