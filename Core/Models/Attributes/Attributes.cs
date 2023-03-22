/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    public class Resources { } // class holding all localization resources for attributes

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    // identifies a property that is never saved by a serializer - Serializers save all properties (not fields)
    public class DontSaveAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PermissionAttribute : Attribute {
        public PermissionAttribute(string level) { Level = level; }
        public string Level { get; private set; }
    }

    /// <summary>
    /// Marks a method as unavailable in Demo mode.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ExcludeDemoModeAttribute : Attribute {
        public ExcludeDemoModeAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CopyAttribute : Attribute { } // copied value (when saving edited properties, this value is copied from original)

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class EnumDescriptionAttribute : Attribute {
        public EnumDescriptionAttribute(string caption, string? Description = null) {
            Caption = caption;
            this.Description = Description;
        }
        public string Caption { get; private set; }
        public string? Description { get; private set; }

        public static string GetStringValue(object value) {
            EnumData enumData = ObjectSupport.GetEnumData(value.GetType());
            EnumDataEntry? entry = enumData.FindValue(value);
            if (entry == null)
                return "";
            return entry.Caption;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class UIHintAttribute : System.ComponentModel.DataAnnotations.UIHintAttribute {
        public UIHintAttribute(string uiHint) : base(TranslateHint(uiHint)) { }
        public UIHintAttribute(string uiHint, string presentationLayer) : base(TranslateHint(uiHint), presentationLayer) { }
        public UIHintAttribute(string uiHint, string presentationLayer, params object[] controlParameters) : base(TranslateHint(uiHint), presentationLayer, controlParameters) { }

        public static string TranslateHint(string uiHint) {
            return uiHint;
        }
    }

    /// <summary>
    /// Used with modules to define a module's unique identifier.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ModuleGuidAttribute : Attribute {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="guid">The module's unique identitier.</param>
        public ModuleGuidAttribute(string guid) { Value = new Guid(guid); }
        /// <summary>
        /// Returns the module's unique identitier.
        /// </summary>
        public Guid Value { get; private set; }
    }

    /// <summary>
    /// Used with modules to indicate that the module's unique identifier (ModuleGuid) is a "published" identifier.
    /// These identifiers are guaranteed to remain the same and can be used by external assemblies/modules to create modules dynamically.
    /// </summary>
    /// <remarks>This attribute is used for documentation generation purposes only.</remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PublishedModuleGuidAttribute : Attribute {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PublishedModuleGuidAttribute() { }
    }

    public enum UniqueModuleStyle {
        NonUnique, // must be designed
        UniqueOnly, // must be unique
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class UniqueModuleAttribute : Attribute {
        public UniqueModuleAttribute(UniqueModuleStyle uniqueStyle = UniqueModuleStyle.UniqueOnly) { Value = uniqueStyle; }
        public UniqueModuleStyle Value { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TrimAttribute : MoreMetadataAttribute {
        public enum EnumStyle { Both = 0, Left, Right, None }
        public TrimAttribute(EnumStyle style = EnumStyle.Both) : base("Trim", style) { }
        public new EnumStyle Value { get { return (EnumStyle)base.Value; } }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CaseAttribute : MoreMetadataAttribute {
        public enum EnumStyle { Upper = 0, Lower }
        public CaseAttribute(EnumStyle style = EnumStyle.Upper) : base("Case", style) { }
        public new EnumStyle Value { get { return (EnumStyle)base.Value; } }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ReadOnlyAttribute : MoreMetadataAttribute, IPropertyValidationFilter {
        public ReadOnlyAttribute() : base("ReadOnly", true) { }

        public bool ShouldValidateEntry(ValidationEntry entry, ValidationEntry parentEntry) {
            return false;
        }
    }
    /// <summary>
    /// Used with tabbed property lists to identify with which tab(s) the property is associated.
    /// </summary>
    /// <remarks>When specifying multiple categories, the property will appear on each tab listed.
    /// Multiple categories should only be used with read/only properties, otherwise fields with identical names are generated multiple times.
    /// This attribute does not test for duplicate categories or whether only read/only fields are used when specifying multiple categories.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CategoryAttribute : Attribute {
        /// <summary>
        /// Defines the list of categories (i.e., tabs) where a property will be shown.
        /// </summary>
        public List<string> Categories { get; private set; }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="categories">The list of categories where a property will be shown. Typically only one category is used.</param>
        public CategoryAttribute(params string[] categories) {
            if (categories != null)
                Categories = categories.ToList();
            else
                Categories = new List<string>();
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DescriptionAttribute : MoreMetadataAttribute {
        public DescriptionAttribute(string description, [CallerLineNumber]int order = 0) : base("Description", description) { Order = order; }
        public new string Value { get { return (string)base.Value; } }
        public int Order { get; set; }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class HelpLinkAttribute : MoreMetadataAttribute {
        public HelpLinkAttribute(string url) : base("HelpLink", url) { }
        public new string Value { get { return (string)base.Value; } }
        public int Order { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TextAboveAttribute : MoreMetadataAttribute {
        public TextAboveAttribute(string description) : base("TextAbove", description) { }
        public new string Value { get { return (string)base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TextBelowAttribute : MoreMetadataAttribute {
        public TextBelowAttribute(string description) : base("TextBelow", description) { }
        public new string Value { get { return (string)base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SuppressEmptyAttribute : MoreMetadataAttribute {
        public SuppressEmptyAttribute(bool suppressed = true) : base("SuppressEmpty", suppressed) { }
        public new string Value { get { return (string)base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SubmitFormOnChangeAttribute : MoreMetadataAttribute {
        public enum SubmitTypeEnum {
            None = 0,
            Submit = 1,
            Apply = 2,
            Reload = 3,
        }
        public SubmitFormOnChangeAttribute(SubmitTypeEnum submit = SubmitTypeEnum.Submit) : base("SubmitFormOnChange", submit) { }
        public new SubmitTypeEnum Value { get { return (SubmitTypeEnum)base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CaptionAttribute : MoreMetadataAttribute {
        public CaptionAttribute(string caption) : base("Caption", caption) { }
        public new string Value { get { return (string)base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class HeaderAttribute : MoreMetadataAttribute {
        public HeaderAttribute(string text) : base("Header", text) { }
        public new string Value { get { return (string)base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class FooterAttribute : MoreMetadataAttribute {
        public FooterAttribute(string text) : base("Footer", text) { }
        public new string Value { get { return (string)base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class LegendAttribute : MoreMetadataAttribute {
        public LegendAttribute(string text) : base("Legend", text) { }
        public new string Value { get { return (string)base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TemplateActionAttribute : MoreMetadataAttribute {
        public TemplateActionAttribute(string name) : base("TemplateAction", name) { }
        public new string Value { get { return (string)base.Value; } }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class MoreMetadataAttribute : Attribute, IAdditionalAttribute {
        private object _typeId = new object();

        public MoreMetadataAttribute(string name, object value) {
            if (name == null)
                throw new ArgumentNullException("name");
            Name = name;
            Value = value;
        }

        public override object TypeId {
            get { return _typeId; }
        }

        public string Name { get; private set; }
        public object Value { get; private set; }

        public void OnAddAdditionalValues(IDictionary<object, object> additionalValues) {
            additionalValues[Name] = Value;
        }
    }
}
