﻿/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using YetaWF.Core.IO;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;
using YetaWF.Core.Upload;

namespace YetaWF.Core.DataProvider {

    public abstract partial class DataProviderImpl : IDisposable {

        protected DataProviderImpl(int siteIdentity) {
            SiteIdentity = siteIdentity;
            DisposableTracker.AddObject(this);
        }
        public void Dispose() {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                DisposableTracker.RemoveObject(this);
                if (_dataProvider != null)
                    _dataProvider.Dispose();
                _dataProvider = null;
            }
        }
        //~DataProviderImpl() { Dispose(false); }

        private dynamic _dataProvider { get; set; }

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        protected bool HaveManager { get { return YetaWFManager.HaveManager; } }

        protected Dictionary<string, object> Options { get; set; }
        protected int SiteIdentity { get; set; }
        protected Package Package { get; set; }
        protected string Dataset { get; set; }

        public const string DefaultString = "Default";
        public const string IOModeString = "IOMode";
        private const string NoIOMode = "none";

        public const int IDENTITY_SEED = 1000;

        public dynamic GetDataProvider() {
            return _dataProvider;
        }
        protected void SetDataProvider(dynamic dp) {
            _dataProvider = dp;
        }

        protected dynamic MakeDataProvider(Package package, string dataset, int dummy = -1, int SiteIdentity = 0, bool Cacheable = false, object Parms = null) {
            BuildOptions(package, dataset, SiteIdentity: SiteIdentity, Cacheable: Cacheable, Parms: Parms);
            return MakeExternalDataProvider(Options);
        }
        protected dynamic CreateDataProviderIOMode(Package package, string dataset, int dummy = -1, int SiteIdentity = 0, bool Cacheable = false, object Parms = null,
                Func<string, Dictionary<string, object>, dynamic> Callback = null) {
            if (Callback == null) throw new InternalError("No callback provided");
            BuildOptions(package, dataset, SiteIdentity: SiteIdentity, Cacheable: Cacheable, Parms: Parms, UsePackageIOMode: false);
            return Callback(ExternalIOMode, Options);
        }
        protected void BuildOptions(Package package, string dataset, int dummy = -1, int SiteIdentity = 0, bool Cacheable = false, object Parms = null, bool UsePackageIOMode = true) {
            Package = package;
            Dataset = dataset;

            if (_defaultIOMode == null) {
                _defaultIOMode = WebConfigHelper.GetValue<string>(DefaultString, IOModeString);
                if (_defaultIOMode == null)
                    throw new InternalError("Default IOMode is missing");
            }
            string ioMode = WebConfigHelper.GetValue<string>(Dataset, IOModeString);
            if (string.IsNullOrWhiteSpace(ioMode)) {
                if (UsePackageIOMode)
                    ioMode = WebConfigHelper.GetValue<string>(Package.AreaName, IOModeString);
                if (string.IsNullOrWhiteSpace(ioMode))
                    ioMode = _defaultIOMode;
            }
            ExternalIOMode = ioMode.ToLower();

            Options = new Dictionary<string, object>();
            if (Parms != null) {
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(Parms)) {
                    object val = property.GetValue(Parms);
                    Options.Add(property.Name, val);
                }
            }
            Options.Add(nameof(Package), Package);
            Options.Add(nameof(Dataset), Dataset);
            Options.Add(nameof(SiteIdentity), SiteIdentity);
            Options.Add(nameof(Cacheable), Cacheable);
        }
        private static string _defaultIOMode = null;

        // TRANSACTIONS
        // TRANSACTIONS
        // TRANSACTIONS

        private IDataProviderTransactions GetIDataProviderTransactions() {
            IDataProviderTransactions transDP = GetDataProvider() as IDataProviderTransactions;
            if (transDP == null) throw new InternalError($"Data provider {GetDataProvider().GetType().FullName} has no IDataProviderTransactions interface");
            return transDP;
        }

        public DataProviderTransaction StartTransaction() {
            return GetIDataProviderTransactions().StartTransaction();
        }
        protected Task CommitTransactionAsync() {
            return GetIDataProviderTransactions().CommitTransactionAsync();
        }
        protected void AbortTransaction() {
            GetIDataProviderTransactions().AbortTransaction();
        }

        // IINSTALLABLEMODEL ASYNC
        // IINSTALLABLEMODEL ASYNC
        // IINSTALLABLEMODEL ASYNC

        public Task<bool> IsInstalledAsync() {
            if (GetDataProvider() == null) return Task.FromResult(true);
            return GetDataProvider().IsInstalledAsync();
        }
        public Task<bool> InstallModelAsync(List<string> errorList) {
            if (YetaWF.Core.Support.Startup.MultiInstance) throw new InternalError("Installing new models is not possible when distributed caching is enabled");
            if (GetDataProvider() == null) return Task.FromResult(true);
            return GetDataProvider().InstallModelAsync(errorList);
        }
        public Task AddSiteDataAsync() {
            if (YetaWF.Core.Support.Startup.MultiInstance) throw new InternalError("Adding site data is not possible when distributed caching is enabled");
            if (GetDataProvider() == null) return Task.CompletedTask;
            return GetDataProvider().AddSiteDataAsync();
        }
        public Task RemoveSiteDataAsync() {
            if (YetaWF.Core.Support.Startup.MultiInstance) throw new InternalError("Removing site data is not possible when distributed caching is enabled");
            if (GetDataProvider() == null) return Task.CompletedTask;
            return GetDataProvider().RemoveSiteDataAsync();
        }
        public Task<bool> UninstallModelAsync(List<string> errorList) {
            if (YetaWF.Core.Support.Startup.MultiInstance) throw new InternalError("Uninstalling models is not possible when distributed caching is enabled");
            if (GetDataProvider() == null) return Task.FromResult(true);
            return GetDataProvider().UninstallModelAsync(errorList);
        }
        public Task<DataProviderExportChunk> ExportChunkAsync(int chunk, SerializableList<SerializableFile> fileList) {
            if (GetDataProvider() == null) return Task.FromResult(new DataProviderExportChunk());
            return GetDataProvider().ExportChunkAsync(chunk, fileList);
        }
        public Task ImportChunkAsync(int chunk, SerializableList<SerializableFile> fileList, object obj) {
            if (GetDataProvider() == null) return Task.CompletedTask;
            return GetDataProvider().ImportChunkAsync(chunk, fileList, obj);
        }
        public Task LocalizeModelAsync(string language, Func<string, bool> isHtml, Func<List<string>, Task<List<string>>> translateStringsAsync, Func<string, Task<string>> translateComplexStringAsync) {
            if (GetDataProvider() == null) return Task.CompletedTask;
            return GetDataProvider().LocalizeModelAsync(language, isHtml, translateStringsAsync, translateComplexStringAsync);
        }

        // IMAGE HANDLING
        // IMAGE HANDLING
        // IMAGE HANDLING

        public static async Task SaveImagesAsync(Guid moduleGuid, object obj) {
            Type objType = obj.GetType();
            List<PropertyData> propData = ObjectSupport.GetPropertyData(objType);
            foreach (var prop in propData) {
                // look for Image UIHint
                if (prop.UIHint == "Image") {
                    bool hasData = prop.GetAdditionalAttributeValue<bool>("Data", true);
                    bool hasFile = prop.GetAdditionalAttributeValue<bool>("File", false);
                    bool hasDontSave = prop.HasAttribute("DontSave");
                    if ((!hasDontSave || hasData) && prop.PropInfo.CanRead && prop.PropInfo.CanWrite) {
                        if (hasFile) {
                            // save as file
                            PropertyData pGuid = ObjectSupport.GetPropertyData(objType, prop.Name + "_Guid");
                            Guid origFileGuid = pGuid.GetPropertyValue<Guid>(obj);
                            string fileName = prop.GetPropertyValue<string>(obj);
                            Guid newFileGuid = await ConvertImageToFileAsync(moduleGuid, prop.Name, origFileGuid, fileName);
                            if (origFileGuid != newFileGuid) {
                                pGuid.PropInfo.SetValue(obj, newFileGuid);
                                prop.PropInfo.SetValue(obj, null);// reset name so it's re-evaluated
                            }
                        } else if (hasData) {
                            // save as data
                            PropertyData pData = ObjectSupport.GetPropertyData(objType, prop.Name + "_Data");
                            byte[] currImageData = pData.GetPropertyValue<byte[]>(obj);
                            string fileName = prop.GetPropertyValue<string>(obj);
                            byte[] newImageData = await ConvertImageToDataAsync(fileName, currImageData);
                            pData.PropInfo.SetValue(obj, newImageData);
                            prop.PropInfo.SetValue(obj, null);// reset name so it's re-evaluated
                        }
                        continue;
                    }
                }
            }
        }
        private static async Task<Guid> ConvertImageToFileAsync(Guid moduleGuid, string folder, Guid origFileGuid, string fileName) {
            // Get the new image
            FileUpload fileUpload = new FileUpload();
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "(CLEARED)") {
                if (origFileGuid != Guid.Empty) {
                    // remove old image file
                    string oldFile = Path.Combine(ModuleDefinition.GetModuleDataFolder(moduleGuid), folder, origFileGuid.ToString());
                    try {
                        await FileSystem.FileSystemProvider.DeleteFileAsync(oldFile);// the file may not exist
                    } catch (Exception) { }
                }
                return Guid.Empty;
            } else if (fileUpload.IsTempName(fileName)) {
                byte[] bytes = await fileUpload.GetImageBytesFromTempNameAsync(fileName);
                if (origFileGuid != Guid.Empty) {
                    // remove old image file
                    string oldFile = Path.Combine(ModuleDefinition.GetModuleDataFolder(moduleGuid), folder, origFileGuid.ToString());
                    try {
                        await FileSystem.FileSystemProvider.DeleteFileAsync(oldFile);// the file may not exist
                    } catch (Exception) { }
                }
                // save new image file
                string path = Path.Combine(ModuleDefinition.GetModuleDataFolder(moduleGuid), folder);
                await FileSystem.FileSystemProvider.CreateDirectoryAsync(path);
                Guid newGuid = Guid.NewGuid();
                string file = Path.Combine(path, newGuid.ToString());
                using (MemoryStream ms = new MemoryStream(bytes)) {
                    using (System.Drawing.Image img = System.Drawing.Image.FromStream(ms)) {
                        img.Save(file);
                    }
                }
                // Remove the temp file (if any)
                await fileUpload.RemoveTempFileAsync(fileName);
                return newGuid;
            } else {
                return origFileGuid;//keep existing file
            }
        }
        private static async Task<byte[]> ConvertImageToDataAsync(string fileName, byte[] currImageData) {
            // Get the new image
            FileUpload fileUpload = new FileUpload();
            byte[] bytes;
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "(CLEARED)")
                bytes = null;
            else if (fileUpload.IsTempName(fileName)) {
                bytes = await fileUpload.GetImageBytesFromTempNameAsync(fileName);
                // Remove the temp file (if any)
                await fileUpload.RemoveTempFileAsync(fileName);
            } else
                bytes = currImageData;
            if (bytes == null)
                bytes = new byte[] { };
            return bytes;
        }
    }
}
