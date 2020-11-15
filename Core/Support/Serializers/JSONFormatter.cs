/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Reflection;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Serializers {

    /// <summary>
    /// Serializes/deserializes objects.
    /// </summary>
    public class JSONFormatter {

        public JSONFormatter() { }

        public const char MARKER1 = '{';

        public class ContractResolver : DefaultContractResolver {

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {

                JsonProperty property = base.CreateProperty(member, memberSerialization);
                PropertyInfo pi = ObjectSupport.GetProperty(property.DeclaringType, property.PropertyName);

                bool shouldSerialize = false;
                if (pi.CanRead && pi.CanWrite) {
                    ParameterInfo[] parms = pi.GetIndexParameters();
                    if (parms.Length == 0) { // indexed parms can't be saved
                        if (Attribute.GetCustomAttribute(pi, typeof(DontSaveAttribute)) != null || Attribute.GetCustomAttribute(pi, typeof(Data_CalculatedProperty)) != null || Attribute.GetCustomAttribute(pi, typeof(Data_DontSave)) != null) {

                        } else {
                            shouldSerialize = true;
                        }
                    }
                }
                property.Ignored = !shouldSerialize;
                return property;
            }
        }

        public void Serialize(FileStream fs, object? obj, bool typed = false) {
            byte[] btes = Serialize(obj, typed);
            fs.Write(btes, 0, btes.Length);
        }
        public byte[] Serialize(object? obj, bool typed = false) {
            string s = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings {
                ContractResolver = new ContractResolver(),
                TypeNameHandling = typed ? TypeNameHandling.All : TypeNameHandling.None,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
            });
            byte[] btes = System.Text.Encoding.UTF8.GetBytes(s);
            return btes;
        }
        public TObj? Deserialize<TObj>(FileStream fs, bool typed = false) {
            byte[] btes = new byte[fs.Length];
            fs.Read(btes, 0, (int)fs.Length);
            return Deserialize<TObj?>(btes, typed);
        }
        public TObj? Deserialize<TObj>(byte[] btes, bool typed = false) {
            string s = System.Text.Encoding.UTF8.GetString(btes);
            if (typed)
                return Newtonsoft.Json.JsonConvert.DeserializeObject<TObj>(s, new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.Auto,
                    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                    ObjectCreationHandling = ObjectCreationHandling.Replace,
                    SerializationBinder = new TypeNameSerializationBinder()
            });
            else
                return Utility.JsonDeserialize<TObj?>(s);
        }
    }
    public class TypeNameSerializationBinder : ISerializationBinder {
        public void BindToName(Type serializedType, out string assemblyName, out string typeName) {
            assemblyName = serializedType.Assembly.FullName!;
            typeName = serializedType.FullName!;
        }
        public Type BindToType(string assemblyName, string typeName) {
#if MVC6
#else
            typeName = typeName.Replace("System.Private.CoreLib", "mscorlib");
#endif
            string resolvedTypeName = string.Format($"{typeName}, {assemblyName}");
            return Type.GetType(resolvedTypeName, true)!;
        }
    }
}
