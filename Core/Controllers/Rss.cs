/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Mvc;
using System.ServiceModel.Syndication;
using System.Xml;

namespace YetaWF.Core.Support.Rss {

    public class RssResult : ActionResult {

        public SyndicationFeed Feed { get; set; }

        public RssResult(SyndicationFeed feed) {
            this.Feed = feed;
        }

        public override void ExecuteResult(ActionContext context) {

            Utility.AllowSyncIO(context.HttpContext);

            context.HttpContext.Response.ContentType = "application/rss+xml";
            Rss20FeedFormatter formatter = new Rss20FeedFormatter(this.Feed);
            using (XmlWriter writer = XmlWriter.Create(context.HttpContext.Response.Body)) {
                formatter.WriteTo(writer);
            }
        }
    }
}
