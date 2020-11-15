/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Support.Repository {

    public class Setting {

        public string Name { get; private set; } = null!;

        [DontSave]
        public SettingsDictionary? Dictionary { get; internal set; }

        // CREATE/REMOVE
        // CREATE/REMOVE
        // CREATE/REMOVE

        public static Setting Create(SettingsDictionary dict, string settingName, object value, bool modified) {
            Setting p = new Setting {
                Name = settingName,
                Value = value,
                Modified = modified,
            };
            p.Dictionary = dict;
            return p;
        }

        public void Remove() {
            Value = null;
        }

        [DontSave]
        public bool Modified {
            get { return _modified; }
            set {
                _modified = value;
                if (_modified && Dictionary != null)
                    Dictionary.Modified = true;
            }
        }
        private bool _modified;

        // VALUE
        // VALUE

        public object? Value { get; set; }

        public TYPE? GetValue<TYPE>() {
            if (Value == null || Value.Equals(default(TYPE))) return default;
            if (typeof(TYPE) == typeof(bool)) {
                try {
                    return (TYPE)(object)((string)Value == "True");
                } catch (Exception) { }
            } else if (typeof(TYPE) == typeof(int)) {
                return (TYPE)(object) Convert.ToInt32(Value.ToString()!);
            }
            try {
                return (TYPE) Value;
            } catch (Exception) { return default; }
        }
        public void SetValue<TYPE>(TYPE value) {
            Value = value;
        }
    }
}
