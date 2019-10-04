/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Identity#License */

#if MVC6

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using YetaWF.Core.Controllers;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

// Inspired by // http://stackoverflow.com/questions/31464359/how-do-you-create-a-custom-authorizeattribute-in-asp-net-core
// An AuthorizationFilter was considered but rumors are that support will be removed

namespace YetaWF.Core.Identity {

    public abstract class AttributeAuthorizationHandler<TRequirement, TAttribute> : AuthorizationHandler<TRequirement> where TRequirement : IAuthorizationRequirement where TAttribute : System.Attribute {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TRequirement requirement) {
            var attributes = new List<TAttribute>();

            var action = (context.Resource as AuthorizationFilterContext)?.ActionDescriptor as ControllerActionDescriptor;
            if (action != null) {
                attributes.AddRange(GetAttributes(action.ControllerTypeInfo.UnderlyingSystemType));
                attributes.AddRange(GetAttributes(action.MethodInfo));
            }

            return HandleRequirementAsync(context, requirement, attributes);
        }

        protected abstract Task HandleRequirementAsync(AuthorizationHandlerContext context, TRequirement requirement, IEnumerable<TAttribute> attributes);

        private static IEnumerable<TAttribute> GetAttributes(MemberInfo memberInfo) {
            return memberInfo.GetCustomAttributes(typeof(TAttribute), false).Cast<TAttribute>();
        }
    }

    public class ResourceAuthorizeRequirement : IAuthorizationRequirement { }

    public class ResourceAuthorizeHandler : AttributeAuthorizationHandler<ResourceAuthorizeRequirement, ResourceAuthorizeAttribute> {
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ResourceAuthorizeRequirement requirement, IEnumerable<ResourceAuthorizeAttribute> attributes) {
            foreach (var permissionAttribute in attributes) {
                if (!await AuthorizeAsync(context.User, permissionAttribute.Name)) {
                    return;
                }
            }
            context.Succeed(requirement);
        }

        private async Task<bool> AuthorizeAsync(ClaimsPrincipal user, string permission) {
            await YetaWFController.SetupEnvironmentInfoAsync();// for ajax calls this may be the first chance to set up identity
            if (!await Resource.ResourceAccess.IsResourceAuthorizedAsync(permission)) {
                // Don't challenge a resource as there is no alternative
                throw new Error(this.__ResStr("notAuth", "Not Authorized"));
            }
            return true;
        }
    }
}
#else
#endif