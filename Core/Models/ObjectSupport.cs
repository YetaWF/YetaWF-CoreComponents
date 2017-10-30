/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Addons;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.IO;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
#else
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Models {

    /// <summary>
    /// Describes a class including all localization attributes.
    /// </summary>
    public class ClassData {

        /// <summary>
        /// The class type.
        /// </summary>
        public Type ClassType { get; set; }
        /// <summary>
        /// The localized text derived from the HeaderAttribute.
        /// </summary>
        public string Header { get; private set; }
        /// <summary>
        /// The localized text derived from the FooterAttribute.
        /// </summary>
        public string Footer { get; private set; }
        /// <summary>
        /// The localized text derived from the LegendAttribute.
        /// </summary>
        public string Legend { get; private set; }
        /// <summary>
        /// All categories derived from the CategoryAttribute of all defined properties in this class.
        /// </summary>
        /// <remarks>A dictionary of all categories. The key is the non-localized category name. The value is the localized category name.</remarks>
        public SerializableDictionary<string, string> Categories { get; private set; }

        private Dictionary<string, object> CustomAttributes { get; set; }

        internal ClassData(Type classType, string header, string footer, string legend, SerializableDictionary<string, string> categories) {
            ClassType = classType;
            Header = header;
            Footer = footer;
            Legend = legend;
            Categories = categories;
        }
        internal ClassData(Type classType) {
            ClassType = classType;
            HeaderAttribute headerAttr = TryGetAttribute<HeaderAttribute>();
            Header = headerAttr != null ? headerAttr.Value : null;
            FooterAttribute footerAttr = TryGetAttribute<FooterAttribute>();
            Footer = footerAttr != null ? footerAttr.Value : null;
            LegendAttribute legendAttr = TryGetAttribute<LegendAttribute>();
            Legend = legendAttr != null ? legendAttr.Value : null;
            Categories = new Serializers.SerializableDictionary<string, string>();
        }
        /// <summary>
        /// Retrieves a class Attribute.
        /// </summary>
        /// <typeparam name="TYPE">The type of the attribute.</typeparam>
        /// <returns>The attribute or null if not found.</returns>
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

    /// <summary>
    /// Describes a property including all localization attributes.
    /// </summary>
    public class PropertyData {

        /// <summary>
        ///  The property name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The PropertyInfo used for reflection for this property.
        /// </summary>
        public PropertyInfo PropInfo { get; private set; }
        /// <summary>
        /// The UIHint derived from the property's UIHint attribute.
        /// </summary>
        public string UIHint { get; private set; }
        /// <summary>
        /// Defines whether the property is read only, derived from the ReadOnlyAttribute.
        /// </summary>
        public bool ReadOnly { get; private set; }
        /// <summary>
        /// The Type of the class where this property is located.
        /// </summary>
        public Type ContainerType { get; private set; }

        /// <summary>
        /// The localized caption derived from the CaptionAttribute.
        /// </summary>
        public string Caption { get; private set; }
        /// <summary>
        /// The localized description derived from the DescriptionAttribute.
        /// </summary>
        public string Description { get; private set; }
        /// <summary>
        /// The help link derived from the HelpLinkAttribute.
        /// </summary>
        public string HelpLink { get; private set; }
        /// <summary>
        /// The localized text shown above the property derived from the TextAboveAttribute.
        /// </summary>
        public string TextAbove { get; private set; }
        /// <summary>
        /// The localized text shown below the property derived from the TextBelowAttribute.
        /// </summary>
        public string TextBelow { get; private set; }
        /// <summary>
        /// The non-localized categories derived from the CategoryAttribute.
        /// </summary>
        /// <remarks>The localized categories can be found in YetaWF.Core.Models.ClassData.Categories.</remarks>
        public List<string> Categories { get; private set; }
        /// <summary>
        /// Defines whether the Data_CalculatedProperty attribute is defined for this property.
        /// </summary>
        public bool CalculatedProperty { get; private set; }
        /// <summary>
        /// Defines the order of the property within the class, derived from the OrderAttribute.
        /// </summary>
        public int Order { get; private set; }

        private ResourceRedirectAttribute Redirect { get; set; }

#if MVC6
        /// <summary>
        /// Retrieves the property caption.
        /// </summary>
        /// <param name="parentType">The type of the parent model containing this property.</param>
        /// <returns>The caption.</returns>
        public string GetCaption(Type parentType) {
            return Caption;
        }
#else
#endif
        /// <summary>
        /// Retrieves the property caption.
        /// </summary>
        /// <param name="parentObject">The parent model containing this property.</param>
        /// <returns>The caption.</returns>
        /// <remarks>If the RedirectAttribute is used, GetCaption returns the redirected caption, otherwise the localized caption derived from the CaptionAttribute is returned.</remarks>
        public string GetCaption(object parentObject) {
            if (parentObject == null || Redirect == null) return Caption;
            return Redirect.GetCaption(parentObject);
        }
#if MVC6
        /// <summary>
        /// Retrieves the property description.
        /// </summary>
        /// <param name="parentType">The type of the parent model containing this property.</param>
        /// <returns>The description.</returns>
        public string GetDescription(Type parentType) {
            return Description;
        }
#else
#endif
        /// <summary>
        /// Retrieves the property description.
        /// </summary>
        /// <param name="parentObject">The parent model containing this property.</param>
        /// <returns>The description.</returns>
        /// <remarks>If the RedirectAttribute is used, GetDescription returns the redirected description, otherwise the localized description derived from the DescriptionAttribute is returned.</remarks>
        public string GetDescription(object parentObject) {
            if (parentObject == null || Redirect == null) return Description;
            return Redirect.GetDescription(parentObject);
        }
#if MVC6
        /// <summary>
        /// Retrieves the property help link.
        /// </summary>
        /// <param name="parentType">The type of the parent model containing this property.</param>
        /// <returns>The help link.</returns>
        /// <remarks>If the RedirectAttribute is used, GetHelpLink returns the redirected help link, otherwise the help link derived from the HelpLinkAttribute is returned.</remarks>
        public string GetHelpLink(Type parentType) {
            return HelpLink;
        }
#else
#endif
        /// <summary>
        /// Retrieves the property help link.
        /// </summary>
        /// <param name="parentObject">The parent model containing this property.</param>
        /// <returns>The help link.</returns>
        /// <remarks>If the RedirectAttribute is used, GetHelpLink returns the redirected help link, otherwise the help link derived from the HelpLinkAttribute is returned.</remarks>
        public string GetHelpLink(object parentObject) {
            if (parentObject == null || Redirect == null) return HelpLink;
            return Redirect.GetHelpLink(parentObject);
        }

        internal PropertyData(string name, Type containerType, PropertyInfo propInfo,
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
            HelpLink = helpLink;
            TextAbove = textAbove;
            TextBelow = textBelow;
            CategoryAttribute cats = TryGetAttribute<CategoryAttribute>();
            if (cats != null)
                Categories = cats.Categories;
            else
                Categories = new List<string>();
            DescriptionAttribute descAttr = TryGetAttribute<DescriptionAttribute>();
            Order = descAttr != null ? descAttr.Order : 0;
            Redirect = TryGetAttribute<ResourceRedirectAttribute>();// Check if there is a resource redirect for this property
            CalculatedProperty = TryGetAttribute<Data_CalculatedProperty>() != null;
        }
        internal PropertyData(string name, Type containerType, PropertyInfo propInfo) {
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
            CategoryAttribute cats = TryGetAttribute<CategoryAttribute>();
            if (cats != null)
                Categories = cats.Categories;
            else
                Categories = new List<string>();
            Redirect = TryGetAttribute<ResourceRedirectAttribute>();// Check if there is a resource redirect for this property
            CalculatedProperty = TryGetAttribute<Data_CalculatedProperty>() != null;
        }

        private Dictionary<string, object> CustomAttributes { get; set; }
        private Dictionary<string, object> AdditionalAttributes { get; set; }

        /// <summary>
        /// Retrieves the property value.
        /// </summary>
        /// <typeparam name="TYPE">The return Type of the property.</typeparam>
        /// <param name="parentObject">The parent model containing this property.</param>
        /// <returns>The property value.</returns>
        public TYPE GetPropertyValue<TYPE>(object parentObject) {
            TYPE val = (TYPE) PropInfo.GetValue(parentObject, null);
            return val;
        }
        /// <summary>
        /// Retrieves a property Attribute.
        /// </summary>
        /// <typeparam name="TYPE">The type of the attribute.</typeparam>
        /// <returns>The attribute or null if not found.</returns>
        public TYPE TryGetAttribute<TYPE>() {
            string name = typeof(TYPE).Name;
            TYPE attr = (TYPE) TryGetAttributeValue(name);
            return attr;
        }
        /// <summary>
        /// Returns whether the specified attribute exists.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <returns>True if the attribute exists, false otherwise.</returns>
        public bool HasAttribute(string name) {
            return TryGetAttributeValue(name) != null;
        }
        private object TryGetAttributeValue(string name) {
            if (!name.EndsWith("Attribute")) name += "Attribute";
            return (from a in GetAttributes() where a.Key == name select a.Value).FirstOrDefault();
        }
        /// <summary>
        /// Retrieves the value specified on an AdditionalMetadataAttribute.
        /// </summary>
        /// <typeparam name="TYPE">The Type of the value.</typeparam>
        /// <param name="name">The name specified on the AdditionalMetadataAttribute.</param>
        /// <param name="dflt">The default value returned if the AdditionalMetadataAttribute is not found.</param>
        /// <returns>The value found on the AdditionalMetadataAttribute, or the value defined using the dflt parameter.</returns>
        public TYPE GetAdditionalAttributeValue<TYPE>(string name, TYPE dflt = default(TYPE)) {
            TYPE val = dflt;
            AdditionalMetadataAttribute attr = (AdditionalMetadataAttribute) (from a in AdditionalAttributes where a.Key == name select a.Value).FirstOrDefault();
            if (attr == null)
                return val;
            val = (TYPE) attr.Value;
            return val;
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
    /// <summary>
    /// Defines one entry value in an enumerated type.
    /// </summary>
    public class EnumDataEntry {
        /// <summary>
        /// The name of the enumerated type entry.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The value of the enumerated type entry.
        /// </summary>
        public object Value { get; private set; }
        /// <summary>
        /// The FieldInfo for the enumerated type entry, used for reflection.
        /// </summary>
        public FieldInfo FieldInfo { get; private set; }
        /// <summary>
        /// The localized caption for the enumerated type entry, derived from the EnumDescriptionAttribute.
        /// </summary>
        public string Caption {
            get {
                if (_caption == null) return Name;
                return _caption;
            }
            private set {
                _caption = value;
            }
        }
        private string _caption = null;

        /// <summary>
        /// The localized description for the enumerated type entry, derived from the EnumDescriptionAttribute.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Returns whether a caption or description is available;
        /// </summary>
        public bool EnumDescriptionProvided { get { return _caption != null || Description != null; } }

        internal EnumDataEntry(string name, object value, FieldInfo fieldInfo, string caption = null, string description = null) {
            Name = name;
            Value = value;
            FieldInfo = fieldInfo;
            Caption = caption;
            Description = description;
            if (caption == null && description == null) {
                EnumDescriptionAttribute enumAttr = (EnumDescriptionAttribute)Attribute.GetCustomAttribute(FieldInfo, typeof(EnumDescriptionAttribute));
                if (enumAttr != null) {
                    Caption = enumAttr.Caption;
                    Description = enumAttr.Description;
                }
            }
        }
    }

    /// <summary>
    /// Describes an enumerated type.
    /// </summary>
    public class EnumData {
        /// <summary>
        /// The name of the enumerated type.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// All entries for the enumerated type.
        /// </summary>
        public List<EnumDataEntry> Entries { get; private set; }
        /// <summary>
        /// Find a enum value.
        /// </summary>
        /// <param name="value">The value searched.</param>
        /// <returns>The entry matching the value.</returns>
        public EnumDataEntry FindValue(object value) {
            return (from e in Entries where e.Value.Equals(value) select e).FirstOrDefault();
        }
        internal EnumData(string name) {
            Name = name;
            Entries = new List<Models.EnumDataEntry>();
        }
    }

    /// <summary>
    /// Implements Reflection for YetaWF.
    /// </summary>
    /// <remarks>
    /// Merges language specific data collected from various attributes (Description, Caption, etc.), merges language localization resources
    /// and caches all data.
    ///
    /// This is the only mechanism that should be used for type reflection.
    /// There is some legacy code and pre-startup code that uses .NET reflection, but this should be minimal.
    /// </remarks>
    public static class ObjectSupport {

        private class LanguageObjectData {
            public string Language { get; set; }
            public Dictionary<Type, ObjectClassData> ObjectClassDatas { get; set; }
            public Dictionary<Type, ObjectEnumData> ObjectEnumDatas { get; set; }
            public LanguageObjectData() {
                ObjectClassDatas = new Dictionary<Type, ObjectClassData>();
                ObjectEnumDatas = new Dictionary<Type, ObjectEnumData>();
            }
        }
        private class ObjectClassData {
            public Type ClassType { get; set; }
            public ClassData ClassData { get; set; }
            public Dictionary<string, PropertyData> PropertyData { get; set; }
            public ObjectClassData() {
                PropertyData = new Dictionary<string, PropertyData>();
            }
        }
        private class ObjectEnumData {
            public Type ClassType { get; set; }
            public EnumData EnumData { get; set; }
        }

        // SITE SPECIFIC in PermanentManager:
        // Dictionary<string, LanguageObjectData> LanguageObject

        /// <summary>
        /// Removes all cached data.
        /// </summary>
        public static void InvalidateAll() {
            PermanentManager.RemoveObject<Dictionary<string, LanguageObjectData>>();
        }
        private static ObjectClassData GetObjectClassData(Type type, bool Cache = true) {

            ObjectClassData objClassData = null;

            StringLocks.DoAction(type.FullName, () => {

                string lang = MultiString.ActiveLanguage;
                LanguageObjectData langObjData = GetLanguageObjectData(lang);

                // Get class info from language info & localization resource files
                if (!langObjData.ObjectClassDatas.TryGetValue(type, out objClassData)) {
                    objClassData = new ObjectClassData() {
                        ClassType = type,
                    };
                    if (Cache) // only cache if we include inherited properties
                        langObjData.ObjectClassDatas.Add(type, objClassData);

                    // check if we have this in resource files
                    LocalizationData locData = null;
                    if (Cache && !type.IsGenericType) {
                        Package package = Package.TryGetPackageFromType(type);
                        if (package != null && (package.IsCorePackage || package.IsModulePackage || package.IsSkinPackage))
                            locData = LocalizationSupport.Load(package, type.FullName, LocalizationSupport.Location.Merge);
                    }
                    // get class data
                    if (locData != null) {
                        LocalizationData.ClassData cls = locData.FindClass(type.FullName);
                        if (cls != null)
                            objClassData.ClassData = new ClassData(type, cls.Header, cls.Footer, cls.Legend, cls.Categories);
                        else
                            objClassData.ClassData = new ClassData(type);
                    } else {
                        objClassData.ClassData = new ClassData(type);
                    }
                    // get property data through reflection
                    foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | (Cache ? BindingFlags.Default : BindingFlags.DeclaredOnly)).ToList()) {
                        LocalizationData.PropertyData locPropData = null;
                        if (locData != null) {
                            locPropData = locData.FindProperty(type.FullName, pi.Name);
                            if (locPropData == null) {
                                // check if we have this in a base class
                                if (pi.DeclaringType != null && pi.DeclaringType != type) {
                                    Package package = Package.TryGetPackageFromType(pi.DeclaringType);
                                    if (package != null && (package.IsCorePackage || package.IsModulePackage || package.IsSkinPackage)) {
                                        LocalizationData baseLocData = LocalizationSupport.Load(package, pi.DeclaringType.FullName, LocalizationSupport.Location.Merge);
                                        if (baseLocData != null)
                                            locPropData = baseLocData.FindProperty(pi.DeclaringType.FullName, pi.Name);
                                    }
                                }
                            }
                        }
                        objClassData.PropertyData.Remove(pi.Name);
                        if (locPropData != null)
                            objClassData.PropertyData.Add(pi.Name, new PropertyData(pi.Name, type, pi, locPropData.Caption, locPropData.Description, locPropData.HelpLink, locPropData.TextAbove, locPropData.TextBelow));
                        else
                            objClassData.PropertyData.Add(pi.Name, new PropertyData(pi.Name, type, pi));
                    }
                }
            });
            return objClassData;
        }
        /// <summary>
        /// Retrieve class information for a given Type.
        /// </summary>
        /// <param name="type">The Type.</param>
        /// <param name="Cache">True if all data should be cached, false otherwise.
        /// If false is specified, only the class without inherited/base class information is returned.</param>
        /// <returns>Class information.</returns>
        public static ClassData GetClassData(Type type, bool Cache = true) {
            ObjectClassData objClassData = GetObjectClassData(type, Cache: Cache);
            return objClassData.ClassData;
        }
        /// <summary>
        /// Retrieve property information for a given Type.
        /// </summary>
        /// <param name="type">The Type.</param>
        /// <param name="Cache">True if all data should be cached, false otherwise.
        /// If false is specified, only the class without inherited/base class information is returned.</param>
        /// <returns>List of all properties.</returns>
        public static List<PropertyData> GetPropertyData(Type type, bool Cache = true) {
            ObjectClassData objClassData = GetObjectClassData(type, Cache: Cache);
            return objClassData.PropertyData.Values.ToList();
        }
        /// <summary>
        /// Retrieve enumeration information for a given Type.
        /// </summary>
        /// <param name="type">The Type.</param>
        /// <param name="Cache">True if all data should be cached, false otherwise.
        /// If false is specified, only the class without inherited/base class information is returned.</param>
        /// <returns>Enumeration information.</returns>
        public static EnumData GetEnumData(Type type, bool Cache = true) {

            ObjectEnumData objEnumData = null;

            StringLocks.DoAction(type.FullName, () => {
                string lang = MultiString.ActiveLanguage;
                LanguageObjectData langObjData = GetLanguageObjectData(lang);

                // Get enum data from language info & localization resource files
                if (!langObjData.ObjectEnumDatas.TryGetValue(type, out objEnumData)) {
                    objEnumData = new ObjectEnumData() {
                        ClassType = type,
                    };
                    if (Cache) // only cache if we include inherited properties
                        langObjData.ObjectEnumDatas.Add(type, objEnumData);

                    // check if we have this in resource files
                    LocalizationData locData = null;
                    if (Cache && !type.IsGenericType) {
                        Package package = Package.TryGetPackageFromType(type);
                        if (package != null && (package.IsCorePackage || package.IsModulePackage || package.IsSkinPackage))
                            locData = LocalizationSupport.Load(package, type.FullName, LocalizationSupport.Location.Merge);
                    }
                    LocalizationData.EnumData locEnumData = null;
                    if (locData != null)
                        locEnumData = locData.FindEnum(type.FullName);
                    // get information through reflection
                    List<FieldInfo> fis = type.GetFields().ToList();
                    objEnumData.EnumData = new EnumData(type.Name);
                    object enumObj = Activator.CreateInstance(type);
                    foreach (FieldInfo fi in fis) {
                        if (fi.IsSpecialName) continue;
                        if (locEnumData != null) {
                            LocalizationData.EnumDataEntry entry = locEnumData.FindEntry(fi.Name);
                            if (entry != null)
                                objEnumData.EnumData.Entries.Add(new EnumDataEntry(fi.Name, fi.GetValue(enumObj), fi, entry.Caption, entry.Description));
                            else
                                if (LocalizationSupport.AbortOnFailure)
                                throw new InternalError("Enumerated type {0} is missing an entry for {1} in the resource file", type.FullName, fi.Name);
                        } else
                            objEnumData.EnumData.Entries.Add(new EnumDataEntry(fi.Name, fi.GetValue(enumObj), fi));
                    }
                }
            });
            return objEnumData.EnumData;
        }
        /// <summary>
        /// Retrieve caption/description for an enum value, derived from EnumDescription attribute.
        /// </summary>
        /// <param name="enumValue">The enum value.</param>
        /// <param name="description">Returns the description found in the EnumDescription attribute.</param>
        /// <returns>Returns the caption found in the EnumDescription attribute.</returns>
        public static string GetEnumDisplayInfo(object enumValue, out string description, bool ShowValue = false) {
            Type enumType = enumValue.GetType();
            EnumData enumData = ObjectSupport.GetEnumData(enumType);

            description = enumValue.ToString();
            string caption = "";

            // try to get enum caption/description
            foreach (EnumDataEntry entry in enumData.Entries) {
                object v = entry.Value;
                if (Equals(enumValue, v)) {
                    description = entry.Description;
                    caption = entry.Caption;
                    if (ShowValue)
                        caption = string.Format("{0} - {1}", (int)v, caption);
                    break;
                }
            }
            return caption;
        }
        /// <summary>
        /// Retrieve information for one property.
        /// </summary>
        /// <param name="type">The Type of the parent model containing the property.</param>
        /// <param name="propName">The name of the property.</param>
        /// <returns>Property information.</returns>
        public static PropertyData GetPropertyData(Type type, string propName) {
            PropertyData propData = TryGetPropertyData(type, propName);
            if (propData == null)
                throw new InternalError("No property {0} in {1}", propName, type.FullName);
            return propData;
        }
        /// <summary>
        /// Retrieve information for one property.
        /// </summary>
        /// <param name="type">The Type of the parent model containing the property.</param>
        /// <param name="propName">The name of the property.</param>
        /// <returns>Property information. null is returned if the property does not exist.</returns>
        public static PropertyData TryGetPropertyData(Type type, string propName) {
            ObjectClassData objClassData = GetObjectClassData(type);
            PropertyData propData;
            if (!objClassData.PropertyData.TryGetValue(propName, out propData))
                return null;
            return propData;
        }
        /// <summary>
        /// Retrieve PropertyInfo used for reflection for one property.
        /// </summary>
        /// <param name="type">The Type of the parent model containing the property.</param>
        /// <param name="propName">The name of the property.</param>
        /// <returns>PropertyInfo.</returns>
        public static PropertyInfo GetProperty(Type type, string name) {
            PropertyInfo prop = TryGetProperty(type, name);
            if (prop == null)
                throw new InternalError("No property named {0} in {1}", name, type.FullName);
            return prop;
        }
        /// <summary>
        /// Retrieve PropertyInfo used for reflection for one property.
        /// </summary>
        /// <param name="type">The Type of the parent model containing the property.</param>
        /// <param name="propName">The name of the property.</param>
        /// <returns>PropertyInfo. null is returned if the property does not exist.</returns>
        public static PropertyInfo TryGetProperty(Type type, string name) {
            PropertyData propData = TryGetPropertyData(type, name);
            if (propData == null) return null;
            return propData.PropInfo;
        }
        /// <summary>
        /// Retrieve PropertyInfo for all properties.
        /// </summary>
        /// <param name="type">The Type of the parent model for which all properties are retrieved.</param>
        /// <returns>PorpertyInfo for all properties.</returns>
        public static List<PropertyInfo> GetProperties(Type type) {
            List<PropertyData> propData = GetPropertyData(type);
            return (from p in propData select p.PropInfo).ToList();
        }
        /// <summary>
        /// Retrieve a property value.
        /// </summary>
        /// <typeparam name="TYPE">The Type of the return value.</typeparam>
        /// <param name="parentObject">The parent model containing the property.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="dflt">The default value returned if the property does not exist.</param>
        /// <returns>The property value.</returns>
        public static TYPE GetPropertyValue<TYPE>(object parentObject, string name) {
            TYPE val;
            if (!TryGetPropertyValue<TYPE>(parentObject, name, out val))
                throw new InternalError("No such property - {0}", name);
            return (TYPE) (object) val;
        }
        /// <summary>
        /// Retrieve a property value.
        /// </summary>
        /// <typeparam name="TYPE">The Type of the return value.</typeparam>
        /// <param name="parentObject">The parent model containing the property.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="dflt">The default value returned if the property does not exist.</param>
        /// <returns>The property value. The default value is returned if the property does not exist.</returns>
        public static bool TryGetPropertyValue<TYPE>(object parentObject, string name, out TYPE val, TYPE dflt = default(TYPE)) {
            val = dflt;
            PropertyInfo prop = TryGetProperty(parentObject.GetType(), name);
            if (prop == null) return false;
            val = (TYPE) prop.GetValue(parentObject, null);
            return true;
        }

        private static object _lockObject = new object();

        private static LanguageObjectData GetLanguageObjectData(string lang) {

            lock (_lockObject) {
                // get language dictionary
                Dictionary<string, LanguageObjectData> langObjDatas;
                if (!PermanentManager.TryGetObject<Dictionary<string, LanguageObjectData>>(out langObjDatas)) {
                    langObjDatas = new Dictionary<string, LanguageObjectData>();
                    PermanentManager.AddObject<Dictionary<string, LanguageObjectData>>(langObjDatas);
                }
                // get language info from language dictionary
                LanguageObjectData langObjData = null;
                if (!langObjDatas.TryGetValue(lang, out langObjData)) {
                    langObjData = new LanguageObjectData {
                        Language = lang,
                    };
                    langObjDatas.Add(lang, langObjData);
                }
                return langObjData;
            }
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
                    VersionManager.AddOnProduct addonVersion = VersionManager.FindPackageVersion(package.Domain, package.Product);
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
    }
}
