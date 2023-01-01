/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Base class for attributes defining HTTP access permissions for controller actions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public abstract class AllowHttpBase : ActionMethodSelectorAttribute {

        /// <summary>
        /// Constructor.
        /// </summary>
        public AllowHttpBase() { }

        internal abstract List<string> Methods { get; }

        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action) {
            HttpRequest request = routeContext.HttpContext.Request;
            string? overRide = request.Headers["X-HTTP-Method-Override"];
            foreach (string verb in Methods) {
                if (overRide == verb || request.Method == verb)
                    return true;
            }
            return false;
        }
    }
    /// <summary>
    /// Defines that the method or class allows access using the specified HTTP verbs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowHttp : AllowHttpBase {
        private List<string> Verbs { get; }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="verbs">A collection of allowed HTTP verbs (GET, POST, etc.).</param>
        public AllowHttp(params string[] verbs) { Verbs = verbs.ToList(); }
        internal override List<string> Methods { get { return Verbs; } }
    }
    /// <summary>
    /// Defines that the method or class allows access using the HTTP GET request verb.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowGet : AllowHttpBase {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AllowGet() { }
        internal override List<string> Methods { get { return new List<string> { "GET" }; } }
    }
    /// <summary>
    /// Defines that the method or class allows access using the HTTP POST request verb.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowPost : AllowHttpBase {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AllowPost() { }
        internal override List<string> Methods { get { return new List<string> { "POST" }; } }
    }
    /// <summary>
    /// Defines that the method or class allows access using the HTTP PUT request verb.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowPut : AllowHttpBase {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AllowPut() { }
        internal override List<string> Methods { get { return new List<string> { "PUT" }; } }
    }
    /// <summary>
    /// Defines that the method or class allows access using the HTTP DELETE request verb.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowDelete : AllowHttpBase {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AllowDelete() { }
        internal override List<string> Methods { get { return new List<string> { "DELETE" }; } }
    }
    /// <summary>
    /// Defines that the method or class allows access using the HTTP HEAD request verb.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowHead : AllowHttpBase {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AllowHead() { }
        internal override List<string> Methods { get { return new List<string> { "HEAD" }; } }
    }
    /// <summary>
    /// Defines that the method or class allows access using the HTTP PATCH request verb.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowPatch : AllowHttpBase {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AllowPatch() { }
        internal override List<string> Methods { get { return new List<string> { "PATCH" }; } }
    }
    /// <summary>
    /// Defines that the method or class allows access using the HTTP OPTIONS request verb.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowOptions : AllowHttpBase {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AllowOptions() { }
        internal override List<string> Methods { get { return new List<string> { "OPTIONS" }; } }
    }
}


