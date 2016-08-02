/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;

namespace YetaWF.Core.Localize {

    public interface IUserSettings {
        object GetProperty(string name, Type type);
        void SetProperty(string name, Type type, object value);
    }

    public static class UserSettings {

        public static IUserSettings UserSettingsAccess { get; set; }

        public static TYPE GetProperty<TYPE>(string name) {
            object obj = UserSettingsAccess.GetProperty(name, typeof(TYPE));
            if (obj == null) return default(TYPE);
            return (TYPE) obj;
        }
        public static void SetProperty<TYPE>(string name, TYPE value) {
            UserSettingsAccess.SetProperty(name, typeof(TYPE), value);
        }
    }
}
