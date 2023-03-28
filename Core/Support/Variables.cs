/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using YetaWF.Core.Extensions;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;

namespace YetaWF.Core.Support {

    public class Variables {

        public enum EncodingTypeEnum {
            HTML = 0,
            XML = 1,
            JSON = 2,
        }

        public Variables(YetaWFManager? manager, object? parms = null) {
            Manager = manager;
            Parameters = parms;
            DoubleEscape = false;
            CurlyBraces = true;
            EncodeDefault = true;
            PreserveAsis = false;
            EncodingType = EncodingTypeEnum.HTML;
        }
        protected YetaWFManager? Manager { get; private set; }
        public object? Parameters { get; private set; }
        public EncodingTypeEnum EncodingType { get; set; }

        /// <summary>
        /// Replace {{ }} and [[ ]] instead of [ ] and { }
        /// </summary>
        public bool DoubleEscape { get; set; }
        /// <summary>
        /// Replace {{ }} or { } in addition to [[ ]] or [ ]
        /// </summary>
        public bool CurlyBraces { get; set; }
        /// <summary>
        /// Defines whether encoding is used by default.
        /// </summary>
        public bool EncodeDefault { get; set; }
        /// <summary>
        /// Defines whether variables marked with "-" are preserved.
        /// </summary>
        /// <remarks>A variable defined with a "-" in front of the variable type is preserved without removing the leading "-".
        ///
        /// If true, [-Var,SomeName] is rendered as [-Var,SomeName]. If false, it is rendered as [Var,SomeName] by removing the leading "-".</remarks>
        public bool PreserveAsis { get; set; }

        public string ReplaceVariables(string? text) {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            if (DoubleEscape) {
                if (CurlyBraces)
                    text = varReDoubleEscapeCB.Replace(text, new MatchEvaluator(VarSubst));
                text = varReDoubleEscape.Replace(text, new MatchEvaluator(VarSubst));
            } else {
                if (CurlyBraces)
                    text = varReSingleEscapeCB.Replace(text, new MatchEvaluator(VarSubst));
                text = varReSingleEscape.Replace(text, new MatchEvaluator(VarSubst));
            }
            return text;
        }
        public string ReplaceModuleVariables(ModuleDefinition? module, string? text) {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            _thisModule = module;
            if (DoubleEscape) {
                if (CurlyBraces)
                    text = varReModuleDoubleEscapeCB.Replace(text, new MatchEvaluator(VarSubstModuleVariables));
                text = varReModuleDoubleEscape.Replace(text, new MatchEvaluator(VarSubstModuleVariables));

            } else {
                if (CurlyBraces)
                    text = varReModuleSingleEscapeCB.Replace(text, new MatchEvaluator(VarSubstModuleVariables));
                text = varReModuleSingleEscape.Replace(text, new MatchEvaluator(VarSubstModuleVariables));
            }
            return text;
        }
        private ModuleDefinition? _thisModule { get; set; }

        private Regex varReSingleEscape {
            get {
                if (_varReSingleEscape == null)
                    _varReSingleEscape = new Regex("\\[(?'neg'(\\-|))\\s*(?'module'[^\\,\\]]+?)\\s*(,|\\:)\\s*(?'var'[^\\.\\]]+?)\\s*(\\.\\s*(?'subvar'[^\\]]+?)){0,1}\\s*\\]", RegexOptions.Compiled | RegexOptions.Singleline);
                return _varReSingleEscape;
            }
        }
        private static Regex? _varReSingleEscape = null;

        private Regex varReDoubleEscape {
            get {
                if (_varReDoubleEscape == null)
                    _varReDoubleEscape = new Regex("\\[\\[(?'neg'(\\-|))\\s*(?'module'[^\\,\\]]+?)\\s*(,|\\:)\\s*(?'var'[^\\.\\]]+?)\\s*(\\.\\s*(?'subvar'[^\\]]+?)){0,1}\\s*\\]\\]", RegexOptions.Compiled | RegexOptions.Singleline);
                return _varReDoubleEscape;
            }
        }
        private static Regex? _varReDoubleEscape = null;

        private Regex varReModuleSingleEscape {
            get {
                if (_varReModuleSingleEscape == null)
                    _varReModuleSingleEscape = new Regex("\\[(?'neg'(\\-|))\\s*ThisModule\\s*(,|\\:)\\s*(?'var'[^\\.\\]]+?)\\s*(\\.\\s*(?'subvar'[^\\]]+?)){0,1}\\s*\\]", RegexOptions.Compiled | RegexOptions.Singleline);
                return _varReModuleSingleEscape;
            }
        }
        private static Regex? _varReModuleSingleEscape = null;

        private Regex varReModuleDoubleEscape {
            get {
                if (_varReModuleDoubleEscape == null)
                    _varReModuleDoubleEscape = new Regex("\\[\\[(?'neg'(\\-|))\\s*ThisModule\\s*(,|\\:)\\s*(?'var'[^\\.\\]]+?)\\s*(\\.\\s*(?'subvar'[^\\]]+?)){0,1}\\s*\\]\\]", RegexOptions.Compiled | RegexOptions.Singleline);
                return _varReModuleDoubleEscape;
            }
        }
        private static Regex? _varReModuleDoubleEscape = null;

        private Regex varReSingleEscapeCB {
            get {
                if (_varReSingleEscapeCB == null)
                    _varReSingleEscapeCB = new Regex("\\{(?'neg'(\\-|))\\s*(?'module'[^\\,\\}]]+?)\\s*(,|\\:)\\s*(?'var'[^\\.\\}\\]]+?)\\s*(\\.\\s*(?'subvar'[^\\}\\]]+?)){0,1}\\s*\\}", RegexOptions.Compiled | RegexOptions.Singleline);
                return _varReSingleEscapeCB;
            }
        }
        private static Regex? _varReSingleEscapeCB = null;

        private Regex varReDoubleEscapeCB {
            get {
                if (_varReDoubleEscapeCB == null)
                    _varReDoubleEscapeCB = new Regex("\\{\\{(?'neg'(\\-|))\\s*(?'module'[^\\,\\]}]+?)\\s*(,|\\:)\\s*(?'var'[^\\.\\]\\}]+?)\\s*(\\.\\s*(?'subvar'[^\\]\\}]+?)){0,1}\\s*\\}\\}", RegexOptions.Compiled | RegexOptions.Singleline);
                return _varReDoubleEscapeCB;
            }
        }
        private static Regex? _varReDoubleEscapeCB = null;

        private Regex varReModuleSingleEscapeCB {
            get {
                if (_varReModuleSingleEscapeCB == null)
                    _varReModuleSingleEscapeCB = new Regex("\\{(?'neg'(\\-|))\\s*ThisModule\\s*(,|\\:)\\s*(?'var'[^\\.\\}\\]]+?)\\s*(\\.\\s*(?'subvar'[^\\]\\}]+?)){0,1}\\s*\\}", RegexOptions.Compiled | RegexOptions.Singleline);
                return _varReModuleSingleEscapeCB;
            }
        }
        private static Regex? _varReModuleSingleEscapeCB = null;

        private Regex varReModuleDoubleEscapeCB {
            get {
                if (_varReModuleDoubleEscapeCB == null)
                    _varReModuleDoubleEscapeCB = new Regex("\\{\\{(?'neg'(\\-|))\\s*ThisModule\\s*(,|\\:)\\s*(?'var'[^\\.\\]\\}]+?)\\s*(\\.\\s*(?'subvar'[^\\]\\}]+?)){0,1}\\s*\\}\\}", RegexOptions.Compiled | RegexOptions.Singleline);
                return _varReModuleDoubleEscapeCB;
            }
        }
        private static Regex? _varReModuleDoubleEscapeCB = null;

        private string VarSubst(Match m) {
            bool encode = EncodeDefault;
            string retString = m.Value;
            try {
                string neg = m.Groups["neg"].Value.Trim();
                if (neg == "-") {
                    if (PreserveAsis)
                        return retString;
                    else
                        return retString.ReplaceFirst("-", "");
                }
                string loc = m.Groups["module"].Value.Trim();
                string var = m.Groups["var"].Value.Trim();
                string subvar = m.Groups["subvar"].Value.Trim();
                string ret;
                if (var.StartsWith("+")) {
                    var = var.Substring(1).Trim();
                    encode = true;
                } else if (var.StartsWith("-")) {
                    var = var.Substring(1).Trim();
                    encode = false;
                }

                if (loc == "Var") {
                    if (Parameters != null) {
                        if (EvalObjectVariable(Parameters, var, subvar, out ret))
                            return (encode) ? EncodeText(ret) : ret;
                        try {
                            if (string.IsNullOrWhiteSpace(subvar)) {// JSON converted objects
                                ret = ((dynamic)Parameters)[var];
                                return (encode) ? EncodeText(ret) : ret;
                            } else {
                                ret = ((dynamic)((dynamic)Parameters)[var])[subvar];
                                return (encode) ? EncodeText(ret) : ret;
                            }
                        } catch (Exception) { }
                    }
                } else if (loc == "Env") {
                    if (!string.IsNullOrWhiteSpace(var)) {
                        string env = Environment.GetEnvironmentVariable(var) ?? string.Empty;
                        return (encode) ? EncodeText(env) : env;
                    }
                } else if (loc == "Opsys") {
                    if (!string.IsNullOrWhiteSpace(var)) {
                        switch (var) {
                            case "MVC":// obsolete
                            case "NET":
                                ret = Utility.GetAspNetName();
                                return (encode) ? EncodeText(ret) : ret;
                            case "Name":
                                ret = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                                return (encode) ? EncodeText(ret) : ret;
                            case "Framework":
                                ret = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
                                return (encode) ? EncodeText(ret) : ret;
                            default:
                                break;
                        }
                    }
                } else if (loc.StartsWith("Unique-")) {
                    // {{Unique-Softelvdm.Modules.ComodoTrustLogo.Modules.ComodoUserTrustConfigModule, -ConfigData.TrustLogoHtml}}
                    string fullName = loc.Substring("Unique-".Length);
                    Type? modType = (from mod in InstalledModules.Modules where mod.Value.Type.FullName == fullName select mod.Value.Type).FirstOrDefault();
                    if (modType != null) {
                        ModuleDefinition dataMod = ModuleDefinition.CreateUniqueModuleAsync(modType).Result;//TODO: This is not good. However rarely used, so we'll wait until we have other asyncs in Variables.
                        if (dataMod != null) {
                            if (EvalObjectVariable(dataMod, var, subvar, out ret)) {
                                if (!string.IsNullOrWhiteSpace(ret))
                                    return (encode) ? EncodeText(ret) : ret;
                            }
                        }
                    }
                } else if (Manager != null) {
                    if (loc == "ThisPage") {
                        if (Manager.CurrentPage != null) {
                            if (EvalObjectVariable(Manager.CurrentPage, var, subvar, out ret))
                                return (encode) ? EncodeText(ret) : ret;
                        }
                    } else if (loc == "Site") {
                        if (EvalSiteVariable(var, subvar, out ret))
                            return (encode) ? EncodeText(ret) : ret;
                    } else if (loc == "PageOrSite") {
                        if (Manager.CurrentPage != null && EvalObjectVariable(Manager.CurrentPage, var, subvar, out ret)) {
                            if (!string.IsNullOrWhiteSpace(ret))
                                return (encode) ? EncodeText(ret) : ret;
                        }
                        if (EvalSiteVariable(var, subvar, out ret))
                            return (encode) ? EncodeText(ret) : ret;
                    } else if (loc == "Manager") {
                        if (EvalManagerVariable(var, subvar, out ret))
                            return (encode) ? EncodeText(ret) : ret;
                    } else if (loc == "Globals") {
                        if (EvalGlobalsVariable(var, subvar, out ret))
                            return (encode) ? EncodeText(ret) : ret;
                    } else if (loc == "HttpRequest") {
                        if (EvalObjectVariable(Manager.CurrentRequest, var, subvar, out ret))
                            return (encode) ? EncodeText(ret) : ret;
                    } else if (loc == "QueryString" || loc == "QS") {
                        if (!string.IsNullOrWhiteSpace(var)) {
                            ret = Manager.RequestQueryString[var] ?? string.Empty;
                            return (encode) ? EncodeText(ret) : ret;
                        }
                    } else if (loc == "Session") {
                        if (!string.IsNullOrWhiteSpace(var)) {
                            if (Manager.SessionSettings.SiteSettings.ContainsKey(var)) {
                                ret = Manager.SessionSettings.SiteSettings.GetValue<string>(var) ?? string.Empty;
                                return (encode) ? EncodeText(ret) : ret;
                            }
                        }
                    }
                }
            } catch { }
            return retString;
        }

        private string VarSubstModuleVariables(Match m) {
            bool encode = false;
            string retString = m.Value;
            try {
                string var = m.Groups["var"].Value.Trim();
                string subvar = m.Groups["subvar"].Value.Trim();
                string ret;
                if (var.StartsWith("+")) {
                    var = var.Substring(1).Trim();
                    encode = true;
                } else if (var.StartsWith("-")) {
                    var = var.Substring(1).Trim();
                    encode = false;
                }

                if (var == "Resource" || var == "Resources") {
                    if (EvalModuleResourceVariable(_thisModule!, subvar, out ret))
                        return (encode) ? EncodeText(ret) : ret;
                }
                if (EvalObjectVariable(_thisModule!, var, subvar, out ret)) {
                    return (encode) ? EncodeText(ret) : ret;
                }
            } catch { }
            return retString;
        }

        private bool EvalModuleResourceVariable(ModuleDefinition module, string resource, out string ret) {
            ret = module.__ResStr(resource, "(unknown)");
            return true;
        }
        private bool EvalObjectVariable(object obj, string var, string subvar, out string retString) {
            try {
                if (GetVariableValue(obj, var, subvar, out retString))
                    return true;
            } catch (Exception e) {
                retString = string.Format("{0} {1} => {2}", var, subvar, ErrorHandling.FormatExceptionMessage(e));
                return true;
            }
            return false;
        }
        private bool EvalSiteVariable(string var, string? subvar, out string retString) {
            retString = string.Empty;
            try {
                if (Manager != null) {
                    if (GetVariableValue(Manager.CurrentSite, var, subvar, out retString))
                        return true;
                }
            } catch { }
            return false;
        }
        private bool GetVariableValue(object? mod, string var, string? subvar, out string retString) {
            retString = string.Empty;
            if (mod == null) return false;
            Type tp = mod.GetType();
            // try using reflection
            PropertyInfo? pi = ObjectSupport.TryGetProperty(tp, var);
            if (pi != null) {
                object? val = pi.GetValue(mod, null);
                if (!string.IsNullOrWhiteSpace(subvar))
                    return GetVariableValue(val, subvar, null, out retString);
                if (val == null)
                    return true;
                // convert to a string
                try {
                    TypeConverter conv = TypeDescriptor.GetConverter(val.GetType());
                    retString = conv.ConvertToString(val) ?? string.Empty;
                } catch {
                    retString = val.ToString() ?? string.Empty;
                }
                return true;
            }
            // try as IDictionary<string, object>
            IDictionary<string, object?>? dict = mod as IDictionary<string, object?>;
            if (dict != null) {
                if (dict.TryGetValue(var, out object? val)) {
                    if (!string.IsNullOrWhiteSpace(subvar))
                        return GetVariableValue(val, subvar, null, out retString);
                    if (val == null)
                        return true;
                    // convert to a string
                    try {
                        TypeConverter conv = TypeDescriptor.GetConverter(val.GetType());
                        retString = conv.ConvertToString(val) ?? string.Empty;
                    } catch {
                        retString = val.ToString() ?? string.Empty;
                    }
                    return true;
                }
            }
            return false;
        }
        private bool EvalGlobalsVariable(string var, string? subvar, out string retString) {
            retString = "";
            try {
                if (GetVariableValueFromStaticField(typeof(Globals), var, subvar, out retString))
                    return true;
            } catch { }
            return false;
        }
        private bool EvalManagerVariable(string var, string subvar, out string retString) {
            retString = "";
            try {
                if (GetVariableValue(YetaWFManager.Manager, var, subvar, out retString))
                    return true;
            } catch { }
            return false;
        }
        private bool GetVariableValueFromStaticField(Type type, string var, string? subvar, out string retString) {
            retString = "";
            FieldInfo? f = type.GetField(var, BindingFlags.Public | BindingFlags.Static);
            if (f == null) return false;
            string? val = (string?) f.GetValue(null);
            if (val == null)
                return true;
            retString = val;
            return true;
        }
        private string EncodeText(string text) {
            switch (EncodingType) {
                case EncodingTypeEnum.HTML:
                    return Utility.HE(text);
                case EncodingTypeEnum.XML:
                    return Utility.HE(text);
                case EncodingTypeEnum.JSON:
                    return Utility.JserEncode(text);
            }
            return text;
        }
    }
}
