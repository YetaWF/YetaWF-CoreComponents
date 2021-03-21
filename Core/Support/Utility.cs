/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;
using YetaWF.Core.Models;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Support {

    /// <summary>
    /// This static class implements utility functions used throughout YetaWF.
    /// </summary>
    public static class Utility {

        // VERSION
        // VERSION
        // VERSION

        /// <summary>
        /// Describes the MVC version.
        /// </summary>
        public enum AspNetMvcVersion {
            /// <summary>
            /// ASP.NET 4 with MVC 5 (no longer used or supported by YetaWF).
            /// </summary>
            MVC5 = 0,
            /// <summary>
            /// .NET with MVC (6).
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
        /// <returns>A user-displayable name for the MVC version used.</returns>
        public static string GetAspNetMvcName(AspNetMvcVersion version) {
            switch (version) {
                case AspNetMvcVersion.MVC5:
                    return "MVC5";
                case AspNetMvcVersion.MVC6:
                    return $".NET {Globals.RUNTIMEVERSION} - MVC ";
                default:
                    return "(unknown)";
            }
        }

        // PATH
        // PATH
        // PATH

        /// <summary>
        /// Translates a URL to a local file name.
        /// </summary>
        /// <remarks>
        /// Doesn't really take special characters (except spaces %20) into account.
        /// </remarks>
        public static string UrlToPhysical(string url) {
            if (!url.StartsWith("/")) throw new InternalError("URLs to translate must start with /.");
            string path;
            if (url.StartsWith(Globals.NodeModulesUrl, StringComparison.OrdinalIgnoreCase)) {
                path = YetaWFManager.RootFolderWebProject + Utility.UrlToPhysicalRaw(url);
            } else if (url.StartsWith(Globals.VaultPrivateUrl, StringComparison.OrdinalIgnoreCase)) {
                path = YetaWFManager.RootFolderWebProject + Utility.UrlToPhysicalRaw(url);
            } else {
                path = $"{YetaWFManager.RootFolder}{UrlToPhysicalRaw(url)}";
            }
            return path;
        }
        private static string UrlToPhysicalRaw(string url) {
            if (!url.StartsWith("/")) throw new InternalError("URLs to translate must start with /.");
            url = FileToPhysical(url);
            url = url.Replace("%20", " ");
            return url;
        }
        /// <summary>
        /// Translates a file path to the appropriate form of the operating system where the application is executing.
        /// YetaWF internally uses the "/" character, which may need to be translated for some operating systems, like Windows.
        /// </summary>
        /// <param name="file">A file path to translate.</param>
        /// <returns>The file path translated to the appropriate form of the operating system where the application is executing.</returns>
        public static string FileToPhysical(string file) {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                file = file.Replace('/', '\\');
            return file;
        }

        /// <summary>
        /// Translates a local file name to a URL.
        /// </summary>
        /// <remarks>
        /// Doesn't really take special characters (except spaces %20) into account.
        /// </remarks>
        public static string PhysicalToUrl(string path) {
            path = ReplaceString(path, YetaWFManager.RootFolder, String.Empty, StringComparison.OrdinalIgnoreCase);
            path = ReplaceString(path, YetaWFManager.RootFolderWebProject, String.Empty, StringComparison.OrdinalIgnoreCase);
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
        public static string JsonSerialize(object? value, bool Indented = false) {
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
        /// A JSON contract resolver so only properties with Get and Set accessors and UIHint are serialized.
        /// </summary>
        public class PropertyGetSetUIHintContractResolver : DefaultContractResolver {

            /// <summary>
            /// Constructor.
            /// </summary>
            public PropertyGetSetUIHintContractResolver() { }

            /// <inheritdoc/>
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
                IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
                if (type != typeof(object)) {
                    List<string> propList = new List<string>();
                    List<PropertyData> props = ObjectSupport.GetPropertyData(type);
                    foreach (PropertyData prop in props) {
                        if (prop.Name.StartsWith("__") || (prop.PropInfo.CanRead && prop.PropInfo.CanWrite && !string.IsNullOrWhiteSpace(prop.UIHint))) {
                            propList.Add(prop.Name);
                        }
                    }
                    properties = (from p in properties where propList.Contains(p.PropertyName) select p).ToList();
                }
                return properties;
            }
        }
        /// <summary>
        /// JSON settings used to de/serialize only properties with Get and Set accessors and UIHint.
        /// </summary>
        public static JsonSerializerSettings GetJsonSettingsGetSetUIHint(Newtonsoft.Json.Formatting formatting = Formatting.None) {
            return new JsonSerializerSettings {
                ContractResolver = new Utility.PropertyGetSetContractResolver(),
                Formatting = formatting,
            };
        }

        /// <summary>
        /// A JSON contract resolver so only properties with Get and Set accessors are serialized.
        /// </summary>
        public class PropertyGetSetContractResolver : DefaultContractResolver {

            /// <summary>
            /// Constructor.
            /// </summary>
            public PropertyGetSetContractResolver() { }

            /// <inheritdoc/>
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
                IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
                if (type != typeof(object)) {
                    List<string> propList = new List<string>();
                    List<PropertyData> props = ObjectSupport.GetPropertyData(type);
                    foreach (PropertyData prop in props) {
                        if (prop.Name.StartsWith("__") || (prop.PropInfo.CanRead && prop.PropInfo.CanWrite)) {
                            propList.Add(prop.Name);
                        }
                    }
                    properties = (from p in properties where propList.Contains(p.PropertyName) select p).ToList();
                }
                return properties;
            }
        }
        /// <summary>
        /// JSON settings used to de/serialize only properties with Get and Set accessors.
        /// </summary>
        public static JsonSerializerSettings GetJsonSettingsGetSet(Newtonsoft.Json.Formatting formatting = Formatting.None) {
            return new JsonSerializerSettings {
                ContractResolver = new Utility.PropertyGetSetContractResolver(),
                Formatting = formatting,
            };
        }

        /// <summary>
        /// Encodes a string for use with JavaScript. The returned string must be surrounded by quotes.
        /// </summary>
        /// <param name="s">The string to encode.</param>
        /// <returns>Returns the encoded string contents.</returns>
        public static string JserEncode(string? s) {
            if (s == null) return string.Empty;
            return System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(s);
        }

        /// <summary>
        /// Encodes a string for use as HTML.
        /// </summary>
        /// <param name="s">The string to encode.</param>
        /// <returns>Returns the encoded HTML.</returns>
        public static string HE(string? s) {
            if (s == null) return string.Empty;
            return System.Net.WebUtility.HtmlEncode(s);
        }
        /// <summary>
        /// Decodes a HTML encoded string.
        /// </summary>
        /// <param name="s">The HTML encoded string.</param>
        /// <returns>Returns the decoded string.</returns>
        public static string HtmlDecode(string? s) {
            if (s == null) return string.Empty;
            return System.Net.WebUtility.HtmlDecode(s);
        }
        /// <summary>
        /// Encodes a string for use as an HTML attribute.
        /// </summary>
        /// <param name="s">The string to encode.</param>
        /// <returns>Returns the string encoded for use as an HTML attribute.</returns>
        public static string HAE(string? s) {
            if (s == null) return string.Empty;
            return System.Net.WebUtility.HtmlEncode(s);
        }
        /// <summary>
        /// Encodes a string for use as a query string argument.
        /// </summary>
        /// <param name="s">The string to encode.</param>
        /// <returns>Returns the string encoded for use as a query string argument.</returns>
        public static string UrlEncodeArgs(string? s) {
            if (s == null) return string.Empty;
            return Uri.EscapeDataString(s);
        }
        /// <summary>
        /// Decodes an encoded query string argument.
        /// </summary>
        /// <param name="s">The encoded query string argument.</param>
        /// <returns>Returns the decoded string.</returns>
        public static string UrlDecodeArgs(string? s) {
            if (s == null) return string.Empty;
            return Uri.UnescapeDataString(s);
        }
        /// <summary>
        /// Encodes a string for use as a URL segment (parts in the URL between /xxx/).
        /// </summary>
        /// <param name="s">The URL segment.</param>
        /// <returns>Returns an encoded URL segment.</returns>
        public static string UrlEncodeSegment(string? s) {
            if (s == null) return string.Empty;
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
        public static string UrlEncodePath(string? s) {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
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
        internal static string UrlDecodePath(string? s) {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
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

        /// <summary>
        /// Returns a URL with query string, given a type implementing a controller, and an action name.
        /// </summary>
        /// <param name="type">The type of the controller.</param>
        /// <param name="actionName">The action name implemented by the controller.</param>
        /// <param name="args">An optional array of arguments that are translated to query string parameters.</param>
        /// <returns>A formatted URL for the current site (without scheme or domain).</returns>
        public static string UrlFor(Type type, string actionName, object? args = null) {
            if (!type.Name.EndsWith("Controller")) throw new InternalError("Type {0} is not a controller", type.FullName);
            string controller = type.Name.Substring(0, type.Name.Length - "Controller".Length);
            Package? package = Package.TryGetPackageFromAssembly(type.Assembly);
            if (package == null)
                throw new InternalError("Type {0} is not part of a package", type.FullName);
            string area = package.AreaName;
            string url = "/" + area + "/" + controller + "/" + actionName;
            QueryHelper query = QueryHelper.FromAnonymousObject(args);
            return query.ToUrl(url);
        }

        // YAML
        // YAML
        // YAML

        /// <summary>
        /// Serializes an object to Yaml.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>Returns a string containing yaml.</returns>
        public static string YamlSerialize(object obj) {
            YamlDotNet.Serialization.ISerializer serializer = GetYamlSerializer();
            return serializer.Serialize(obj);
        }
        private static ISerializer GetYamlSerializer() {
            if (_YamlSerializer == null)
                _YamlSerializer = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults).Build();
            return _YamlSerializer;
        }
        private static ISerializer? _YamlSerializer;

        /// <summary>
        /// Deserializes a Yaml string to an object.
        /// </summary>
        /// <typeparam name="TYPE">The type of the deserialized object.</typeparam>
        /// <param name="value">The Yaml string.</param>
        /// <returns>Returns the object.</returns>
        public static TYPE YamlDeserialize<TYPE>(string value) {
            YamlDotNet.Serialization.IDeserializer deserializer = GetYamlDeserializer();
            return deserializer.Deserialize<TYPE>(value);
        }

        private static IDeserializer GetYamlDeserializer() {
            if (_YamlDeserializer == null)
                _YamlDeserializer = new DeserializerBuilder().WithNodeDeserializer(inner => new ValidatingNodeDeserializer(inner), s => s.InsteadOf<ObjectNodeDeserializer>()).Build();
            return _YamlDeserializer;
        }
        private static IDeserializer? _YamlDeserializer;

        public class ValidatingNodeDeserializer : INodeDeserializer {
            private readonly INodeDeserializer _nodeDeserializer;

            public ValidatingNodeDeserializer(INodeDeserializer nodeDeserializer) {
                _nodeDeserializer = nodeDeserializer;
            }

            public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value) {
                if (_nodeDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value)) {
                    System.ComponentModel.DataAnnotations.ValidationContext context = new ValidationContext(value!, null, null);
                    Validator.ValidateObject(value!, context, true);
                    return true;
                }
                return false;
            }
        }

        // HTTP Sync I/O Handling
        // HTTP Sync I/O Handling
        // HTTP Sync I/O Handling

        /// <summary>
        /// Used to enable sync I/O for the current request. Only enable sync I/O when there is no way to use async I/O (e.g., when using a 3rd party library).
        /// </summary>
        /// <param name="httpContext">The Http context.</param>
        /// <remarks>See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-3.0#synchronous-io for more info.</remarks>
        public static void AllowSyncIO(HttpContext httpContext) {
            var syncIOFeature = httpContext.Features.Get<IHttpBodyControlFeature>();
            if (syncIOFeature != null)
                syncIOFeature.AllowSynchronousIO = true;
        }
    }
}
