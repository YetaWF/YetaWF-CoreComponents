/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.Controllers;
using YetaWF.Core.DataProvider;
using YetaWF.Core.HttpHandler;
using YetaWF.Core.Identity;
using YetaWF.Core.Language;
using YetaWF.Core.Log;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;
using YetaWF.Core.Pages;
using YetaWF.Core.Site;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Middleware;
using YetaWF.Core.Support.Services;
using YetaWF.Core.Views;
using YetaWF2.LetsEncrypt;
using YetaWF2.Middleware;
using YetaWF2.Support;

#if !DEBUG
using Microsoft.AspNetCore.Server.Kestrel.Core;
#endif

// Migrating to asp.net core 3.0 https://docs.microsoft.com/en-us/aspnet/core/migration/22-to-30?view=aspnetcore-3.0

namespace YetaWF.Core.WebStartup {

    /// <summary>
    /// The class implementing all startup processing for a YetaWF website.
    /// </summary>
    public partial class StartupMVC6 {

        private IServiceCollection Services = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="env">An instance of an IWebHostEnvironment interface.</param>
        public StartupMVC6(IWebHostEnvironment env) {

            YetaWFManager.RootFolder = env.WebRootPath;
            YetaWFManager.RootFolderWebProject = env.ContentRootPath;

            WebConfigHelper.InitAsync(GetAppSettingsFile()).Wait();
            LanguageSection.InitAsync(Path.Combine(YetaWFManager.RootFolderWebProject, Globals.DataFolder, YetaWF.Core.Support.Startup.LANGUAGESETTINGS)).Wait();
        }

        /// <summary>
        /// This method gets called by the runtime. This method adds services to the container.
        /// </summary>
        /// <param name="services">An instance of an IServiceCollection interface.</param>
        public void ConfigureServices(IServiceCollection services) {

            Services = services;

            // Some assemblies need to be preloaded if they're used before YetaWFApplicationPartManager is called.
            // usually any types used by AddDynamicServices or AddDynamicAuthentication.
            List<string> asms = WebConfigHelper.GetValue<List<string>>("YetaWF_Core", "PreloadedAssemblies");
            if (asms != null) {
                foreach (string asm in asms)
                    Assemblies.Load(asm);
            }

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

            services.AddRouting();
            services.AddHealthChecks();

            services.AddResponseCompression();

            services.Configure<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new DataProtectionKeyRepository();
            });

            //https://stackoverflow.com/questions/43860631/how-do-i-handle-validateantiforgerytoken-across-linux-servers
            //https://nicolas.guelpa.me/blog/2017/01/11/dotnet-core-data-protection-keys-repository.html
            //https://long2know.com/2017/06/net-core-sql-dataprotection-key-storage-provider-using-entity-framework/
            var encryptionSettings = new AuthenticatedEncryptorConfiguration() {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
            };
            services
                .AddDataProtection()
                .PersistKeysToAppSettings()
                .SetDefaultKeyLifetime(new TimeSpan(100*365, 0, 0, 0))
                .SetApplicationName("__YetaWFDP_" + YetaWFManager.DefaultSiteName)
                .UseCryptographicAlgorithms(encryptionSettings);

            // set antiforgery cookie
            services.AddAntiforgery(opts => {
                opts.Cookie.Name = "__ReqVerToken_" + YetaWFManager.DefaultSiteName;
                opts.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                opts.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
                opts.SuppressXFrameOptionsHeader = true;
            });
            // antiforgery filter for conditional antiforgery attribute
            services.AddSingleton<ConditionalAntiForgeryTokenFilter>();

            services.AddMemoryCache();

            // Memory or distributed caching
            string distProvider = WebConfigHelper.GetValue<string>("SessionState", "Provider", "", Package: false).ToLower();
            if (distProvider == "redis") {
                string config = WebConfigHelper.GetValue<string>("SessionState", "RedisConfig", "localhost:6379", Package: false);
                services.AddDistributedRedisCache(o => {
                    o.Configuration = config;
                });
            } else if (distProvider == "sql") {
                string sqlConn = WebConfigHelper.GetValue<string>("SessionState", "SqlCache-Connection", null, Package: false);
                string sqlSchema = WebConfigHelper.GetValue<string>("SessionState", "SqlCache-Schema", null, Package: false);
                string sqlTable = WebConfigHelper.GetValue<string>("SessionState", "SqlCache-Table", null, Package: false);
                if (string.IsNullOrWhiteSpace(sqlConn) || string.IsNullOrWhiteSpace(sqlSchema) || string.IsNullOrWhiteSpace(sqlTable)) {
                    services.AddDistributedMemoryCache();
                } else {
                    // Create sql table (in .\src folder): dotnet sql-cache create "Data Source=...;Initial Catalog=...;Integrated Security=True;" dbo SessionCache
                    // to use distributed sql server cache
                    // MAKE SURE TO CHANGE \\ TO \ WHEN COPYING THE CONNECTION STRING!!!
                    services.AddDistributedSqlServerCache(options => {
                        options.ConnectionString = sqlConn;
                        options.SchemaName = sqlSchema;
                        options.TableName = sqlTable;
                    });
                }
            } else {
                services.AddDistributedMemoryCache();
            }

            // Session state
            int sessionTimeout = WebConfigHelper.GetValue<int>("SessionState", "Timeout", 1440, Package: false);
            string sessionCookie = WebConfigHelper.GetValue<string>("SessionState", "CookieName", ".YetaWF.Session", Package: false);
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeout);
                options.Cookie.Name = sessionCookie;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

            services.AddSingleton<IAuthorizationHandler, ResourceAuthorizeHandler>();
            services.AddAuthorization(options => {
                options.AddPolicy("ResourceAuthorize", policyBuilder => {
                    policyBuilder.Requirements.Add(new ResourceAuthorizeRequirement());
                });
            });

            // load assembly and initialize identity services
            IdentityCreator.Setup(services);
            IdentityCreator.SetupLoginProviders(services);

            // We need to replace the default Html Generator because it adds id= to every tag despite not explicitly requested, which is dumb and can cause duplicate ids (not
            // permitted  in w3c validation). Why would MVC6 start adding ids to tags when they're not requested. If they're not requested, does the caller really need or use them???
            services.AddSingleton(typeof(IHtmlGenerator), typeof(YetaWFDefaultHtmlGenerator));

            // Replace the default simple type model binder provider with our own.
            // The built in simple type model binder converts spaces to null. Or if ConvertEmptyStringToNull is set to true an empty string remains an empty string, instead of null.
            // This restores "" -> null and "   " -> "   " which is not an option with the built in binder. This behavior was used on ASP.NET 4 and I want to keep it.
            // I'm not about that retesting life.
            services.AddControllers(options => {
                options.ModelBinderProviders.Insert(0, new YetaWFSimpleTypeModelBinderProvider());
            });

            // Add framework services.
            services.AddMvc((options) => {
                // we have to remove the SaveTempDataAttribute filter, otherwise our ActionHelper.Action extension
                // doesn't work as the filter sets httpContext.Items[someobject] to signal that the action has completed.
                // obviously this doesn't work if there are multiple actions (which there always are).
                // YetaWF doesn't use tempdata so this is useless anyway. And this SaveTempDataAttribute seems borked...
                options.Filters.Remove(new Microsoft.AspNetCore.Mvc.ViewFeatures.SaveTempDataAttribute());
                // We need to roll our own support for AdditionalMetadataAttribute, IMetadataAware
                options.ModelMetadataDetailsProviders.Add(new AdditionalMetadataProvider());

                // Error handling for controllers, not used, we handle action errors instead so this is not needed
                // options.Filters.Add(new ControllerExceptionFilterAttribute()); // controller exception filter, not used

                // AreaConvention to simplify Area discovery (using IControllerModelConvention)
                options.Conventions.Add(new AreaConventionAttribute());
            })
            .AddNewtonsoftJson()
            .ConfigureApplicationPartManager((partManager) => { YetaWFApplicationPartManager.AddAssemblies(partManager); })
            .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            // We don't need a view engine
            services.Configure<MvcViewOptions>(
                options => {
                    options.ViewEngines.Clear();
                });

            services.Configure<ForwardedHeadersOptions>(options => {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddDynamicServices();

            YetaWF.Core.SignalR.ConfigureServices(services);

            services.AddLetsEncrypt();
        }

        /// <summary>
        /// This method gets called by the runtime. This method configures the HTTP request pipeline.
        /// </summary>
        /// <param name="app">An instance of an IApplicationBuilder interface.</param>
        /// <param name="env">An instance of an IWebHostEnvironment interface.</param>
        /// <param name="svp">An instance of an IServiceProvider interface.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider svp) {
            ConfigureAsync(app, env, svp).Wait(); // sync Wait because we want to be async in Configure()/ConfigureAsync()
        }
        private async Task ConfigureAsync(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider svp) {

            IHttpContextAccessor httpContextAccessor = (IHttpContextAccessor)svp.GetService(typeof(IHttpContextAccessor));
            IMemoryCache memoryCache = (IMemoryCache)svp.GetService(typeof(IMemoryCache));
            YetaWFManager.Init(httpContextAccessor, memoryCache, app.ApplicationServices);

            app.UseYetaWFForwardedHeaders();
#if DEBUG
            app.UseDeveloperExceptionPage();
#endif
            try {
                RewriteOptions rewriteOptions = new RewriteOptions();
                if (File.Exists("Web.config"))
                    rewriteOptions.AddIISUrlRewrite(env.ContentRootFileProvider, "Web.config");
                if (File.Exists(".htaccess"))
                    rewriteOptions.AddApacheModRewrite(env.ContentRootFileProvider, ".htaccess");
            } catch (Exception exc) {
                Logging.AddLog($"URL rewrite failed - {ErrorHandling.FormatExceptionMessage(exc)}");
            }

            app.UseResponseCompression();

            // Error handler for ajax/post exceptions - returns special text with errors for display client side
            // This must appear after more generic error handlers (like UseDeveloperExceptionPage)
            app.UseMiddleware(typeof(ErrorHandlingMiddleware));

            // Ignore extensions that are known not to be valid files
            app.UseMiddleware<IgnoreRouteMiddleware>();

            // Css Handler
            app.MapWhen(
                context => {
                    string path = context.Request.Path.ToString().ToLower();
                    return path.EndsWith(".css");
                },
                appBranch => {
                    appBranch.UseMiddleware<CssMiddleware>();
                });

            // Image Handler
            app.MapWhen(
                context => {
                    string path = context.Request.Path.ToString().ToLower();
                    return path == "/filehndlr.image";
                },
                appBranch => {
                    appBranch.UseMiddleware<ImageMiddleware>();
                });

            // PNG,JPG -> WEBP Handler
            app.MapWhen(
                context => {
                    string path = context.Request.Path.ToString().ToLower();
                    return WebpHttpHandler.IsValidExtension(path);
                },
                appBranch => {
                    appBranch.UseMiddleware<WebpMiddleware>();
                });

            // Set up custom content types for static files based on MimeSettings.json and location
            {
                // Serve any file from these locations
                app.UseStaticFiles(new StaticFileOptions {
                    FileProvider = new PhysicalFileProvider(Path.Combine(YetaWFManager.RootFolderWebProject, @"node_modules")),
                    RequestPath = new PathString("/" + Globals.NodeModulesFolder),
                    OnPrepareResponse = (context) => {
                        YetaWFManager.SetStaticCacheInfo(context.Context);
                    }
                });
                app.UseStaticFiles(new StaticFileOptions {
                    FileProvider = new PhysicalFileProvider(Path.Combine(YetaWFManager.RootFolderWebProject, @"bower_components")),
                    RequestPath = new PathString("/" + Globals.BowerComponentsFolder),
                    OnPrepareResponse = (context) => {
                        YetaWFManager.SetStaticCacheInfo(context.Context);
                    }
                });

                // everything else
                MimeSection staticMimeSect = new MimeSection();
                await staticMimeSect.InitAsync(Path.Combine(Globals.DataFolder, MimeSection.MimeSettingsFile));

                app.UseStaticFiles(new StaticFileOptions {
                    ContentTypeProvider = new FileExtensionContentTypeProvider(),
                    OnPrepareResponse = (context) => {
                        YetaWFManager.SetStaticCacheInfo(context.Context);
                    }
                });
            }

            app.UseSession();

            app.UseMiddleware<DynamicPreRoutingMiddleware>();

            app.UseRouting();

            //app.UseCors();

            // Everything else
            app.Use(async (context, next) => {
                await StartupRequest.StartRequestAsync(context, false);
                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseLetsEncrypt();

            app.UseEndpoints(endpoints => {

                endpoints.MapHealthChecks("/_health");

                Logging.AddLog($"Calling {nameof(AreaRegistrationBase)}.{nameof(AreaRegistrationBase.RegisterPackages)}");
                AreaRegistrationBase.RegisterPackages(endpoints);

                YetaWF.Core.SignalR.ConfigureHubs(endpoints);

                Logging.AddLog("Adding catchall route");
                endpoints.MapAreaControllerRoute(name: "Page", areaName: "YetaWF_Core", pattern: "{**path}", defaults: new { controller = "Page", action = "Show" });
            });


            StartYetaWF();
        }

        private static object _lockObject = new object();

        private static void StartYetaWF() {

            if (!YetaWF.Core.Support.Startup.Started) {

                lock (_lockObject) { // protect from duplicate startup

                    if (!YetaWF.Core.Support.Startup.Started) {

                        YetaWFManager.Syncify(async () => { // startup code

                            // Create a startup log file
                            StartupLogging startupLog = new StartupLogging();
                            await Logging.RegisterLoggingAsync(startupLog);

                            Logging.AddLog($"{nameof(StartYetaWF)} starting");

                            YetaWFManager manager = YetaWFManager.MakeInitialThreadInstance(new SiteDefinition() { SiteDomain = "__STARTUP" }, null); // while loading packages we need a manager
                            YetaWFManager.Syncify(async () => {
                                // External data providers
                                ExternalDataProviders.RegisterExternalDataProviders();
                                // Call all classes that expose the interface IInitializeApplicationStartup
                                await YetaWF.Core.Support.Startup.CallStartupClassesAsync();

                                if (!YetaWF.Core.Support.Startup.MultiInstance)
                                    await Package.UpgradeToNewPackagesAsync();

                                YetaWF.Core.Support.Startup.Started = true;
                            });

                            // Stop startup log file
                            Logging.UnregisterLogging(startupLog);

                            // start real logging
                            await Logging.SetupLoggingAsync();

                            YetaWFManager.RemoveThreadInstance(); // Remove startup manager

                            Logging.AddLog($"{nameof(StartYetaWF)} completed");
                        });
                    }
                }
            }
        }
    }
}
