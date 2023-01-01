/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using System.Linq;
using YetaWF.Core.IO;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Serializers;

namespace YetaWF.Core.Support.Repository {

    public class SettingsDictionary : SerializableDictionary<string, Setting> {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        public SettingsDictionary() { } // public for serialization

        [DontSave]
        public ObjectType ObjectType { get; private set; }
        [DontSave]
        public string ObjectName { get; private set; } = null!;
        [DontSave]
        public bool Modified { get; set; }

        public static SettingsDictionary Load(ObjectType objectType, string objectName) {

            SessionStateIO<SettingsDictionary> session = new SessionStateIO<SettingsDictionary> {
                Key = string.Format("Setting_{0}_{1}", ((int) objectType), objectName),
            };
            SettingsDictionary? settings = session.Load();
            if (settings == null)
                settings = new SettingsDictionary();
            settings.Modified = false;
            settings.ObjectType = objectType;
            settings.ObjectName = objectName;
            return settings;
        }

        public void Save() {
            SessionStateIO<SettingsDictionary> session = new SessionStateIO<SettingsDictionary> {
                Key = string.Format("Setting_{0}_{1}", ((int) ObjectType), ObjectName),
                Data = this
            };
            session.Save();
            Modified = false;
        }
        public static void ClearAllTempItems(string key) {
            if (key.StartsWith("Setting_")) { // one of our settings dictionaries
                SessionStateIO<SettingsDictionary> session = new SessionStateIO<SettingsDictionary> {
                    Key = key,
                };
                SettingsDictionary? settings = session.Load();
                if (settings == null) return;
                settings.ClearAllNotStartingWith(Globals.Session_Permanent);
                if (settings.Count == 0)
                    session.Remove();
                else
                    session.Save();
            } else { // regular item
                Manager.CurrentSession.Remove(key);
            }
        }

        public TYPE? GetValue<TYPE>(string settingName, TYPE? dfltVal = default(TYPE)) {
            if (!ContainsKey(settingName))
                return dfltVal;
            Setting? setting = this[settingName];
            if (setting == null)
                return dfltVal;
            return setting.GetValue<TYPE>();
        }
        public void SetValue<TYPE>(string settingName, TYPE value) {
            if (!TryGetValue(settingName, out Setting? setting)) {
                setting = Setting.Create(this, settingName, "", false);
                Add(setting.Name, setting);
            }
            setting.SetValue<TYPE>(value);
        }
        public void ClearValue(string settingName) {
            base.Remove(settingName);
        }
        internal void ClearAllStartingWith(string prefix) {
            // clear all settings that start with that prefix
            List<string> keys = (from string k in this.Keys where k.StartsWith(prefix) select k).ToList();
            foreach (string key in keys)
                base.Remove(key);
        }
        internal void ClearAllNotStartingWith(string prefix) {
            // clear all settings that start with that prefix
            List<string> keys = (from string k in this.Keys where !k.StartsWith(prefix) select k).ToList();
            foreach (string key in keys)
                base.Remove(key);
        }

        public TYPE? GetPermanentValue<TYPE>(string settingName, TYPE? dfltVal = default(TYPE)) {
            settingName = Globals.Session_Permanent + settingName;
            if (!ContainsKey(settingName))
                return dfltVal;
            Setting setting = this[settingName];
            if (setting == null)
                return dfltVal;
            return setting.GetValue<TYPE>();
        }
        public void SetPermanentValue<TYPE>(string settingName, TYPE value) {
            settingName = Globals.Session_Permanent + settingName;
            if (!TryGetValue(settingName, out Setting? setting)) {
                setting = Setting.Create(this, settingName, "", false);
                Add(setting.Name, setting);
            }
            setting.SetValue<TYPE>(value);
        }
        public void RemovePermanentValue(string settingName) {
            settingName = Globals.Session_Permanent + settingName;
            base.Remove(settingName);
        }
    }
}
