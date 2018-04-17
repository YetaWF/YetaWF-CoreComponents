/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

#if MVC6

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YetaWF.Core.Packages;

namespace YetaWF2.Support
{

    public class YetaWFApplicationPartManager : ApplicationPartManager {
        public YetaWFApplicationPartManager() {

            IEnumerable<ApplicationPart> parts = DefaultAssemblyPartDiscoveryProvider.DiscoverAssemblyParts("YetaWF");
            foreach (ApplicationPart part in parts) {
                ApplicationParts.Add(part);
            }

            List<Assembly> assemblies = (from Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart p in ApplicationParts select p.Assembly).ToList();
            List<ApplicationPart> extraParts = FindExtraAssemblies(assemblies, AppDomain.CurrentDomain.BaseDirectory);
            foreach (ApplicationPart part in extraParts)
                ApplicationParts.Add(part);
        }
        // Add assemblies located in the folder which are not part of referenced assemblies (these are from installed binary packages)
        private List<ApplicationPart> FindExtraAssemblies(List<Assembly> assemblies, string baseDirectory) {
            List<ApplicationPart> list = new List<ApplicationPart>();
            string[] files = Directory.GetFiles(baseDirectory, "*.dll");
            foreach (string file in files) {
                Assembly found = (from Assembly a in assemblies where a.ManifestModule.FullyQualifiedName == file select a).FirstOrDefault();
                if (found == null) {
                    Assembly newAssembly = null;
                    if (!file.EndsWith("\\libuv.dll")) {// avoid exception spam
                        try {
                            newAssembly = Assembly.LoadFile(file);
                        } catch (Exception) { }
                    }
                    if (newAssembly != null) {
                        Package package = new Package(newAssembly);
                        if (package.IsValid) {
                            list.Add(new AssemblyPart(newAssembly));
                        }
                    }
                }
            }
            return list;
        }
    }
}

#else
#endif
