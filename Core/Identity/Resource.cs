/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
#else
using System.Web;
using System.Web.Mvc;
#endif

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
#if MVC6
    public class ResourceAuthorizeAttribute : AuthorizeAttribute
#else
    public class ResourceAuthorizeAttribute : AuthorizeAttribute
#endif
    {
        public ResourceAuthorizeAttribute(string name)
#if MVC6
             : base("ResourceAuthorize")
#else
#endif
        {
            Name = name;
        }
        public string Name { get; private set; }

#if MVC6
        // This is using AttributeAuthorizationHandler
#else
        protected override bool AuthorizeCore(HttpContextBase httpContext) {
            return YetaWFManager.Syncify<bool>(() => Resource.ResourceAccess.IsResourceAuthorizedAsync(Name)); // Must sync, no async attributes in mvc5
        }
#endif
    }

    public interface IResource {

        bool IsBackDoorWideOpen();
        Task ShutTheBackDoorAsync();
        Task ResolveUserAsync();
        Task LogoffAsync();
        Task LoginAsAsync(int userId);

        Task<bool> IsResourceAuthorizedAsync(string resourceName);

        Task AddRoleAsync(string roleName, string description);
        Task RemoveRoleAsync(string roleName);
        Task AddRoleToUserAsync(int userId, string roleName);
        Task RemoveRoleFromUserAsync(int userId, string roleName);

        Task<int> GetUserIdAsync(string userName);
        Task<string> GetUserNameAsync(int userId);
        Task<string> GetUserEmailAsync(int userId);
        int GetSuperuserId();
        List<RoleInfo> GetDefaultRoleList(bool Exclude2FA = false);
        List<User> GetDefaultUserList();
        int GetSuperuserRoleId();
        int GetUserRoleId();
        int GetUser2FARoleId();
        int GetAnonymousRoleId();
        int GetAdministratorRoleId();
        int GetEditorRoleId();
        int GetRoleId(string roleName);

        ModuleAction GetSelectTwoStepAction(int userId, string userName, string email);
        Task<ModuleAction> GetForceTwoStepActionSetupAsync(string url);
        void ShowNeed2FA();

        Task AddEnabledTwoStepAuthenticationAsync(int userId, string auth);
        Task RemoveEnabledTwoStepAuthenticationAsync(int userId, string auth);
        Task<bool> HasEnabledTwoStepAuthenticationAsync(int userId, string auth);
        Task AddTwoStepLoginFailureAsync();
        Task<bool> GetTwoStepLoginFailuresExceededAsync();
    }

    public static class Resource {

        public static IResource ResourceAccess { get; set; }

    }
}
