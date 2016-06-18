/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Runtime.CompilerServices;
using System.Web.Mvc;
using YetaWF.Core.Addons;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    public class Resources { } // class holding all localization resources for attributes

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    // identifies a property that is never saved by a serializer - Serializers save all properties (not fields)
    public class DontSaveAttribute : Attribute, IMetadataAware {
        public void OnMetadataCreated(ModelMetadata metadata) {
            if (metadata == null)
                throw new ArgumentNullException("metadata");
            metadata.AdditionalValues["DontSave"] = true;
        }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PermissionAttribute : Attribute {
        public PermissionAttribute(string level) { Level = level; }
        public string Level { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CopyAttribute : Attribute { } // copied value (when saving edited properties, this value is copied from original)

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class EnumDescriptionAttribute : Attribute {
        public EnumDescriptionAttribute(string caption, string desc = null) {
            Caption = caption;
            Description = desc;
        }
        public string Caption { get; set; }
        public string Description { get; set; }

        public static string GetStringValue(object value) {
            EnumData enumData = ObjectSupport.GetEnumData(value.GetType());
            EnumDataEntry entry = enumData.FindValue(value);
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

        protected static YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        protected static bool HaveManager { get { return YetaWFManager.HaveManager; } }

        public static string TranslateHint(string uiHint) {
            if (!HaveManager) return uiHint;
            if (uiHint == "Grid") {
                return "GridjqGrid";
            } else if (uiHint == "GridDataRecords") {
                return "GridDataRecordsjqGrid";
            } else if (uiHint == "FileUpload1") {
                if (Manager.CurrentSite.FileUploadStyle == Site.FileUploadStyleEnum.Kendo) {
                    if (VersionManager.KendoAddonType == VersionManager.KendoAddonTypeEnum.Pro)
                        return "FileUploadKendo1";
                }
                return "FileUploadDanielm1";
            }
            return uiHint;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ModuleGuidAttribute : Attribute {
        public ModuleGuidAttribute(string guid) { Value = new Guid(guid); }
        public Guid Value { get; private set; }
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
        public TrimAttribute(EnumStyle style = EnumStyle.Both) : base("Trim", (object) style) { }
        public new EnumStyle Value { get { return (EnumStyle) base.Value; } }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CaseAttribute : MoreMetadataAttribute {
        public enum EnumStyle { Upper = 0, Lower }
        public CaseAttribute(EnumStyle style = EnumStyle.Upper) : base("Case", (object)style) { }
        public new EnumStyle Value { get { return (EnumStyle)base.Value; } }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ReadOnlyAttribute : MoreMetadataAttribute {
        public ReadOnlyAttribute() : base("ReadOnly", (object) true) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CategoryAttribute : MoreMetadataAttribute {
        public CategoryAttribute(string category) : base("Category", (object) category) { }
        public new string Value { get { return (string) base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DescriptionAttribute : MoreMetadataAttribute {
        public DescriptionAttribute(string description, [CallerLineNumber]int order = 0) : base("Description", (object) description) { Order = order; }
        public new string Value { get { return (string) base.Value; } }
        public int Order { get; set; }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class HelpLinkAttribute : MoreMetadataAttribute {
        public HelpLinkAttribute(string url) : base("HelpLink", (object) url) { }
        public new string Value { get { return (string) base.Value; } }
        public int Order { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TextAboveAttribute : MoreMetadataAttribute {
        public TextAboveAttribute(string description) : base("TextAbove", (object) description) { }
        public new string Value { get { return (string) base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TextBelowAttribute : MoreMetadataAttribute {
        public TextBelowAttribute(string description) : base("TextBelow", (object) description) { }
        public new string Value { get { return (string) base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SuppressEmptyAttribute : MoreMetadataAttribute {
        public SuppressEmptyAttribute(bool suppressed = true) : base("SuppressEmpty", suppressed) { }
        public new string Value { get { return (string) base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SubmitFormOnChangeAttribute : MoreMetadataAttribute {
        public enum SubmitTypeEnum {
            None = 0,
            Submit = 1,
            Apply = 2,
        }
        public SubmitFormOnChangeAttribute(SubmitTypeEnum submit = SubmitTypeEnum.Submit) : base("SubmitFormOnChange", submit) { }
        public new SubmitTypeEnum Value { get { return (SubmitTypeEnum) base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CaptionAttribute : MoreMetadataAttribute {
        public CaptionAttribute(string caption) : base("Caption", (object) caption) { }
        public new string Value { get { return (string) base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class HeaderAttribute : MoreMetadataAttribute {
        public HeaderAttribute(string text) : base("Header", (object) text) {  }
        public new string Value { get { return (string)base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class FooterAttribute : MoreMetadataAttribute {
        public FooterAttribute(string text) : base("Footer", (object) text) {  }
        public new string Value { get { return (string)base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class LegendAttribute : MoreMetadataAttribute {
        public LegendAttribute(string text) : base("Legend", (object) text) { }
        public new string Value { get { return (string) base.Value; } }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TemplateActionAttribute : MoreMetadataAttribute {
        public TemplateActionAttribute(string name) : base("TemplateAction", name) { }
        public new string Value { get { return (string) base.Value; } }
    }

    public enum GridHAlignmentEnum {
        Unspecified = -1, Left = 0, Center = 1, Right = 2
    }

    public class MoreMetadataAttribute : Attribute, IMetadataAware {
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

        public void OnMetadataCreated(ModelMetadata metadata) {
            if (metadata == null)
                throw new ArgumentNullException("metadata");
            metadata.AdditionalValues[Name] = Value;
        }
    }

}
