/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace YetaWF.Core.Controllers {

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public abstract class AllowHttpBase : ActionMethodSelectorAttribute {

        public AllowHttpBase() { }
        public abstract List<string> Methods { get; }

        public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo) {
            HttpRequestBase request = controllerContext.HttpContext.Request;
            foreach (string verb in Methods) {
                if (request.Headers["X-HTTP-Method-Override"] == verb || request.HttpMethod == verb)
                    return true;
            }
            return false;
        }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowHttp : AllowHttpBase {
        private List<string> Verbs { get; }
        public AllowHttp(params string[] verbs) { Verbs = verbs.ToList(); }
        public override List<string> Methods { get { return Verbs; } }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowGet : AllowHttpBase {
        public AllowGet() { }
        public override List<string> Methods { get { return new List<string> { "GET" }; } }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowPost : AllowHttpBase {
        public AllowPost() { }
        public override List<string> Methods { get { return new List<string> { "POST" }; } }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowPut : AllowHttpBase {
        public AllowPut() { }
        public override List<string> Methods { get { return new List<string> { "PUT" }; } }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowDelete : AllowHttpBase {
        public AllowDelete() { }
        public override List<string> Methods { get { return new List<string> { "DELETE" }; } }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowHead : AllowHttpBase {
        public AllowHead() { }
        public override List<string> Methods { get { return new List<string> { "HEAD" }; } }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowPatch : AllowHttpBase {
        public AllowPatch() { }
        public override List<string> Methods { get { return new List<string> { "PATCH" }; } }
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowOptions : AllowHttpBase {
        public AllowOptions() { }
        public override List<string> Methods { get { return new List<string> { "OPTIONS" }; } }
    }
}


