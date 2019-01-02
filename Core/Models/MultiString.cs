/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Extensions;
using YetaWF.Core.Language;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models {

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
                        throw new InternalError("The default language in Appsettings.json is currently restricted to en-US. The site (or users) can select a default language using Admin > Site Settings or User > Settings.");
                }
                return _defaultId;
            }
        }
        private static string _defaultId;

        // active languages
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public static List<LanguageData> Languages {
            get {
                if (_languages == null) {
                    LanguageEntryElement defaultLanguage = (from LanguageEntryElement l in LanguageSection.Languages where l.Id == MultiString.DefaultLanguage select l).FirstOrDefault();
                    if (defaultLanguage == null)
                        throw new InternalError("The defined default language doesn't exist");
                    _languages = (from LanguageEntryElement l in LanguageSection.Languages
                                  where l.Id != MultiString.DefaultLanguage
                                  select new LanguageData {
                                      Id = l.Id,
                                      ShortName = l.ShortName,
                                      Description = l.Description
                                  }).ToList();
                    _languages.Insert(0, new LanguageData {
                        Id = defaultLanguage.Id,
                        ShortName = defaultLanguage.ShortName,
                        Description = defaultLanguage.Description
                    });// default at the top
                }
                return _languages;
            }
        }
        private static List<LanguageData> _languages;

        /// <summary>
        /// Given a language id (which may be invalid or deleted), return a valid language id.
        /// </summary>
        public static string NormalizeLanguageId(string id) {
            if (LanguageIdList.Contains(id)) return id;
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
        private static List<string> _languageIdList;

        public MultiString(MultiString ms) : base(ms) { }
        public MultiString() { this[MultiString.DefaultLanguage] = ""; }
        public MultiString(string s) { this[MultiString.DefaultLanguage] = s; }

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

        static public implicit operator MultiString(string value) {
            return new MultiString(value);
        }
        static public implicit operator string(MultiString value) {
            return value != null ? value.ToString() : null;
        }
        public new string ToString() {
            return this[MultiString.ActiveLanguage];
        }

        // COMPARISON
        // COMPARISON
        // COMPARISON

        public int CompareTo(object obj) {
            MultiString o = obj as MultiString;
            if (o == null) throw new ArgumentException();
            return string.Compare(this.ToString(), o.ToString());
        }
        public static bool operator ==(MultiString value1, MultiString value2) {
            if (((object)value1) == null && ((object)value2) == null) return true;
            if (((object)value1) == null || ((object)value2) == null) return false;
            return value1.CompareTo(value2) == 0;
        }
        public static bool operator !=(MultiString value1, MultiString value2) {
            return !(value1 == value2);
        }
        public static bool operator <(MultiString value1, MultiString value2) {
            if (((object)value1) == null && ((object)value2) == null) return false;
            if (((object)value1) == null) return true;
            if (((object)value2) == null) return false;
            return value1.CompareTo(value2) < 0;
        }
        public static bool operator >(MultiString value1, MultiString value2) {
            if (((object)value1) == null && ((object)value2) == null) return false;
            if (((object)value1) == null) return false;
            if (((object)value2) == null) return true;
            return value1.CompareTo(value2) > 0;
        }
        public static bool operator <=(MultiString value1, MultiString value2) {
            if (((object)value1) == null && ((object)value2) == null) return true;
            if (((object)value1) == null) return true;
            if (((object)value2) == null) return false;
            return value1.CompareTo(value2) <= 0;
        }
        public static bool operator >=(MultiString value1, MultiString value2) {
            if (((object)value1) == null && ((object)value2) == null) return true;
            if (((object)value2) == null) return true;
            if (((object)value1) == null) return false;
            return value1.CompareTo(value2) >= 0;
        }
        public override bool Equals(object o) {
            if (!(o is MultiString)) return false;
            return (((MultiString)o).ToString() != this.ToString());
        }
        // PROPERTIES
        // PROPERTIES
        // PROPERTIES

        public string DefaultText { get { return this[MultiString.DefaultLanguage]; } }
        public bool HasLanguageText(string id) {
            string val;
            return TryGetValue(id, out val);
        }
        public new string this[string id] {
            get {
                string val;
                if (TryGetValue(id, out val))
                    return val;
                if (id != DefaultLanguage)
                    return DefaultText;
                return "";
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

        public string ToLower() {
            string s = ToString();
            if (s == null) return "";
            return s.ToLower();
        }
        public bool Contains(MultiString ms) {
            string s = ToString();
            if (s == null) return false;
            return s.Contains(ms.ToString());
        }
        public bool StartsWith(MultiString ms) {
            string s = ToString();
            if (s == null) return false;
            return s.StartsWith(ms.ToString());
        }
        public bool EndsWith(MultiString ms) {
            string s = ToString();
            if (s == null) return false;
            return s.EndsWith(ms.ToString());
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


