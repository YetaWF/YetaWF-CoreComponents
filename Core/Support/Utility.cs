/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json;
using System;
using System.Text;
using YetaWF.Core.Packages;
#if MVC6
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
#else
using System.Linq;
using System.Web;
using System.Web.Hosting;
using YetaWF.Core.Extensions;
#endif

namespace YetaWF.Core.Support {

    public class Utility {

        // VERSION
        // VERSION
        // VERSION

        /// <summary>
        /// Describes the MVC version.
        /// </summary>
        public enum AspNetMvcVersion {
            /// <summary>
            /// ASP.NET 4 with MVC 5
            /// </summary>
            MVC5 = 0,
            /// <summary>
            /// ASP.NET Core with MVC 6
            /// </summary>
            MVC6 = 6,
        }

        /// <summary>
        /// Returns a value indicating which MVC version is used.
        /// </summary>
        public static AspNetMvcVersion AspNetMvc {
            get {
                return AspNetMvcVersion.MVC6;
            }
        }
        /// <summary>
        /// Returns a user-displayable name for the MVC version used.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static string GetAspNetMvcName(AspNetMvcVersion version) {
            switch (version) {
                case AspNetMvcVersion.MVC5:
                    return "MVC5";
                case AspNetMvcVersion.MVC6:
                    return $".NET Core {Globals.RUNTIMEVERSION} - MVC ";
                default:
                    return "(unknown)";
            }
        }

        // PATH
        // PATH
        // PATH

        /// <summary>
        /// Translates a Url to a local file name.
        /// </summary>
        /// <remarks>
        /// Doesn't really take special characters (except spaces %20) into account.
        /// </remarks>
        public static string UrlToPhysical(string url) {
            if (!url.StartsWith("/")) throw new InternalError("Urls to translate must start with /.");
            string path;
            if (url.StartsWith(Globals.NodeModulesUrl, StringComparison.OrdinalIgnoreCase)) {
                path = YetaWFManager.RootFolderWebProject + Utility.UrlToPhysicalRaw(url);
            } else if (url.StartsWith(Globals.BowerComponentsUrl, StringComparison.OrdinalIgnoreCase)) {
                path = YetaWFManager.RootFolderWebProject + Utility.UrlToPhysicalRaw(url);
            } else if (url.StartsWith(Globals.VaultPrivateUrl, StringComparison.OrdinalIgnoreCase)) {
                path = YetaWFManager.RootFolderWebProject + Utility.UrlToPhysicalRaw(url);
            } else {
                path = $"{YetaWFManager.RootFolder}{UrlToPhysicalRaw(url)}";
            }
            return path;
        }
        private static string UrlToPhysicalRaw(string url) {
            if (!url.StartsWith("/")) throw new InternalError("Urls to translate must start with /.");
            url = FileToPhysical(url);
            url = url.Replace("%20", " ");
            return url;
        }
        public static string FileToPhysical(string file) {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                file = file.Replace('/', '\\');
            return file;
        }

        /// <summary>
        /// Translates a local file name to a Url.
        /// </summary>
        /// <remarks>
        /// Doesn't really take special characters (except spaces %20) into account.
        /// </remarks>
        public static string PhysicalToUrl(string path) {
            path = ReplaceString(path, YetaWFManager.RootFolder, String.Empty, StringComparison.OrdinalIgnoreCase);
#if MVC6
            path = ReplaceString(path, YetaWFManager.RootFolderWebProject, String.Empty, StringComparison.OrdinalIgnoreCase);
#else
#endif
            path = path.Replace(" ", "%20");
            return path.Replace('\\', '/');
        }
        private static string ReplaceString(string str, string oldValue, string newValue, StringComparison comparison) {
            StringBuilder sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, comparison);
            while (index != -1) {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));
            return sb.ToString();
        }

        // SERIALIZATON/ENCODING
        // SERIALIZATON/ENCODING
        // SERIALIZATON/ENCODING

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="Indented">Defines whether indentation is used in the generated JSON string.</param>
        /// <returns>Returns the serialized object as a JSON string.</returns>
        public static string JsonSerialize(object value, bool Indented = false) {
            return JsonConvert.SerializeObject(value, Indented ? _JsonSettingsIndented : _JsonSettings);
        }
        /// <summary>
        /// Deserializes a JSON string to an object.
        /// </summary>
        /// <param name="value">The JSON string.</param>
        /// <returns>Returns the object.</returns>
        public static object JsonDeserialize(string value) {
            return JsonConvert.DeserializeObject(value, _JsonSettings);
        }
        /// <summary>
        /// Deserializes a JSON string to an object.
        /// </summary>
        /// <param name="value">The JSON string.</param>
        /// <param name="type">The type of the deserialized object.</param>
        /// <returns>Returns the object.</returns>
        public static object JsonDeserialize(string value, Type type) {
            return JsonConvert.DeserializeObject(value, type, _JsonSettings);
        }
        /// <summary>
        /// Deserializes a JSON string to an object.
        /// </summary>
        /// <typeparam name="TYPE">The type of the deserialized object.</typeparam>
        /// <param name="value">The JSON string.</param>
        /// <returns>Returns the object.</returns>
        public static TYPE JsonDeserialize<TYPE>(string value) {
            return JsonConvert.DeserializeObject<TYPE>(value, _JsonSettings);
        }
        private static JsonSerializerSettings _JsonSettings = new JsonSerializerSettings {
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };
        private static JsonSerializerSettings _JsonSettingsIndented = new JsonSerializerSettings {
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            ObjectCreationHandling = ObjectCreationHandling.Replace,

            Formatting = Newtonsoft.Json.Formatting.Indented,
        };

        /// <summary>
        /// Encodes a string for use with JavaScript. The returned string must be surrounded by quotes.
        /// </summary>
        /// <param name="s">The string to encode.</param>
        /// <returns>Returns the encoded string contents.</returns>
        public static string JserEncode(string s) {
#if MVC6
            if (s == null) return "";
            return System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(s);
#else
            return HttpUtility.JavaScriptStringEncode(s);
#endif
        }

        /// <summary>
        /// Encodes a string for use as HTML.
        /// </summary>
        /// <param name="s">The string to encode.</param>
        /// <returns>Returns the encoded HTML.</returns>
        public static string HtmlEncode(string s) {
            if (s == null) return "";
#if MVC6
            return System.Net.WebUtility.HtmlEncode(s);
#else
            return HttpUtility.HtmlEncode(s);
#endif
        }
        /// <summary>
        /// Decodes a HTML encoded string.
        /// </summary>
        /// <param name="s">The HTML encoded string.</param>
        /// <returns>Returns the decoded string.</returns>
        public static string HtmlDecode(string s) {
            if (s == null) return "";
#if MVC6
            return System.Net.WebUtility.HtmlDecode(s);
#else
            return HttpUtility.HtmlDecode(s);
#endif
        }
        /// <summary>
        /// Encodes a string for use as an HTML attribute.
        /// </summary>
        /// <param name="s">The string to encode.</param>
        /// <returns>Returns the string encoded for use as an HTML attribute.</returns>
        public static string HtmlAttributeEncode(string s) {
            if (s == null) return "";
#if MVC6
            return System.Net.WebUtility.HtmlEncode(s);
#else
            return HttpUtility.HtmlAttributeEncode(s);
#endif
        }
        /// <summary>
        /// Encodes a string for use as a query string argument.
        /// </summary>
        /// <param name="s">The string to encode.</param>
        /// <returns>Returns the string encoded for use as a query string argument.</returns>
        public static string UrlEncodeArgs(string s) {
            if (s == null) return "";
            return Uri.EscapeDataString(s);
        }
        /// <summary>
        /// Decodes an encoded query string argument.
        /// </summary>
        /// <param name="s">The encoded query string argument.</param>
        /// <returns>Returns the decoded string.</returns>
        public static string UrlDecodeArgs(string s) {
            if (s == null) return "";
            return Uri.UnescapeDataString(s);
        }
        /// <summary>
        /// Encodes a string for use as a URL segment (parts in the URL between /xxx/).
        /// </summary>
        /// <param name="s">The URL segment.</param>
        /// <returns>Returns an encoded URL segment.</returns>
        // used to encode the page path segments
        public static string UrlEncodeSegment(string s) {
            if (s == null) return "";
            string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-[]@!$";
            int inv = 0;
            StringBuilder sb = new StringBuilder();
            foreach (char c in s) {
                if (validChars.Contains(c)) {
                    if (inv > 0) {
                        sb.Append("%20");
                        inv = 0;
                    }
                    sb.Append(c);
                } else
                    ++inv;
            }
            return sb.ToString();
        }
        /// <summary>
        /// Encodes a string for use as a URL path.
        /// </summary>
        /// <param name="s">The URL path.</param>
        /// <returns>Returns the encoded URL path.</returns>
        public static string UrlEncodePath(string s) {
            if (string.IsNullOrWhiteSpace(s)) return null;
            StringBuilder sb = new StringBuilder();
            s = SkipSchemeAndDomain(sb, s);
            string validChars = "_./ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-[]@!$";
            for (int len = s.Length, ix = 0; ix < len; ++ix) {
                char c = s[ix];
                if (c == '?' || c == '#') {
                    sb.Append(s.Substring(ix));
                    break;
                }
                if (validChars.Contains(c)) {
                    sb.Append(c);
                } else if (c == '%') {
                    if (ix + 1 < len && char.IsNumber(s[ix + 1])) {
                        if (ix + 2 < len && char.IsNumber(s[ix + 2])) {
                            sb.Append(s.Substring(ix, 3));
                            ix += 2;// all good, skip %nn
                            continue;
                        }
                    }
                    sb.Append(string.Format("%{0:X2}", (int)c));
                } else {
                    sb.Append(string.Format("%{0:X2}", (int)c));
                }
            }
            return sb.ToString();
        }
        internal static string UrlDecodePath(string s) {
            if (string.IsNullOrWhiteSpace(s)) return null;
            StringBuilder sb = new StringBuilder();
            s = SkipSchemeAndDomain(sb, s);
            sb.Append(Uri.UnescapeDataString(s));
            return sb.ToString();
        }

        private static string SkipSchemeAndDomain(StringBuilder sb, string s) {
            // handle this: (some parts optional)  see https://en.wikipedia.org/wiki/Uniform_Resource_Identifier
            //   abc://username:password@example.com:123/path/data?key=value#fragid1
            int iScheme = s.IndexOf(':');
            if (iScheme >= 0) {
                sb.Append(s.Substring(0, iScheme + 1));
                s = s.Substring(iScheme + 1);
            }
            if (s.StartsWith("//")) {
                sb.Append("//");
                s = s.Substring(2);
            }
            int iAuth = s.IndexOf('@');
            if (iAuth >= 0) {
                sb.Append(s.Substring(0, iAuth + 1));
                s = s.Substring(iAuth + 1);
            }
            int iPort = s.IndexOf(':');
            if (iPort >= 0) {
                sb.Append(s.Substring(0, iPort + 1));
                s = s.Substring(iPort + 1);
            }
            if (iAuth >= 0 || iPort >= 0 || iPort >= 0) {
                int iPath = s.IndexOf('/');
                if (iPath >= 0) {
                    sb.Append(s.Substring(0, iPath + 1));
                    s = s.Substring(iPath + 1);
                }
            }
            return s;
        }

        private static string SkipDomain(StringBuilder sb, string s) {
            int i = s.IndexOf('/');
            if (i >= 0) {
                sb.Append(s.Substring(0, i));
                s = s.Substring(i);
            }
            return s;
        }

        public static string UrlFor(Type type, string actionName, object args = null) {
            if (!type.Name.EndsWith("Controller")) throw new InternalError("Type {0} is not a controller", type.FullName);
            string controller = type.Name.Substring(0, type.Name.Length - "Controller".Length);
            Package package = Package.TryGetPackageFromAssembly(type.Assembly);
            if (package == null)
                throw new InternalError("Type {0} is not part of a package", type.FullName);
            string area = package.AreaName;
            string url = "/" + area + "/" + controller + "/" + actionName;
            QueryHelper query = QueryHelper.FromAnonymousObject(args);
            return query.ToUrl(url);
        }

        // HTTP Sync I/O Handling
        // HTTP Sync I/O Handling
        // HTTP Sync I/O Handling

        /// <summary>
        /// Used to enable sync I/O for the current request. Only enable sync I/O when there is no way to use async I/O (e.g., when using a 3rd party library).
        /// </summary>
        /// <param name="httpContext">The Http context.</param>
        /// <remarks>See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-3.0#synchronous-io for more info.</remarks>
#if MVC6
        public static void AllowSyncIO(HttpContext httpContext) {
            var syncIOFeature = httpContext.Features.Get<IHttpBodyControlFeature>();
            if (syncIOFeature != null)
                syncIOFeature.AllowSynchronousIO = true;
        }
#else
        public static void AllowSyncIO(object dummy) { }
#endif
    }
}
