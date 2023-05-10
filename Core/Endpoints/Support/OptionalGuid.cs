/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Http;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace YetaWF.Core.Endpoints;

/// <summary>
/// Endpoint support for optional Guid.
/// </summary>
public class OptionalGuid {

    public Guid? Value { get; set; }

    /// <summary>
    /// Parses endpoint parameters as optional Guid.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="optionalGuid"></param>
    /// <returns></returns>
    public static bool TryParse(string? value, out OptionalGuid? optionalGuid) {
        Guid? guid = null;
        if (value != null) {
            if (Guid.TryParse(value, out Guid g))
                guid = g;
        }
        optionalGuid = new OptionalGuid { Value = guid };
        return true;
    }

    /// <summary>
    /// Parses endpoint parameters as optional Guid.
    /// </summary>
    public static ValueTask<OptionalGuid> BindAsync(HttpContext context, ParameterInfo parameter) {
        Guid? guid = null;
        if (parameter != null && parameter.Name != null) {
            string? val = context.Request.Query[parameter.Name];
            if (val != null) {
                if (Guid.TryParse(val, out Guid g))
                    guid = g;
            }
        }
        return ValueTask.FromResult(new OptionalGuid { Value = guid });
    }
}
