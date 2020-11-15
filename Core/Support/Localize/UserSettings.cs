/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using System;
using System.Threading.Tasks;
using YetaWF.Core.Support;

namespace YetaWF.Core.Localize {

    public interface IUserSettings {
        Task<object> ResolveUserAsync();
        object? GetProperty(string name, Type type);
        Task SetPropertyAsync(string name, Type type, object? value);
    }

    public static class UserSettings {

        public static IUserSettings UserSettingsAccess { get; set; } = null!;

        public static TYPE? GetProperty<TYPE>(string name) {
            if (UserSettingsAccess == null) return default(TYPE);
            object? obj = UserSettingsAccess.GetProperty(name, typeof(TYPE));
            if (obj == null) return default;
            return (TYPE)obj;
        }
        public static async Task SetPropertyAsync<TYPE>(string name, TYPE value) {
            if (UserSettingsAccess == null)
                throw new InternalError("IUserSettings UserSettingsAccess missing");
            await UserSettingsAccess.SetPropertyAsync(name, typeof(TYPE), value);
        }
    }
}
