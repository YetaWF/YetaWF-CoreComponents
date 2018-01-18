/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Support;

namespace YetaWF.Core.Localize {

    public interface IUserSettings {
        object GetProperty(string name, Type type);
        void SetProperty(string name, Type type, object value);
    }

    public static class UserSettings {

        public static IUserSettings UserSettingsAccess { get; set; }

        public static TYPE GetProperty<TYPE>(string name) {
            if (UserSettingsAccess == null) return default(TYPE);
            object obj = UserSettingsAccess.GetProperty(name, typeof(TYPE));
            if (obj == null) return default(TYPE);
            return (TYPE) obj;
        }
        public static void SetProperty<TYPE>(string name, TYPE value) {
            if (UserSettingsAccess == null) 
                throw new InternalError("IUserSettings UserSettingsAccess missing");
            UserSettingsAccess.SetProperty(name, typeof(TYPE), value);
        }
    }
}
