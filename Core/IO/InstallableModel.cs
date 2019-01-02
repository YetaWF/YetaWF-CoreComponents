/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.DataProvider;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;

namespace YetaWF.Core.IO {

    public interface IInstallableModel {

        /// <summary>
        /// Returns whether the model is installed (assemblies may be present but the data source is not available)
        /// </summary>
        Task<bool> IsInstalledAsync();

        /// <summary>
        /// Installs all required files/folders/SQL data.
        /// </summary>
        Task<bool> InstallModelAsync(List<string> errorList);

        /// <summary>
        /// Uninstalls all files/folders/SQL data managed by this model.
        /// </summary>
        Task<bool> UninstallModelAsync(List<string> errorList);

        /// <summary>
        /// Add site-specific data for this model
        /// </summary>
        Task AddSiteDataAsync();

        /// <summary>
        /// Removes site-specific data for this model
        /// </summary>
        Task RemoveSiteDataAsync();

        /// <summary>
        /// Exports data this model implements (in model defined chunk increments (0..n))
        /// </summary>
        Task<DataProviderExportChunk> ExportChunkAsync(int chunk, SerializableList<SerializableFile> fileList);

        /// <summary>
        /// Imports all data this model implements (in model defined chunk increments (0..n))
        /// </summary>
        Task ImportChunkAsync(int chunk, SerializableList<SerializableFile> fileList, object obj);

        /// <summary>
        /// Translate all data for this model into the specified language.
        /// </summary>
        Task LocalizeModelAsync(string language, Func<string, bool> isHtml, Func<List<string>, Task<List<string>>> translateStringsAsync, Func<string, Task<string>> translateComplexStringAsync);
    }
    public interface IInstallableModel2 {
        /// <summary>
        /// Upgrades all required files/folders/SQL data from lastSeenVersion to current package version.
        /// </summary>
        Task<bool> UpgradeModelAsync(List<string> errorList, string lastSeenVersion);
    }
}