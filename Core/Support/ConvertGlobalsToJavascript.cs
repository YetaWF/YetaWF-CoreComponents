/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using YetaWF.Core.Log;

namespace YetaWF.Core.Support {

    public static class ConvertGlobalsToJavascript {
        internal static void Convert(string outputFile, Object inputObject, string jsObjectName)
        {
            Logging.AddLog("Generating {0} for {1}", outputFile, inputObject.GetType().Name);

            JavaScriptSerializer jser = YetaWFManager.Jser;
            ScriptBuilder sb = new ScriptBuilder();

            sb.Append("var ");

            string[] objComponents = jsObjectName.Split(new char[] { '.' });
            if (objComponents.Length > 1) {
                sb.Append("{0}={{}};", objComponents[0]);
                int count = objComponents.Length;
                for (int i = 1 ; i < count ; ++i) {
                    for (int j = 0 ; j < i ; ++j) {
                        sb.Append("{0}.", objComponents[j]);
                    }
                    sb.Append("{0}", objComponents[i]);
                    if (i == count - 1)
                        sb.Append("={");
                    else
                        sb.Append("={};");
                }
            } else {
                sb.Append("{0}={{", objComponents[0]);
            }

            bool first = true;
            FieldInfo[] fi = inputObject.GetType().GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var f in fi) {
                Attribute attr = f.GetCustomAttribute(typeof(JSAttribute));
                if (attr != null) {
                    if (!first) sb.Append(",");
                    first = false;
                    string val = f.GetValue(inputObject).ToString();
                    sb.Append("'{0}':{1}", f.Name, jser.Serialize(val));
                }
            }
            sb.Append("};\n");
            File.WriteAllText(outputFile, sb.ToString());
        }
    }
}
