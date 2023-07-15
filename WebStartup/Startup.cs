/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using YetaWF.Core.Endpoints;
using YetaWF.Core.HttpHandler;
using YetaWF.Core.Identity;
using YetaWF.Core.Language;
using YetaWF.Core.Log;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.PackageSupport;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;
using YetaWF.Core.Support.Middleware;
using YetaWF.Core.Support.Services;
using YetaWF2.LetsEncrypt;
using YetaWF2.Logger;
using YetaWF2.Middleware;
using YetaWF2.Support;

namespace YetaWF.Core.WebStartup;

/// <summary>
/// This class is used by web application during startup to initialize packages and perform startup processing.
/// </summary>
public partial class Startup {

    /// <summary>
    /// The main application entry point.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="preRun">An optional method to execute before starting the application.</param>
    public static void Main(string[] args, Action? preRun = null) {

        string currPath = Directory.GetCurrentDirectory();

        if (Support.Startup.RunningInContainer) {
            {
                // Copy any new files from /DataInit to /Data
                // This is needed with Docker during first-time installs if a DataInit folder is present.
                string dataInitFolder = Path.Combine(currPath, "DataInit");
                if (Directory.Exists(dataInitFolder)) { // this is optional
                    string dataFolder = Path.Combine(currPath, Globals.DataFolder);
                    System.Console.WriteLine($"Initializing {dataFolder}");
                    CopyMissingFiles(dataInitFolder, dataFolder);
                }
            }
            {
                // Copy any new files from /DataLocalInit to /DataLocal
                // This is needed with Docker during first-time installs if a DataLocalInit folder is present.
                string dataLocalInitFolder = Path.Combine(currPath, "DataLocalInit");
                if (Directory.Exists(dataLocalInitFolder)) { // this is optional
                    string dataLocalFolder = Path.Combine(currPath, Globals.DataLocalFolder);
                    System.Console.WriteLine($"Initializing {dataLocalFolder}");
                    CopyMissingFiles(dataLocalInitFolder, dataLocalFolder);
                }
            }
            {
                // Copy any new files from the ./wwwroot/MaintenanceInit folder to ./wwwroot/Maintenance
                // This is needed with Docker during first-time installs.
                string maintInitFolder = Path.Combine(currPath, "wwwroot", "MaintenanceInit");
                if (Directory.Exists(maintInitFolder)) { // this is optional
                    string maintFolder = Path.Combine(currPath, "wwwroot", Globals.MaintenanceFolder);
                    System.Console.WriteLine($"Initializing {maintFolder}");
                    CopyMissingFiles(maintInitFolder, maintFolder);
                }
            }
        }

        string? hosting = GetHostingFile();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            Args = args,
            ApplicationName = Assembly.GetExecutingAssembly().FullName,
            ContentRootPath = currPath,
            WebRootPath = "wwwroot"
        });

        builder.Configuration.AddJsonFile(GetAppSettingsFile(), reloadOnChange: false, optional: false);
        if (hosting is not null)
            builder.Configuration.AddJsonFile(hosting);
        builder.Configuration.AddCommandLine(args);
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.SetBasePath(currPath);

        builder.WebHost.UseIIS();

        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddConsole();
        builder.Logging.AddEventSourceLogger();
        builder.Logging.AddYetaWFLogger();
        builder.Logging.AddDebug();

        YetaWFManager.RootFolder = builder.Environment.WebRootPath;
        YetaWFManager.RootFolderWebProject = builder.Environment.ContentRootPath;

        WebConfigHelper.InitAsync(GetAppSettingsFile()).Wait();
        LanguageSection.InitAsync(Path.Combine(YetaWFManager.RootFolderWebProject, Globals.DataFolder, YetaWF.Core.Support.Startup.LANGUAGESETTINGS)).Wait();

        builder.WebHost.UseKestrel(kestrelOptions => {
            long? maxReq = WebConfigHelper.GetValue<long?>("YetaWF_Core", "MaxRequestBodySize", 30000000);
            if (maxReq == 0)
                kestrelOptions.Limits.MaxRequestBodySize = null;
            else
                kestrelOptions.Limits.MaxRequestBodySize = maxReq;
#if !DEBUG
            kestrelOptions.AllowSynchronousIO = true;
#endif
        });
        builder.WebHost.UseIIS();
        builder.WebHost.UseIISIntegration();
        builder.WebHost.CaptureStartupErrors(true);
        builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, "true");

        // Some assemblies need to be preloaded if they're used before YetaWFApplicationPartManager is called.
        // usually any types used by AddDynamicServices or AddDynamicAuthentication.
        List<string>? asms = WebConfigHelper.GetValue<List<string>>("YetaWF_Core", "PreloadedAssemblies");
        if (asms != null) {
            foreach (string asm in asms)
                Assemblies.Load(asm);
        }



        // Services

        builder.Services.Configure<IISServerOptions>(iisOptions => {
#if !DEBUG
            iisOptions.AllowSynchronousIO = true;
#endif
        });

        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        builder.Services.AddRouting();
        builder.Services.AddHealthChecks();

        builder.Services.AddResponseCompression();

        builder.Services.Configure<KeyManagementOptions>(options => {
            options.XmlRepository = new DataProtectionKeyRepository();
        });

        //https://stackoverflow.com/questions/43860631/how-do-i-handle-validateantiforgerytoken-across-linux-servers
        //https://nicolas.guelpa.me/blog/2017/01/11/dotnet-core-data-protection-keys-repository.html
        //https://long2know.com/2017/06/net-core-sql-dataprotection-key-storage-provider-using-entity-framework/
        var encryptionSettings = new AuthenticatedEncryptorConfiguration() {
            EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
            ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
        };
        builder.Services
            .AddDataProtection()
            .PersistKeysToAppSettings()
            .SetDefaultKeyLifetime(new TimeSpan(100 * 365, 0, 0, 0))
            .SetApplicationName("__YetaWFDP_" + YetaWFManager.DefaultSiteName)
            .UseCryptographicAlgorithms(encryptionSettings);

        // set antiforgery cookie
        builder.Services.AddAntiforgery(opts => {
            opts.Cookie.Name = "__ReqVerToken_" + YetaWFManager.DefaultSiteName;
            opts.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
            opts.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
            opts.SuppressXFrameOptionsHeader = true;
            opts.HeaderName = "RequestVerificationToken";
        });

        builder.Services.AddMemoryCache((o) => {
            o.TrackStatistics = WebConfigHelper.GetValue<bool>("MemoryCache", "TrackStatistics", true, Package: false);
            o.TrackLinkedCacheEntries = WebConfigHelper.GetValue<bool>("MemoryCache", "TrackLinkedCacheEntries", true, Package: false);
            o.SizeLimit = WebConfigHelper.GetValue<long>("MemoryCache", "SizeLimit", 20 * 1024 * 1024, Package: false);
        });

        // Memory or distributed caching
        string distProvider = WebConfigHelper.GetValue<string>("SessionState", "Provider", "", Package: false)!.ToLower();
        if (distProvider == "redis") {
            string config = WebConfigHelper.GetValue<string>("SessionState", "RedisConfig", "localhost:6379", Package: false)!;
            builder.Services.AddStackExchangeRedisCache(o => {
                o.Configuration = config;
            });
        } else if (distProvider == "sql") {
            string sqlConn = WebConfigHelper.GetValue<string>("SessionState", "SqlCache-Connection", null, Package: false)!;
            string sqlSchema = WebConfigHelper.GetValue<string>("SessionState", "SqlCache-Schema", null, Package: false)!;
            string sqlTable = WebConfigHelper.GetValue<string>("SessionState", "SqlCache-Table", null, Package: false)!;
            if (string.IsNullOrWhiteSpace(sqlConn) || string.IsNullOrWhiteSpace(sqlSchema) || string.IsNullOrWhiteSpace(sqlTable)) {
                builder.Services.AddDistributedMemoryCache();
            } else {
                // Create sql table (in .\src folder): dotnet sql-cache create "Data Source=...;Initial Catalog=...;Integrated Security=True;" dbo SessionCache
                // to use distributed sql server cache
                // MAKE SURE TO CHANGE \\ TO \ WHEN COPYING THE CONNECTION STRING!!!
                builder.Services.AddDistributedSqlServerCache(options => {
                    options.ConnectionString = sqlConn;
                    options.SchemaName = sqlSchema;
                    options.TableName = sqlTable;
                });
            }
        } else {
            builder.Services.AddDistributedMemoryCache();
        }

        // Session state
        int sessionTimeout = WebConfigHelper.GetValue<int>("SessionState", "Timeout", 1440, Package: false);
        string sessionCookie = WebConfigHelper.GetValue<string>("SessionState", "CookieName", ".YetaWF.Session", Package: false)!;
        builder.Services.AddSession(options => {
            options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeout);
            options.Cookie.Name = sessionCookie;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        builder.Services.AddSingleton<IAuthorizationHandler, ResourceAuthorizeHandler>();
        builder.Services.AddAuthorization(options => {
            options.AddPolicy("ResourceAuthorize", policyBuilder => {
                policyBuilder.Requirements.Add(new ResourceAuthorizeRequirement());
            });
        });


        // load assembly and initialize identity services
        IdentityCreator.Setup(builder.Services);
        IdentityCreator.SetupLoginProviders(builder.Services);

        // Add framework services.
        builder.Services.AddMvcCore()
            .ConfigureApplicationPartManager(YetaWFApplicationPartManager.AddAssemblies);

        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => { // Minimal API serialization
            options.SerializerOptions.PropertyNamingPolicy = null;
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            Utility.AddConverters(options.SerializerOptions.Converters);
        });

        // We don't need a view engine
        builder.Services.Configure<MvcViewOptions>(
            options => {
                options.ViewEngines.Clear();
            });

        builder.Services.Configure<ForwardedHeadersOptions>(options => {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        builder.Services.AddDynamicServices();

        YetaWF.Core.SignalR.ConfigureServices(builder.Services);

        builder.Services.AddLetsEncrypt();



        // Pipeline

        var app = builder.Build();

        IHttpContextAccessor httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
        YetaWFManager.Init(httpContextAccessor);

        app.UseYetaWFForwardedHeaders();
#if DEBUG
        app.UseDeveloperExceptionPage();
#endif
        try {
            RewriteOptions rewriteOptions = new RewriteOptions();
            if (File.Exists("Web.config"))
                rewriteOptions.AddIISUrlRewrite(builder.Environment.ContentRootFileProvider, "Web.config");
            if (File.Exists(".htaccess"))
                rewriteOptions.AddApacheModRewrite(builder.Environment.ContentRootFileProvider, ".htaccess");
        } catch (Exception exc) {
            Logging.AddLog($"URL rewrite failed - {ErrorHandling.FormatExceptionMessage(exc)}");
        }

        app.UseResponseCompression();

        // Error handler for ajax/post exceptions - returns special text with errors for display client side
        // This must appear after more generic error handlers (like UseDeveloperExceptionPage)
        app.UseMiddleware(typeof(ErrorHandlingMiddleware));

        // request blocking middleware
        app.UseMiddleware<BlockRequestMiddleware>();
        BlockRequestMiddleware.LoadBlockSettingsAsync().Wait();

        // Ignore extensions that are known to be invalid files
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
                FileProvider = new PhysicalFileProvider(Path.Combine(YetaWFManager.RootFolder, @".well-known")),
                RequestPath = new PathString("/.well-known")
            });
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = new PhysicalFileProvider(Path.Combine(YetaWFManager.RootFolder, Globals.MaintenanceFolder)),
                RequestPath = new PathString(Utility.PhysicalToUrl(Path.Combine(YetaWFManager.RootFolder, Globals.MaintenanceFolder))),
                OnPrepareResponse = (context) => {
                    YetaWFManager.SetStaticCacheInfo(context.Context);
                }
            });
        }

        {
            // Set up custom content types for static files based on MimeSettings.json and location
            // Everything else in wwwroot is based on mimetype. Only mime types with Download=true can be downloaded.
            FileExtensionContentTypeProvider provider = new FileExtensionContentTypeProvider(new Dictionary<string, string>());

            MimeSection staticMimeSect = new MimeSection();
            staticMimeSect.InitAsync(Path.Combine(Globals.DataFolder, MimeSection.MimeSettingsFile)).Wait();
            List<MimeSection.MimeEntry>? mimeTypes = staticMimeSect.GetMimeTypes();
            if (mimeTypes != null) {
                foreach (MimeSection.MimeEntry entry in mimeTypes) {
                    if (entry.Download && entry.Extensions != null) {
                        string[] extensions = entry.Extensions.Split(new char[] { ';' });
                        foreach (string extension in extensions) {
                            if (!provider.Mappings.ContainsKey(extension.Trim()))
                                provider.Mappings.Add(extension.Trim(), entry.Type);
                        }
                    }
                }
            }
            app.UseStaticFiles(new StaticFileOptions {
                FileProvider = new PhysicalFileProvider(Path.Combine(YetaWFManager.RootFolder, Globals.AddonsFolder)),
                RequestPath = new PathString(Globals.AddonsUrl),
                ContentTypeProvider = provider,
                OnPrepareResponse = (context) => {
                    YetaWFManager.SetStaticCacheInfo(context.Context);
                }
            });
            string vaultFolder = Path.Combine(YetaWFManager.RootFolder, Globals.VaultFolder);
            if (Directory.Exists(vaultFolder)) {
                app.UseStaticFiles(new StaticFileOptions {
                    FileProvider = new PhysicalFileProvider(vaultFolder),
                    RequestPath = $"/{Globals.VaultFolder}",
                    ContentTypeProvider = provider,
                    OnPrepareResponse = (context) => {
                        YetaWFManager.SetStaticCacheInfo(context.Context);
                    }
                });
            }
        }

        app.UseSession();

        app.UseMiddleware<DynamicPreRoutingMiddleware>();

        app.UseRouting();// required so static files are served, otherwise catchall below overrides static middleware because it matches all endpoints

        //app.UseCors();

        // Everything else
        app.Use(async (context, next) => {
            await YetaWF.Core.Support.StartupRequest.StartRequestAsync(context, false);
            await next();
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseLetsEncrypt();

        app.Use(async (context, next) => {
            await PageContentEndpoints.SetupEnvironmentInfoAsync();
            await next();
        });

        app.MapHealthChecks("/_health");

        Logging.AddLog($"Calling {nameof(AreaRegistrationBase)}.{nameof(AreaRegistrationBase.RegisterPackages)}");
        AreaRegistrationBase.RegisterPackages(app);

        YetaWF.Core.SignalR.ConfigureHubs(app);

        app.StartYetaWF();

        app.Run();
    }

    /// <summary>
    /// Returns an environment and runtime specific AppSettings.json file name.
    /// </summary>
    /// <returns>Returns an environment and runtime specific AppSettings.json file name.</returns>
    public static string GetAppSettingsFile() {
        if (_AppSettingsFile == null)
            _AppSettingsFile = Support.Startup.GetEnvironmentFile(Path.Combine(Directory.GetCurrentDirectory(), Globals.DataFolder), "AppSettings", "json")!;
        return _AppSettingsFile;
    }
    private static string? _AppSettingsFile = null;

    /// <summary>
    /// Returns an environment and runtime specific hosting.json file name.
    /// </summary>
    /// <returns>Returns an environment and runtime specific hosting.json file name.</returns>
    public static string? GetHostingFile() {
        return Support.Startup.GetEnvironmentFile(Directory.GetCurrentDirectory(), "hosting", "json", Optional: true);
    }

    //private static void CopyFiles(string srcInitFolder, string targetFolder) {
    //    Directory.CreateDirectory(targetFolder);
    //    string[] files = Directory.GetFiles(srcInitFolder);
    //    foreach (string file in files) {
    //        File.Copy(file, Path.Combine(targetFolder, Path.GetFileName(file)));
    //    }
    //    string[] dirs = Directory.GetDirectories(srcInitFolder);
    //    foreach (string dir in dirs) {
    //        CopyFiles(dir, Path.Combine(targetFolder, Path.GetFileName(dir)));
    //    }
    //}
    //private static bool IsEmptyDirectory(string folder) {
    //    return Directory.GetFiles(folder).Length == 0 && Directory.GetDirectories(folder).Length == 0;
    //}
    private static void CopyMissingFiles(string srcInitFolder, string targetFolder) {
        Directory.CreateDirectory(targetFolder);
        string[] files = Directory.GetFiles(srcInitFolder);
        foreach (string file in files) {
            string targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
            if (!File.Exists(targetFile))
                File.Copy(file, targetFile);
        }
        string[] dirs = Directory.GetDirectories(srcInitFolder);
        foreach (string dir in dirs) {
            CopyMissingFiles(dir, Path.Combine(targetFolder, Path.GetFileName(dir)));
        }
    }
}
