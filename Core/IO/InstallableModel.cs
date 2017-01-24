/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System.Collections.Generic;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;

namespace YetaWF.Core.IO {
    public interface IInstallableModel {

        /// <summary>
        /// Returns whether the model is installed (assemblies may be present but the data source is not available)
        /// </summary>
        bool IsInstalled();

        /// <summary>
        /// Installs all required files/folders/SQL data.
        /// </summary>
        bool InstallModel(List<string> errorList);

        /// <summary>
        /// Uninstalls all files/folders/SQL data managed by this model.
        /// </summary>
        bool UninstallModel(List<string> errorList);

        /// <summary>
        /// Add site-specific data for this model
        /// </summary>
        void AddSiteData();

        /// <summary>
        /// Removes site-specific data for this model
        /// </summary>
        void RemoveSiteData();

        /// <summary>
        /// Exports data this model implements (in model defined chunk increments (0..n))
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="fileList"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool ExportChunk(int chunk, SerializableList<SerializableFile> fileList, out object obj);

        /// <summary>
        /// Imports all data this model implements (in model defined chunk increments (0..n))
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="fileList"></param>
        /// <param name="obj"></param>
        void ImportChunk(int chunk, SerializableList<SerializableFile> fileList, object obj);
    }
    public interface IInstallableModel2 {
        /// <summary>
        /// Upgrades all required files/folders/SQL data from lastSeenVersion to current package version.
        /// </summary>
        bool UpgradeModel(List<string> errorList, string lastSeenVersion);
    }
}