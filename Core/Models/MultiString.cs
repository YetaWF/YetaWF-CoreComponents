/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Extensions;
using YetaWF.Core.Language;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models {

    [DynamicLinqType]
    [TypeConverter(typeof(MultiStringConv))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "Not used for serialization")]
    public class MultiString : Dictionary<string, string>, IComparable {

        public const int MaxLanguage = 10; // longest language id

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public static string DefaultLanguage {
            get {
                if (_defaultId == null) {
                    _defaultId = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "LanguageId");
                    if (string.IsNullOrEmpty(_defaultId))
                        throw new InternalError("No LanguageId found in Appsettings.json");
                    if (_defaultId != "en-US")
                        throw new InternalError("The default language in AppSettings.json is currently restricted to en-US. The site (or users) can select a default language using Admin > Site Settings or User > Settings.");
                }
                return _defaultId;
            }
        }
        private static string? _defaultId;

        // active languages
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public static List<LanguageData> Languages {
            get {
                if (_languages == null) {
                    LanguageEntryElement? defaultLanguage = (from LanguageEntryElement l in LanguageSection.Languages where l.Id == MultiString.DefaultLanguage select l).FirstOrDefault();
                    if (defaultLanguage == null)
                        throw new InternalError("The defined default language doesn't exist");
                    List<LanguageData> languages = (from LanguageEntryElement l in LanguageSection.Languages
                                  where l.Id != MultiString.DefaultLanguage
                                  select new LanguageData {
                                      Id = l.Id,
                                      ShortName = l.ShortName,
                                      Description = l.Description
                                  }).ToList();
                    languages.Insert(0, new LanguageData {
                        Id = defaultLanguage.Id,
                        ShortName = defaultLanguage.ShortName,
                        Description = defaultLanguage.Description
                    });// default at the top
                    _languages = languages;
                }
                return _languages;
            }
        }
        private static List<LanguageData>? _languages;

        /// <summary>
        /// Given a language id (which may be invalid or deleted), return a valid language id.
        /// </summary>
        public static string NormalizeLanguageId(string? id) {
            if (id != null && LanguageIdList.Contains(id)) return id;
            return MultiString.DefaultLanguage;
        }

        // returns the language Ids, used by javascript to set text box values in a multistring
        public static List<string> LanguageIdList {
            get {
                if (_languageIdList == null)
                    _languageIdList = (from l in Languages select l.Id).ToList();
                return _languageIdList;
            }
        }
        private static List<string>? _languageIdList;

        public MultiString(MultiString ms) : base(ms) { }
        public MultiString() { this[MultiString.DefaultLanguage] = string.Empty; }
        public MultiString(string? s) { this[MultiString.DefaultLanguage] = s ?? string.Empty; }

        //public MultiString(SerializationInfo info, StreamingContext context)
        //{
        //    if (info == null) throw new ArgumentNullException("info");
        //    try {
        //        foreach (var v in info)
        //            Add(v.Name, (string)v.Value);
        //    } catch (Exception) { }
        //}
        //public new void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    if (info == null) throw new ArgumentNullException("info");
        //    foreach (var d in this)
        //        info.AddValue(d.Key, d.Value);
        //}

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods",
            Justification = "The deserialization (e.g., TextFormatter, SimpleFormatter) uses generic Add() instead of typed as it simplifies deserialization")]
        public void Add(object key, object value) // for TextFormatter
        {
            Remove((string)key);
            base.Add((string)key, (string)value);
        }

        public new void Add(string key, string value) {
            if (key != MultiString.DefaultLanguage) {
                if (value == DefaultText) {
                    Remove(key);
                    return;
                }
            }
            base.Add(key, value);
        }

        // CONVERSION OPERATORS
        // CONVERSION OPERATORS
        // CONVERSION OPERATORS

        static public implicit operator MultiString(string? value) {
            return new MultiString(value);
        }
        static public implicit operator string?(MultiString? value) {
            return value?.ToString();
        }
        public new string ToString() {
            return this[MultiString.ActiveLanguage];
        }

        // COMPARISON
        // COMPARISON
        // COMPARISON

        public int CompareTo(object? obj) {
            MultiString? o = obj as MultiString;
            if (o == null) throw new ArgumentException();
            return string.Compare(this.ToString(), o.ToString());
        }
        public static bool operator ==(MultiString? value1, MultiString? value2) {
            if (value1 == null && value2 == null) return true;
            if (value1 == null || value2 == null) return false;
            return value1.CompareTo(value2) == 0;
        }
        public static bool operator !=(MultiString? value1, MultiString? value2) {
            if (value1 == null && value2 == null) return false;
            if (value1 == null || value2 == null) return true;
            return !(value1 == value2);
        }
        public static bool operator <(MultiString? value1, MultiString? value2) {
            if (value1 == null && value2 == null) return false;
            if (value1 == null) return true;
            if (value2 == null) return false;
            return value1.CompareTo(value2) < 0;
        }
        public static bool operator >(MultiString? value1, MultiString? value2) {
            if (value1 == null && value2 == null) return false;
            if (value1 == null) return false;
            if (value2 == null) return true;
            return value1.CompareTo(value2) > 0;
        }
        public static bool operator <=(MultiString? value1, MultiString? value2) {
            if (value1 == null && value2 == null) return true;
            if (value1 == null) return true;
            if (value2 == null) return false;
            return value1.CompareTo(value2) <= 0;
        }
        public static bool operator >=(MultiString? value1, MultiString? value2) {
            if (value1 == null && value2 == null) return true;
            if (value2 == null) return true;
            if (value1 == null) return false;
            return value1.CompareTo(value2) >= 0;
        }
        public override bool Equals(object? o) {
            if (!(o is MultiString)) return false;
            return (((MultiString)o).ToString() != this.ToString());
        }

        // PROPERTIES
        // PROPERTIES
        // PROPERTIES

        public string DefaultText { get { return this[MultiString.DefaultLanguage]; } }
        public bool HasLanguageText(string id) {
            return TryGetValue(id, out string? val);
        }
        public new string this[string id] {
            get {
                if (TryGetValue(id, out string? val))
                    return val;
                if (id != DefaultLanguage)
                    return DefaultText;
                return string.Empty;
            }
            set {
                Remove(id);
                Add(id, value);
            }
        }
        public static string ActiveLanguage {
            get {
                if (YetaWFManager.HaveManager && !string.IsNullOrWhiteSpace(YetaWFManager.Manager.UserLanguage))
                    return YetaWFManager.Manager.UserLanguage;
                return MultiString.DefaultLanguage;
            }
        }
        public override int GetHashCode() {
            return base.GetHashCode();
        }
        /// <summary>
        /// Returns the primary language given a language id.
        /// </summary>
        /// <param name="language">The language id.</param>
        /// <returns>The primary language.</returns>
        /// <remarks>Language ids can consist of a major and minor portion (for example, "en-US", "en-GB").
        /// Use GetPrimaryLanguage to retrieve the just major portion, i.e., "en".</remarks>
        public static string GetPrimaryLanguage(string language) {
            int i = language.IndexOf("-");
            if (i < 0) return language;
            return language.Truncate(i);
        }

        // TRIM
        // TRIM
        // TRIM

        public void Trim(char c = ' ') {
            List<string> ids = (from l in this select l.Key).ToList();
            foreach (var id in ids) {
                string s = this[id];
                if (s != null)
                    this[id] = s.Trim(new char[] { c });
            }
        }
        public void Case(CaseAttribute.EnumStyle style) {
            List<string> ids = (from l in this select l.Key).ToList();
            foreach (var id in ids) {
                string s = this[id];
                if (s != null) {
                    if (style == CaseAttribute.EnumStyle.Lower)
                        this[id] = s.ToLower();
                    else if (style == CaseAttribute.EnumStyle.Upper)
                        this[id] = s.ToUpper();
                }
            }
        }

        // DYNAMICSQL SUPPORT
        // DYNAMICSQL SUPPORT
        // DYNAMICSQL SUPPORT

        public static string DynToLower(MultiString m1) {
            string s = m1.ToString() ?? "";
            return s.ToLower();
        }
        public static bool DynContains(MultiString m1, MultiString m2) {
            string s1 = m1[MultiString.ActiveLanguage] ?? "";
            string s2 = m2[MultiString.ActiveLanguage] ?? "";
            s1 = s1.ToLower();
            s2 = s2.ToLower();
            return s1.Contains(s2);
        }
        public static bool DynStartsWith(MultiString m1, MultiString m2) {
            string s1 = m1[MultiString.ActiveLanguage] ?? "";
            string s2 = m2[MultiString.ActiveLanguage] ?? "";
            s1 = s1.ToLower();
            s2 = s2.ToLower();
            return s1.StartsWith(s2);
        }
        public static bool DynEndsWith(MultiString m1, MultiString m2) {
            string s1 = m1[MultiString.ActiveLanguage] ?? "";
            string s2 = m2[MultiString.ActiveLanguage] ?? "";
            s1 = s1.ToLower();
            s2 = s2.ToLower();
            return s1.EndsWith(s2);
        }
        public static bool DynCompare(MultiString m1, string op, MultiString m2) {
            string s1 = m1[MultiString.ActiveLanguage] ?? "";
            string s2 = m2[MultiString.ActiveLanguage] ?? "";
            s1 = s1.ToLower();
            s2 = s2.ToLower();
            switch (op) {
                case "==":
                    return string.Compare(s1, s2) == 0;
                case "!=":
                    return string.Compare(s1, s2) != 0;
                case "<":
                    return string.Compare(s1, s2) < 0;
                case "<=":
                    return string.Compare(s1, s2) <= 0;
                case ">":
                    return string.Compare(s1, s2) > 0;
                case ">=":
                    return string.Compare(s1, s2) >= 0;
            }
            throw new InternalError($"Unexpected operator {op}");
        }
    }

    public class MultiStringConv : TypeConverter {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            if (destinationType == typeof(string))
                return true;
            //else if (destinationType == typeof(EditorDefinition))
            //    return true;
            return base.CanConvertTo(context, destinationType);
        }
        public override Object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, Object value, Type destinationType) {
            if (destinationType == typeof(string))
                return ((MultiString)value).ToString();
            //else if (destinationType == typeof(EditorDefinition))
            //    return new EditorDefinition((MultiString)value);
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}


