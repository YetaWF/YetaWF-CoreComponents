/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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

    /// <summary>
    /// An instance of the MultiString class defines a string with individual strings for each language supported by the site (defined in LanguageSettings.json).
    /// </summary>
    /// <remarks>Anywhere a language-specific string could be used, a MultiString object can be used, as it defines strings for all site-supported languages.
    /// The MultiString class is also supported by all data providers.
    ///
    /// Forms that require input of all language-specific string can use the MultiString component, see UIHint("MultiString").
    /// Otherwise forms can use regular strings, but store data using the MultiString class. Conversion between string and MultiString objects is usually automatic.</remarks>
    [DynamicLinqType]
    [TypeConverter(typeof(MultiStringConv))]
    public class MultiString : Dictionary<string, string>, IComparable {

        /// <summary>
        /// Defines the maximum length of a language id.
        /// </summary>
        public const int MaxLanguage = 10;

        /// <summary>
        /// Returns the site-defined default language id.
        /// </summary>
        public static string DefaultLanguage {
            get {
                if (_defaultId == null) {
                    _defaultId = WebConfigHelper.GetValue<string>(DataProviderImpl.DefaultString, "LanguageId");
                    if (string.IsNullOrEmpty(_defaultId))
                        throw new InternalError("No LanguageId found in AppSettings.json");
                    if (_defaultId != "en-US")
                        throw new InternalError("The default language in AppSettings.json is currently restricted to en-US. The site (or users) can select a default language using Admin > Site Settings or User > Settings.");
                }
                return _defaultId;
            }
        }
        private static string? _defaultId;

        /// <summary>
        /// Returns a list of languages that are defined in LanguageSettings.json.
        /// </summary>
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
        /// <param name="id">The language id.</param>
        /// <returns>Returns a valid language id.</returns>
        public static string NormalizeLanguageId(string? id) {
            if (id != null && LanguageIdList.Contains(id)) return id;
            return MultiString.DefaultLanguage;
        }

        /// <summary>
        /// Returns a list of language ids that are defined in LanguageSettings.json.
        /// </summary>
        public static List<string> LanguageIdList {
            get {
                if (_languageIdList == null)
                    _languageIdList = (from l in Languages select l.Id).ToList();
                return _languageIdList;
            }
        }
        private static List<string>? _languageIdList;

        /// <summary>
        /// Constructor. Makes a copy of an existing MultiString object.
        /// </summary>
        /// <param name="ms">The MultiString object to copy.</param>
        public MultiString(MultiString ms) : base(ms) { }
        /// <summary>
        /// Constructor.
        /// </summary>
        public MultiString() { this[MultiString.DefaultLanguage] = string.Empty; }
        /// <summary>
        /// Constructor. Makes a copy of a string.
        /// </summary>
        /// <param name="s">The string to copy.</param>
        public MultiString(string? s) { this[MultiString.DefaultLanguage] = s ?? string.Empty; }

        /// <summary>
        /// Adds a language-specific value to the MultiString instance. This is used internally only.
        /// </summary>
        /// <param name="key">Defines the language id.</param>
        /// <param name="value">Defines the string to add for the specified language.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods", Justification = "The deserialization (e.g., TextFormatter, SimpleFormatter) uses generic Add() instead of typed as it simplifies deserialization")]
        public void Add(object key, object value) // for TextFormatter
        {
            Remove((string)key);
            base.Add((string)key, (string)value);
        }

        /// <summary>
        /// Adds a language-specific value to the MultiString instance. This is used internally only.
        /// </summary>
        /// <param name="key">Defines the language id.</param>
        /// <param name="value">Defines the string to add for the specified language.</param>
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

        /// <summary>
        /// Conversion operator.
        /// </summary>
        /// <param name="s">The string to convert to a MultiString instance.</param>
        /// <remarks>The specified string is assigned as default text for all language-specific strings in the MultiString instance.</remarks>
        static public implicit operator MultiString(string? s) {
            return new MultiString(s);
        }
        /// <summary>
        /// Conversion operator.
        /// </summary>
        /// <param name="ms">The MultiString instance to convert to a string.</param>
        /// <returns>Returns a string.</returns>
        /// <remarks>The active language is used to determine the string returned.</remarks>
        static public implicit operator string?(MultiString? ms) {
            return ms?.ToString();
        }
        /// <summary>
        /// Returns a string for the currently active language.
        /// </summary>
        /// <returns>Returns the string for the currently active language.</returns>
        public new string ToString() {
            return this[MultiString.ActiveLanguage];
        }

        // COMPARISON
        // COMPARISON
        // COMPARISON

        /// <inheritdoc/>
        public int CompareTo(object? obj) {
            if (obj is not MultiString o) throw new ArgumentException();
            return string.Compare(this.ToString(), o.ToString());
        }

        // PROPERTIES
        // PROPERTIES
        // PROPERTIES

        /// <summary>
        /// Returns the default text for all language-specific string. The default text is used for languages for which no string has been defined.
        /// </summary>
        public string DefaultText { get { return this[MultiString.DefaultLanguage]; } }

        /// <summary>
        /// Returns whether a language-specific string has been defined.
        /// </summary>
        /// <param name="id">The language id.</param>
        /// <returns>Returns true if a language-specific string has been defined, false otherwise.</returns>
        public bool HasLanguageText(string id) {
            return TryGetValue(id, out _);
        }
        /// <summary>
        /// Defines the language-specific string for a language id.
        /// </summary>
        /// <param name="id">The language id.</param>
        /// <returns>Returns the language-specific string for the specified language, or the defined default text DefaultText if no language-specific string has been defined.</returns>
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
        /// <summary>
        /// Returns the currently active language id.
        /// Users can change the currently active language using User > Settings (standard YetaWF site).
        /// </summary>
        public static string ActiveLanguage {
            get {
                if (YetaWFManager.HaveManager && !string.IsNullOrWhiteSpace(YetaWFManager.Manager.UserLanguage))
                    return YetaWFManager.Manager.UserLanguage;
                return MultiString.DefaultLanguage;
            }
        }
        /// <inheritdoc/>
        public override int GetHashCode() {
            return base.GetHashCode();
        }
        /// <summary>
        /// Returns the primary language given a language id.
        /// </summary>
        /// <param name="language">The language id.</param>
        /// <returns>The primary language.</returns>
        /// <remarks>Language ids can consist of a major and minor portion (for example, "en-US", "en-GB").
        /// Use GetPrimaryLanguage to retrieve just the major portion, i.e., "en".</remarks>
        public static string GetPrimaryLanguage(string language) {
            int i = language.IndexOf("-", StringComparison.Ordinal);
            if (i < 0) return language;
            return language.Truncate(i);
        }

        // TRIM
        // TRIM
        // TRIM

        /// <summary>
        /// Trims the specified character from all language-specific strings and the default text.
        /// </summary>
        /// <param name="c">The character trimmed from all strings.</param>
        public void Trim(char c = ' ') {
            List<string> ids = (from l in this select l.Key).ToList();
            foreach (var id in ids) {
                string s = this[id];
                if (s != null)
                    this[id] = s.Trim(new char[] { c });
            }
        }
        /// <summary>
        /// Changes all language-specific strings and the default text to the specified casing.
        /// </summary>
        /// <param name="style">Defines whether to change the strings to all uppercase or lowercase.</param>
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

        // DYNAMICSQL SUPPORT  (used explicitly by sorts/filters)
        // DYNAMICSQL SUPPORT
        // DYNAMICSQL SUPPORT

        internal static string DynToLower(MultiString m1) {
            string s = m1.ToString() ?? string.Empty;
            return s.ToLower();
        }
        internal static bool DynContains(MultiString m1, MultiString m2) {
            string s1 = m1[MultiString.ActiveLanguage] ?? string.Empty;
            string s2 = m2[MultiString.ActiveLanguage] ?? string.Empty;
            s1 = s1.ToLower();
            s2 = s2.ToLower();
            return s1.Contains(s2);
        }
        internal static bool DynStartsWith(MultiString m1, MultiString m2) {
            string s1 = m1[MultiString.ActiveLanguage] ?? string.Empty;
            string s2 = m2[MultiString.ActiveLanguage] ?? string.Empty;
            s1 = s1.ToLower();
            s2 = s2.ToLower();
            return s1.StartsWith(s2);
        }
        internal static bool DynEndsWith(MultiString m1, MultiString m2) {
            string s1 = m1[MultiString.ActiveLanguage] ?? string.Empty;
            string s2 = m2[MultiString.ActiveLanguage] ?? string.Empty;
            s1 = s1.ToLower();
            s2 = s2.ToLower();
            return s1.EndsWith(s2);
        }
        internal static bool DynCompare(MultiString m1, string op, MultiString m2) {
            string s1 = m1[MultiString.ActiveLanguage] ?? string.Empty;
            string s2 = m2[MultiString.ActiveLanguage] ?? string.Empty;
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

    /// <summary>
    /// The TimeOfDayConv class is used to convert YetaWF.Core.Models.MultiString instances to/from other types.
    /// Intended for internal use only.
    /// </summary>
    public class MultiStringConv : TypeConverter {
        /// <inheritdoc/>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            if (destinationType == typeof(string))
                return true;
            //else if (destinationType == typeof(EditorDefinition))
            //    return true;
            return base.CanConvertTo(context, destinationType);
        }
        /// <inheritdoc/>
        public override Object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, Object value, Type destinationType) {
            if (destinationType == typeof(string))
                return ((MultiString)value).ToString();
            //else if (destinationType == typeof(EditorDefinition))
            //    return new EditorDefinition((MultiString)value);
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}


