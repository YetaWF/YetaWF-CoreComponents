/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using YetaWF.Core.Localize;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;

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
            if (!Resource.ResourceAccess.IsResourceAuthorized(Name)) {
                // Don't challenge a resource as there is no alternative
                throw new Error(this.__ResStr("notAuth", "Not Authorized"));
            }
            return true;
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

        ModuleAction GetSelectTwoStepAction(int userId, string userName, string email);
        List<string> GetEnabledTwoStepAuthentications(int userId);
        void SetEnabledTwoStepAuthentications(int userId, List<string> auths);
        void AddEnabledTwoStepAuthentication(int userId, string auth);
        void RemoveEnabledTwoStepAuthentication(int userId, string auth);
        bool HasEnabledTwoStepAuthentication(int userId, string auth);
        void AddTwoStepLoginFailure();
        bool GetTwoStepLoginFailuresExceeded();
    }

    public static class Resource {

        public static IResource ResourceAccess { get; set; }

    }
}
