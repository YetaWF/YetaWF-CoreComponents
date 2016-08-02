/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Configuration;
using System.Linq;

namespace YetaWF.Core.Upload {

    public class MimeSection : ConfigurationSection {

        public static MimeSection GetMimeSection() {
            return (MimeSection)System.Configuration.ConfigurationManager.GetSection("YetaWF/MimeSection");
        }

        [ConfigurationProperty("MimeTypes", IsDefaultCollection=true, IsKey=false, IsRequired=true)]
        public MimeEntryCollection MimeTypes {
            get {
                return (MimeEntryCollection)base["MimeTypes"];
            }
            set {
                base["MimeTypes"] = value;
            } 
        }

        public string GetContentTypeFromExtension(string strExtension)
        {
            MimeEntry me = GetElementFromExtension(strExtension);
            if (me == null)
                return null;
            return me.Type;
        }
        public MimeEntry GetElementFromExtension(string strExtension) {
            if (MimeTypes != null) {
                foreach (var mt in MimeTypes) {
                    MimeEntry me = (MimeEntry)mt;
                    if (me.Extensions.ToLower().Contains(strExtension+";") || me.Extensions.ToLower().EndsWith(strExtension))
                        return me;
                }
            }
            return null;
        }
        public MimeEntry GetElementFromContentType(string contentType) {
            contentType = contentType.ToLower();
            var v = (from MimeEntry entry in MimeTypes where entry.Type.ToLower() == contentType select entry).FirstOrDefault();
            if (v != null)
                return v;
            return null;
        }
    }

    public class MimeEntryCollection : ConfigurationElementCollection {
        protected override ConfigurationElement CreateNewElement() {
            return new MimeEntry();
        }

        protected override object GetElementKey(ConfigurationElement element) {
            return ((MimeEntry)element).Type;
        }
    }
    public class MimeEntry : ConfigurationElement 
    {
        [ConfigurationProperty("Type", IsKey=true , IsRequired=true)]
        public string Type {
            get { 
                return (string)this["Type"]; 
            }
            set { 
                this["Type"] = value; 
            }
        }

        [ConfigurationProperty("Extensions", IsKey=false, IsRequired=true)]
        public string Extensions {
            get { 
                return (string)this["Extensions"]; 
            }
            set { 
                this["Extensions"] = value; 
            }
        }

        [ConfigurationProperty("ImageUse", IsKey=false, IsRequired=false)]
        public bool ImageUse {
            get {
                return (bool)this["ImageUse"];
            }
            set {
                this["ImageUse"] = value;
            }
        }
        [ConfigurationProperty("FlashUse", IsKey=false, IsRequired=false)]
        public bool FlashUse {
            get {
                return (bool)this["FlashUse"];
            }
            set {
                this["FlashUse"] = value;
            }
        }
        [ConfigurationProperty("FileUse", IsKey=false, IsRequired=false)]
        public bool FileUse {
            get {
                return (bool)this["FileUse"];
            }
            set {
                this["FileUse"] = value;
            }
        }
        [ConfigurationProperty("PackageUse", IsKey=false, IsRequired=false)]
        public bool PackageUse {
            get {
                return (bool)this["PackageUse"];
            }
            set {
                this["PackageUse"] = value;
            }
        }
    } 
}
