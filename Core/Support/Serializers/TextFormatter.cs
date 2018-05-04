/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Models;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Serializers {
    public static class XmlWriterExtender {

        public static void Initialize(this XmlWriter xmlWrt) { }
        public static string Attr(this XmlTextReader xmlRd, string name) {
            string strAttr = xmlRd[name];
            if (strAttr == null)
                throw new InternalError("{0} element at line {1} doesn't have a required {2} attribute value", xmlRd.Name, xmlRd.LineNumber, name);
            return strAttr;
        }
        public static void MustRead(this XmlTextReader xmlRd) {
            if (!xmlRd.Read())
                throw new InternalError("Premature end of input at line {0}.", xmlRd.LineNumber);
        }
    }
    public class TextFormatter : IFormatter {

        SerializationBinder binder;
        StreamingContext context;
        ISurrogateSelector surrogateSelector;

        public TextFormatter() {
            context = new StreamingContext(StreamingContextStates.All);
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

            XmlWriter xmlOut = XmlWriter.Create(serializationStream, new XmlWriterSettings { Indent = true, IndentChars = "  ", });
            xmlOut.Initialize();
            xmlOut.WriteComment(string.Format("{0}", graph.GetType()));

            SerializeObjectProperties(xmlOut, graph);

            xmlOut.Flush();
            xmlOut.Close();
        }

        private Regex reVers = new Regex(@",\s*Version=.*?,", RegexOptions.Compiled);

        private void SerializeObjectProperties(XmlWriter xmlOut, object obj) {

            xmlOut.WriteStartElement("Object");

            // "YetaWF.Core.Serializers.SerializableList`1[[YetaWF.Core.Localize.LocalizationData+ClassData, YetaWF.Core, Version=1.0.6.0, Culture=neutral, PublicKeyToken=null]]"
            // remove version as it's not needed and just clutters up the xml files
            string typeName = obj.GetType().FullName;
            typeName = reVers.Replace(typeName, ",");
            xmlOut.WriteAttributeString("Type", typeName);

            string asmName = obj.GetType().Assembly.GetName().Name;

            xmlOut.WriteAttributeString("Assembly", asmName);
            if (asmName != YetaWF.Core.Controllers.AreaRegistration.CurrentPackage.Name)// we only save the full name if it's not YetaWF.Core
                xmlOut.WriteAttributeString("AssemblyFull", obj.GetType().Assembly.FullName);

            xmlOut.WriteStartElement("Properties");

            // we only want properties
            List<PropertyInfo> pi = ObjectSupport.GetProperties(obj.GetType());
            foreach (var p in pi) {
                if (!p.CanRead) continue;
                if (!p.CanWrite) continue;
                ParameterInfo[] parms = p.GetIndexParameters();
                if (parms.Length > 0) continue;// indexed parms can't be saved
                if (Attribute.GetCustomAttribute(p, typeof(DontSaveAttribute)) != null || Attribute.GetCustomAttribute(p, typeof(Data_CalculatedProperty)) != null || Attribute.GetCustomAttribute(p, typeof(Data_DontSave)) != null)
                    continue;

                xmlOut.WriteStartElement(p.Name);

                object o = p.GetValue(obj, null);
                SerializeOneProperty(xmlOut, o);

                xmlOut.WriteEndElement();
            }

            xmlOut.WriteEndElement();

            if (obj is Byte[])
#pragma warning disable 642 // Possible mistaken empty statement
                ; // byte array are handled as simple properties
#pragma warning restore 642
            else if (obj is IDictionary) {
                IDictionary idict = (IDictionary)obj;
                IDictionaryEnumerator denum = idict.GetEnumerator();
                denum.Reset();

                xmlOut.WriteStartElement("Dictionary");

                for ( ; ; ) {
                    if (!denum.MoveNext())
                        break;
                    object key = denum.Key;
                    object val = denum.Value;

                    xmlOut.WriteStartElement("Key");
                    SerializeOneProperty(xmlOut, key);
                    xmlOut.WriteEndElement();

                    xmlOut.WriteStartElement("Value");
                    SerializeOneProperty(xmlOut, val);
                    xmlOut.WriteEndElement();
                }
                xmlOut.WriteEndElement();
            } else if (obj is IList) {
                IList ilist = (IList)obj;
                IEnumerator lenum = ilist.GetEnumerator();
                lenum.Reset();

                xmlOut.WriteStartElement("List");

                for ( ; ; ) {
                    if (!lenum.MoveNext())
                        break;
                    object val = lenum.Current;

                    xmlOut.WriteStartElement("Value");
                    SerializeOneProperty(xmlOut, val);
                    xmlOut.WriteEndElement();
                }

                xmlOut.WriteEndElement();
            }
            xmlOut.WriteEndElement();
        }

        private void SerializeOneProperty(XmlWriter xmlOut, object o) {

            if (o == null) return;
            Type tp = o.GetType();
#if DEBUG
            if (tp.IsAbstract)
                throw new InternalError("Abstract property??? {0} is not serializable.", tp.FullName);
            if (tp.IsInterface)
                throw new InternalError("Interface {0} is not serializable.", tp.FullName);
#endif
            if (tp == typeof(Byte[])) {
                if (o != null)
                    xmlOut.WriteAttributeString("Value", Convert.ToBase64String((byte[])o));
            } else if (tp.IsArray) {
                SerializeObjectProperties(xmlOut, o);
            } else if (tp.IsEnum) {
                string val = Convert.ToInt64(o).ToString(CultureInfo.InvariantCulture);
                if (val != null)
                    xmlOut.WriteAttributeString("Value", val);
            } else if (tp == typeof(DateTime) || tp == typeof(DateTime?)) {
                xmlOut.WriteAttributeString("Value", ((DateTime)o).Ticks.ToString());
            } else if (tp == typeof(TimeSpan) || tp == typeof(TimeSpan?)) {
                xmlOut.WriteAttributeString("Value", ((TimeSpan)o).Ticks.ToString());
            } else if (tp == typeof(Guid) || tp == typeof(Guid?)) {
                string val = Convert.ToString(o, CultureInfo.InvariantCulture);
                if (val != null)
                    xmlOut.WriteAttributeString("Value", val);
            } else if (tp == typeof(System.Drawing.Image) || tp == typeof(Bitmap)) {
                if (o != null) {
                    System.Drawing.Image img = (System.Drawing.Image)o;
                    using (MemoryStream ms = new MemoryStream()) {
                        img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        xmlOut.WriteAttributeString("Value", Convert.ToBase64String(ms.ToArray()));
                    }
                }
            } else if (tp.IsValueType) {
                string val = Convert.ToString(o, CultureInfo.InvariantCulture);
                if (val != null)
                    WriteValueType(xmlOut, val);
            } else if (tp.IsClass) {
                IConvertible iconv = (o as IConvertible);
                if (iconv != null) {
                    // this object can represent itself as a string
                    string s = iconv.ToString(CultureInfo.InvariantCulture);
                    WriteValueType(xmlOut, s);
                } else {
                    SerializeObjectProperties(xmlOut, o);
                }
            } else
                throw new InternalError("Unexpected type {0} cannot be serialized.", tp.FullName);
        }

        private static void WriteValueType(XmlWriter xmlOut, string s) {
            bool ok = false;
            try {
                XmlConvert.VerifyXmlChars(s);
                ok = true;
            } catch (Exception) { }
            if (ok)
                xmlOut.WriteAttributeString("Value", s);
            else
                xmlOut.WriteAttributeString("ValueBin", Convert.ToBase64String(Encoding.UTF8.GetBytes(s)));
        }

        public object Deserialize(System.IO.Stream serializationStream) {


            //XmlReaderSettings xmlSet = new XmlReaderSettings();
            //xmlSet.IgnoreComments = true;
            //xmlSet.IgnoreWhitespace = true;
            XmlTextReader xmlIn = new XmlTextReader(serializationStream);
            xmlIn.WhitespaceHandling = WhitespaceHandling.None;

            if (!xmlIn.Read())
                throw new InternalError("Empty input.");

            object obj = null;
            if (xmlIn.IsStartElement())
                obj = DeserializeOneObject(xmlIn);

            if (!xmlIn.EOF)
                throw new InternalError("Unexpected data beyond end of input at line {0}.", xmlIn.LineNumber);

            return obj;
        }

        public object DeserializeOneObject(XmlTextReader xmlIn) {
            object obj = null;
            if (!xmlIn.IsStartElement() || xmlIn.Name != "Object")
                throw new InternalError("Unexpected element {0} {1} at line {2}.", xmlIn.NodeType.ToString(), xmlIn.Name, xmlIn.LineNumber);

            string strType = xmlIn.Attr("Type");
            string strAsm = xmlIn.Attr("Assembly");

            Type t = null;
            try {
                Assembly asm = Assemblies.Load(strAsm);
                t = asm.GetType(strType, true);
            } catch {
                t = null;
            }
            if (t == null) {
                try {
                    string strAsmFull = xmlIn.Attr("AssemblyFull");
                    Assembly asm = Assemblies.Load(strAsmFull);
                    t = asm.GetType(strType, true);
                } catch (Exception exc) {
                    throw new InternalError("{0} element at line {1} has an invalid Type attribute {2} - {3} - AssemblyFull missing or invalid.", xmlIn.Name, xmlIn.LineNumber, strType, ErrorHandling.FormatExceptionMessage(exc));
                }
            }
            xmlIn.MustRead(); // skip over Object entry

            try {
                if (t == typeof(byte[]))
                    obj = new byte[] { };
                else
                    obj = Activator.CreateInstance(t);
            } catch (Exception exc) {
                throw new InternalError("Unable to create an instance of type {0} at line {1} - {2}.", strType, xmlIn.LineNumber, ErrorHandling.FormatExceptionMessage(exc));
            }

            for ( ; ; ) {
                if (xmlIn.NodeType == XmlNodeType.EndElement) {
                    xmlIn.Read();
                    break;
                }
                if (xmlIn.NodeType != XmlNodeType.Element)
                    throw new InternalError("Unexpected node {0} {1} at line {2}.", xmlIn.Name, xmlIn.NodeType, xmlIn.LineNumber);
                if (xmlIn.IsEmptyElement) {
                    xmlIn.MustRead(); // skip over property
                } else if (xmlIn.Name == "Properties") {
                    DeserializeProperties(xmlIn, obj);
                } else if (xmlIn.Name == "List") {
                    DeserializeList(xmlIn, obj);
                } else if (xmlIn.Name == "Dictionary") {
                    DeserializeDictionary(xmlIn, obj);
                } else
                    throw new InternalError("Unexpected element {0} at line {1}.", xmlIn.Name, xmlIn.LineNumber);
            }
            return obj;
        }

        private void DeserializeProperties(XmlTextReader xmlIn, object obj)
        {
            if (!xmlIn.IsStartElement() || xmlIn.Name != "Properties")
                throw new InternalError("Unexpected element {0} {1} at line {2}.", xmlIn.NodeType.ToString(), xmlIn.Name, xmlIn.LineNumber);
            if (xmlIn.IsEmptyElement) {
                xmlIn.MustRead();
                return;
            }

            xmlIn.MustRead(); // skip over Properties entry

            Type tpObj = obj.GetType();

            for ( ; ; ) {
                if (xmlIn.NodeType == XmlNodeType.EndElement) {
                    xmlIn.MustRead();
                    break;
                }
                DeserializeOneProperty(xmlIn, obj, tpObj);
            }
        }

        private object DeserializeOneProperty(XmlTextReader xmlIn, object obj, Type tpObj, bool set = true, string expectedName = null) {
            if (xmlIn.NodeType != XmlNodeType.Element)
                throw new InternalError("Unexpected node {0} {1} at line {2}.", xmlIn.Name, xmlIn.NodeType, xmlIn.LineNumber);

            string propName = xmlIn.Name; // property name
            if (expectedName != null && expectedName != propName)
                throw new InternalError("Expected element {0} at line {1}.", expectedName, xmlIn.LineNumber);

            object objVal = null;
            PropertyInfo pi = null;
            if (set) {
                pi = ObjectSupport.TryGetProperty(tpObj, propName);
                if (pi == null) {
                    //Logging.AddLog("Element found for non-existent property {0}.", propName);
                    //throw new InternalError("Element found for non-existent property {0}.", propName);
                    // This is OK as it can happen when data models change
                }
            }

            if (xmlIn.IsEmptyElement) {
                // simple value
                string strVal = null;
                if (xmlIn["Value"] != null) {
                    strVal = xmlIn.Attr("Value"); // property value
                } else if (!string.IsNullOrEmpty(xmlIn["ValueBin"])) {
                    strVal = xmlIn.Attr("ValueBin"); // property value
                    strVal = Encoding.UTF8.GetString(Convert.FromBase64String(strVal));
                }
                if (set && pi != null) {
                    bool fail = false;
                    string failMsg = null;

                    try {
                        if (pi.PropertyType == typeof(Byte[])) {
                            if (strVal == null)
                                objVal = new byte[] { };
                            else
                                objVal = Convert.FromBase64String(strVal);
                        } else if (strVal == null) {
                            objVal = null;
                        } else if (pi.PropertyType.IsEnum) {
                            objVal = Convert.ChangeType(strVal, typeof(long), CultureInfo.InvariantCulture);
                            objVal = Enum.ToObject(pi.PropertyType, objVal);
                        } else if (pi.PropertyType == typeof(DateTime) || pi.PropertyType == typeof(DateTime?)) {
                            long ticks = (long)Convert.ChangeType(strVal, typeof(long), CultureInfo.InvariantCulture);
                            objVal = new DateTime(ticks);
                        } else if (pi.PropertyType == typeof(Guid) || pi.PropertyType == typeof(Guid?)) {
                            if (!string.IsNullOrWhiteSpace(strVal))
                                objVal = new Guid(strVal);
                        } else if (pi.PropertyType == typeof(TimeSpan) || pi.PropertyType == typeof(TimeSpan?)) {
                            objVal = new TimeSpan((long)Convert.ToInt64(strVal));
                        } else if (pi.PropertyType == typeof(System.Drawing.Image) || pi.PropertyType == typeof(Bitmap)) {
                            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(strVal))) {
                                objVal = System.Drawing.Image.FromStream(ms);
                            }
                        } else {
                            objVal = Convert.ChangeType(strVal, pi.PropertyType, CultureInfo.InvariantCulture);
                        }
                        pi.SetValue(obj, objVal, null);
                    } catch (Exception exc) {
                        fail = true;
                        failMsg = ErrorHandling.FormatExceptionMessage(exc);
                    }
                    if (fail) {
                        // try using a constructor (types like Guid can't simply be assigned)
                        ConstructorInfo ci = pi.PropertyType.GetConstructor(new Type[] { typeof(string) });
                        if (ci == null) {
#if DEBUG
                            if (propName == "..something...") // use this for specific debugging
                                goto skip;
#endif
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
#if DEBUG
skip: ;
#endif
                xmlIn.MustRead();

            } else {
                // complex value (must be Object)
                if (xmlIn["Value"] != null)
                    throw new InternalError("Unexpected Value attribute on element {0} {1} at line {2}.", xmlIn.Name, xmlIn.NodeType, xmlIn.LineNumber);

                xmlIn.MustRead();// skip over property name element

                objVal = DeserializeOneObject(xmlIn);
                if (set && pi != null) {
                    try {
                        pi.SetValue(obj, objVal, null);
                    } catch (Exception exc) {
                        throw new InternalError("Element for property {0} has an invalid value - {1}.", propName, ErrorHandling.FormatExceptionMessage(exc));
                    }
                }

                if (xmlIn.NodeType != XmlNodeType.EndElement)
                    throw new InternalError("Ending element not found for property {0}.", propName);

                xmlIn.MustRead();
            }
            return objVal;
        }

        private void DeserializeDictionary(XmlTextReader xmlIn, object obj) {
            if (!xmlIn.IsStartElement() || xmlIn.Name != "Dictionary")
                throw new InternalError("Unexpected element {0} {1} at line {2}.", xmlIn.NodeType.ToString(), xmlIn.Name, xmlIn.LineNumber);
            if (xmlIn.IsEmptyElement) {
                xmlIn.MustRead();
                return;
            }

            xmlIn.MustRead(); // skip over Dictionary entry

            Type tpObj = obj.GetType();

            for ( ; ; ) {
                if (xmlIn.NodeType == XmlNodeType.EndElement) {
                    xmlIn.MustRead();
                    break;
                }
                object objKey = DeserializeOneProperty(xmlIn, obj, tpObj, false, "Key");
                object objVal = DeserializeOneProperty(xmlIn, obj, tpObj, false, "Value");

                MethodInfo mi = tpObj.GetMethod("Add", new Type[] { typeof(object), typeof(object) });
                if (mi == null)
                    throw new InternalError("Dictionary type {0} doesn't implement the required void Add(object,object) method.", tpObj.Name);
                try {
                    mi.Invoke(obj, new object[] { objKey, objVal });
                } catch (Exception exc) {
                    throw new InternalError("Couldn't add new entry to dictionary type {0} - {1}.", tpObj.Name, ErrorHandling.FormatExceptionMessage(exc));
                }
            }
        }
        private void DeserializeList(XmlTextReader xmlIn, object obj) {
            if (!xmlIn.IsStartElement() || xmlIn.Name != "List")
                throw new InternalError("Unexpected element {0} {1} at line {2}.", xmlIn.NodeType.ToString(), xmlIn.Name, xmlIn.LineNumber);
            if (xmlIn.IsEmptyElement) {
                xmlIn.MustRead();
                return;
            }

            xmlIn.MustRead(); // skip over List entry

            Type tpObj = obj.GetType();

            for ( ; ; ) {
                if (xmlIn.NodeType == XmlNodeType.EndElement) {
                    xmlIn.MustRead();
                    break;
                }
                object objVal = DeserializeOneProperty(xmlIn, obj, tpObj, false, "Value");

                MethodInfo mi = tpObj.GetMethod("Add", new Type[] { typeof(object) });
                if (mi == null)
                    throw new InternalError("List type {0} doesn't implement the required void Add(object,object) method.", tpObj.Name);
                try {
                    mi.Invoke(obj, new object[] { objVal });
                } catch (Exception exc) {
                    throw new InternalError("Couldn't add new entry to list type {0} - {1}.", tpObj.Name, ErrorHandling.FormatExceptionMessage(exc));
                }
            }
        }
    }
}
