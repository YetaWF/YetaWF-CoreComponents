﻿/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using System;

namespace YetaWF.Core.Support {

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class AdditionalMetadataAttribute : Attribute {

        public AdditionalMetadataAttribute(string name, object value) {
            if (name == null) {
                throw new ArgumentNullException("name");
            }
            Name = name;
            Value = value;
        }

        public string Name { get; private set; }
        public object Value { get; private set; }
    }
}

#else
#endif
