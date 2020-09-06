/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.Serializers {

    /// <summary>
    /// Serializes an object to a byte array.
    /// </summary>
    /// <remarks>This should only be used with small-ish objects as they're entirely in memory.
    ///
    /// This tolerates version changes to a certain extent (new/deleted properties are not a problem).
    ///
    /// It's a shame that .Net makes it so hard to get at raw data. There is a significant performance hit to serialization/deserialization.
    /// Purists will not like this approach, but this only has to work within the YetaWF framework.
    /// Lists and dictionaries could probably be optimized further (this is going to be a never ending task).
    /// The use of (cached) reflection is not really helping (except that it's cached).
    /// Don't serialize/deserialize if you can avoid it.</remarks>
    public class Simple2Formatter {

        public Simple2Formatter() { }

        public const char MARKER1 = 'S';
        public const char MARKER2 = '2';

        BinaryWriter Output;

        private void WriteBytes(byte[] btes) {
            if (btes == null) {
                Output.Write(unchecked((byte)((-1 << 2) + 0x3)));
            } else if (btes.Length == 0) {
                Output.Write((byte)(0 + 0x3));
            } else {
                int len = btes.Length;
                //WARNING ASSUMES LITTLE-ENDIAN
                //WARNING ASSUMES LITTLE-ENDIAN
                //WARNING ASSUMES LITTLE-ENDIAN
                if (!BitConverter.IsLittleEndian) throw new InternalError("Little endian only please");
                // the least significant bits in the least significant byte (which is stored first) holds a special indicator
                if (len < (256 >> 2)) { // use 1 byte for length
                    Output.Write((byte)((len << 2) + 0x2));
                } else if (len < (65535 >> 2)) { // use 2 bytes for length
                    Output.Write((ushort)((len << 2) + 0x1));
                } else {
                    Output.Write((len << 2) + 0x0);
                }
                Output.Write(btes);
            }
        }
        private void WriteString(string s) {
            if (s == null) {
                Output.Write(unchecked((byte)((-1 << 2) + 0x3)));
            } else if (s == "") {
                Output.Write((byte)(0 + 0x3));
            } else {
                WriteBytes(Encoding.UTF8.GetBytes(s));
            }
        }
        private void WriteString(string fmt, params object[] args) {
            string s = string.Format(fmt, args);
            WriteString(s);
        }

        byte[] Bytes = null;
        int Offset = 0;

        private void ReadBytes(Action<byte[], int, int> setBytes, Action setNull, Action setNone) {
            if (_unread != null)
                throw new InternalError("Should not happen");
            int lsb = Bytes[Offset];
            int len;
            if (lsb == unchecked((byte)((-1 << 2) + 0x3))) {
                Offset += 1;
                setNull();
                return;
            } else if ((lsb & 0x3) == 0x3) {
                if (lsb != 0x03) throw new InternalError("Unexpected");
                Offset += 1;
                setNone();
                return;
            } else if ((lsb & 0x3) == 0x2) {
                Offset += 1;
                len = lsb >> 2;
            } else if ((lsb & 0x3) == 0x1) {
                len = ((Bytes[Offset + 1] << 8) + lsb) >> 2;
                Offset += 2;
            } else {
                len = BitConverter.ToInt32(Bytes, Offset) >> 2;
                Offset += 4;
            }
            byte[] btes = new byte[len];
            setBytes(Bytes, Offset, len);
            Offset += len;
        }
        private string ReadString() {
            if (_unread != null) {
                string s = _unread;
                _unread = null;
                return s;
            }
            int lsb = Bytes[Offset];
            int len;
            if (lsb == unchecked((byte)((-1 << 2) + 0x3))) {
                Offset += 1;
                return null;
            } else if ((lsb & 0x3) == 0x3) {
                if (lsb != 0x03) throw new InternalError("Unexpected");
                Offset += 1;
                return "";
            } else if ((lsb & 0x3) == 0x2) {
                Offset += 1;
                len = lsb >> 2;
            } else if ((lsb & 0x3) == 0x1) {
                len = ((Bytes[Offset + 1] << 8) + lsb) >> 2;
                Offset += 2;
            } else {
                len = BitConverter.ToInt32(Bytes, Offset) >> 2;
                Offset += 4;
            }
            string text = Encoding.UTF8.GetString(Bytes, Offset, len);
            Offset += len;
            return text;
        }
        private string _unread = null;

        private void UnreadString(string input) {
#if DEBUG
            if (_unread != null) throw new InternalError("Unread token {0} while unreading another token {1}", _unread, input);
#endif
            _unread = input;
        }

        /// <summary>
        /// Serialize an object.
        /// </summary>
        /// <param name="obj">The object to be serialized.</param>
        /// <returns>A byte array.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public byte[] Serialize(object obj) {
            using (MemoryStream ms = new MemoryStream()) {
                using (BinaryWriter bw = new BinaryWriter(ms)) {
                    Output = bw;
                    Output.Write(MARKER1); // Marker
                    Output.Write(MARKER2);
                    SerializeObjectProperties(obj);
                    Output = null;
                }
                byte[] btes = ms.GetBuffer();
                //#if DEBUG
                //                object o2 = Deserialize(btes);
                //#endif
                return btes;
            }
        }
        private void SerializeObjectProperties(object obj) {

            string asmName = obj.GetType().Assembly.GetName().Name;
            string asmFullName = "";// we only save the full name if it's not YetaWF.Core
            if (asmName != YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Name)
                asmFullName = obj.GetType().Assembly.FullName;

            // "YetaWF.Core.Serializers.SerializableList`1[[YetaWF.Core.Localize.LocalizationData+ClassData, YetaWF.Core, Version=1.0.6.0, Culture=neutral, PublicKeyToken=null]]"
            // remove version as it's not needed and just clutters up the save files
            string typeName = obj.GetType().FullName;
            typeName = GeneralFormatter.UpdateTypeForSerialization(typeName);

            WriteString("Object:{0}:{1}:{2}", typeName, asmName, asmFullName);
            WriteString("P");

            // we only want properties
            List<PropertyInfo> pi = ObjectSupport.GetProperties(obj.GetType());
            foreach (var p in pi) {
                if (!p.CanRead) continue;
                if (!p.CanWrite) continue;
                ParameterInfo[] parms = p.GetIndexParameters();
                if (parms.Length > 0) continue;// indexed parms can't be saved
                if (Attribute.GetCustomAttribute(p, typeof(DontSaveAttribute)) != null || Attribute.GetCustomAttribute(p, typeof(Data_CalculatedProperty)) != null || Attribute.GetCustomAttribute(p, typeof(Data_DontSave)) != null)
                    continue;

                WriteString("N:{0}", p.Name);
                object o = p.GetValue(obj, null);
                SerializeOneProperty(o);
            }

            WriteString("E");

            if (obj is Byte[]) {
                // byte arrays are handled as simple properties
            } else if (obj is IDictionary) {
                IDictionary idict = (IDictionary)obj;
                IDictionaryEnumerator denum = idict.GetEnumerator();
                denum.Reset();

                WriteString("DICT");

                for (;;) {
                    if (!denum.MoveNext())
                        break;
                    object key = denum.Key;
                    object val = denum.Value;

                    SerializeOneProperty(key);
                    SerializeOneProperty(val);
                }

                WriteString("E");
            } else if (obj is IList) {
                IList ilist = (IList)obj;
                IEnumerator lenum = ilist.GetEnumerator();
                lenum.Reset();

                WriteString("LIST");

                for (;;) {
                    if (!lenum.MoveNext())
                        break;
                    object val = lenum.Current;

                    SerializeOneProperty(val);
                }
                WriteString("E");
            }
            WriteString("E");
        }
        private void SerializeOneProperty(object o) {

            if (o == null) {
                WriteString("V");
                WriteString(null);
                return;
            }
            Type tp = o.GetType();
#if DEBUG
            if (tp.IsAbstract)
                throw new InternalError("Abstract property??? {0} is not serializable", tp.FullName);
            if (tp.IsInterface)
                throw new InternalError("Interface {0} is not serializable", tp.FullName);
#endif
            if (tp == typeof(Byte[])) {
                WriteString("V");
                WriteBytes((byte[])o);
            } else if (tp.IsArray) {
                SerializeObjectProperties(o);
            } else if (tp == typeof(int) || tp == typeof(int?)) {
                WriteString("V");
                WriteBytes(BitConverter.GetBytes((int)o));
            } else if (tp == typeof(long) || tp == typeof(long?)) {
                WriteString("V");
                WriteBytes(BitConverter.GetBytes((long)o));
            } else if (tp == typeof(string)) {
                WriteString("V");
                WriteString((string)o);
            } else if (tp == typeof(bool) || tp == typeof(bool?)) {
                WriteString("V");
                WriteBytes(BitConverter.GetBytes((bool)o));
            } else if (tp.IsEnum) {
                long val = Convert.ToInt64(o);
                WriteString("V");
                WriteBytes(BitConverter.GetBytes(val));
            } else if (tp == typeof(DateTime) || tp == typeof(DateTime?)) {
                WriteString("V");
                WriteBytes(BitConverter.GetBytes(((DateTime)o).Ticks));
            } else if (tp == typeof(TimeSpan) || tp == typeof(TimeSpan?)) {
                WriteString("V");
                WriteBytes(BitConverter.GetBytes(((TimeSpan)o).Ticks));
            } else if (tp == typeof(DayOfWeek) || tp == typeof(DayOfWeek?)) {
                WriteString("V");
                WriteBytes(BitConverter.GetBytes((int)o));
            } else if (tp == typeof(Guid) || tp == typeof(Guid?)) {
                WriteString("V");
                WriteBytes(((Guid)o).ToByteArray());
            } else if (tp == typeof(System.Drawing.Image) || tp == typeof(Bitmap)) {
                System.Drawing.Image img = (System.Drawing.Image)o;
                using (MemoryStream ms = new MemoryStream()) {
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    WriteString("V");
                    WriteBytes(ms.GetBuffer());
                }
            } else if (tp.IsValueType) {
                // Occasionally test here that no commonly used types are converted to strings (performance penalty)
                string val = Convert.ToString(o, CultureInfo.InvariantCulture);
                if (val != null) {
                    WriteString("V");
                    WriteString(val);
                }
            } else if (tp.IsClass) {
                IConvertible iconv = (o as IConvertible);
                if (iconv != null) {
                    // this object can represent itself as a string
                    string s = iconv.ToString(CultureInfo.InvariantCulture);
                    WriteString("V");
                    WriteString(s);
                } else {
                    SerializeObjectProperties(o);
                }
            } else
                throw new InternalError("Unexpected type {0} cannot be serialized", tp.FullName);
        }
        /// <summary>
        /// Deserialize a byte array into an object.
        /// </summary>
        /// <param name="btes">The byte array.</param>
        /// <returns>The object.</returns>
        public object Deserialize(byte[] btes) {
            Bytes = btes;
            if (btes[0] != MARKER1 || btes[1] != MARKER2)
                throw new InternalError("Invalid marker");
            Offset = 2;// skip marker
            return DeserializeOneObject();
        }
        private object DeserializeOneObject() {

            CacheLevel++;

            object obj = null;
            string input = ReadString();

            // Get Object type
            string[] s = input.Split(new char[] { ':' }, 4);
            if (s.Length != 4 || s[0] != "Object")
                throw new InternalError("Invalid Object encountered - {0}", input);

            string strType = s[1];
            string strAsm = s[2];
            GeneralFormatter.UpdateTypeForDeserialization(ref strType, ref strAsm);
            string strAsmFull = s[3];

            Type t;
            try {
                Assembly asm = Assemblies.Load(strAsm);
                t = asm.GetType(strType, true);
            } catch {
                t = null;
            }
            if (t == null) {
                try {
                    Assembly asm = Assemblies.Load(strAsmFull);
                    t = asm.GetType(strType, true);
                } catch (Exception exc) {
                    throw new InternalError("Invalid object type {0} - {1} - AssemblyFull missing or invalid", input, ErrorHandling.FormatExceptionMessage(exc));
                }
            }

            input = ReadString();
            if (input != "E") { // end of object (empty)?

                try {
                    obj = Activator.CreateInstance(t);
                } catch (Exception exc) {
                    throw new InternalError("Unable to create an instance of type {0} - {1}", strType, ErrorHandling.FormatExceptionMessage(exc));
                }
                Type tpObj = obj.GetType();

                for (;;) {
                    if (input == "E")
                        break;
                    if (input == "P") {
                        DeserializeProperties(obj, tpObj);
                    } else if (input == "DICT") {
                        DeserializeDictionary(obj, tpObj);
                    } else if (input == "LIST") {
                        DeserializeList(obj, tpObj);
                    } else
                        throw new InternalError("Unexpected input {0}", input);

                    input = ReadString();
                }
            }
            CacheLevel--;

            return obj;
        }

        // Cache last used object type and property info - this way we can avoid passing this around as parameters
        // This is mostly used for lists/dictionaries - Because of the recursion we need to keep a cache stack
        private const int MaxLevel = 20;
        private int CacheLevel = -1;
        private Type[] LastObjType = new Type[MaxLevel];
        private List<PropertyInfo>[] LastPropInfos = new List<PropertyInfo>[MaxLevel];

        private void DeserializeProperties(object obj, Type tpObj) {
            string input = ReadString();
            if (input == "E")
                return;

            List<PropertyInfo> propInfos = null;

            for (;;) {
                if (input == "E")
                    return;
                else {
                    string[] s = input.Split(new char[] { ':' }, 2);
                    if (s.Length != 2 || s[0] != "N")
                        throw new InternalError("Invalid property encountered - {0}", input);
                    string propName = s[1];

                    if (propInfos == null) {
                        if (LastObjType[CacheLevel] == tpObj)
                            propInfos = LastPropInfos[CacheLevel];
                        else {
                            propInfos = ObjectSupport.GetProperties(tpObj);
                            LastPropInfos[CacheLevel] = propInfos;
                            LastObjType[CacheLevel] = tpObj;
                        }
                    }
                    DeserializeOneProperty(propName, obj, tpObj, propInfos);
                }
                input = ReadString();
            }
        }
        private object DeserializeOneProperty(string propName, object obj, Type tpObj, List<PropertyInfo> propInfos) {

            string input = ReadString();
            UnreadString(input); // pushback

            object objVal = null;
            PropertyInfo pi = (from PropertyInfo p in propInfos where p.Name == propName select p).FirstOrDefault();
            if (pi == null) {
                //Logging.AddLog("Element found for non-existent property {0}", propName);
                //throw new InternalError("Element found for non-existent property {0}", propName);
                // This is OK as it can happen when data models change
                return null;
            }

            if (input == "V") {

                objVal = GetDeserializedProperty(pi.PropertyType);

                bool fail = false;
                string failMsg = null;
                try {
                    pi.SetValue(obj, objVal, null);
                } catch (Exception exc) {
                    fail = true;
                    failMsg = ErrorHandling.FormatExceptionMessage(exc);
                }
                if (fail) {
                    // try using a constructor (types like Guid can't simply be assigned)
                    ConstructorInfo ci = pi.PropertyType.GetConstructor(new Type[] { typeof(string) });
                    if (ci == null) {
                        throw new InternalError("Property {0} can't be assigned and doesn't have a suitable constructor - {1}", propName, failMsg);
                    }
                    try {
                        objVal = ci.Invoke(new object[] { objVal });
                        pi.SetValue(obj, objVal, null);
                    } catch (Exception exc) {
                        throw new InternalError("Property {0} can't be assigned using a constructor - {1} - {2}", propName, failMsg, ErrorHandling.FormatExceptionMessage(exc));
                    }
                }

            } else {

                objVal = DeserializeOneObject();

                try {
                    pi.SetValue(obj, objVal, null);
                } catch (Exception exc) {
                    throw new InternalError("Element for property {0} has an invalid value - {1}", propName, ErrorHandling.FormatExceptionMessage(exc));
                }
            }
            return objVal;
        }

        private object GetDeserializedProperty(Type pType) {

            string input = ReadString();

            object objVal = null;

            if (input == "V") {

                // simple value
                if (pType == typeof(Byte[])) {
                    ReadBytes((btes, offs, len) => {
                        objVal = new byte[len];
                        Buffer.BlockCopy(btes, offs, (byte[])objVal, 0, len);
                    }, () => { objVal = new byte[] { }; }, () => { objVal = new byte[] { }; });
                } else if (pType == typeof(int) || pType == typeof(int?)) {
                    ReadBytes((btes, offs, len) => {
                        objVal = BitConverter.ToInt32(btes, offs);
                    }, () => { objVal = null; }, () => { objVal = null; });
                } else if (pType == typeof(long) || pType == typeof(long?)) {
                    ReadBytes((btes, offs, len) => {
                        objVal = BitConverter.ToInt64(btes, offs);
                    }, () => { objVal = null; }, () => { objVal = null; });
                } else if (pType == typeof(string)) {
                    objVal = ReadString();
                } else if (pType == typeof(bool) || pType == typeof(bool?)) {
                    ReadBytes((btes, offs, len) => {
                        objVal = BitConverter.ToBoolean(btes, offs);
                    }, () => { objVal = null; }, () => { objVal = null; });
                } else if (pType.IsEnum) {
                    ReadBytes((btes, offs, len) => {
                        objVal = Enum.ToObject(pType, BitConverter.ToInt64(btes, offs));
                    }, () => { objVal = null; }, () => { objVal = null; });
                } else if (pType == typeof(DateTime) || pType == typeof(DateTime?)) {
                    ReadBytes((btes, offs, len) => {
                        objVal = new DateTime(BitConverter.ToInt64(btes, offs));
                    }, () => { objVal = null; }, () => { objVal = null; });
                } else if (pType == typeof(TimeSpan) || pType == typeof(TimeSpan?)) {
                    ReadBytes((btes, offs, len) => {
                        objVal = new TimeSpan(BitConverter.ToInt64(btes, offs));
                    }, () => { objVal = null; }, () => { objVal = null; });
                } else if (pType == typeof(DayOfWeek) || pType == typeof(DayOfWeek?)) {
                    ReadBytes((btes, offs, len) => {
                        objVal = (DayOfWeek)BitConverter.ToInt32(btes, offs);
                    }, () => { objVal = null; }, () => { objVal = null; });
                } else if (pType == typeof(Guid) || pType == typeof(Guid?)) {
                    ReadBytes((btes, offs, len) => {
                        byte[] b = new byte[len];
                        Buffer.BlockCopy(btes, offs, b, 0, len);
                        objVal = new Guid(b);
                    }, () => { objVal = null; }, () => { objVal = Guid.Empty; });
                } else if (pType == typeof(System.Drawing.Image) || pType == typeof(Bitmap)) {
                    ReadBytes((btes, offs, len) => {
                        byte[] b = new byte[len];
                        Buffer.BlockCopy(btes, offs, b, 0, len);
                        using (MemoryStream ms = new MemoryStream(b)) {
                            objVal = System.Drawing.Image.FromStream(ms);
                        }
                    }, () => { objVal = null; }, () => { objVal = Guid.Empty; });
                } else {
                    string strVal = ReadString();
                    objVal = Convert.ChangeType(strVal, pType, CultureInfo.InvariantCulture);
                }
            } else {
                UnreadString(input); // pushback
                objVal = DeserializeOneObject();
            }
            return objVal;
        }

        private void DeserializeDictionary(object obj, Type tpObj) {
            string input = ReadString();

            MethodInfo mi = tpObj.GetMethod("Add", new Type[] { typeof(object), typeof(object) });
            if (mi == null)
                throw new InternalError("Dictionary type {0} doesn't implement the required void Add(object,object) method", tpObj.Name);

            // handle derived types
            Type baseType = tpObj;
            while (baseType.GenericTypeArguments.Count() != 2) {
                if (baseType.BaseType == null)
                    throw new InternalError($"SerializableDictionary must have 2 generic types - {tpObj.Name}");
                baseType = baseType.BaseType;
            }
            Type elemKeyType = baseType.GenericTypeArguments[0].UnderlyingSystemType;
            Type elemValType = baseType.GenericTypeArguments[1].UnderlyingSystemType;

            for (;;) {
                if (input == "E")
                    return;

                UnreadString(input);
                object objKey = GetDeserializedProperty(elemKeyType);
                object objVal = GetDeserializedProperty(elemValType);

                try {
                    mi.Invoke(obj, new object[] { objKey, objVal });
                } catch (Exception exc) {
                    throw new InternalError("Couldn't add new entry to dictionary type {0} - {1}", tpObj.Name, ErrorHandling.FormatExceptionMessage(exc));
                }
                input = ReadString();
            }
        }
        private void DeserializeList(object obj, Type tpObj) {
            string input = ReadString();

            MethodInfo mi = tpObj.GetMethod("Add", new Type[] { typeof(object) });
            if (mi == null)
                throw new InternalError("List type {0} doesn't implement the required void Add(object,object) method", tpObj.Name);

            // handle derived types
            Type baseType = tpObj;
            while (baseType.GenericTypeArguments.Count() == 0) {
                if (baseType.BaseType == null)
                    throw new InternalError($"SerializableList must have 1 generic type - {tpObj.Name}");
                baseType = baseType.BaseType;
            }
            Type elemType = baseType.GenericTypeArguments[0].UnderlyingSystemType;

            for (;;) {
                if (input == "E")
                    return;

                UnreadString(input);
                object objVal = GetDeserializedProperty(elemType);

                try {
                    mi.Invoke(obj, new object[] { objVal });
                } catch (Exception exc) {
                    throw new InternalError("Couldn't add new entry to list type {0} - {1}", tpObj.Name, ErrorHandling.FormatExceptionMessage(exc));
                }
                input = ReadString();
            }
        }
    }
}
