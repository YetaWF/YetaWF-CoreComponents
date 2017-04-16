/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using Microsoft.Extensions.Options;
using System.Collections.Generic;
#else
using System.Configuration;
#endif
using System.Linq;


namespace YetaWF.Core.Upload {

#if MVC6
    public class MimeSection {

        private static MimeSection _settings;

        public List<MimeEntry> MimeTypes { get; set; }

        public static void Init(MimeSection settings) {
            _settings = settings;
        }
        public static MimeSection GetMimeSection() {
            return _settings;
        }

        public string GetContentTypeFromExtension(string strExtension) {
            MimeEntry me = GetElementFromExtension(strExtension);
            if (me == null)
                return null;
            return me.Type;
        }
        public MimeEntry GetElementFromExtension(string strExtension) {
            if (MimeTypes != null) {
                foreach (var mt in MimeTypes) {
                    if (mt.Extensions.ToLower().Contains(strExtension + ";") || mt.Extensions.ToLower().EndsWith(strExtension))
                        return mt;
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

    public class MimeEntry {
        public string Type { get; set; }
        public string Extensions { get; set; }
        public bool ImageUse { get; set; }
        public bool FlashUse { get; set; }
        public bool FileUse { get; set; }
        public bool PackageUse { get; set; }
    }
#else
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
#endif
}
