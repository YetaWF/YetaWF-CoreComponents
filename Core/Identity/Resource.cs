/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace YetaWF.Core.Identity {

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true)]
    public class ResourceAttribute : Attribute {
        public ResourceAttribute(string name, string description) {
            Name = name;
            Description = description;
            Anonymous = false;
            User = false;
            Editor = false;
            Administrator = false;
            Superuser = false;
            All = false;
        }
        public string Name { get; private set; }
        public string Description { get; private set; }

        public bool All { get { return _all; } set { Anonymous = User = Administrator = Editor = Superuser = value; } }
        private bool _all { get; set; }

        public bool Anonymous { get; set; }
        public bool User { get; set; }
        public bool Editor { get; set; }
        public bool Administrator { get; set; }
        public bool Superuser { get; set; }// mostly for documentation purposes - all resources are available to the superuser
    }

    // Used for plain MVC controllers (without moduleguid)
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ResourceAuthorizeAttribute : AuthorizeAttribute {
        public ResourceAuthorizeAttribute(string name) {
            Name = name;
        }
        public string Name { get; private set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext) {
            return Resource.ResourceAccess.IsResourceAuthorized(Name);
        }
    }

    public interface IResource {

        bool IsBackDoorWideOpen();
        void ShutTheBackDoor();
        void ResolveUser();
        void Logoff();
        void LoginAs(int userId);

        bool IsResourceAuthorized(string resourceName);

        void AddRole(string roleName, string description);
        void RemoveRole(string roleName);
        void AddRoleToUser(int userId, string roleName);
        void RemoveRoleFromUser(int userId, string roleName);

        int GetUserId(string userName);
        string GetUserName(int userId);
        string GetUserEmail(int userId);
        int GetSuperuserId();

        List<RoleInfo> GetDefaultRoleList();
        List<User> GetDefaultUserList();

        int GetSuperuserRoleId();
        int GetUserRoleId();
        int GetAnonymousRoleId();
        int GetAdministratorRoleId();
        int GetEditorRoleId();
        int GetRoleId(string roleName);
    }

    public static class Resource {

        public static IResource ResourceAccess { get; set; }

    }
}
