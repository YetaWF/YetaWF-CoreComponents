/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Serializers {

    [Serializable]
    public class SerializableList<Type> : List<Type> {

        public SerializableList() { }
        public SerializableList(Type[] val) : base(val) { }
        public SerializableList(List<Type> val) : base(val) { }
        public SerializableList(IEnumerable<Type> val) : base(val) { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods",
            Justification = "The deserialization (e.g., TextFormatter, SimpleFormatter) uses generic Add() instead of typed as it simplifies deserialization")]
        public void Add(object o) // for TextFormatter
        {
             Type val;
             try {
                 if (typeof(Type) == typeof(int))
                     val = (Type)(object)Convert.ToInt32(o);
                 else if (typeof(Type) == typeof(Guid))
                    val = (Type)(object)new Guid(o.ToString());
                else
                    val = (Type)o;
             } catch (Exception) {
                 val = (Type)Activator.CreateInstance(typeof(Type), new object[] { o });
             }
             base.Add((Type)val);
        }

        [DontSave]
        public new int Capacity { 
            get {
                return base.Capacity;
            }
            set {
                base.Capacity = value;
            }
        }
    }
}
