/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.DataProvider.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Data_PrimaryKey : MoreMetadataAttribute {

        public static string AttributeName { get { return "Data_PrimaryKey"; } }

        public Data_PrimaryKey() : base(AttributeName, true) { }
        public new bool Value { get { return true; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Data_PrimaryKey2 : MoreMetadataAttribute {

        public static string AttributeName { get { return "Data_PrimaryKey2"; } }

        public Data_PrimaryKey2() : base(AttributeName, true) { }
        public new bool Value { get { return true; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Data_Identity : MoreMetadataAttribute {

        public static string AttributeName { get { return "Data_Identity"; } }

        public Data_Identity() : base(AttributeName, true) { }
        public new bool Value { get { return true; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Data_IndexAttribute : MoreMetadataAttribute {

        public static string AttributeName { get { return "Data_Index"; } }

        public Data_IndexAttribute() : base(AttributeName, true) { }
        public new bool Value { get { return true; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Data_UniqueAttribute : MoreMetadataAttribute {

        public static string AttributeName { get { return "Data_Unique"; } }

        public Data_UniqueAttribute() : base(AttributeName, true) { }
        public new bool Value { get { return true; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Data_BinaryAttribute : MoreMetadataAttribute {

        public static string AttributeName { get { return "Data_Binary"; } }

        public Data_BinaryAttribute() : base(AttributeName, true) { }
        public new bool Value { get { return true; } }
    }

    /// <summary>
    /// Used in data models to identity properties that are new and must be added.
    /// </summary>
    /// <remarks>When updating existing data tables, the default value is 0/null in all cases. This cannot be changed.
    ///
    /// This attribute enforces that any new properties are properly marked, so no properties are accidentally saved.
    /// If the attribute is missing, the model will fail to update any data tables.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Data_NewValue : MoreMetadataAttribute {

        public static string AttributeName { get { return "Data_NewValue"; } }

        [Obsolete("Warning: Default values are no longer used. All new properties are added with a default constraint of 0/null.")]
        public Data_NewValue(string value) : base(AttributeName, value) { }

        public Data_NewValue() : base(AttributeName, null) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Data_DontSave : MoreMetadataAttribute {

        public static string AttributeName { get { return "Data_DontSave"; } }

        public Data_DontSave() : base(AttributeName, true) { }
        public new string Value { get { return (string)base.Value; } }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Data_CalculatedProperty : MoreMetadataAttribute {
        public static string AttributeName { get { return "Data_CalculatedProperty"; } }
        public Data_CalculatedProperty() : base(AttributeName, true) { }
        public new string Value { get { return (string)base.Value; } }
    }

}
