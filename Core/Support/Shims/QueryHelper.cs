/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
#else
#endif

namespace YetaWF.Core.Support {

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "Not intended for serialization")]
    public class QueryDictionary : Dictionary<string, object> { }

    /// <summary>
    /// Query string manipulation.
    /// </summary>
    /// <remarks>There is no support for duplicate keys in a querystring. Keep it simple...</remarks>
    public class QueryHelper {

        public class Entry {
            public string Key { get; set; }
            public string Value { get; set; }
        }
        public List<Entry> Entries { get; private set; }
        public string Anchor { get; set; }

        public QueryHelper() {
            Entries = new List<Entry>();
        }
        public QueryHelper(object args) {
            Entries = new List<Entry>();
            if (args != null) {
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(args)) {
                    object val = property.GetValue(args);
                    Add(property.Name, val != null ? val.ToString(): null);
                }
            }
        }
        public QueryHelper(QueryDictionary query) {
            Entries = new List<Entry>();
            foreach (string k in query.Keys) {
                object o = query[k];
                if (o == null)
                    Entries.Add(new Entry { Key = k, Value = null, });
                else
                    Entries.Add(new Entry { Key = k, Value = o.ToString(), });
            }
        }
        public static QueryHelper FromUrl(string url, out string urlOnly) {
            QueryHelper query;
            string anchor = null;
            int index = url.IndexOf('#'); // strip off anchor
            if (index >= 0 && index <= url.Length - 1) {
                anchor = url.Substring(index + 1);
                url = url.Substring(0, index);
            }
            index = url.IndexOf('?');
            if (index >= 0 && index <= url.Length - 1) {
                query = QueryHelper.FromQueryString(url.Substring(index + 1));
                urlOnly = url.Substring(0, index);
            } else {
                query = new QueryHelper();
                urlOnly = url;
            }
            query.Anchor = anchor;
            return query;
        }
        public static QueryHelper FromUrl(string url) {
            string urlOnly;
            return FromUrl(url, out urlOnly);
        }
        public static QueryHelper FromNameValueCollection(NameValueCollection query) {
            QueryHelper qh = new QueryHelper();
            foreach (string k in query.AllKeys) {
                qh.Entries.Add(new Entry { Key = k, Value = query[k], });
            }
            return qh;
        }
        public static QueryHelper FromDictionary(IDictionary<string,string> query) {
            return new QueryHelper(query);
        }
#if MVC6
        public static QueryHelper FromQueryCollection(IQueryCollection query) {
            QueryHelper qh = new QueryHelper();
            foreach (string k in query.Keys) {
                qh.Entries.Add(new Entry { Key = k, Value = query[k], });
            }
            return qh;
        }
#else
#endif
        public static QueryHelper FromQueryString(string queryString) {
            QueryHelper qh = new QueryHelper();
#if MVC6
            Dictionary<string,StringValues> queryDictionary = QueryHelpers.ParseQuery(queryString);
            foreach (KeyValuePair<string, StringValues> e in queryDictionary) {
                foreach (string sv in e.Value) {
                    qh.Entries.Add(new Entry { Key = e.Key, Value = sv, });
                }
            }
#else
            NameValueCollection coll = System.Web.HttpUtility.ParseQueryString(queryString);
            foreach (string k in coll.AllKeys) {
                qh.Entries.Add(new Entry { Key = k, Value = coll[k], });
            }
#endif
            return qh;
        }
        public static QueryHelper FromAnonymousObject(object args) {
            return new QueryHelper(args);
        }

        public string this[string key] {
            get {
                Entry entry = (from e in Entries where string.Compare(e.Key, key, true) == 0 select e).FirstOrDefault();
                if (entry == null) return null;
                return entry.Value;
            }
            set {
                Add(key, value, Replace: true);
            }
        }
        public bool HasEntry(string key) {
            Entry entry = (from e in Entries where string.Compare(e.Key, key, true) == 0 select e).FirstOrDefault();
            return entry != null;
        }
        public void Remove(string key) {
            Entries = (from e in Entries where string.Compare(e.Key, key, true) != 0 select e).ToList();
        }
        public void Add(string key, string value, bool Replace = false) {
            if (Replace)
                Remove(key);
            Entries.Add(new Entry { Key = key, Value = value, });
        }
        public string ToQueryString() {
            StringBuilder sb = new StringBuilder();
            foreach (Entry entry in Entries) {
                if (entry.Value == null)
                    sb.AppendFormat("{0}&", entry.Key);
                else
                    sb.AppendFormat("{0}={1}&", entry.Key, Utility.UrlEncodeArgs(entry.Value));
            }
            if (sb.Length > 0) sb.Remove(sb.Length - 1, 1); // remove last &
            return sb.ToString();
        }
        public string ToUrl(string url) {
            string completeUrl = ToUrl(url, ToQueryString());
            if (Anchor != null)
                completeUrl += "#" + Anchor;
            return completeUrl;
        }
        public static string ToUrl(string url, string queryString) {
            if (string.IsNullOrWhiteSpace(queryString)) return url;
            if (url.Contains('?'))
                return string.Format("{0}&{1}", url, queryString);
            else if (!queryString.StartsWith("?"))
                return string.Format("{0}?{1}", url, queryString);
            else
                return string.Format("{0}{1}", url, queryString);
        }
        public string ToUrlHumanReadable(string url) {
            foreach (Entry entry in Entries) {
                if (entry.Value != null) {
                    if (!url.EndsWith("/"))
                        url += "/";
                    url += string.Format("{0}/{1}", Utility.UrlEncodeSegment(entry.Key), Utility.UrlEncodeSegment(entry.Value));
                }
            }
            if (Anchor != null)
                url = "#" + Anchor;
            return url;
        }
#if MVC6
        internal static QueryString MakeQueryString(string newQS)
        {
            if (string.IsNullOrWhiteSpace(newQS)) return new QueryString();
            if (newQS.StartsWith("?")) return new QueryString(newQS);
            return new QueryString("?" + newQS);
        }
#endif
        //
        /// <summary>
        /// Add some random query string to the url to defeat client-side caching for a page.
        /// </summary>
        /// <param name="url">The Url.</param>
        /// <returns>Returns the same Url with a random query string argument added to defeat client-side caching for a page.
        /// This is typically used when transitioning from anonymous to authenticated users so the page is re-rendered.
        /// Otherwise a static page will not be rendered correctly if the client has already cached the page.</returns>
        public static string AddRando(string url) {
            return ToUrl(url, "__rand=" + DateTime.UtcNow.Ticks.ToString());
        }
    }

    /// <summary>
    /// Form variables.
    /// </summary>
    public class FormHelper {

        public string this[string key] {
            get {
                if (Collection == null) return null;
                return Collection[key];
            }
        }
#if MVC6
        public FormHelper() {
            Collection = null;
        }
        public FormHelper(IFormCollection collection) {
            Collection = collection;
        }

        private IFormCollection Collection { get; set; }

        public static FormHelper FromFormCollection(IFormCollection collection) {
            return new FormHelper(collection);
        }
#else
        public FormHelper(NameValueCollection collection) {
            Collection = collection;
        }
        private NameValueCollection Collection { get; set; }

        public static FormHelper FromNameValueCollection(NameValueCollection collection) {
            return new FormHelper(collection);
        }
#endif
    }
}
