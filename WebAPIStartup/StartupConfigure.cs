/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using YetaWF.Core.Log;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.PackageSupport;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Middleware;
using YetaWF.Core.Support.Services;
using YetaWF2.LetsEncrypt;
using YetaWF2.Support;

#if !DEBUG
using Microsoft.AspNetCore.Server.Kestrel.Core;
#endif

namespace YetaWF.Core.WebAPIStartup;

/// <summary>
/// This class is used by a Web API service (typically a Docker container) during startup to initialize packages and perform startup processing.
/// </summary>
public class Startup {

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="configuration">An instance of an IConfiguration interface.</param>
    public Startup(IConfiguration configuration) { }

    /// <summary>
    /// Configures all services used by the application.
    /// </summary>
    /// <param name="services">An instance of an IServiceCollection interface.</param>
    public void ConfigureServices(IServiceCollection services) {

        // Some assemblies need to be preloaded if they're used before YetaWFApplicationPartManager is called.
        // usually any types used by AddDynamicServices or AddDynamicAuthentication.
        List<string>? asms = WebConfigHelper.GetValue<List<string>>("YetaWF_Core", "PreloadedAssemblies");
        if (asms != null) {
            foreach (string asm in asms)
                Assemblies.Load(asm);
        }

        services.AddMvc((options) => { })
            .ConfigureApplicationPartManager((partManager) => { YetaWFApplicationPartManager.AddAssemblies(partManager); });

#if !DEBUG
        // in release builds we allow sync I/O - Simply can't be sure that all sync I/O has been corrected in development
        services.Configure<KestrelServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
        });
        // If using IIS:
        services.Configure<IISServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
        });
#endif

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddRouting();
        services.AddHealthChecks();
        services.AddResponseCompression();

        services.AddDynamicServices();

        services.AddLetsEncrypt();

        // Add handling of ResourceAuthorize attribute otherwise we get
        // 'The AuthorizationPolicy named: 'ResourceAuthorize' was not found.'
        services.AddAuthorization(options => {
            options.AddPolicy("ResourceAuthorize", policyBuilder => {
                policyBuilder.Requirements.Add(new ResourceAuthorizeRequirement());
            });
        });
    }

    /// <summary>
    /// Configures the application.
    /// </summary>
    /// <param name="app">An instance of an IApplicationBuilder interface.</param>
    /// <param name="env">An instance of an IWebHostEnvironment interface.</param>
    /// <param name="svp">An instance of an IServiceProvider interface.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider svp) {

        IHttpContextAccessor httpContextAccessor = (IHttpContextAccessor)svp.GetService(typeof(IHttpContextAccessor))!;
        YetaWFManager.Init(httpContextAccessor);

        app.UseForwardedHeaders();
#if DEBUG
        app.UseDeveloperExceptionPage();
#endif
        app.UseResponseCompression();

        app.UseLetsEncrypt();

        app.UseMiddleware<DynamicPreRoutingMiddleware>();

        app.UseRouting();

        // Everything else
        app.Use(async (context, next) => {
            StartupRequest.StartRequest(context);
            await next();
        });

        app.UseEndpoints(endpoints => {

            endpoints.MapHealthChecks("/_health");

            Logging.AddLog($"Calling {nameof(AreaRegistrationBase)}.{nameof(AreaRegistrationBase.RegisterPackages)}");
            AreaRegistrationBase.RegisterPackages(endpoints);
        });

        app.StartYetaWF();
    }
}

/// <summary>
/// A dummy implementation of IAuthorizationRequirement to provide an implementation of the IAuthorizationRequirement interface.
/// </summary>
public class ResourceAuthorizeRequirement : IAuthorizationRequirement { }
