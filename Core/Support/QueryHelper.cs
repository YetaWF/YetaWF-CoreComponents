/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace YetaWF.Core.Support {

    /// <summary>
    /// An instance of this class represents query string arguments as a dictionary.
    /// </summary>
    public class QueryDictionary : Dictionary<string, object?> { }

    /// <summary>
    /// An instance of this class is used to parse and build a URL query string.
    /// </summary>
    /// <remarks>There is no support for duplicate keys in a query string. Keep it simple...</remarks>
    public class QueryHelper {

        private class Entry {
            public string Key { get; set; } = null!;
            public string? Value { get; set; }
        }
        private List<Entry> Entries { get; set; }
        private string? Anchor { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public QueryHelper() {
            Entries = new List<Entry>();
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">An anonymous object containing query string parameters.</param>
        public QueryHelper(object? args) {
            Entries = new List<Entry>();
            if (args != null) {
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(args)) {
                    object? val = property.GetValue(args);
                    Add(property.Name, val?.ToString());
                }
            }
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="query">A dictionary with query string parameters.</param>
        public QueryHelper(QueryDictionary query) {
            Entries = new List<Entry>();
            foreach (string k in query.Keys) {
                object? o = query[k];
                if (o == null)
                    Entries.Add(new Entry { Key = k, Value = null, });
                else
                    Entries.Add(new Entry { Key = k, Value = o.ToString(), });
            }
        }
        /// <summary>
        /// Parses a URL and returns a QueryHelper object.
        /// </summary>
        /// <param name="url">The URL to parse.</param>
        /// <param name="urlOnly">Returns the URL without query string.</param>
        /// <returns>Returns a QueryHelper object.</returns>
        public static QueryHelper FromUrl(string url, out string urlOnly) {
            QueryHelper query;
            string? anchor = null;
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
        /// <summary>
        /// Parses a URL and returns a QueryHelper object.
        /// </summary>
        /// <param name="url">The URL to parse.</param>
        /// <returns>Returns a QueryHelper object.</returns>
        public static QueryHelper FromUrl(string url) {
            return FromUrl(url, out _);
        }
        /// <summary>
        /// Returns a QueryHelper object with the query string parameters populated by the NameValueCollection.
        /// </summary>
        /// <param name="query">The NameValueCollection containing the query string parameters.</param>
        /// <returns>Returns a QueryHelper object.</returns>
        public static QueryHelper FromNameValueCollection(NameValueCollection query) {
            QueryHelper qh = new QueryHelper();
            foreach (string? k in query.AllKeys) {
                qh.Entries.Add(new Entry { Key = k!, Value = query[k], });
            }
            return qh;
        }
        /// <summary>
        /// Returns a QueryHelper object with the query string parameters populated by the dictionary.
        /// </summary>
        /// <param name="query">The dictionary containing the query string parameters.</param>
        /// <returns>Returns a QueryHelper object.</returns>
        public static QueryHelper FromDictionary(IDictionary<string,string> query) {
            return new QueryHelper(query);
        }
        /// <summary>
        /// Returns a QueryHelper object with the query string parameters populated by the HttpRequest query string collection.
        /// </summary>
        /// <param name="query">The HttpRequest query string collection.</param>
        /// <returns>Returns a QueryHelper object.</returns>
        public static QueryHelper FromQueryCollection(IQueryCollection query) {
            QueryHelper qh = new QueryHelper();
            foreach (string k in query.Keys) {
                qh.Entries.Add(new Entry { Key = k, Value = query[k], });
            }
            return qh;
        }
        /// <summary>
        /// Returns a QueryHelper object with the query string parameters populated by the query string.
        /// </summary>
        /// <param name="queryString">The query string. The query string may or may not include a leading '?'.</param>
        /// <returns>Returns a QueryHelper object.</returns>
        public static QueryHelper FromQueryString(string queryString) {
            QueryHelper qh = new QueryHelper();
            Dictionary<string,StringValues> queryDictionary = QueryHelpers.ParseQuery(queryString);
            foreach (KeyValuePair<string, StringValues> e in queryDictionary) {
                foreach (string sv in e.Value) {
                    qh.Entries.Add(new Entry { Key = e.Key, Value = sv, });
                }
            }
            return qh;
        }
        /// <summary>
        /// Returns a QueryHelper object with the query string parameters populated by the anonymous object.
        /// </summary>
        /// <param name="args">The anonymous object containing the query string.</param>
        /// <returns>Returns a QueryHelper object.</returns>
        public static QueryHelper FromAnonymousObject(object? args) {
            return new QueryHelper(args);
        }

        public string? this[string key] {
            get {
                Entry? entry = (from e in Entries where string.Compare(e.Key, key, true) == 0 select e).FirstOrDefault();
                if (entry == null) return null;
                return entry.Value;
            }
            set {
                Add(key, value, Replace: true);
            }
        }
        public bool HasEntry(string key) {
            Entry? entry = (from e in Entries where string.Compare(e.Key, key, true) == 0 select e).FirstOrDefault();
            return entry != null;
        }
        public void Remove(string key) {
            Entries = (from e in Entries where string.Compare(e.Key, key, true) != 0 select e).ToList();
        }
        public void Add(string key, string? value, bool Replace = false) {
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
                completeUrl += $"#{Anchor}";
            return completeUrl;
        }
        public static string ToUrl(string url, string? queryString) {
            if (string.IsNullOrWhiteSpace(queryString)) return url;
            if (url.Contains('?'))
                return string.Format($"{url}&{queryString}");
            else if (!queryString.StartsWith("?"))
                return string.Format($"{url}?{queryString}");
            else
                return string.Format($"{url}{queryString}");
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
                url = $"#{Anchor}";
            return url;
        }
        internal static QueryString MakeQueryString(string? newQS) {
            if (string.IsNullOrWhiteSpace(newQS)) return new QueryString();
            if (newQS.StartsWith("?")) return new QueryString(newQS);
            return new QueryString("?" + newQS);
        }
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

        public string? this[string key] {
            get {
                if (Collection == null) return null;
                return Collection[key];
            }
        }
        public Dictionary<string, string> GetCollection() {
            Dictionary<string, string> d = new Dictionary<string, string>();
            if (Collection == null) return d;
            foreach (string key in Collection.Keys)
                d.Add(key, Collection[key]);
            return d;
        }
        public FormHelper() {
            Collection = null;
        }
        public FormHelper(IFormCollection collection) {
            Collection = collection;
        }

        private IFormCollection? Collection { get; set; }

        public static FormHelper FromFormCollection(IFormCollection collection) {
            return new FormHelper(collection);
        }
    }
}
