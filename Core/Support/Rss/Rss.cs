/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.ServiceModel.Syndication;
using System.Web.Mvc;
using System.Xml;

namespace YetaWF.Core.Support.Rss {

    public class RssResult : ActionResult {

        public SyndicationFeed Feed { get; set; }

        public RssResult() { }

        public RssResult(SyndicationFeed feed) {
            this.Feed = feed;
        }
        public override void ExecuteResult(ControllerContext context) {
            context.HttpContext.Response.ContentType = "application/rss+xml";
            Rss20FeedFormatter formatter = new Rss20FeedFormatter(this.Feed);
            using (XmlWriter writer = XmlWriter.Create(context.HttpContext.Response.Output)) {
                formatter.WriteTo(writer);
            }
        }
    }
}
