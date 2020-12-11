/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;

namespace YetaWF.Core.Support {

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class AdditionalMetadataAttribute : Attribute {

        public AdditionalMetadataAttribute(string name, object value) {
            Name = name;
            Value = value;
        }

        public string Name { get; private set; }
        public object? Value { get; private set; }
    }
}

