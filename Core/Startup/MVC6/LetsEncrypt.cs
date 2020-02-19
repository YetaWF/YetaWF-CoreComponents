/* Copyright �2020 Softel vdm, Inc.. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Certes;
using FluffySpoon.AspNet.LetsEncrypt;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core;
using YetaWF.Core.Support;

namespace YetaWF2.LetsEncrypt {

    public static class LetsEncrypt {

        public static void AddLetsEncrypt(this IServiceCollection services) {

            // https://github.com/ffMathy/FluffySpoon.AspNet.LetsEncrypt
            string domains = WebConfigHelper.GetValue<string>("LetsEncrypt", "Domains", null, Package: false);
            if (!string.IsNullOrWhiteSpace(domains)) {
                services.AddFluffySpoonLetsEncrypt(new LetsEncryptOptions() {
                    Email = WebConfigHelper.GetValue<string>("LetsEncrypt", "Email", Package: false, Required: true), // LetsEncrypt will send you an e-mail here when the certificate is about to expire
                    UseStaging = WebConfigHelper.GetValue<bool>("LetsEncrypt", "Staging", Package: false, Required: true), // false for production
                    Domains = WebConfigHelper.GetValue<string>("LetsEncrypt", "Domains", Package: false, Required: true).Split(new char[] { ',' }),
                    TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(WebConfigHelper.GetValue<int>("LetsEncrypt", "TimeUntilExpiryBeforeRenewal", 30, Package: false)), // renew automatically 30 days before expiry
                    TimeAfterIssueDateBeforeRenewal = TimeSpan.FromDays(WebConfigHelper.GetValue<int>("LetsEncrypt", "TimeAfterIssueDateBeforeRenewal", 7, Package: false)), // renew automatically 7 days after the last certificate was issued
                    CertificateSigningRequest = new CsrInfo() // these are your certificate details
                    {
                        CountryName = WebConfigHelper.GetValue<string>("LetsEncrypt", "CountryName", "United States", Package: false, Required: true),
                        Locality = WebConfigHelper.GetValue<string>("LetsEncrypt", "Locality", "US", Package: false, Required: true),
                        Organization = WebConfigHelper.GetValue<string>("LetsEncrypt", "Organization", Package: false, Required: true),
                        OrganizationUnit = WebConfigHelper.GetValue<string>("LetsEncrypt", "OrganizationUnit", Package: false, Required: true),
                        State = WebConfigHelper.GetValue<string>("LetsEncrypt", "State", Package: false, Required: true),
                    }
                });
                string certFolder = WebConfigHelper.GetValue<string>("LetsEncrypt", "Certs", Globals.DataFolder, Package: false);
                string certPath = Path.Combine(YetaWFManager.RootFolderWebProject, certFolder);
                services.AddFluffySpoonLetsEncryptCertificatePersistence(
                    (key, bytes) => {
                        Directory.CreateDirectory(certPath);
                        File.WriteAllBytes(Path.Combine(certPath, "certificate_" + key), bytes);
                        return Task.CompletedTask;
                    },
                    (key) => {
                        string certFile = Path.Combine(certPath, "certificate_" + key);
                        if (!File.Exists(certFile))
                            return Task.FromResult<byte[]>(null);
                        return Task.FromResult(File.ReadAllBytes(certFile));
                    });

                services.AddFluffySpoonLetsEncryptMemoryChallengePersistence();

                services.AddFluffySpoonLetsEncryptRenewalLifecycleHook<LetsEncryptLifecycleHook>();
            }

        }
        public static void UseLetsEncrypt (this IApplicationBuilder app) {
            string domains = WebConfigHelper.GetValue<string>("LetsEncrypt", "Domains", null, Package: false);
            if (!string.IsNullOrWhiteSpace(domains)) {
                app.UseFluffySpoonLetsEncrypt();
            }
        }
    }
}

#else
#endif
