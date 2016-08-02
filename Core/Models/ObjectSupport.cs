/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models {

    public class ClassData {

        public Type ClassType { get; set; }
        private Dictionary<string, object> CustomAttributes { get; set; }
        public string Header { get; private set; }
        public string Footer { get; private set; }
        public string Legend { get; private set; }

        public ClassData(Type classType, string header, string footer, string legend) {
            ClassType = classType;
            Header = header;
            Footer = footer;
            Legend = legend;
        }
        public ClassData(Type classType) {
            ClassType = classType;
            HeaderAttribute headerAttr = TryGetAttribute<HeaderAttribute>();
            Header = headerAttr != null ? headerAttr.Value : null;
            FooterAttribute footerAttr = TryGetAttribute<FooterAttribute>();
            Footer = footerAttr != null ? footerAttr.Value : null;
            LegendAttribute legendAttr = TryGetAttribute<LegendAttribute>();
            Legend = legendAttr != null ? legendAttr.Value : null;
        }
        public TYPE TryGetAttribute<TYPE>() {
            string name = typeof(TYPE).Name;
            TYPE attr = (TYPE) TryGetAttributeValue(name);
            return attr;
        }
        private object TryGetAttributeValue(string name) {
            if (!name.EndsWith("Attribute")) name += "Attribute";
            return (from a in GetAttributes() where a.Key == name select a.Value).FirstOrDefault();
        }
        private Dictionary<string, object> GetAttributes() {
            if (CustomAttributes == null) {
                CustomAttributes = new Dictionary<string, object>();
                foreach (Attribute a in ClassType.GetCustomAttributes()) {
                    CustomAttributes.Add(a.GetType().Name, a);
                }
            }
            return CustomAttributes;
        }
    }

    public class PropertyData {

        public string Name { get; private set; }
        public PropertyInfo PropInfo { get; private set; }
        public string UIHint { get; private set; }
        public bool ReadOnly { get; set; }
        public Type ContainerType { get; private set; }

        public string Caption { get; private set; }
        public string Description { get; private set; }
        public string HelpLink { get; private set; }
        public string TextAbove { get; private set; }
        public string TextBelow { get; private set; }
        public bool CalculatedProperty { get; set; }
        public int Order { get; private set; }
        public ResourceRedirectAttribute Redirect { get; private set; }

        public string GetCaption(object parentObject) {
            if (parentObject == null || Redirect == null) return Caption;
            return Redirect.GetCaption(parentObject);
        }
        public string GetDescription(object parentObject) {
            if (parentObject == null || Redirect == null) return Description;
            return Redirect.GetDescription(parentObject);
        }
        public string GetHelpLink(object parentObject) {
            if (parentObject == null || Redirect == null) return HelpLink;
            return Redirect.GetHelpLink(parentObject);
        }

        public PropertyData(string name, Type containerType, PropertyInfo propInfo,
            string caption = null, string description = null, string helpLink = null, string textAbove = null, string textBelow = null) {
            Name = name;
            ContainerType = containerType;
            PropInfo = propInfo;
            UIHint = null;
            UIHintAttribute uiHint = TryGetAttribute<UIHintAttribute>();
            if (uiHint != null) UIHint = uiHint.UIHint;
            ReadOnlyAttribute readOnly = TryGetAttribute<ReadOnlyAttribute>();
            if (readOnly != null) ReadOnly = true;
            Caption = caption;
            Description = description;
            HelpLink = HelpLink;
            TextAbove = textAbove;
            TextBelow = textBelow;
            DescriptionAttribute descAttr = TryGetAttribute<DescriptionAttribute>();
            Order = descAttr != null ? descAttr.Order : 0;
            Redirect = TryGetAttribute<ResourceRedirectAttribute>();// Check if there is a resource redirect for this property
            CalculatedProperty = TryGetAttribute<Data_CalculatedProperty>() != null;
        }
        public PropertyData(string name, Type containerType, PropertyInfo propInfo) {
            Name = name;
            ContainerType = containerType;
            PropInfo = propInfo;
            UIHint = null;
            UIHintAttribute uiHint = TryGetAttribute<UIHintAttribute>();
            if (uiHint != null) UIHint = uiHint.UIHint;
            ReadOnlyAttribute readOnly = TryGetAttribute<ReadOnlyAttribute>();
            if (readOnly != null) ReadOnly = true;
            CaptionAttribute captionAttr = TryGetAttribute<CaptionAttribute>();
            Caption = captionAttr != null ? captionAttr.Value : null;
            DescriptionAttribute descAttr = TryGetAttribute<DescriptionAttribute>();
            Description = descAttr != null ? descAttr.Value : null;
            HelpLinkAttribute helpLinkAttr = TryGetAttribute<HelpLinkAttribute>();
            HelpLink = helpLinkAttr != null ? helpLinkAttr.Value : null;
            Order = descAttr != null ? descAttr.Order : 0;
            TextAboveAttribute aboveAttr = TryGetAttribute<TextAboveAttribute>();
            TextAbove = aboveAttr != null ? aboveAttr.Value : null;
            TextBelowAttribute belowAttr = TryGetAttribute<TextBelowAttribute>();
            TextBelow = belowAttr != null ? belowAttr.Value : null;
            Redirect = TryGetAttribute<ResourceRedirectAttribute>();// Check if there is a resource redirect for this property
            CalculatedProperty = TryGetAttribute<Data_CalculatedProperty>() != null;
        }

        private Dictionary<string, object> CustomAttributes { get; set; }
        private Dictionary<string, object> AdditionalAttributes { get; set; }

        public TYPE GetPropertyValue<TYPE>(object parentObject) {
            TYPE val = (TYPE) PropInfo.GetValue(parentObject, null);
            return val;
        }
        public TYPE TryGetAttribute<TYPE>() {
            string name = typeof(TYPE).Name;
            TYPE attr = (TYPE) TryGetAttributeValue(name);
            return attr;
        }
        private object TryGetAttributeValue(string name) {
            if (!name.EndsWith("Attribute")) name += "Attribute";
            return (from a in GetAttributes() where a.Key == name select a.Value).FirstOrDefault();
        }
        public TYPE GetAdditionalAttributeValue<TYPE>(string name, TYPE dflt = default(TYPE)) {
            TYPE val = dflt;
            AdditionalMetadataAttribute attr = (AdditionalMetadataAttribute) (from a in AdditionalAttributes where a.Key == name select a.Value).FirstOrDefault();
            if (attr == null)
                return val;
            val = (TYPE) attr.Value;
            return val;
        }
        public bool HasAttribute(string name) {
            return TryGetAttributeValue(name) != null;
        }
        private Dictionary<string, object> GetAttributes() {
            if (CustomAttributes == null) {
                CustomAttributes = new Dictionary<string, object>();
                AdditionalAttributes = new Dictionary<string, object>();
                foreach (Attribute a in PropInfo.GetCustomAttributes()) {
                    string name = a.GetType().Name;
                    if (!name.EndsWith("Attribute")) name += "Attribute";
                    if (name == "AdditionalMetadataAttribute") {
                        // these are added as if they were attributes (based on name/value)
                        AdditionalMetadataAttribute am = (AdditionalMetadataAttribute) a;
                        name = am.Name;
                        AdditionalAttributes.Add(name, am);
                    } else {
                        CustomAttributes.Add(name, a);
                    }
                }
            }
            return CustomAttributes;
        }
    }
    public class EnumDataEntry {
        public string Name { get; set; }
        public object Value { get; set; }
        public FieldInfo FieldInfo { get; set; }
        public string Caption {
            get {
                if (_caption == null) return Name;
                return _caption;
            }
            set {
                _caption = value;
            }
        }
        private string _caption = null;
        public string Description { get; set; }

        public bool EnumDescriptionProvided { get { return _caption != null || Description != null; } }

        public EnumDataEntry(string name, object value, FieldInfo fieldInfo, string caption = null, string description = null) {
            Name = name;
            Value = value;
            FieldInfo = fieldInfo;
            Caption = caption;
            Description = description;
            EnumDescriptionAttribute enumAttr = (EnumDescriptionAttribute) Attribute.GetCustomAttribute(FieldInfo, typeof(EnumDescriptionAttribute));
            if (enumAttr != null) {
                Caption = enumAttr.Caption;
                Description = enumAttr.Description;
            }
        }
    }

    public class EnumData {
        public string Name { get; set; }
        public List<EnumDataEntry> Entries { get; set; }
        public Type ContainerType { get; set; }

        public EnumDataEntry FindValue(object value) {
            return (from e in Entries where e.Value.Equals(value) select e).FirstOrDefault();
        }
    }

    public class GridColumnInfo {
        public int ChWidth { get; set; }
        public int PixWidth { get; set; }
        public bool Sortable { get; set; }
        public bool Locked { get; set; }
        public bool Hidden { get; set; }
        public bool OnlySubmitWhenChecked { get; set; }
        public GridHAlignmentEnum Alignment { get; set; }
        public int Icons { get; set; }
        public List<FilterOptionEnum> FilterOptions { get; set; }
        public enum FilterOptionEnum {
            Equal = 1,
            NotEqual,
            LessThan,
            LessEqual,
            GreaterThan,
            GreaterEqual,
            StartsWith,
            NotStartsWith,
            Contains,
            NotContains,
            Endswith,
            NotEndswith,
            All = 0xffff,
        }

        public GridColumnInfo() {
            PixWidth = ChWidth = 0;
            Sortable = false;
            Locked = false;
            Hidden = false;
            OnlySubmitWhenChecked = false;
            Alignment = GridHAlignmentEnum.Unspecified;
            Icons = 0;
            FilterOptions = new List<FilterOptionEnum>();
        }
    }

    public static class ObjectSupport {

        private class ClassPropertyData {
            public Type ClassType { get; set; }
            public List<PropertyData> PropertyData { get; set; }
        }

        private class ClassEnumData {
            public Type ClassType { get; set; }
            public EnumData EnumData { get; set; }
        }

        // SITE SPECIFIC in PermanentManager:
        // Dictionary<string, ClassData> ClassAttrs
        // Dictionary<string, ClassPropertyData> ClassProperties
        // Dictionary<string, ClassEnumData> ClassEnums

        public static void InvalidateAll() {
            PermanentManager.RemoveObject<Dictionary<string, ClassData>>();
            PermanentManager.RemoveObject<Dictionary<string, ClassPropertyData>>();
            PermanentManager.RemoveObject<Dictionary<string, ClassEnumData>>();
        }
        public static ClassData GetClassData(Type type, bool Cache = true) {
            ClassData classData = null;
            Cache = Cache && YetaWFManager.HaveManager /* why did we use this? && YetaWFManager.Manager.LocalizationSupportEnabled */;
            Dictionary<string, ClassData> classAttrs = null;
            if (Cache && !PermanentManager.TryGetObject<Dictionary<string, ClassData>>(out classAttrs)) {
                classAttrs = new Dictionary<string, ClassData>();
                PermanentManager.AddObject<Dictionary<string, ClassData>>(classAttrs);
            }
            if (!Cache || classAttrs == null || !classAttrs.TryGetValue(type.FullName, out classData)) {
                LocalizationData locData = null;
                // check if we have this in resource files
                if (Cache && !type.IsGenericType) {
                    Package package = Package.TryGetPackageFromType(type);
                    if (package != null && (package.IsCorePackage || package.IsModulePackage || package.IsSkinPackage))
                        locData = LocalizationSupport.Load(package, type.FullName, LocalizationSupport.Location.Merge);
                }
                if (locData != null) {
                    LocalizationData.ClassData cls = locData.FindClass(type.FullName);
                    if (cls != null)
                        classData = new ClassData(type, cls.Header, cls.Footer, cls.Legend);
                    else
                        classData = new ClassData(type);
                } else {
                    classData = new ClassData(type);
                }
                if (Cache) {
                    try {
                        classAttrs.Add(type.FullName, classData);
                    } catch (Exception) { } // add may fail because there was a concurrent evaluation - just ignore
                }
            }
            return classData;
        }
        public static List<PropertyData> GetPropertyData(Type type, bool Cache = true, bool WithInherited = true) {

            Cache = Cache && YetaWFManager.HaveManager /* why did we use this? && YetaWFManager.Manager.LocalizationSupportEnabled */;
            if (Cache && !WithInherited) throw new InternalError("Can't use caching when requesting declared properties");

            // use reflection to get info
            Dictionary<string, ClassPropertyData> classProperties = null;
            if (Cache && !PermanentManager.TryGetObject<Dictionary<string, ClassPropertyData>>(out classProperties)) {
                classProperties = new Dictionary<string, ClassPropertyData>();
                PermanentManager.AddObject<Dictionary<string, ClassPropertyData>>(classProperties);
            }
            ClassPropertyData classData = null;
            if (classProperties != null && !classProperties.TryGetValue(type.FullName, out classData)) {
                classData = new ClassPropertyData() {
                    ClassType = type
                };
                try {
                    classProperties.Add(type.FullName, classData);
                } catch (Exception) { } // add may fail because there was a concurrent evaluation - just ignore
            }
            List<PropertyData> propertyData = null;
            if (Cache)
                propertyData = classData.PropertyData;
            if (propertyData == null) {
                propertyData = new List<PropertyData>();
                LocalizationData locData = null;
                // check if we have this in resource files
                if (Cache && !type.IsGenericType) {
                    Package package = Package.TryGetPackageFromType(type);
                    if (package != null && (package.IsCorePackage || package.IsModulePackage || package.IsSkinPackage))
                        locData = LocalizationSupport.Load(package, type.FullName, LocalizationSupport.Location.Merge);
                }
                // get information through reflection
                foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | (WithInherited ? BindingFlags.Default : BindingFlags.DeclaredOnly)).ToList()) {
                    if (locData != null) {
                        LocalizationData.PropertyData locPropData = locData.FindProperty(type.FullName, pi.Name);
                        if (locPropData != null) {
                            propertyData.Add(new PropertyData(pi.Name, type, pi, locPropData.Caption, locPropData.Description, locPropData.HelpLink, locPropData.TextAbove, locPropData.TextBelow));
                        } else
                            propertyData.Add(new PropertyData(pi.Name, type, pi));
                    } else {
                        propertyData.Add(new PropertyData(pi.Name, type, pi));
                    }
                }
            }
            if (Cache)
                classData.PropertyData = propertyData;
            return propertyData;
        }
        public static PropertyData GetPropertyData(Type type, string propName) {
            PropertyData propData = (from p in GetPropertyData(type) where p.Name == propName select p).FirstOrDefault();
            if (propData == null) throw new InternalError("No property {0} in {1}", propName, type.FullName);
            return propData;
        }
        public static PropertyData TryGetPropertyData(Type type, string propName) {
            PropertyData propData = (from p in GetPropertyData(type) where p.Name == propName select p).FirstOrDefault();
            return propData;
        }

        public static PropertyInfo GetProperty(Type type, string name)
        {
            PropertyInfo prop = TryGetProperty(type, name);
            if (prop == null)
                throw new InternalError("No property named {0} in {1}", name, type.FullName);
            return prop;
        }
        public static PropertyInfo TryGetProperty(Type type, string name) {
            List<PropertyInfo> props = GetProperties(type);
            return (from p in props where p.Name == name select p).FirstOrDefault();
        }
        public static List<PropertyInfo> GetProperties(Type type) {
            List<PropertyData> propData = GetPropertyData(type);
            return (from p in propData select p.PropInfo).ToList();
        }
        public static TYPE GetPropertyValue<TYPE>(object parentObject, string name, TYPE dflt = default(TYPE)) {
            TYPE val;
            if (!TryGetPropertyValue<TYPE>(parentObject, name, out val, dflt))
                throw new InternalError("No such property - {0}", name);
            return (TYPE) (object) val;
        }
        public static bool TryGetPropertyValue<TYPE>(object parentObject, string name, out TYPE val, TYPE dflt = default(TYPE)) {
            val = dflt;
            PropertyInfo prop = TryGetProperty(parentObject.GetType(), name);
            if (prop == null) return false;
            val = (TYPE) prop.GetValue(parentObject, null);
            return true;
        }

        public static EnumData GetEnumData(Type type) {

            Dictionary<string, ClassEnumData> classEnums;
            if (!PermanentManager.TryGetObject<Dictionary<string, ClassEnumData>>(out classEnums)) {
                classEnums = new Dictionary<string, ClassEnumData>();
                PermanentManager.AddObject<Dictionary<string, ClassEnumData>>(classEnums);
            }
            ClassEnumData classEnumData;
            if (!classEnums.TryGetValue(type.FullName, out classEnumData)) {
                classEnumData = new ClassEnumData() {
                    ClassType = type
                };
                try {
                    classEnums.Add(type.FullName, classEnumData);
                } catch (Exception) { } // add may fail because there was a concurrent evaluation - just ignore
            }
            if (classEnumData.EnumData == null) {
                List<FieldInfo> fis = type.GetFields().ToList();
                classEnumData.EnumData = new EnumData {
                     Name = type.Name,
                     Entries = new List<EnumDataEntry>(),
                     ContainerType = type,
                };
                // check if we have this in resource files
                LocalizationData.EnumData enumData = null;
                LocalizationData data = LocalizationSupport.Load(Package.GetPackageFromType(type), type.FullName, LocalizationSupport.Location.Merge);
                if (data != null)
                    enumData = data.FindEnum(type.FullName);
                else if (LocalizationSupport.AbortOnFailure)
                        throw new InternalError("Enumerated type {0} has no resource file", type.FullName);
                // get information through reflection
                object enumObj = Activator.CreateInstance(type);
                foreach (FieldInfo fi in fis) {
                    if (fi.IsSpecialName) continue;
                    if (enumData != null) {
                        LocalizationData.EnumDataEntry entry = enumData.FindEntry(fi.Name);
                        if (entry != null)
                            classEnumData.EnumData.Entries.Add(new EnumDataEntry(fi.Name, fi.GetValue(enumObj), fi, entry.Caption, entry.Description));
                        else
                            if (LocalizationSupport.AbortOnFailure)
                                throw new InternalError("Enumerated type {0} is missing an entry for {1} in the resource file", type.FullName, fi.Name);
                    } else
                        classEnumData.EnumData.Entries.Add(new EnumDataEntry(fi.Name, fi.GetValue(enumObj), fi));
                }
            }
            return classEnumData.EnumData;
        }

        // GRID
        // GRID
        // GRID

        //RESEARCH: this could use some caching
        public static Dictionary<string, GridColumnInfo> ReadGridDictionary(Package package, Type recordType, string file, ref string sortCol, ref GridDefinition.SortBy sortDir) {
            Dictionary<string, GridColumnInfo> dict = new Dictionary<string, GridColumnInfo>();
            if (!File.Exists(file)) return dict;
            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines) {
                string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                GridColumnInfo gridCol = new GridColumnInfo();
                int len = parts.Length;
                if (len > 0) {
                    bool add = true;
                    string name = parts[0];
                    for (int i = 1 ; i < len ; ++i) {
                        string part = GetPart(parts[i], package, recordType, file, name);
                        if (string.Compare(part, "sort", true) == 0) gridCol.Sortable = true;
                        else if (string.Compare(part, "locked", true) == 0) gridCol.Locked = true;
                        else if (string.Compare(part, "left", true) == 0) gridCol.Alignment = GridHAlignmentEnum.Left;
                        else if (string.Compare(part, "center", true) == 0) gridCol.Alignment = GridHAlignmentEnum.Center;
                        else if (string.Compare(part, "right", true) == 0) gridCol.Alignment = GridHAlignmentEnum.Right;
                        else if (string.Compare(part, "hidden", true) == 0) gridCol.Hidden = true;
                        else if (string.Compare(part, "onlysubmitwhenchecked", true) == 0) gridCol.OnlySubmitWhenChecked = true;
                        else if (string.Compare(part, "icons", true) == 0) {
                            int n = GetNextNumber(parts, i, part, file, name);
                            if (n < 1) throw new InternalError("Icons must be >= 1 for column {0} in {1}", name, file);
                            gridCol.Icons = n;
                            ++i;
                        } else if (string.Compare(part, "defaultSort", true) == 0) {
                            sortCol = name;
                            part = GetNextPart(parts, i, part, file, name);
                            if (part == "asc") sortDir = GridDefinition.SortBy.Ascending;
                            else if (part == "desc") sortDir = GridDefinition.SortBy.Descending;
                            else throw new InternalError("Missing Asc/Desc following defaultSort for column {1} in {2}", part, name, file);
                            ++i;
                        } else if (string.Compare(part, "internal", true) == 0) {
                            bool showInternals = UserSettings.GetProperty<bool>("ShowInternals");
                            if (!showInternals) {
                                add = false;
                                break;
                            }
                        } else if (string.Compare(part, "filter", true) == 0) {
                            if (gridCol.FilterOptions.Count > 0) throw new InternalError("Multiple filter options in {0} for {1}", file, name);
                            gridCol.FilterOptions = GetAllFilterOptions();
                        } else if (part.StartsWith("filter(", StringComparison.InvariantCultureIgnoreCase)) {
                            if (gridCol.FilterOptions.Count > 0) throw new InternalError("Multiple filter options in {0} for {1}", file, name);
                            gridCol.FilterOptions = GetFilterOptions(part.Substring(6), file, name);
                        } else if (part.EndsWith("pix", StringComparison.InvariantCultureIgnoreCase)) {
                            if (gridCol.ChWidth != 0) throw new InternalError("Can't use character width and pixel width at the same time in {0} for {1}", file, name);
                            part = part.Substring(0, part.Length - 3);
                            int n = GetNumber(part, file, name);
                            gridCol.PixWidth = n;
                        } else {
                            if (gridCol.PixWidth != 0) throw new InternalError("Can't use character width and pixel width at the same time in {0} for {1}", file, name);
                            int n = GetNumber(part, file, name);
                            gridCol.ChWidth = n;
                        }
                    }
                    if (add) {
                        try {
                            dict.Add(name, gridCol);
                        } catch (Exception exc) {
                            throw new InternalError("Can't add {1} in {0} - {2}", file, name, exc.Message);
                        }
                    }
                }
            }
            return dict;
        }

        private static List<GridColumnInfo.FilterOptionEnum> GetAllFilterOptions() {
            List<GridColumnInfo.FilterOptionEnum> filterFlags = new List<GridColumnInfo.FilterOptionEnum>() {
                GridColumnInfo.FilterOptionEnum.Contains,
                GridColumnInfo.FilterOptionEnum.NotContains,
                GridColumnInfo.FilterOptionEnum.Equal,
                GridColumnInfo.FilterOptionEnum.NotEqual,
                GridColumnInfo.FilterOptionEnum.LessThan,
                GridColumnInfo.FilterOptionEnum.LessEqual,
                GridColumnInfo.FilterOptionEnum.GreaterThan,
                GridColumnInfo.FilterOptionEnum.GreaterEqual,
                GridColumnInfo.FilterOptionEnum.StartsWith,
                GridColumnInfo.FilterOptionEnum.NotStartsWith,
                GridColumnInfo.FilterOptionEnum.Endswith,
                GridColumnInfo.FilterOptionEnum.NotEndswith,
            };
            return filterFlags;
        }

        private static List<GridColumnInfo.FilterOptionEnum> GetFilterOptions(string part, string file, string name) {
            if (!part.StartsWith("(") || !part.EndsWith(")")) throw new InternalError("Invalid filters() options");
            part = part.Substring(1, part.Length - 2);
            string[] fs = part.Split(new char[] { ',' });
            List<GridColumnInfo.FilterOptionEnum> filterFlags = new List<GridColumnInfo.FilterOptionEnum>();
            foreach (string f in fs) {
                switch (f) {
                    case "==": filterFlags.Add(GridColumnInfo.FilterOptionEnum.Equal); break;
                    case "!=": filterFlags.Add(GridColumnInfo.FilterOptionEnum.NotEqual); break;
                    case "<": filterFlags.Add(GridColumnInfo.FilterOptionEnum.LessThan); break;
                    case "<=": filterFlags.Add(GridColumnInfo.FilterOptionEnum.LessEqual); break;
                    case ">": filterFlags.Add(GridColumnInfo.FilterOptionEnum.GreaterThan); break;
                    case ">=": filterFlags.Add(GridColumnInfo.FilterOptionEnum.GreaterEqual); break;
                    case "x*": filterFlags.Add(GridColumnInfo.FilterOptionEnum.StartsWith); break;
                    case "!x*": filterFlags.Add(GridColumnInfo.FilterOptionEnum.NotStartsWith); break;
                    case "*x": filterFlags.Add(GridColumnInfo.FilterOptionEnum.Endswith); break;
                    case "!*x": filterFlags.Add(GridColumnInfo.FilterOptionEnum.NotEndswith); break;
                    case "*x*": filterFlags.Add(GridColumnInfo.FilterOptionEnum.Contains); break;
                    case "!*x*": filterFlags.Add(GridColumnInfo.FilterOptionEnum.NotContains); break;
                    default:
                        throw new InternalError("Invalid filter option {0} in {1} for {2}", f, file, name);
                }
            }
            filterFlags = filterFlags.Distinct().ToList();
            return filterFlags;
        }

        private static string GetPart(string part, Package package, Type recordType, string file, string name) {
            if (part.StartsWith("[") && part.EndsWith("]") && part.Length > 2) {
                string[] vars = part.Substring(1, part.Length - 2).Split(new[] { '.' });
                if (vars.Length != 2) throw new InternalError("Invalid variable {0} for column {1} in {2}", part, name, file);
                if (vars[0] == "Globals") {
                    FieldInfo fi = typeof(Globals).GetField(vars[1], BindingFlags.Public | BindingFlags.Static);
                    if (fi == null) throw new InternalError("Globals.{0} doesn't exist - column {1} in {2}", vars[1], name, file);
                    part = fi.GetValue(null).ToString();
                } else if (vars[0] == "Package") {
                    VersionManager.AddOnProduct addonVersion = VersionManager.FindModuleVersion(package.Domain, package.Product);
                    foreach (var type in addonVersion.SupportTypes) {
                        object o = Activator.CreateInstance(type);
                        if (o == null)
                            throw new InternalError("Type {0} can't be created for {1}/{2}", type.Name, package.Domain, package.Product);
                        FieldInfo fi = type.GetField(vars[1], BindingFlags.Public | BindingFlags.Static);
                        if (fi != null) {
                            part = fi.GetValue(null).ToString();
                            break;
                        }
                    }
                } else throw new InternalError("Unknown variable {0} for column {1} in {2}", part, name, file);
            }
            return part;
        }
        private static int GetNextNumber(string[] parts, int i, string part, string file, string name) {
            part = GetNextPart(parts, i, part, file, name);
            return GetNumber(part, file, name);
        }
        private static string GetNextPart(string[] parts, int i, string part, string file, string name) {
            if (i + 1 >= parts.Length) throw new InternalError("Missing token following {0} column {1} in {2}", part, name, file);
            return parts[i + 1];
        }
        private static int GetNumber(string part, string file, string name) {
            try {
                int val = Convert.ToInt32(part);
                return val;
            } catch (Exception) {
                throw new InternalError("Invalid number for part {0} column {1} in {2}", part, name, file);
            }
        }

        /// <summary>
        /// Copies properties from one object to another.
        /// </summary>
        /// <param name="fromObject"></param>
        /// <param name="toObject"></param>
        /// <param name="ReadOnly"></param>
        /// <param name="ForceReadOnlyFromCopy"></param>
        public static void CopyData(object fromObject, object toObject, bool ReadOnly = false, bool ForceReadOnlyFromCopy = false) {
            // copy all properties from fromObject to toObject
            // toObject probably has fewer properties
            Type tpFrom = fromObject.GetType();
            Type tpTo = toObject.GetType();
            foreach (var toPropData in GetPropertyData(tpTo)) {
                PropertyData fromPropData = ObjectSupport.TryGetPropertyData(tpFrom, toPropData.Name);
                if (fromPropData != null && toPropData.PropInfo.CanWrite) {
                    if (!ForceReadOnlyFromCopy && fromPropData.ReadOnly)// we don't copy read/only properties from the fromObject because it means we didn't make changes (and could potentially incorrectly override the target)
                        continue;
                    if (ReadOnly) { // only copy properties that are marked read/only in toObject because they need to be refreshed
                        if (!toPropData.ReadOnly)
                            continue;
                    }
                    try {
                        object o = fromPropData.PropInfo.GetValue(fromObject, null);
                        toPropData.PropInfo.SetValue(toObject, o, null);
                    } catch (Exception) { }
                }
            }
        }

        public static void CopyDataFromOriginal(object originalObject, object toObject) {

            // since the model contains all editable data from ModuleDefinition
            // we have to explicitly copy properties that don't make it into the model,
            // i.e. mark any data with the CopyAttribute.

            Type tp = originalObject.GetType();
            foreach (var propData in GetPropertyData(tp)) {
                if (propData.PropInfo.CanRead && propData.PropInfo.CanWrite) {
                    CopyAttribute copyAttr = propData.TryGetAttribute<CopyAttribute>();
                    if (copyAttr != null) {
                        try {
                            object o = propData.PropInfo.GetValue(originalObject, null);
                            propData.PropInfo.SetValue(toObject, o, null);
                        } catch (Exception) { }
                    }
                }
            }
        }

        /// <summary>
        /// Add all string properties of the object (used by dynamic url search to add all string properties as search terms)
        /// </summary>
        public static void AddStringProperties(object originalObject, Action<YetaWF.Core.Models.MultiString, PageDefinition, string, string, DateTime, DateTime?> addTermsForPage,
                PageDefinition page, string url, string title, DateTime dateCreated, DateTime? dateUpdated) {
            Type tp = originalObject.GetType();
            foreach (var propData in GetPropertyData(tp)) {
                if (propData.PropInfo.CanRead && propData.PropInfo.CanWrite) {
                    if (propData.PropInfo.PropertyType == typeof(string)) {
                        string s = (string) propData.PropInfo.GetValue(originalObject, null);
                        addTermsForPage(s, page, url, title, dateCreated, dateUpdated);
                    } else if (propData.PropInfo.PropertyType == typeof(MultiString)) {
                        MultiString ms = (MultiString) propData.PropInfo.GetValue(originalObject, null);
                        addTermsForPage(ms, page, url, title, dateCreated, dateUpdated);
                    }
                }
            }
        }
    }
}
