/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
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

        protected int SiteIdentity { get; set; }
        protected string AreaName { get; set; }
        protected WebConfigHelper.IOModeEnum IOMode { get; private set; }

        public const string DefaultString = "Default";
        public const string SQLConnectString = "SQLConnect";
        public const string IOModeString = "IOMode";
        private const string SQLDboString = "SQLDbo";

        protected void SetDataProvider(dynamic dp) {
            _dataProvider = dp;
        }
        public dynamic GetDataProvider() {
            return _dataProvider;
        }

        private WebConfigHelper.IOModeEnum GetIOMode() {
            if (_defaultIOMode == null) {
                _defaultIOMode = WebConfigHelper.GetValue<string>(DefaultString, IOModeString);
                if (_defaultIOMode == null)
                    throw new InternalError("Default IOMode is missing");
            }
            string ioMode = WebConfigHelper.GetValue<string>(AreaName, IOModeString);
            if (string.IsNullOrWhiteSpace(ioMode))
                ioMode = _defaultIOMode;

            switch (ioMode.ToLower()) {
                default:
                    throw new InternalError($"Invalid IOMode {ioMode}");
                case "ext":
                    return IOMode = WebConfigHelper.IOModeEnum.External;
                case "file":
                    return IOMode = WebConfigHelper.IOModeEnum.File;
                case "sql":
                    return IOMode = WebConfigHelper.IOModeEnum.Sql;
            }
        }
        private static string _defaultIOMode = null;

        protected string GetSqlConnectionString() {
            if (AreaName == null || IOMode == WebConfigHelper.IOModeEnum.Determine) throw new InternalError($"Must call {nameof(GetIOMode)} first");
            string connString = WebConfigHelper.GetValue<string>(AreaName, SQLConnectString);
            if (string.IsNullOrWhiteSpace(connString))
                connString = WebConfigHelper.GetValue<string>(DefaultString, SQLConnectString);
            if (string.IsNullOrWhiteSpace(connString)) throw new InternalError($"No SQL connection string provided (also no default)");
            return connString;
        }
        protected string GetSqlDbo() {
            if (AreaName == null || IOMode == WebConfigHelper.IOModeEnum.Determine) throw new InternalError($"Must call {nameof(GetIOMode)} first");
            string dbo = WebConfigHelper.GetValue<string>(AreaName, SQLDboString);
            if (string.IsNullOrWhiteSpace(dbo))
                dbo = WebConfigHelper.GetValue<string>(DefaultString, SQLDboString);
            if (string.IsNullOrWhiteSpace(dbo)) throw new InternalError($"No SQL dbo provided (also no default)");
            return dbo;
        }
        public static void GetSQLInfo(out string dbo, out string connString) {
            connString = WebConfigHelper.GetValue<string>(DefaultString, SQLConnectString);
            dbo = WebConfigHelper.GetValue<string>(DefaultString, SQLDboString);
        }

        protected dynamic MakeDataProvider(string areaName, Func<dynamic> newFileDP, Func<string, string, dynamic> newSqlDP, Func<dynamic> newExtDP) {
            AreaName = areaName;
            switch (GetIOMode()) {
                case WebConfigHelper.IOModeEnum.File:
                    return newFileDP();
                case WebConfigHelper.IOModeEnum.Sql:
                    return newSqlDP(GetSqlDbo(), GetSqlConnectionString());
                case WebConfigHelper.IOModeEnum.External:
                    return newExtDP();
                default:
                    throw new InternalError("Unsupported IOMode");
            }
        }

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
        protected void CommitTransaction() {
            GetIDataProviderTransactions().CommitTransaction();
        }
        protected void AbortTransaction() {
            GetIDataProviderTransactions().AbortTransaction();
        }

        // IINSTALLABLEMODEL
        // IINSTALLABLEMODEL
        // IINSTALLABLEMODEL

        private dynamic GetIInstallableModel() {
            return GetDataProvider();
        }
        public bool IsInstalled() {
            return GetDataProvider().IsInstalled();
        }
        public bool InstallModel(List<string> errorList) {
            return GetDataProvider().InstallModel(errorList);
        }
        public void AddSiteData() {
            GetDataProvider().AddSiteData();
        }
        public void RemoveSiteData() {
            GetDataProvider().RemoveSiteData();
        }
        public bool UninstallModel(List<string> errorList) {
            return GetDataProvider().UninstallModel(errorList);
        }
        public bool ExportChunk(int chunk, SerializableList<SerializableFile> fileList, out object obj) {
            return GetDataProvider().ExportChunk(chunk, fileList, out obj);
        }
        public void ImportChunk(int chunk, SerializableList<SerializableFile> fileList, object obj) {
            GetDataProvider().ImportChunk(chunk, fileList, obj);
        }

        // ISQLTableInfo
        // ISQLTableInfo
        // ISQLTableInfo

        private ISQLTableInfo GetISQLTableInfo() {
            ISQLTableInfo sqlDP = GetDataProvider() as ISQLTableInfo;
            if (sqlDP == null) throw new InternalError($"Data provider {GetType().FullName} has no ISQLTableInfo interface");
            return sqlDP;
        }
        public string GetConnectionString() {
            return GetISQLTableInfo().GetConnectionString();
        }
        public string GetDbOwner() {
            return GetISQLTableInfo().GetDbOwner();
        }
        public string GetTableName() {
            return GetISQLTableInfo().GetTableName();
        }
        public string ReplaceWithTableName(string text, string searchText) {
            return GetISQLTableInfo().ReplaceWithTableName(text, searchText);
        }
        public string ReplaceWithLanguage(string text, string searchText) {
            return GetISQLTableInfo().ReplaceWithLanguage(text, searchText);
        }
        public string GetDatabaseName() {
            return GetISQLTableInfo().GetDatabaseName();
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
