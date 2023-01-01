/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;

namespace YetaWF.Core.Support.Repository {

    public enum ObjectType {
        Site = 0,
        Page = 1,
        Module = 2,
        ModuleInstance = 3,
    }

    public class SessionSettings {

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private List<SettingsDictionary>? _loadedDicts;
        internal List<SettingsDictionary> LoadedDicts {
            get {
                if (_loadedDicts == null)
                    _loadedDicts = new List<SettingsDictionary>();
                return _loadedDicts;
            }
        }

        private SettingsDictionary CreateDictionary(ObjectType objectType, string objectName) {
            objectName = objectName.ToLower();
            SettingsDictionary? dict = FindLoadedDictionary(objectType, objectName);
            if (dict != null) return dict;

            dict = SettingsDictionary.Load(objectType, objectName);
            LoadedDicts.Add(dict);
            return dict;
        }

        private SettingsDictionary? FindLoadedDictionary(ObjectType objectType, string objectName) {
            foreach (var d in LoadedDicts) {
                if (d.ObjectType == objectType && d.ObjectName == objectName)
                    return d;
            }
            return null;
        }

        /// <summary>
        /// User-specific site settings
        /// </summary>
        public SettingsDictionary SiteSettings {
            get {
                return CreateDictionary(ObjectType.Site, "Site");
            }
        }

        /// <summary>
        /// User-specific page settings
        /// </summary>
        public SettingsDictionary? GetPageSettings(string pageName) {
            if (pageName == null) return null;
            return CreateDictionary(ObjectType.Page, pageName);
        }

        /// <summary>
        /// User-specific module settings
        /// </summary>
        public SettingsDictionary GetModuleSettings(Guid moduleGuid) {
            return CreateDictionary(ObjectType.Module, moduleGuid.ToString());
        }

        /// <summary>
        /// User-specific, module specific settings
        /// </summary>
        public SettingsDictionary GetModuleInstanceSettings(string moduleGuid) {
            return CreateDictionary(ObjectType.ModuleInstance, moduleGuid);
        }

        public void SaveUpdates() {
            foreach (var d in LoadedDicts) {
                d.Save();
            }
        }

        /// <summary>
        /// Clear all session settings
        /// </summary>
        public void ClearAll(bool forceAll = false) {
            _loadedDicts = null;
            if (forceAll) {
                Manager.CurrentSession.Clear();
            } else {
                // clear all session settings except those marked permanent (superuser, temp invoice. etc.)
                List<string> keys = (from string k in Manager.CurrentSession.Keys where !k.StartsWith(Globals.Session_Permanent) select k).ToList();
                foreach (var key in keys)
                    SettingsDictionary.ClearAllTempItems(key);
            }
        }
    }
}
