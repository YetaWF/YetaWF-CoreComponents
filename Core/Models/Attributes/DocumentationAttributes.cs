/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System;

namespace YetaWF.Core.Models.Attributes {

    /// <summary>
    /// Used to document the category of a module. E.g., Configuration, Comments, etc.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ModuleCategoryAttribute : Attribute {
        public ModuleCategoryAttribute(string name) : base() {
            Name = name;
        }
        public string Name { get; private set; }
    }
    /// <summary>
    /// Used to document URL Query String arguments used by the module.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ModuleUsesQSArgs : Attribute {
        public ModuleUsesQSArgs(string name, string typeString, string description) : base() {
            Name = name;
            Type = typeString;
            Description = description;
        }
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string Description { get; private set; }
    }


    /// <summary>
    /// Used to document AdditionalMetadata attributes available for use with a component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class,AllowMultiple = true, Inherited = true)]
    public class UsesAdditionalAttribute : Attribute {
        public UsesAdditionalAttribute(string name, string typeString, string defaultValue, string description) : base() {
            Name = name;
            Type = typeString;
            Default = defaultValue;
            Description = description;
        }
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string Default { get; private set; }
        public string Description { get; private set; }
    }

    /// <summary>
    /// Used to document sibling properties available for use with a component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class UsesSiblingAttribute : Attribute {
        public UsesSiblingAttribute(string name, string typeString, string description) : base() {
            Name = name;
            Type = typeString;
            Description = description;
        }
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string Description { get; private set; }
    }

    /// <summary>
    /// Indicates that this component is only for use by a package and is not intended for use by an application.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PrivateComponentAttribute : Attribute {
        public PrivateComponentAttribute() : base() { }
    }
}
