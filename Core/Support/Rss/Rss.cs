/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System.ServiceModel.Syndication;
using System.Xml;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif


namespace YetaWF.Core.Support.Rss {

    public class RssResult : ActionResult {

        public SyndicationFeed Feed { get; set; }

        public RssResult() { }

        public RssResult(SyndicationFeed feed) {
            this.Feed = feed;
        }

#if MVC6
        public override void ExecuteResult(ActionContext context) {
#else
        public override void ExecuteResult(ControllerContext context) {
#endif

            context.HttpContext.Response.ContentType = "application/rss+xml";
            Rss20FeedFormatter formatter = new Rss20FeedFormatter(this.Feed);
#if MVC6
            using (XmlWriter writer = XmlWriter.Create(context.HttpContext.Response.Body)) {
#else
            using (XmlWriter writer = XmlWriter.Create(context.HttpContext.Response.Output)) {
#endif
                formatter.WriteTo(writer);
            }
        }
    }
}
