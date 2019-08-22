/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Serializers;

namespace YetaWF.Core.Serializers {

    // A bit fragile... When the Stream class added Read/Write overrides, Write(null) stopped working because it wouldn't use StreamExtender.Write
    // Make sure to use Write((string)...) instead.
    // TODO: prob need to change this to not use extender - who's got time...

    public static class StreamExtender {

        public static void Write(this Stream stream, string text, params object[] parms) {
            if (text == null) {
                int len = -1;
                byte[] buffer = BitConverter.GetBytes(len);
                stream.Write(buffer, 0, buffer.Length);
            } else {
                string s;
                if (parms == null)
                    s = text;
                else
                    s = string.Format(text, parms);
                int len = Encoding.UTF8.GetByteCount(s);
                byte[] buffer = BitConverter.GetBytes(len);
                stream.Write(buffer, 0, buffer.Length);
                if (s.Length > 0)
                    stream.Write(Encoding.UTF8.GetBytes(s), 0, Encoding.UTF8.GetByteCount(s));
            }
        }
        public static void Write(this Stream stream, string text) { stream.Write(text, null); }

        public static string Readxx(this Stream stream) {
            int len = 0;
            byte[] buffer = BitConverter.GetBytes(len);
            if (stream.Read(buffer, 0, buffer.Length) != buffer.Length)
                throw new InternalError("Invalid format in input buffer - length marker invalid.");
            len = BitConverter.ToInt32(buffer, 0);

            if (len == -1) {
                return null;
            } else if (len == 0) {
                return "";
            } else {
                buffer = new byte[len];
                if (stream.Read(buffer, 0, len) != len)
                    throw new InternalError("Invalid format in input buffer - data invalid.");
                return Encoding.UTF8.GetString(buffer);
            }
        }
    }

    public class SimpleFormatter : IFormatter {

        SerializationBinder binder;
        StreamingContext context;
        ISurrogateSelector surrogateSelector;

        private string _unread;

        public SimpleFormatter() {
            context = new StreamingContext(StreamingContextStates.All);
            _unread = null;
        }

        private string Read(Stream stream) {
            if (string.IsNullOrEmpty(_unread))
                return stream.Readxx();
            else {
                string s = _unread;
                _unread = null;
                return s;
            }
        }
        private void Unread(string s) {
            _unread = s;
        }

        public SerializationBinder Binder {
            get { return binder; }
            set { binder = value; }
        }
        public ISurrogateSelector SurrogateSelector {
            get { return surrogateSelector; }
            set { surrogateSelector = value; }
        }
        public StreamingContext Context {
            get { return context; }
            set { context = value; }
        }

        public void Serialize(System.IO.Stream serializationStream, object graph) {
            SerializeObjectProperties(serializationStream, graph);
        }

        private void SerializeObjectProperties(Stream stream, object obj) {

            string asmName = obj.GetType().Assembly.GetName().Name;
            string asmFullName = "";// we only save the full name if it's not YetaWF.Core
            if (asmName != YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Name)
                asmFullName = obj.GetType().Assembly.FullName;

            // "YetaWF.Core.Serializers.SerializableList`1[[YetaWF.Core.Localize.LocalizationData+ClassData, YetaWF.Core, Version=1.0.6.0, Culture=neutral, PublicKeyToken=null]]"
            // remove version as it's not needed and just clutters up the save files
            string typeName = obj.GetType().FullName;
            typeName = GeneralFormatter.UpdateTypeForSerialization(typeName);

            stream.Write("Object:{0}:{1}:{2}", typeName, asmName, asmFullName);
            stream.Write("P");

            // we only want properties
            List<PropertyInfo> pi = ObjectSupport.GetProperties(obj.GetType());
            foreach (var p in pi) {
                if (!p.CanRead) continue;
                if (!p.CanWrite) continue;
                ParameterInfo[] parms = p.GetIndexParameters();
                if (parms.Length > 0) continue;// indexed parms can't be saved
                if (Attribute.GetCustomAttribute(p, typeof(DontSaveAttribute)) != null || Attribute.GetCustomAttribute(p, typeof(Data_CalculatedProperty)) != null || Attribute.GetCustomAttribute(p, typeof(Data_DontSave)) != null)
                    continue;

                stream.Write("N:{0}", p.Name);
                object o = p.GetValue(obj, null);
                SerializeOneProperty(stream, o);
            }

            stream.Write("E");

            if (obj is Byte[])
#pragma warning disable 642 // Possible mistaken empty statement
                ; // byte array are handled as simple properties
#pragma warning restore 642
            else if (obj is IDictionary) {
                IDictionary idict = (IDictionary)obj;
                IDictionaryEnumerator denum = idict.GetEnumerator();
                denum.Reset();

                stream.Write("DICT");

                for ( ; ; ) {
                    if (!denum.MoveNext())
                        break;
                    object key = denum.Key;
                    object val = denum.Value;

                    SerializeOneProperty(stream, key);
                    SerializeOneProperty(stream, val);
                }

                stream.Write("E");
            } else if (obj is IList) {
                IList ilist = (IList)obj;
                IEnumerator lenum = ilist.GetEnumerator();
                lenum.Reset();

                stream.Write("LIST");

                for ( ; ; ) {
                    if (!lenum.MoveNext())
                        break;
                    object val = lenum.Current;

                    SerializeOneProperty(stream, val);
                }
                stream.Write("E");
            }
            stream.Write("E");
        }

        private void SerializeOneProperty(Stream stream, object o) {

            if (o == null) {
                stream.Write("V");
                stream.Write((string)null);
                return;
            }
            Type tp = o.GetType();
#if DEBUG
            if (tp.IsAbstract)
                throw new InternalError("Abstract property??? {0} is not serializable.", tp.FullName);
            if (tp.IsInterface)
                throw new InternalError("Interface {0} is not serializable.", tp.FullName);
#endif
            if (tp == typeof(Byte[])) {
                stream.Write("V");
                stream.Write(Convert.ToBase64String((byte[]) o));
            } else if (tp.IsArray) {
                SerializeObjectProperties(stream, o);
            } else if (tp.IsEnum) {
                string val = Convert.ToInt64(o).ToString(CultureInfo.InvariantCulture);
                stream.Write("V");
                stream.Write(val);
            } else if (tp == typeof(DateTime) || tp == typeof(DateTime?)) {
                stream.Write("V");
                stream.Write(((DateTime)o).Ticks.ToString());
            } else if (tp == typeof(TimeSpan) || tp == typeof(TimeSpan?)) {
                stream.Write("V");
                stream.Write(((TimeSpan)o).Ticks.ToString());
            } else if (tp == typeof(DayOfWeek) || tp == typeof(DayOfWeek?)) {
                stream.Write("V");
                string val = Convert.ToInt64(o).ToString(CultureInfo.InvariantCulture);
            } else if (tp == typeof(System.Drawing.Image) || tp == typeof(Bitmap)) {
                System.Drawing.Image img = (System.Drawing.Image)o;
                using (MemoryStream ms = new MemoryStream()) {
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    stream.Write("V");
                    stream.Write(Convert.ToBase64String(ms.ToArray()));
                }
            } else if (tp.IsValueType) {
                string val = Convert.ToString(o, CultureInfo.InvariantCulture);
                stream.Write("V");
                stream.Write(val);
            } else if (tp.IsClass) {
                IConvertible iconv = (o as IConvertible);
                if (iconv != null) {
                    // this object can represent itself as a string
                    string s = iconv.ToString(CultureInfo.InvariantCulture);
                    stream.Write("V");
                    stream.Write(s);
                } else {
                    SerializeObjectProperties(stream, o);
                }
            } else
                throw new InternalError("Unexpected type {0} cannot be serialized.", tp.FullName);
        }

        public object Deserialize(Stream serializationStream) {

            object obj = null;
            obj = DeserializeOneObject(serializationStream);

            //if (serializationStream.Position < serializationStream.Length)
            //    throw new InternalError("Unexpected data beyond end of input.");

            return obj;
        }

        public object DeserializeOneObject(Stream stream) {
            object obj = null;
            string input = Read(stream);
            string[] s = input.Split(new char[] { ':' }, 4);
            if (s.Length != 4 || s[0] != "Object")
                throw new InternalError("Invalid Object encountered - {0}.", input);

            string strType = s[1];
            strType = GeneralFormatter.UpdateTypeForDeserialization(strType);
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
                    throw new InternalError("Invalid object type {0} - {1} - AssemblyFull missing or invalid", input, ErrorHandling.FormatExceptionMessage(exc));
                }
            }
            input = Read(stream);
            if (input == "E") // end of object (empty)
                return null;

            try {
                obj = Activator.CreateInstance(t);
            } catch (Exception exc) {
                throw new InternalError("Unable to create an instance of type {0} - {1}.", strType, ErrorHandling.FormatExceptionMessage(exc));
            }

            for ( ; ; ) {
                if (input == "E")
                    break;
                if (input == "P") {
                    DeserializeProperties(stream, obj);
                } else if (input == "DICT") {
                    DeserializeDictionary(stream, obj);
                } else if (input == "LIST") {
                    DeserializeList(stream, obj);
                } else
                    throw new InternalError("Unexpected input {0}.", input);

                input = Read(stream);
            }
            return obj;
        }

        private void DeserializeProperties(Stream stream, object obj) {
            string input = Read(stream);
            if (input == "E")
                return;

            Type tpObj = obj.GetType();

            for ( ; ; ) {
                if (input == "E")
                    return;
                else {
                    string[] s = input.Split(new char[] { ':' }, 2);
                    if (s.Length != 2 || s[0] != "N")
                        throw new InternalError("Invalid property encountered - {0}.", input);
                    string propName = s[1];

                    DeserializeOneProperty(stream, propName, obj, tpObj);
                }
                input = Read(stream);
            }
        }

        private object DeserializeOneProperty(Stream stream, string propName, object obj, Type tpObj, bool set = true) {
            string input = Read(stream);

            object objVal = null;
            PropertyInfo pi = null;
            if (set) {
                pi = ObjectSupport.TryGetProperty(tpObj, propName);
                //if (pi == null) {
                    //Logging.AddLog("Element found for non-existent property {0}.", propName);
                    //throw new InternalError("Element found for non-existent property {0}.", propName);
                    // This is OK as it can happen when data models change
                //}
            }

            if (input == "V") {
                // simple value
                string strVal = Read(stream);

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
                        } else if (pType == typeof(int) || pType == typeof(int?)) {
                            objVal = Convert.ToInt32(strVal);
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
                        } else if (pType == typeof(DayOfWeek) || pType == typeof(DayOfWeek?)) {
                            objVal = (DayOfWeek)(Convert.ToInt64(strVal));
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
                        failMsg = ErrorHandling.FormatExceptionMessage(exc);
                    }
                    if (fail) {
                        // try using a constructor (types like Guid can't simply be assigned)
                        ConstructorInfo ci = pType.GetConstructor(new Type[] { typeof(string) });
                        if (ci == null) {
                            throw new InternalError("Property {0} can't be assigned and doesn't have a suitable constructor - {1}.", propName, failMsg);
                        }
                        try {
                            objVal = ci.Invoke(new object[] { strVal });
                            pi.SetValue(obj, objVal, null);
                        } catch (Exception exc) {
                            throw new InternalError("Property {0} can't be assigned using a constructor - {1} - {2}.", propName, failMsg, ErrorHandling.FormatExceptionMessage(exc));
                        }
                    }
                } else {
                    objVal = strVal;
                }
            } else {
                Unread(input); // pushback

                objVal = DeserializeOneObject(stream);
                if (set && pi != null) {
                    try {
                        pi.SetValue(obj, objVal, null);
                    } catch (Exception exc) {
                        throw new InternalError("Element for property {0} has an invalid value - {1}.", propName, ErrorHandling.FormatExceptionMessage(exc));
                    }
                }
            }
            return objVal;
        }

        private void DeserializeDictionary(Stream stream, object obj) {
            string input = Read(stream);

            Type tpObj = obj.GetType();
            MethodInfo mi = tpObj.GetMethod("Add", new Type[] { typeof(object), typeof(object) });
            if (mi == null)
                throw new InternalError("Dictionary type {0} doesn't implement the required void Add(object,object) method.", tpObj.Name);

            for ( ; ; ) {
                if (input == "E")
                    return;

                Unread(input);
                object objKey = DeserializeOneProperty(stream, null, obj, tpObj, false);
                object objVal = DeserializeOneProperty(stream, null, obj, tpObj, false);

                try {
                    mi.Invoke(obj, new object[] { objKey, objVal });
                } catch (Exception exc) {
                    throw new InternalError("Couldn't add new entry to dictionary type {0} - {1}.", tpObj.Name, ErrorHandling.FormatExceptionMessage(exc));
                }
                input = Read(stream);
            }
        }
        private void DeserializeList(Stream stream, object obj) {
            string input = Read(stream);

            Type tpObj = obj.GetType();
            MethodInfo mi = tpObj.GetMethod("Add", new Type[] { typeof(object) });
            if (mi == null)
                throw new InternalError("List type {0} doesn't implement the required void Add(object,object) method.", tpObj.Name);

            for ( ; ; ) {
                if (input == "E")
                    return;

                Unread(input);
                object objVal = DeserializeOneProperty(stream, null, obj, tpObj, false);

                try {
                    mi.Invoke(obj, new object[] { objVal });
                } catch (Exception exc) {
                    throw new InternalError("Couldn't add new entry to list type {0} - {1}.", tpObj.Name, ErrorHandling.FormatExceptionMessage(exc));
                }
                input = Read(stream);
            }
        }
    }
}
