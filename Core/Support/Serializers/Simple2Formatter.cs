/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Serializers {

    public class Simple2Formatter {

        public Simple2Formatter() { }

        public const char MARKER1 = 'S';
        public const char MARKER2 = '2';

        BinaryWriter Output;

        private void WriteString(string s) {
            if (s == null) {
                Output.Write(unchecked((byte)((-1 << 2) + 0x3)));
            } else if (s == "") {
                Output.Write((byte)(0 + 0x3));
            } else {
                byte[] btes = Encoding.UTF8.GetBytes(s);
                int len = btes.Length;
                //WARNING ASSUMES LITTLE-ENDIAN
                //WARNING ASSUMES LITTLE-ENDIAN
                //WARNING ASSUMES LITTLE-ENDIAN
                if (!BitConverter.IsLittleEndian) throw new InternalError("Little endian only please");
                // the least significant bits in the least significant byte (which is stored first) holds a special indicator
                if (len < 256 >> 2) { // use 1 byte for length
                    Output.Write((byte)((len<<2) + 0x2));
                } else if (len < (65535 >> 2)) { // use 2 bytes for length
                    Output.Write((ushort)((len << 2) + 0x1));
                } else {
                    Output.Write((len << 2) + 0x0);
                }
                Output.Write(btes);
            }
        }
        private void WriteString(string fmt, params object[] args) {
            string s = string.Format(fmt, args);
            WriteString(s);
        }

        byte[] Bytes = null;
        int Offset = 0;

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

        private static Regex reVers = new Regex(@",\s*Version=.*?,", RegexOptions.Compiled);

        public byte[] Serialize(object graph) {
            using (MemoryStream ms = new MemoryStream()) {
                using (BinaryWriter bw = new BinaryWriter(ms)) {
                    Output = bw;
                    Output.Write(MARKER1); // Marker
                    Output.Write(MARKER2);
                    SerializeObjectProperties(graph);
                    Output = null;
                }
                return ms.GetBuffer();
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
            typeName = reVers.Replace(typeName, ",");

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

            if (obj is Byte[])
#pragma warning disable 642 // Possible mistaken empty statement
                ; // byte array are handled as simple properties
#pragma warning restore 642
            else if (obj is IDictionary) {
                IDictionary idict = (IDictionary)obj;
                IDictionaryEnumerator denum = idict.GetEnumerator();
                denum.Reset();

                WriteString("DICT");

                for ( ; ; ) {
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

                for ( ; ; ) {
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
                if (o != null)
                    WriteString(Convert.ToBase64String((byte[]) o));
            } else if (tp.IsArray) {
                SerializeObjectProperties(o);
            } else if (tp.IsEnum) {
                string val = Convert.ToInt64(o).ToString(CultureInfo.InvariantCulture);
                if (val != null) {
                    WriteString("V");
                    WriteString(val);
                }
            } else if (tp == typeof(DateTime) || tp == typeof(DateTime?)) {
                WriteString("V");
                WriteString(((DateTime)o).Ticks.ToString());
            } else if (tp == typeof(TimeSpan) || tp == typeof(TimeSpan?)) {
                WriteString("V");
                WriteString(((TimeSpan)o).Ticks.ToString());
            } else if (tp == typeof(System.Drawing.Image) || tp == typeof(Bitmap)) {
                if (o != null) {
                    System.Drawing.Image img = (System.Drawing.Image)o;
                    using (MemoryStream ms = new MemoryStream()) {
                        img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        WriteString("V");
                        WriteString(Convert.ToBase64String(ms.ToArray()));
                    }
                }
            } else if (tp.IsValueType) {
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

        public object Deserialize(byte[] btes) {
            Bytes = btes;
            if (btes[0] != MARKER1 || btes[1] != MARKER2)
                throw new InternalError("Invalid marker");
            Offset = 2;// skip marker
            return DeserializeOneObject();
        }
        private object DeserializeOneObject() {
            object obj = null;
            string input = ReadString();

            // Get Object type
            string[] s = input.Split(new char[] { ':' }, 4);
            if (s.Length != 4 || s[0] != "Object")
                throw new InternalError("Invalid Object encountered - {0}", input);

            string strType = s[1];
            string strAsm = s[2];
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
                    throw new InternalError("Invalid object type {0} - {1} - AssemblyFull missing or invalid", input, exc.Message);
                }
            }

            input = ReadString();
            if (input == "E") // end of object (empty)
                return null;

            try {
                obj = Activator.CreateInstance(t);
            } catch (Exception exc) {
                throw new InternalError("Unable to create an instance of type {0} - {1}", strType, exc.Message);
            }
            Type tpObj = obj.GetType();

            for ( ; ; ) {
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
            return obj;
        }

        // Cache last used object type and property info - this way we can avoid passing this around as parameters
        // This is most used for lists/dictionaries
        private Type LastObjType = null;
        private List<PropertyInfo> LastPropInfos;

        private void DeserializeProperties(object obj, Type tpObj) {
            string input = ReadString();
            if (input == "E")
                return;

            List<PropertyInfo> propInfos = null;

            for ( ; ; ) {
                if (input == "E")
                    return;
                else {
                    string[] s = input.Split(new char[] { ':' }, 2);
                    if (s.Length != 2 || s[0] != "N")
                        throw new InternalError("Invalid property encountered - {0}", input);
                    string propName = s[1];

                    if (propInfos == null) {
                        if (LastObjType == tpObj)
                            propInfos = LastPropInfos;
                        else {
                            propInfos = ObjectSupport.GetProperties(tpObj);
                            LastPropInfos = propInfos;
                            LastObjType = tpObj;
                        }
                    }
                    DeserializeOneProperty(propName, obj, tpObj, propInfos);
                }
                input = ReadString();
            }
        }
        private object DeserializeOneProperty(string propName, object obj, Type tpObj, List<PropertyInfo> propInfos, bool set = true) {
            string input = ReadString();

            object objVal = null;
            PropertyInfo pi = null;
            if (set) {
                pi = (from PropertyInfo p in propInfos where p.Name == propName select p).FirstOrDefault();
                //$$ pi = ObjectSupport.TryGetProperty(tpObj, propName);
                //if (pi == null) {
                //Logging.AddLog("Element found for non-existent property {0}", propName);
                //throw new InternalError("Element found for non-existent property {0}", propName);
                // This is OK as it can happen when data models change
                //}
            }

            if (input == "V") {

                // simple value
                string strVal = ReadString();

                if (set && pi != null) {
                    bool fail = false;
                    string failMsg = null;
                    Type pType = pi.PropertyType;
                    try {
                        if (pType == typeof(Byte[])) {
                            if (strVal == null)
                                objVal = new byte[] { };
                            else
                                objVal = Convert.FromBase64String(strVal);
                        } else if (strVal == null) {
                            objVal = null;
                        } else if (pType.IsEnum) {
                            objVal = Convert.ChangeType(strVal, typeof(long), CultureInfo.InvariantCulture);
                            objVal = Enum.ToObject(pType, objVal);
                        } else if (pType == typeof(DateTime) || pType == typeof(DateTime?)) {
                            long ticks = (long)Convert.ChangeType(strVal, typeof(long), CultureInfo.InvariantCulture);
                            objVal = new DateTime(ticks);
                        } else if (pType == typeof(Guid) || pType == typeof(Guid?)) {
                            objVal = new Guid(strVal);
                        } else if (pType == typeof(TimeSpan) || pType == typeof(TimeSpan?)) {
                            objVal = new TimeSpan(Convert.ToInt64(strVal));
                        } else if (pType == typeof(System.Drawing.Image) || pType == typeof(Bitmap)) {
                            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(strVal))) {
                                objVal = System.Drawing.Image.FromStream(ms);
                            }
                        } else {
                            objVal = Convert.ChangeType(strVal, pType, CultureInfo.InvariantCulture);
                        }
                        pi.SetValue(obj, objVal, null);
                    } catch (Exception exc) {
                        fail = true;
                        failMsg = exc.Message;
                    }
                    if (fail) {
                        // try using a constructor (types like Guid can't simply be assigned)
                        ConstructorInfo ci = pType.GetConstructor(new Type[] { typeof(string) });
                        if (ci == null) {
                            throw new InternalError("Property {0} can't be assigned and doesn't have a suitable constructor - {1}", propName, failMsg);
                        }
                        try {
                            objVal = ci.Invoke(new object[] { strVal });
                            pi.SetValue(obj, objVal, null);
                        } catch (Exception exc) {
                            throw new InternalError("Property {0} can't be assigned using a constructor - {1} - {2}", propName, failMsg, exc.Message);
                        }
                    }
                } else {
                    objVal = strVal;
                }
            } else {
                UnreadString(input); // pushback

                objVal = DeserializeOneObject();
                if (set && pi != null) {
                    try {
                        pi.SetValue(obj, objVal, null);
                    } catch (Exception exc) {
                        throw new InternalError("Element for property {0} has an invalid value - {1}", propName, exc.Message);
                    }
                }
            }
            return objVal;
        }
        private void DeserializeDictionary(object obj, Type tpObj) {
            string input = ReadString();

            MethodInfo mi = tpObj.GetMethod("Add", new Type[] { typeof(object), typeof(object) });
            if (mi == null)
                throw new InternalError("Dictionary type {0} doesn't implement the required void Add(object,object) method", tpObj.Name);

            for ( ; ; ) {
                if (input == "E")
                    return;

                UnreadString(input);
                object objKey = DeserializeOneProperty(null, obj, tpObj, null, false);
                object objVal = DeserializeOneProperty(null, obj, tpObj, null, false);

                try {
                    mi.Invoke(obj, new object[] { objKey, objVal });
                } catch (Exception exc) {
                    throw new InternalError("Couldn't add new entry to dictionary type {0} - {1}", tpObj.Name, exc.Message);
                }
                input = ReadString();
            }
        }
        private void DeserializeList(object obj, Type tpObj) {
            string input = ReadString();

            MethodInfo mi = tpObj.GetMethod("Add", new Type[] { typeof(object) });
            if (mi == null)
                throw new InternalError("List type {0} doesn't implement the required void Add(object,object) method", tpObj.Name);

            for ( ; ; ) {
                if (input == "E")
                    return;

                UnreadString(input);
                object objVal = DeserializeOneProperty(null, obj, tpObj, null, false);

                try {
                    mi.Invoke(obj, new object[] { objVal });
                } catch (Exception exc) {
                    throw new InternalError("Couldn't add new entry to list type {0} - {1}", tpObj.Name, exc.Message);
                }
                input = ReadString();
            }
        }
    }
}
