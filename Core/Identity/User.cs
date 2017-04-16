/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;

namespace YetaWF.Core.Identity {

    public class User {
        public User() { }
        public int UserId { get; set; }
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
