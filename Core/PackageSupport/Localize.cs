/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;

namespace YetaWF.Core.Packages {

    public partial class Package {

        //private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Package), name, defaultValue, parms); }

        /// <summary>
        /// Loads a package's types and extracts all localizable information (mainly from attributes) and
        /// saves the information in a customizable data file
        /// </summary>
        /// <param name="errorList"></param>
        /// <returns></returns>
        public async Task<bool> LocalizeAsync(List<string> errorList) {

            await Localization.ClearPackageDataAsync(this, MultiString.DefaultLanguage);

            // parse all source files to extract strings
            await ParseSourceFilesAsync(PackageSourceRoot);

            // enumerate all types
            List<Type> types;
            try {
                types = PackageAssembly.GetTypes().ToList();
            } catch (ReflectionTypeLoadException ex) {
                types = (from t in ex.Types where t != null select t).ToList();
            }
            foreach (Type type in types) {

                //DEBUG if (type.FullName == "YetaWF.Core.Scheduler.SchedulerFrequency+TimeUnitEnum")
                //    path = path;

                LocalizationData data = Localization.Load(Package.GetPackageFromType(type), type.FullName, Localization.Location.DefaultResources);
                if (data == null)
                    data = new LocalizationData();
                bool hasData = false;

                if (!type.IsAbstract && !type.IsGenericType) {
                    if (type.IsEnum && (type.IsPublic || type.IsNested)) {
                        if (!ProcessEnum(data, type, errorList, ref hasData))
                            break;
                    } else if (type.IsClass && (type.IsPublic || type.IsNested)) {
                        if (!ProcessType(data, type, errorList, ref hasData))
                            break;
                    }
                }
                if (hasData)
                    await Localization.SaveAsync(Package.GetPackageFromType(type), type.FullName, Localization.Location.DefaultResources, data);
            }
            return true;
        }

        private bool ProcessEnum(LocalizationData data, Type type, List<string> errorList, ref bool hasData) {

            //DEBUG if (type.FullName.Contains("ExpressionParser"))
            //DEBUG     type = type;

            LocalizationData.EnumData enm = new LocalizationData.EnumData();
            enm.Name = type.FullName;

            EnumData enumData = ObjectSupport.GetEnumData(type, Cache: false);
            if (enumData != null) {
                foreach (EnumDataEntry entry in enumData.Entries) {
                    if (entry.EnumDescriptionProvided)
                        hasData = true;
                    enm.Entries.Add(new LocalizationData.EnumDataEntry {
                        Name = entry.Name,
                        Value = entry.Value.ToString(),
                        Caption = FixNL(entry.Caption),
                        Description = FixNL(entry.Description),
                    });
                }
            }
            if (hasData)
                data.Enums.Add(enm);
            return true;
        }

        private static Type[] AllowedBaseTypes = {
                        typeof(ModuleDefinition),
                        typeof(ModuleDefinition.GridAllowedRole),
                        typeof(ModuleDefinition.GridAllowedUser),
        };

        private bool ProcessType(LocalizationData data, Type type, List<string> errorList, ref bool hasData) {

            ClassData classData = ObjectSupport.GetClassData(type, Cache: false);
            List<PropertyData> propData = ObjectSupport.GetPropertyData(type, Cache: false);

            LocalizationData.ClassData cls = new LocalizationData.ClassData();
            cls.Name = type.FullName;

            Type baseType = type.BaseType;
            if (baseType != null && baseType != typeof(object))
                cls.BaseTypeName = baseType.FullName;

            if (classData.Header != null) {
                cls.Header = FixNL(classData.Header);
                hasData = true;
            }
            if (classData.Footer != null) {
                cls.Footer = FixNL(classData.Footer);
                hasData = true;
            }
            if (classData.Legend != null) {
                cls.Legend = FixNL(classData.Legend);
                hasData = true;
            }

            // get all properties
            foreach (PropertyData prop in propData) {
                if (prop.Caption != null || prop.Description != null || prop.HelpLink != null || prop.TextAbove != null || prop.TextBelow != null) {
                    hasData = true;
                    cls.Properties.Add(new LocalizationData.PropertyData {
                        Name = prop.Name,
                        Caption = FixNL(prop.Caption),
                        Description = FixNL(prop.Description),
                        HelpLink = FixNL(prop.HelpLink),
                        TextAbove = FixNL(prop.TextAbove),
                        TextBelow = FixNL(prop.TextBelow),
                    });
                }
                cls.Categories = AddCategories(cls.Categories, prop.Categories);
                cls.Categories = new SerializableDictionary<string, string>(cls.Categories.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value));
            }

            if (hasData) {
                if (baseType != null && baseType != typeof(object)) {
                    if (!AllowedBaseTypes.Contains(baseType)) {
                        // even if the base type is not allowed, if the base type is in the same package, it still OK.
                        if (!IsSamePackage(type, baseType))
                            throw new InternalError("Can't localize - unsupported base type {0} for {1}", baseType.FullName, type.FullName);
                    }
                }
                data.Classes.Add(cls);
            }
            return true;
        }
        private SerializableDictionary<string, string> AddCategories(SerializableDictionary<string,string> cats, List<string> categories) {
            foreach (string cat in categories) {
                if (!cats.ContainsKey(cat))
                    cats.Add(cat, cat);
            }
            return cats;
        }

        private bool IsSamePackage(Type type, Type baseType) {
            Package package = Package.GetPackageFromType(type);
            Package basePackage = Package.GetPackageFromType(baseType);
            return (package.Name == basePackage.Name);
        }

        private string FixNL(string text) {
            if (text == null) return null;
            text = text.Replace("\n", ScriptBuilder.NL);
            text = text.Replace(@"\\", @"\");
            text = text.Replace("\\\"", "\"");
            return text;
        }

        private async Task ParseSourceFilesAsync(string path) {
            string folder = Path.GetFileName(path);
            if (string.Compare(folder, "Addons", true) == 0) return;
            if (string.Compare(folder, "AddonsBundles", true) == 0) return;
            if (string.Compare(folder, "AddonsCustom", true) == 0) return;
            if (string.Compare(folder, "Properties", true) == 0) return;
            if (string.Compare(folder, "bin", true) == 0) return;
            if (string.Compare(folder, "bower_components", true) == 0) return;
            if (string.Compare(folder, "Data", true) == 0) return;
            if (string.Compare(folder, "Docs", true) == 0) return;
            if (string.Compare(folder, "node_modules", true) == 0) return;
            if (string.Compare(folder, "obj", true) == 0) return;
            if (string.Compare(folder, "Properties", true) == 0) return;
            if (string.Compare(folder, "Sites", true) == 0) return;
            if (string.Compare(folder, "Vault", true) == 0) return;
            List<string> files = await FileSystem.FileSystemProvider.GetFilesAsync(path, "*.cs");
            foreach (string file in files)
                await ParseCsSourceFileAsync(file);
            List<string> dirs = await FileSystem.FileSystemProvider.GetDirectoriesAsync(path);
            foreach (string dir in dirs)
                await ParseSourceFilesAsync(dir);
        }

        // C#

        private async Task ParseCsSourceFileAsync(string file) {
            if (file.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)) return;
            if (file.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)) return;

            string fileText = await FileSystem.FileSystemProvider.ReadAllTextAsync(file);
            string ns = GetCsFileNamespace(file, fileText);
            string cls = GetCsFileClass(file, fileText);
            string explicitClass = GetCsFileExplicitClass(file, fileText);

            if (!string.IsNullOrWhiteSpace(explicitClass))
                cls = explicitClass;

            List<LocalizationData.StringData> strings = GetCsFileStrings(file, fileText, !string.IsNullOrWhiteSpace(explicitClass));

            if (strings.Count > 0) {

                if (string.IsNullOrWhiteSpace(cls))
                    throw new InternalError($"File {file} can't contain resource string definitions because its class doesn't support resource access");

                string filename = string.Format("{0}.{1}", ns, cls);
                LocalizationData data = Localization.Load(this, filename, Localization.Location.DefaultResources);
                if (data == null)
                    data = new LocalizationData();
                foreach (LocalizationData.StringData sd in strings) {
                    string text = data.FindString(sd.Name);
                    if (!string.IsNullOrWhiteSpace(text)) {
                        if (text != sd.Text)
                            throw new InternalError("The key {0} occurs more than once with different values in file {1}", sd.Name, file);
                    }
                    data.Strings.Add(sd);
                }
                await Localization.SaveAsync(this, filename, Localization.Location.DefaultResources, data);
            }
        }

        private static readonly Regex csNsRegex = new Regex("namespace\\s+(?'namespace'[A-Za-z0-9_\\.]+)\\s*\\{", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Get the namespace used in this file.
        /// </summary>
        private string GetCsFileNamespace(string fileName, string fileText) {
            Match m = csNsRegex.Match(fileText);
            if (!m.Success)
                throw new InternalError("No namespace found in {0}", fileName);
            return m.Groups["namespace"].Value;
        }

        private static readonly Regex csClassRegex = new Regex("(?'leadspace'[\t ]*)public\\s+(static\\s+|partial\\s+){0,1}class\\s+(?'class'[A-Za-z0-9_]+)\\s*", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Get the least indented or last class in this file.
        /// </summary>
        private string GetCsFileClass(string fileName, string fileText) {
            string cls = null;
            int lead = int.MaxValue;

            // find a public class xxxx
            Match m = csClassRegex.Match(fileText);
            while (m.Success) {
                string newCls = m.Groups["class"].Value;
                int newLead = m.Groups["leadspace"].Value.Length;
                if (newLead <= lead) { // we keep the least indented or the last class
                    lead = newLead;
                    cls = newCls;
                }
                m = m.NextMatch();
            }
            //if (cls == null)
            //    throw new InternalError("No class found in {0}", fileName);
            return cls;
        }

        /// <summary>
        /// Get the class name used in a static __ResStr definition.
        /// </summary>
        private string GetCsFileExplicitClass(string file, string fileText) {
            string cls = null;

            Match m = csExplResRegex.Match(fileText);
            while (m.Success) {
                string newCls = m.Groups["explCls"].Value;
                if (!string.IsNullOrWhiteSpace(cls) && cls != newCls)
                    throw new InternalError($"File {file} has multiple explicit classes ({cls}, {newCls}) defined in __ResStr() methods");
                cls = newCls;
                m = m.NextMatch();
            }
            return cls;
        }

        private static readonly Regex csExplResRegex = new Regex(@"(private|protected|internal)\s+static\s+string\s+__ResStr\s*\(\s*string\s+[a-zA-Z0-9_]+\s*,\s*string\s+[a-zA-Z0-9_]+\s*,\s*params\s+object\s*\[\s*\]\s+[a-zA-Z0-9_]+\s*\)\s*\{\s*return\s+ResourceAccess\s*\.\s*GetResourceString\s*\(\s*typeof\s*\((?'explCls'[a-zA-Z0-9_]+)\)", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex csResstrRegex = new Regex(@"((?'object'[A-Za-z0-9_]+)\s*\.\s*){0,1}\s*__ResStr\s*\(\s*""(?'name'[^""]*)""\s*,\s*""(?'text'(\\""|[^""])*)""\s*(,|\))", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex csBadResstrRegex = new Regex(@"__ResStr\s*\(\s*""(?'name'[^""]*)""\s*,\s*\$""", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex csBadParmsRegex = new Regex(@"\{[^0-9]", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Extract all __ResStr() string definitions
        /// </summary>
        private List<LocalizationData.StringData> GetCsFileStrings(string fileName, string fileText, bool explicitClass) {

            Dictionary<string,string> dict = new Dictionary<string,string>();

            Match m = csResstrRegex.Match(fileText);
            while (m.Success) {
                string obj = m.Groups["object"].Value;
                string name = m.Groups["name"].Value;
                string text = m.Groups["text"].Value;

                if (name == "something" && text == "something") {
                    ; // ignore this, it's from a generated template
                } else {
                    if (obj != "this" && obj != "")
                        throw new InternalError($"Invalid use of __ResStr() in file {fileName} - The instance {obj} can't be used. Use this or the static method __ResStr (see ResourceAccess.cs for info)");
                    if (obj == "this" && explicitClass)
                        throw new InternalError($"Invalid use of this.__ResStr() in file {fileName} - Use the static method __ResStr instead");
                    if (obj == "" && !explicitClass)
                        throw new InternalError($"Invalid use of the static method __ResStr() in file {fileName} - Use this.__ResStr() instead");

                    text = FixNL(text);
                    if (dict.ContainsKey(name)) {
                        if (dict[name] != text)
                            throw new InternalError("The key {0} occurs more than once with different values in file {1}", name, fileName);
                    } else
                        dict.Add(name, text);

                    Match mParms = csBadParmsRegex.Match(text);
                    if (mParms.Success) {
                        throw new InternalError($"Invalid use of __ResStr() in file {fileName} - \"{name}\" uses a named parameter which is not supported");
                    }
                }
                m = m.NextMatch();
            }

            m = csBadResstrRegex.Match(fileText);
            if (m.Success) {
                string name = m.Groups["name"].Value;
                throw new InternalError($"Invalid use of __ResStr() in file {fileName} - \"{name}\" uses $ formatting which is not supported");
            }

            List<LocalizationData.StringData> list = (from d in dict select new LocalizationData.StringData { Name = d.Key, Text = d.Value }).ToList();
            return list;
        }
    }
}
