/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
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
            return GetDataProvider().IsInstalledAsync();
        }
        public Task<bool> InstallModelAsync(List<string> errorList) {
            return GetDataProvider().InstallModelAsync(errorList);
        }
        public Task AddSiteDataAsync() {
            return GetDataProvider().AddSiteDataAsync();
        }
        public Task RemoveSiteDataAsync() {
            return GetDataProvider().RemoveSiteDataAsync();
        }
        public Task<bool> UninstallModelAsync(List<string> errorList) {
            return GetDataProvider().UninstallModelAsync(errorList);
        }
        public Task<DataProviderExportChunk> ExportChunkAsync(int chunk, SerializableList<SerializableFile> fileList) {
            return GetDataProvider().ExportChunkAsync(chunk, fileList);
        }
        public Task ImportChunkAsync(int chunk, SerializableList<SerializableFile> fileList, object obj) {
            return GetDataProvider().ImportChunk(chunk, fileList, obj);
        }

        // IMAGE HANDLING
        // IMAGE HANDLING
        // IMAGE HANDLING

        public static void SaveImages(Guid moduleGuid, object obj) {
            Type objType = obj.GetType();
            List<PropertyData> propData = ObjectSupport.GetPropertyData(objType);
            foreach (var prop in propData) {
                // look for Image UIHint
                if (prop.UIHint == "Image") {
                    if (prop.GetAdditionalAttributeValue<bool>("File", false)) {
                        // save as file
                        PropertyData pGuid = ObjectSupport.GetPropertyData(objType, prop.Name + "_Guid");
                        Guid fileGuid = pGuid.GetPropertyValue<Guid>(obj);
                        string fileName = prop.GetPropertyValue<string>(obj);
                        ConvertImageToFile(moduleGuid, prop.Name, fileGuid, fileName);
                        prop.PropInfo.SetValue(obj, null);// reset name so it's re-evaluated
                    } else if (prop.GetAdditionalAttributeValue<bool>("Data", true)) {
                        // save as data
                        PropertyData pData = ObjectSupport.GetPropertyData(objType, prop.Name + "_Data");
                        byte[] currImageData = pData.GetPropertyValue<byte[]>(obj);
                        string fileName = prop.GetPropertyValue<string>(obj);
                        byte[] newImageData = ConvertImageToData(fileName, currImageData);
                        pData.PropInfo.SetValue(obj, newImageData);
                        prop.PropInfo.SetValue(obj, null);// reset name so it's re-evaluated
                    }
                    continue;
                }
            }
        }
        private static void ConvertImageToFile(Guid guid, string folder, Guid fileGuid, string fileName) {
            // Get the new image
            FileUpload fileUpload = new FileUpload();
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "(CLEARED)") {
                // remove image file
                string file = Path.Combine(ModuleDefinition.GetModuleDataFolder(guid), folder, fileGuid.ToString());
                try {
                    File.Delete(file);// the file may not exist
                } catch (Exception) { }
                return;
            } else if (fileUpload.IsTempName(fileName)) {
                byte[] bytes = fileUpload.GetImageBytesFromTempName(fileName);
                // save new image file
                string path = Path.Combine(ModuleDefinition.GetModuleDataFolder(guid), folder);
                Directory.CreateDirectory(path);
                string file = Path.Combine(path, fileGuid.ToString());
                using (MemoryStream ms = new MemoryStream(bytes)) {
                    using (System.Drawing.Image img = System.Drawing.Image.FromStream(ms)) {
                        img.Save(file);
                    }
                }
                // Remove the temp file (if any)
                fileUpload.RemoveTempFile(fileName);
            } else {
                ;//keep existing file
            }
        }
        private static byte[] ConvertImageToData(string fileName, byte[] currImageData) {
            // Get the new image
            FileUpload fileUpload = new FileUpload();
            byte[] bytes;
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "(CLEARED)")
                bytes = null;
            else if (fileUpload.IsTempName(fileName)) {
                bytes = fileUpload.GetImageBytesFromTempName(fileName);
                // Remove the temp file (if any)
                fileUpload.RemoveTempFile(fileName);
            } else
                bytes = currImageData;
            if (bytes == null)
                bytes = new byte[] { };
            return bytes;
        }
    }
}
