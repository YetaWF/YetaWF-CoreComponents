/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#nullable enable

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YetaWF.Core.Addons;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Extensions;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;

namespace YetaWF.Core.Controllers {

    /// <summary>
    /// Used to return a CSV file from a controller.
    /// </summary>
    /// <remarks>
    /// This action result works in conjunction with JavaScript in Basics.ts returning a cookie indicating the file is available.
    /// </remarks>
    public class CSVResult<TYPE> : ActionResult {

        private DataProviderGetRecords<TYPE> Data { get; set; }
        private string FileName { get; set; }
        private long CookieToReturn { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">The data to return as a CSV file.</param>
        /// <param name="fileName">The name of the CSV file.</param>
        /// <param name="cookieToReturn">The cookie to return, indicating that the file has been downloaded. Works in conjunction with client-side code in basics.js.</param>
        public CSVResult(DataProviderGetRecords<TYPE> data, string fileName, long cookieToReturn) {
            this.Data = data;
            this.FileName = fileName;
            this.CookieToReturn = cookieToReturn;
        }

        /// <summary>
        /// Processes the result of an action method.
        /// </summary>
        /// <param name="context">The controller context.</param>
        public override async Task ExecuteResultAsync(ActionContext context) {

            Type objType = typeof(TYPE);
            List<PropertyInfo> propInfos = ObjectSupport.GetProperties(objType);

            StringBuilder sb = new StringBuilder();

            // Header
            foreach (PropertyInfo propInfo in propInfos) {
                sb.Append($@"""{propInfo.Name}""");
                sb.Append(",");
            }
            sb.RemoveLastComma();
            sb.Append("\r\n");

            // Data
            foreach (TYPE rec in Data.Data) {
                foreach (PropertyInfo propInfo in propInfos) {
                    object? o = propInfo.GetValue(rec);
                    if (o == null) {

                    } else {
                        if (propInfo.PropertyType == typeof(string))
                            sb.Append($@"""{o.ToString()}""");
                        else if (propInfo.PropertyType == typeof(DateTime) || propInfo.PropertyType == typeof(DateTime?))
                            sb.Append($@"""{Formatting.GetUserDateTime((DateTime)o)}""");
                        else
                            sb.Append($@"{o.ToString()}");
                    }
                    sb.Append(",");
                }
                sb.RemoveLastComma();
                sb.Append("\r\n");
            }

            var Response = context.HttpContext.Response;

            string contentType = "application/octet-stream";
            Response.ContentType = contentType;

            Response.Headers.Add("Content-Disposition", "attachment;" + (string.IsNullOrWhiteSpace(FileName) ? "" : $@"filename=""{FileName}"""));
            Response.Cookies.Append(Basics.CookieDone, CookieToReturn.ToString(), new Microsoft.AspNetCore.Http.CookieOptions { HttpOnly = false, Path = "/" });

            byte[] btes = Encoding.ASCII.GetBytes(sb.ToString());
            await context.HttpContext.Response.Body.WriteAsync(btes, 0, btes.Length);
        }
    }
}
