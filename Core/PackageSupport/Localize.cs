/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            await LocalizationSupport.ClearPackageDataAsync(this);

            // parse all source files to extract strings
            await ParseSourceFilesAsync(PackageSourceRoot);

            // enumerate all types
            List<Type> types = PackageAssembly.GetTypes().ToList();
            foreach (Type type in types) {

                //DEBUG if (type.FullName == "YetaWF.Core.Scheduler.SchedulerFrequency+TimeUnitEnum")
                //    path = path;

                LocalizationData data = LocalizationSupport.Load(Package.GetPackageFromType(type), type.FullName, LocalizationSupport.Location.DefaultResources);
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
                    await LocalizationSupport.SaveAsync(Package.GetPackageFromType(type), type.FullName, LocalizationSupport.Location.DefaultResources, data);
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
            if (path.EndsWith("\\Addons", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\AddonsBundles", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\AddonsCustom", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\Properties", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\bin", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\Data", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\DataXFER", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\Docs", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\obj", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\Properties", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\Sites", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\SitesHtml", StringComparison.OrdinalIgnoreCase)) return;
            if (path.EndsWith("\\Vault", StringComparison.OrdinalIgnoreCase)) return;
            List<string> files = await FileSystem.FileSystemProvider.GetFilesAsync(path, "*.cs");
            foreach (string file in files)
                await ParseCsSourceFileAsync(file);
            files = await FileSystem.FileSystemProvider.GetFilesAsync(path, "*.cshtml");
            foreach (string file in files)
                await ParseCshtmlSourceFileAsync(file);
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

            List<LocalizationData.StringData> strings = GetCsFileStrings(file, fileText);

            if (strings.Count > 0) {

                if (string.IsNullOrWhiteSpace(cls))
                    throw new InternalError("File {0} can't contain resource string definitions because its class doesn't support resource access");

                string filename = string.Format("{0}.{1}", ns, cls);
                LocalizationData data = LocalizationSupport.Load(this, filename, LocalizationSupport.Location.DefaultResources);
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
                await LocalizationSupport.SaveAsync(this, filename, LocalizationSupport.Location.DefaultResources, data);
            }
        }

        private static readonly Regex csNsRegex = new Regex("namespace\\s+(?'namespace'[A-Za-z0-9_\\.]+)\\s*\\{", RegexOptions.Compiled | RegexOptions.Multiline);

        private string GetCsFileNamespace(string fileName, string fileText) {
            Match m = csNsRegex.Match(fileText);
            if (!m.Success)
                throw new InternalError("No namespace found in {0}", fileName);
            return m.Groups["namespace"].Value;
        }

        private static readonly Regex csClassRegex = new Regex("(?'leadspace'[\t ]*)public\\s+(static\\s+|partial\\s+){0,1}class\\s+(?'class'[A-Za-z0-9_]+)\\s*", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex csCombResRegex = new Regex(@"\[\s*CombinedResources\s*\]", RegexOptions.Compiled | RegexOptions.Multiline);

        private string GetCsFileClass(string fileName, string fileText) {
            string cls = null;
            int lead = int.MaxValue;

            //[CombinedResources]
            Match m = csCombResRegex.Match(fileText);
            if (m.Success)
                return "Resources";

            // find a public class xxxx
            m = csClassRegex.Match(fileText);
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

        private static readonly Regex csResstrRegex = new Regex(@"((?'object'[A-Za-z0-9_]+)\s*\.\s*){0,1}\s*__ResStr\s*\(\s*""(?'name'[^""]*)""\s*,\s*""(?'text'(\\""|[^""])*)""\s*(,|\))", RegexOptions.Compiled | RegexOptions.Multiline);

        private List<LocalizationData.StringData> GetCsFileStrings(string fileName, string fileText) {

            Dictionary<string,string> dict = new Dictionary<string,string>();

            Match m = csResstrRegex.Match(fileText);
            while (m.Success) {
                string obj = m.Groups["object"].Value;
                string name = m.Groups["name"].Value;
                string text = m.Groups["text"].Value;

                if (name == "something" && text == "something") {
                    ; // ignore this, it's form the generated template
                } else {
                    if (obj != "this" && obj != "")
                        throw new InternalError("Invalid use of __ResStr(): The instance {0} can't be used. Use this or the static function __ResStr (see ResourceAccess.cs for info)", obj);
                    text = FixNL(text);
                    if (dict.ContainsKey(name)) {
                        if (dict[name] != text)
                            throw new InternalError("The key {0} occurs more than once with different values in file {1}", name, fileName);
                    } else
                        dict.Add(name, text);
                }
                m = m.NextMatch();
            }
            List<LocalizationData.StringData> list = (from d in dict select new LocalizationData.StringData { Name = d.Key, Text = d.Value }).ToList();
            return list;
        }

        // CSHTML

        private async Task ParseCshtmlSourceFileAsync(string file) {

            string fileText = await FileSystem.FileSystemProvider.ReadAllTextAsync(file);
            string cls = GetCshtmlFileClass(file, fileText);

            List<LocalizationData.StringData> strings = GetCshtmlFileStrings(file, fileText);

            if (strings.Count > 0) {

                if (string.IsNullOrWhiteSpace(cls))
                    throw new InternalError("File {0} can't contain resource string definitions because its class doesn't support resource access", file);

                string filename = cls;
                LocalizationData data = LocalizationSupport.Load(this, filename, LocalizationSupport.Location.DefaultResources);
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
                await LocalizationSupport.SaveAsync(this, filename, LocalizationSupport.Location.DefaultResources, data);
            }
        }

        private static readonly Regex cshtmlClassRegex = new Regex(@"\s*@\s*inherits\s*(?'base'[A-Za-z0-9_.]+)\s*\<(?'class'[A-Za-z0-9_\?\<\>.]+)\s*(?'more'(,|\>))", RegexOptions.Compiled | RegexOptions.Multiline);

        private string GetCshtmlFileClass(string fileName, string fileText) {

            // a template:  @inherits YetaWF.Modules.Identity.Views.Shared.LoginUsersHelper<int>
            //              @inherits YetaWF.Modules.Identity.Views.Shared.UsersHelper<YetaWF.Core.Serializers.SerializableList<YetaWF.Core.Identity.User>>
            // a view:      @inherits YetaWF.Core.Views.RazorView<YetaWF.Modules.Identity.Modules.RolesEditModule, YetaWF.Modules.Identity.Controllers.RolesEditModuleController.EditModel>
            //              @inherits YetaWF.Core.Views.RazorView<YetaWF.Modules.Identity.Modules.RolesBrowseModule, YetaWF.Modules.Identity.Controllers.RolesBrowseModuleController.BrowseModel>
            //DEBUG: if (fileText.Contains("@inherits .....Url<System.String>"))
            //DEBUG:    fileName = fileName;

            Match m = cshtmlClassRegex.Match(fileText);
            if (!m.Success)
                throw new InternalError("No class found in {0}", fileName);
            string pageCls = m.Groups["base"].Value;
            string moduleCls = m.Groups["class"].Value;
            string more = m.Groups["more"].Value;
            if (more.Contains(","))
                return moduleCls;// it's a regular view, so we return the module's class as the class for resources
            else
                return pageCls;// it's a template
        }

        private static readonly Regex cshtmlResstrRegex = new Regex(@"__ResStr(Html){0,1}\s*\(\s*""(?'name'[^""]*)""\s*,\s*""(?'text'(\\""|[^""])*)""\s*(,|\))", RegexOptions.Compiled | RegexOptions.Multiline);

        private List<LocalizationData.StringData> GetCshtmlFileStrings(string fileName, string fileText) {

            Dictionary<string, string> dict = new Dictionary<string, string>();

            Match m = cshtmlResstrRegex.Match(fileText);
            while (m.Success) {
                string name = m.Groups["name"].Value;
                string text = m.Groups["text"].Value;

                text = FixNL(text);
                if (dict.ContainsKey(name)) {
                    if (dict[name] != text)
                        throw new InternalError("The key {0} occurs more than once with different values in file {1}", name, fileName);
                } else
                    dict.Add(name, text);
                m = m.NextMatch();
            }
            List<LocalizationData.StringData> list = (from d in dict select new LocalizationData.StringData { Name = d.Key, Text = d.Value }).ToList();
            return list;
        }
    }
}
