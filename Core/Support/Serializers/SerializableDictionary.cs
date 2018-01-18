/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace YetaWF.Core.Serializers {

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "Not used for serialization")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue> {

        public SerializableDictionary() { }
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) {}
        public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
        public SerializableDictionary(int capacity) : base(capacity) { }
        public SerializableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }
        public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }
        protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods",
            Justification = "The deserialization (e.g., TextFormatter, SimpleFormatter) uses generic Add() instead of typed as it simplifies deserialization")]
        public void Add(object k, object v) // for TextFormatter/SimpleFormatter
        {
            TKey key;
            try {
                key = (TKey)k;
            } catch (Exception) {
                key = (TKey)Activator.CreateInstance(typeof(TKey), new object[] { k });
            }
            TValue val;
            try {
                val = (TValue)v;
            } catch (Exception) {
                val = (TValue)Activator.CreateInstance(typeof(TValue), new object[] { v });
            }
            base.Add(key, (TValue)val);
        }
    }
}
