/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Components;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;
using YetaWF.Core.Models.Attributes;
#if MVC6
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
#else
using System.Web;
using System.Web.Mvc;
#endif

namespace YetaWF.Core.Identity {

    /// <summary>
    /// Used to define protected named resources.
    /// Protected resources can be used to restrict access to controllers based on user permissions.
    ///
    /// The ResourceAttribute is typically used in a package's AssemblyInfo.cs file to define all named resources the package implements.
    /// These are collected during application startup by the AuthorizationResourceDataProvider.InitializeApplicationStartupAsync method.
    ///
    /// The ResourceAuthorizeAttribute is used with controllers to protect by a named resource.
    /// Resource authorization is provided using Admin > Identity > Resources (standard YetaWF site).
    /// The AuthorizationDataProvider class is used to maintain authorization settings for roles and users.
    /// </summary>

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true)]
    public class ResourceAttribute : Attribute {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the protected resource. The name must start with the package name defining the resource to avoid collisions. Example: YetaWF_Identity-AllowUserLogon.</param>
        /// <param name="description">A brief description of the protected resource. This description is shown when reviewing resources using Admin > Identity > Resources (standard YetaWF site).</param>
        public ResourceAttribute(string name, string description) {
            Name = name;
            Description = description;
            Anonymous = false;
            User = false;
            Editor = false;
            Administrator = false;
            Superuser = false;
        }
        /// <summary>
        /// The name of the protected resource.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// A brief description of the protected resource. This description is shown when reviewing resources using Admin > Identity > Resources (standard YetaWF site).
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Defines whether anonymous users have access to the protected resource.
        /// </summary>
        public bool Anonymous { get; set; }
        /// <summary>
        /// Defines whether logged on users have access to the protected resource.
        /// </summary>
        public bool User { get; set; }
        /// <summary>
        /// Defines whether a user with the editor role has access to the protected resource.
        /// </summary>
        public bool Editor { get; set; }
        /// <summary>
        /// Defines whether a user with the administrator role has access to the protected resource.
        /// </summary>
        public bool Administrator { get; set; }
        /// <summary>
        /// Defines whether a user with the superuser role has access to the protected resource.
        /// This is mostly for documentation purposes as all resources are accessible by a superuser.
        /// </summary>
        public bool Superuser { get; set; }
    }

    /// <summary>
    /// Used with controller methods which must be authorized for access.
    /// The ResourceAuthorize attribute must use a protected named resource (defined using ResourceAttribute).
    /// When the method is invoked, validation occurs to insure the user is authorized to access the protected name resource.
    ///
    /// This can be used for any type of controller, including plain MVC controllers, without ModuleGuid (i.e., no associated module).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
#if MVC6
    public class ResourceAuthorizeAttribute : AuthorizeAttribute
#else
    public class ResourceAuthorizeAttribute : AuthorizeAttribute
#endif
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The protected named resource which was defined using ResourceAttribute.</param>
        public ResourceAuthorizeAttribute(string name)
#if MVC6
             : base("ResourceAuthorize")
#else
#endif
        {
            Name = name;
        }
        /// <summary>
        /// Defines the name of the protected named resource (defined using ResourceAttribute).
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Handles authorization checking based for protected named resources.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>Returns whether access to the protected named resource is permitted.</returns>
#if MVC6
        // This is using AttributeAuthorizationHandler
#else
        protected override bool AuthorizeCore(HttpContextBase httpContext) {
            return YetaWFManager.Syncify<bool>(() => Resource.ResourceAccess.IsResourceAuthorizedAsync(Name)); // Must sync, no async attributes in mvc5
        }
#endif
    }

    public enum UserStatusEnum {
        [EnumDescription("Needs Verification", "A new user account that has not yet been verified or approved")]
        NeedValidation = 0,
        [EnumDescription("Needs Approval", "A user account whose email address has been validated but still needs approval")]
        NeedApproval = 1,
        [EnumDescription("Approved User", "A user account that has been approved")]
        Approved = 2,
        [EnumDescription("Rejected User", "A user account that has been rejected")]
        Rejected = 20,
        [EnumDescription("Suspended User", "A user account that has been suspended")]
        Suspended = 21,
    }

    /// <summary>
    /// Used with the IResource.AddUserAsync method to add a new user to the current site.
    /// </summary>
    public class AddUserInfo {
        /// <summary>
        /// The error indicator.
        /// </summary>
        public enum ErrorTypeEnum {
            /// <summary>
            /// No error occurred. The user has been added.
            /// </summary>
            None = 0,
            /// <summary>
            /// A user with the specified name already exists.
            /// </summary>
            Name = 1,
            /// <summary>
            /// A user with the specified email address already exists.
            /// </summary>
            Email = 2,
        };
        /// <summary>
        /// Defines the error that occurred when the IResource.AddUserAsync method was called.
        /// </summary>
        public ErrorTypeEnum ErrorType { get; set; }
        /// <summary>
        /// Contains a list of error messages if an error occurred when the IResource.AddUserAsync method was called.
        /// </summary>
        public List<string> Errors { get; set; }
        /// <summary>
        /// Contains the user ID of the user that was added using the IResource.AddUserAsync method. 0 if the call was unsuccessful.
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// Constructor.
        /// </summary>
        public AddUserInfo() {
            Errors = new List<string>();
        }
    }

    public interface IResource {

        bool IsBackDoorWideOpen();
        Task ShutTheBackDoorAsync();
        Task ResolveUserAsync();
        Task LogoffAsync();
        Task LoginAsAsync(int userId);

        Task<bool> IsResourceAuthorizedAsync(string resourceName);

        Task AddRoleAsync(string roleName, string description);
        Task AddRoleWithUrlAsync(string roleName, string description, string postLoginUrl);
        Task RemoveRoleAsync(string roleName);
        Task AddRoleToUserAsync(int userId, string roleName);
        Task RemoveRoleFromUserAsync(int userId, string roleName);
        Task<List<SelectionItem<string>>> GetUserRolesAsync(int userId);
        Task<string> GetUserPostLoginUrlAsync(List<int> userRoles);

        Task<AddUserInfo> AddUserAsync(string name, string email, string password, bool needsNewPassword, string comment);
        Task<bool> RemoveUserAsync(int userId);

        Task<int> GetUserIdAsync(string userName);
        Task<string> GetUserNameAsync(int userId);
        Task<string> GetUserEmailAsync(int userId);
        Task<UserStatusEnum> GetUserStatusAsync(int userId);
        Task SetUserStatusAsync(int userId, UserStatusEnum status);
        Task<string> GetUserVerificationCodeAsync(int userId);
        Task<DateTime?> GetUserLastLoginDateAsync(int userId);
        Task<string> GetUserPasswordAsync(int userId);

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

        void ShowNeedNewPassword();

        Task AddEnabledTwoStepAuthenticationAsync(int userId, string auth);
        Task RemoveEnabledTwoStepAuthenticationAsync(int userId, string auth);
        Task<bool> HasEnabledTwoStepAuthenticationAsync(int userId, string auth);
        Task AddTwoStepLoginFailureAsync();
        Task<bool> GetTwoStepLoginFailuresExceededAsync();
        Task<bool> VerifyTwoStepAuthenticationRecoveryCodeAsync(int userId, string code);
    }

    public static class Resource {

        public static IResource ResourceAccess { get; set; } = new DefaultResourceAccess();
    }

    public class DefaultResourceAccess : IResource {

        public Task AddEnabledTwoStepAuthenticationAsync(int userId, string auth) {
            throw new NotImplementedException();
        }

        public Task AddRoleAsync(string roleName, string description) {
            throw new NotImplementedException();
        }

        public Task AddRoleToUserAsync(int userId, string roleName) {
            throw new NotImplementedException();
        }

        public Task AddRoleWithUrlAsync(string roleName, string description, string postLoginUrl) {
            throw new NotImplementedException();
        }

        public Task AddTwoStepLoginFailureAsync() {
            throw new NotImplementedException();
        }

        public Task<AddUserInfo> AddUserAsync(string name, string email, string password, bool needsNewPassword, string comment) {
            throw new NotImplementedException();
        }

        public int GetAdministratorRoleId() {
            throw new NotImplementedException();
        }

        public int GetAnonymousRoleId() {
            throw new NotImplementedException();
        }

        public List<RoleInfo> GetDefaultRoleList(bool Exclude2FA = false) {
            throw new NotImplementedException();
        }

        public List<User> GetDefaultUserList() {
            throw new NotImplementedException();
        }

        public int GetEditorRoleId() {
            throw new NotImplementedException();
        }

        public Task<ModuleAction> GetForceTwoStepActionSetupAsync(string url) {
            throw new NotImplementedException();
        }

        public int GetRoleId(string roleName) {
            throw new NotImplementedException();
        }

        public ModuleAction GetSelectTwoStepAction(int userId, string userName, string email) {
            throw new NotImplementedException();
        }

        public int GetSuperuserId() {
            throw new NotImplementedException();
        }

        public int GetSuperuserRoleId() {
            throw new NotImplementedException();
        }

        public Task<bool> GetTwoStepLoginFailuresExceededAsync() {
            throw new NotImplementedException();
        }

        public Task<bool> VerifyTwoStepAuthenticationRecoveryCodeAsync(int userId, string code) {
            throw new NotImplementedException();
        }

        public int GetUser2FARoleId() {
            throw new NotImplementedException();
        }

        public Task<string> GetUserEmailAsync(int userId) {
            throw new NotImplementedException();
        }

        public Task<int> GetUserIdAsync(string userName) {
            throw new NotImplementedException();
        }

        public Task<string> GetUserNameAsync(int userId) {
            throw new NotImplementedException();
        }

        public Task<string> GetUserPostLoginUrlAsync(List<int> userRoles) {
            throw new NotImplementedException();
        }

        public int GetUserRoleId() {
            throw new NotImplementedException();
        }

        public Task<List<SelectionItem<string>>> GetUserRolesAsync(int userId) {
            throw new NotImplementedException();
        }

        public Task<UserStatusEnum> GetUserStatusAsync(int userId) {
            throw new NotImplementedException();
        }

        public Task<bool> HasEnabledTwoStepAuthenticationAsync(int userId, string auth) {
            throw new NotImplementedException();
        }

        public bool IsBackDoorWideOpen() {
            return false;
        }

        public Task<bool> IsResourceAuthorizedAsync(string resourceName) {
            throw new NotImplementedException();
        }

        public Task LoginAsAsync(int userId) {
            throw new NotImplementedException();
        }

        public Task LogoffAsync() {
            throw new NotImplementedException();
        }

        public Task RemoveEnabledTwoStepAuthenticationAsync(int userId, string auth) {
            throw new NotImplementedException();
        }

        public Task RemoveRoleAsync(string roleName) {
            throw new NotImplementedException();
        }

        public Task RemoveRoleFromUserAsync(int userId, string roleName) {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveUserAsync(int userId) {
            throw new NotImplementedException();
        }

        public Task ResolveUserAsync() {
            return Task.CompletedTask;
        }

        public Task SetUserStatusAsync(int userId, UserStatusEnum status) {
            throw new NotImplementedException();
        }

        public Task<string> GetUserVerificationCodeAsync(int userId) {
            throw new NotImplementedException();
        }

        public Task<DateTime?> GetUserLastLoginDateAsync(int userId) {
            throw new NotImplementedException();
        }

        public Task<string> GetUserPasswordAsync(int userId) {
            throw new NotImplementedException();
        }

        public void ShowNeed2FA() {
            throw new NotImplementedException();
        }

        public void ShowNeedNewPassword() {
            throw new NotImplementedException();
        }

        public Task ShutTheBackDoorAsync() {
            throw new NotImplementedException();
        }
    }
}
