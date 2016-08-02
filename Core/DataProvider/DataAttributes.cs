/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

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
    public class Data_BinaryAttribute : MoreMetadataAttribute {

        public static string AttributeName { get { return "Data_Binary"; } }

        public Data_BinaryAttribute() : base(AttributeName, true) { }
        public new bool Value { get { return true; } }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Data_NewValue : MoreMetadataAttribute {

        public static string AttributeName { get { return "Data_NewValue"; } }

        public Data_NewValue(string value) : base(AttributeName, value) { }
        public new string Value { get { return (string)base.Value; } }
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
