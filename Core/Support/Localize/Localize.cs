/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core.Audit;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;

namespace YetaWF.Core.Localize {

    public class LocalizationData {

        public const int MaxString = 1000;
        public const int MaxComment = 1000;

        public class ClassData {
            [StringLength(MaxString)]
            public string Name { get; set; }
            [StringLength(MaxString)]
            public string BaseTypeName { get; set; }

            [StringLength(MaxString)]
            public string Header { get; set; }
            [StringLength(MaxString)]
            public string Footer { get; set; }
            [StringLength(MaxString)]
            public string Legend { get; set; }

            [Data_Binary]
            public SerializableDictionary<string, string> Categories { get; set; }

            [Data_Binary]
            public SerializableList<PropertyData> Properties { get; set; }

            public ClassData() {
                Properties = new SerializableList<PropertyData>();
                Categories = new Serializers.SerializableDictionary<string, string>();
            }
        }
        public class PropertyData {
            [StringLength(MaxString)]
            public string Name { get; set; }
            [StringLength(MaxString)]
            public string Caption { get; set; }
            [StringLength(MaxString)]
            public string Description { get; set; }
            [StringLength(Globals.MaxUrl)]
            public string HelpLink { get; set; }
            [StringLength(MaxString)]
            public string TextAbove { get; set; }
            [StringLength(MaxString)]
            public string TextBelow { get; set; }
        }
        public class EnumData {
            [StringLength(MaxString)]
            public string Name { get; set; }
            [Data_Binary]
            public SerializableList<EnumDataEntry> Entries { get; set; }

            public EnumData() {
                Entries = new SerializableList<EnumDataEntry>();
            }
            public EnumDataEntry FindEntry(string name) {
                return (from e in Entries where e.Value == name select e).FirstOrDefault();
            }
        }
        public class EnumDataEntry {
            [StringLength(MaxString)]
            public string Name { get; set; }
            [StringLength(MaxString)]
            public string Value { get; set; }
            [StringLength(MaxString)]
            public string Caption { get; set; }
            [StringLength(MaxString)]
            public string Description { get; set; }
        }
        public class StringData {
            [StringLength(MaxString)]
            public string Name { get; set; }
            [StringLength(MaxString)]
            public string Text { get; set; }
        }

        public class ResourceProvider {

        }
        public class Resource {
            public string Caption { get; set; }
            public string Description { get; set; }
        }

        [StringLength(MaxComment)]
        public string Comment { get; set; }
        [Data_Binary]
        public SerializableList<ClassData> Classes { get; set; }
        [Data_Binary]
        public SerializableList<EnumData> Enums { get; set; }
        [Data_Binary]
        public SerializableList<StringData> Strings { get; set; }

        public LocalizationData() {
            Classes = new SerializableList<ClassData>();
            Enums = new SerializableList<EnumData>();
            Strings = new SerializableList<StringData>();
        }
        public string FindString(string name) {
            StringData sd = FindStringEntry(name);
            if (sd == null) return null;
            return (sd.Text == null) ? "" : sd.Text;
        }
        public StringData FindStringEntry(string name) {
            StringData sd = (from s in Strings where s.Name == name select s).FirstOrDefault();
            return sd;
        }
        public EnumData FindEnum(string name) {
            return (from e in Enums where e.Name == name select e).FirstOrDefault();
        }
        public ClassData FindClass(string typeName) {
            return (from c in Classes where c.Name == typeName select c).FirstOrDefault();
        }
        public PropertyData FindProperty(string typeName, string propName) {
            // find the class
            ClassData classData = FindClass(typeName);
            if (classData == null) return null;
            // find the property in the class
            PropertyData propData = (from p in classData.Properties where p.Name == propName select p).FirstOrDefault();
            if (propData != null) return propData;
            // if we didn't find the property, search the base type
            if (string.IsNullOrWhiteSpace(classData.BaseTypeName))
                return null;
            return FindProperty(classData.BaseTypeName, propName);
        }
    }

    public class LocalizationSupport {

        // STARTUP
        // STARTUP
        // STARTUP

        private const string AbortOnFailureKey = "Fail-On-Missing-Localization-Resource";
        private const string UseKey = "Use-Localization-Resources";

        public static bool AbortOnFailure {
            get {
                if (abortOnFailure == null) {
                    if (!Startup.Started) return false;
                    abortOnFailure = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, AbortOnFailureKey);
                }
                return (bool) abortOnFailure;
            }
        }
        public async Task SetAbortOnFailureAsync(bool abort) {
            if (AbortOnFailure != abort) {
                WebConfigHelper.SetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, AbortOnFailureKey, abort);
                await WebConfigHelper.SaveAsync();
                abortOnFailure = abort;
                await Auditing.AddAuditAsync($"{nameof(LocalizationSupport)}.{nameof(SetAbortOnFailureAsync)}", "Localization", Guid.Empty,
                    $"{nameof(SetAbortOnFailureAsync)}({abort})"
                );
            }
        }
        private static bool? abortOnFailure = null;

        public static bool UseLocalizationResources {
            get {
                if (useResources == null) {
                    if (!Startup.Started || !YetaWFManager.HaveManager) return false;
                    useResources = WebConfigHelper.GetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, UseKey);
                }
                return (bool) useResources;
            }
        }
        public async Task SetUseLocalizationResourcesAsync(bool use) {
            if (UseLocalizationResources != use) {
                WebConfigHelper.SetValue<bool>(YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.AreaName, UseKey, use);
                await WebConfigHelper.SaveAsync();
                useResources = use;
                await Auditing.AddAuditAsync($"{nameof(LocalizationSupport)}.{nameof(SetUseLocalizationResourcesAsync)}", "Localization", Guid.Empty,
                    $"{nameof(SetUseLocalizationResourcesAsync)}({use})"
                );
            }
        }
        private static bool? useResources = null;
    }
}
