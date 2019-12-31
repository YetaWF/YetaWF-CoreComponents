/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Identity {

    /// <summary>
    /// Interface supported by any class that needs to remove data when a user is removed.
    /// </summary>
    public interface IRemoveUser {
        Task RemoveAsync(int userId);
    }

    public class User {

        public User() { }
        public int UserId { get; set; }

        /// <summary>
        /// Calls all classes implementing the IRemoveUser interface to remove any data associated with the specified user.
        /// </summary>
        /// <param name="userId">The id of the user being removed.</param>
        public static async Task RemoveDependentPackagesAsync(int userId) {
            List<Type> types = GetRemoveUserTypes();
            foreach (Type type in types) {
                try {
                    IRemoveUser iRemoveUser = Activator.CreateInstance(type) as IRemoveUser;
                    if (iRemoveUser != null)
                        await iRemoveUser.RemoveAsync(userId);
                } catch (Exception) { }
            }
        }
        private static List<Type> GetRemoveUserTypes() {
            return Package.GetClassesInPackages<IRemoveUser>();
        }
    }

    public class UserComparer : IEqualityComparer<User> {
        public bool Equals(User x, User y) {
            return x.UserId == y.UserId;
        }
        public int GetHashCode(User x) {
            return x.UserId.GetHashCode() + x.UserId.GetHashCode();
        }
    }
}
