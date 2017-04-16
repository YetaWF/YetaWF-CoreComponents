/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6
using System.Collections.Generic;
#else
using System.Configuration;
#endif

namespace YetaWF.Core.Language {

#if MVC6
    public class LanguageSection {

        public LanguageEntryElementCollection Languages { get; set; }

        private static LanguageSection _settings;

        public static void Init(LanguageSection settings) {
            _settings = settings;
        }
        public static LanguageSection GetLanguageSection() {
            return _settings;
        }
    }
    public class LanguageEntryElementCollection : List<LanguageEntryElement> { }
    public class LanguageEntryElement {
        public string Id { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
    }
#else
    public class LanguageSection : ConfigurationSection {

        public static LanguageSection GetLanguageSection(System.Configuration.Configuration config = null) {
            return (LanguageSection) System.Configuration.ConfigurationManager.GetSection("YetaWF/LanguageSection");
        }

        public override bool IsReadOnly() { return false; }

        [ConfigurationProperty("Languages", IsDefaultCollection = true, IsKey = false, IsRequired = true)]
        public LanguageEntryElementCollection Languages {
            get {
                LanguageEntryElementCollection coll = (LanguageEntryElementCollection) base["Languages"];
                if (coll == null)
                    coll = new LanguageEntryElementCollection();
                return coll;
            }
            set {
                base["Languages"] = value;
            }
        }
        public LanguageEntryElement GetLanguageFromID(string id) {
            if (Languages != null) {
                foreach (var lang in Languages) {
                    LanguageEntryElement le = (LanguageEntryElement) lang;
                    if (le.Id == id)
                        return le;
                }
            }
            return null;
        }
        //public LanguageEntryElement GetElementFromExtension(string strExtension) {
        //    if (MimeTypes != null) {
        //        foreach (var mt in MimeTypes) {
        //            LanguageEntryElement me = (LanguageEntryElement)mt;
        //            if (me.Extensions.ToLower().Contains(strExtension+";") || me.Extensions.ToLower().EndsWith(strExtension))
        //                return me;
        //        }
        //    }
        //    return null;
        //}
        //public LanguageEntryElement GetElementFromContentType(string contentType) {
        //    contentType = contentType.ToLower();
        //    var v = (from LanguageEntryElement entry in MimeTypes where entry.Type.ToLower() == contentType select entry).FirstOrDefault();
        //    if (v != null)
        //        return (LanguageEntryElement)v;
        //    return null;
        //}
    }

    public class LanguageEntryElementCollection : ConfigurationElementCollection {

        public override bool IsReadOnly() { return false; }

        public bool AddElement(LanguageEntryElement le) {
            try {
                BaseAdd(le, true);
                return true;
            } catch {
                return false;
            }
        }
        public void ReplaceElement(LanguageEntryElement le) {
            RemoveElement(le.Id);
            AddElement(le);
        }
        public void RemoveElement(string id) {
            try {
                BaseRemove(id);
            } catch { }
        }

        protected override ConfigurationElement CreateNewElement() {
            return new LanguageEntryElement();
        }
        protected override object GetElementKey(ConfigurationElement element) {
            return ((LanguageEntryElement) element).Id;
        }
    }
    public class LanguageEntryElement : ConfigurationElement {
        [ConfigurationProperty("Id", IsKey = true, IsRequired = true)]
        public string Id {
            get {
                return (string) this["Id"];
            }
            set {
                this["Id"] = value;
            }
        }
        [ConfigurationProperty("ShortName", IsKey = false, IsRequired = true)]
        public string ShortName {
            get {
                return (string) this["ShortName"];
            }
            set {
                this["ShortName"] = value;
            }
        }
        [ConfigurationProperty("Description", IsKey = false, IsRequired = false)]
        public string Description {
            get {
                return (string) this["Description"];
            }
            set {
                this["Description"] = value;
            }
        }
    }
#endif
}
