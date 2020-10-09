/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YetaWF.Core;
using YetaWF.Core.Support;

namespace YetaWF2.Middleware {

    /// <summary>
    /// Middleware to block requests based on UserAgent and requested URL. The inspiration for this came from a ridiculous amount of bots scanning our sites
    /// for exploits. So many .php requests in general, wp-login.php in particular and then oh the bots scraping email addresses, like sleazoid zoominfobot which doesn't even honor rel=nofollow.
    /// Well given their reputation (google it), that's not surprising.
    /// </summary>
    /// <remarks>
    /// This middleware blocks requests by URL containing or ending in certain strings, and user agents that contain certain strings, all case insensitive.
    /// The configuration is provided via a JSON file, with a UI in Admin > Settings > Request Block Settings (standard YetaWF site), which can also dynamically reload the settings.
    ///
    /// There is no logging as we don't care who the sleaze balls are.
    /// </remarks>
    public class BlockRequestMiddleware {

        public const string SETTINGSFILE = "BlockSettings.json";

        private readonly RequestDelegate next;
        private static BlockSettingsDefinition BlockSettings = null;
        private static BlockSettingsDefinition BlockSettingsNone = new BlockSettingsDefinition { };

        public BlockRequestMiddleware(RequestDelegate next) {
            this.next = next;
        }

        public Task Invoke(HttpContext context) {
            if (BlockSettings != BlockSettingsNone) {
                if (context.Request.Path.HasValue) {
                    string path = context.Request.Path.Value.ToLower();
                    if (IsNotAuthorizedPath(path)) {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                    if (IsSuccessfulPath(path)) {
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        return Task.CompletedTask;
                    }
                    string userAgent = context.Request.Headers["User-Agent"].ToString();
                    if (IsNotAuthorizedUserAgent(userAgent)) {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                    if (IsSuccessfulUserAgent(path)) {
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        return Task.CompletedTask;
                    }
                }
            }
            return next.Invoke(context);
        }

        private bool IsNotAuthorizedPath(string path) {
            if ((from u in BlockSettings.NotAuthorized.UrlPathContains where path.Contains(u, StringComparison.OrdinalIgnoreCase) select u).Any())
                return true;
            if ((from u in BlockSettings.NotAuthorized.UrlPathEndsWith where path.EndsWith(u, StringComparison.OrdinalIgnoreCase) select u).Any())
                return true;
            return false;
        }
        private bool IsNotAuthorizedUserAgent(string userAgent) {
            if ((from u in BlockSettings.NotAuthorized.UserAgentContains where userAgent.Contains(u, StringComparison.OrdinalIgnoreCase) select u).Any())
                return true;
            return false;
        }
        private bool IsSuccessfulPath(string path) {
            if ((from u in BlockSettings.Successful.UrlPathContains where path.Contains(u, StringComparison.OrdinalIgnoreCase) select u).Any())
                return true;
            if ((from u in BlockSettings.Successful.UrlPathEndsWith where path.EndsWith(u, StringComparison.OrdinalIgnoreCase) select u).Any())
                return true;
            return false;
        }
        private bool IsSuccessfulUserAgent(string userAgent) {
            if ((from u in BlockSettings.Successful.UserAgentContains where userAgent.Contains(u, StringComparison.OrdinalIgnoreCase) select u).Any())
                return true;
            return false;
        }

        /// <summary>
        /// Load or reload the block settings file.
        /// </summary>
        public static Task LoadBlockSettingsAsync() {
            if (BlockSettings == null) {
                BlockSettings = BlockSettingsNone;
                string settingsFile = Path.Combine(Globals.DataFolder, SETTINGSFILE);
                if (File.Exists(settingsFile)) // usually used during startup, no filesystem dataprovider available
                    BlockSettings = Utility.JsonDeserialize<BlockSettingsDefinition>(File.ReadAllText(settingsFile));
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Definitions for blocked/successful requests.
    /// </summary>
    public class BlockSettingsDefinition {
        /// <summary>
        /// Defines requests that return 401 Not Authorized.
        /// </summary>
        public AuthorizationDefinition NotAuthorized { get; set; }
        /// <summary>
        /// Defines requests that return 200 OK.
        /// </summary>
        public AuthorizationDefinition Successful { get; set; }

        public BlockSettingsDefinition() {
            NotAuthorized = new AuthorizationDefinition();
            Successful = new AuthorizationDefinition();
        }
    }
    public class AuthorizationDefinition {
        /// <summary>
        /// List of strings to check within the URL path.
        /// </summary>
        public List<string> UrlPathContains { get; set; }
        /// <summary>
        /// List of strings to check the end of the URL path.
        /// </summary>
        public List<string> UrlPathEndsWith { get; set; }
        /// <summary>
        /// List of strings to check within the user agent.
        /// </summary>
        public List<string> UserAgentContains { get; set; }

        public AuthorizationDefinition() {
            UrlPathContains = new List<string>();
            UrlPathEndsWith = new List<string>();
            UserAgentContains = new List<string>();
        }
    }
}
