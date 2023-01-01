/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;

namespace YetaWF.Core.Identity {

    public class Role {
        public Role() { }
        public int RoleId { get; set; }
    }
    public class RoleComparer : IEqualityComparer<Role> {
        public bool Equals(Role? x, Role? y) {
            return x?.RoleId == y?.RoleId;
        }
        public int GetHashCode(Role x) {
            return x.RoleId.GetHashCode() + x.RoleId.GetHashCode();
        }
    }

    public class RoleInfo {
        public RoleInfo() { }
        public int RoleId { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? PostLoginUrl { get; set; }
    }
}
