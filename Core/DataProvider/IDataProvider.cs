/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;

namespace YetaWF.Core.DataProvider {

    public enum UpdateStatusEnum { OK = 0, NewKeyExists, RecordDeleted }

    public sealed class DataProviderTransaction : IDisposable {

        private Action abortTransaction;
        private Func<Task> commitTransactionAsync;
        public DataProviderTransaction(Func<Task> commitTransaction, Action abortTransaction) {
            this.abortTransaction = abortTransaction;
            this.commitTransactionAsync = commitTransaction;
            DisposableTracker.AddObject(this);
        }
        public async Task CommitAsync() {
            await commitTransactionAsync();
        }
        public void Dispose() {
            DisposableTracker.RemoveObject(this);
            abortTransaction();
        }
    }
    public sealed class DataProviderGetRecords<OBJTYPE> {
        public List<OBJTYPE> Data { get; set; }
        public int Total { get; set; }
        public DataProviderGetRecords() {
            Data = new List<OBJTYPE>();
        }
    }
    public sealed class DataProviderExportChunk {
        public object? ObjectList { get; set; }
        public bool More { get; set; }
    }

    /// <summary>
    /// This interface is implemented by low-level data providers to support transactions that can be committed, saving all updates, or aborted to abandon all updates.
    /// </summary>
    public interface IDataProviderTransactions {
        /// <summary>
        /// Starts a transaction that can be committed, saving all updates, or aborted to abandon all updates.
        /// </summary>
        /// <returns>Returns a YetaWF.Core.DataProvider.DataProviderTransaction object.</returns>
        DataProviderTransaction StartTransaction(DataProviderImpl ownerDP, params DataProviderImpl[] dps);
        /// <summary>
        /// Commits a transaction, saving all updates.
        /// </summary>
        Task CommitTransactionAsync();
        /// <summary>
        /// Aborts a transaction, abandoning all updates.
        /// </summary>
        void AbortTransaction();
    }

    /// <summary>
    /// This interface is implemented by low-level data providers that offer access to record-based data with one primary key, without record identity.
    /// </summary>
    /// <typeparam name="KEYTYPE">The type of the primary key property.</typeparam>
    /// <typeparam name="OBJTYPE">The type of the object (one record) in the dataset.</typeparam>
    public interface IDataProvider<KEYTYPE, OBJTYPE> {

        /// <summary>
        /// Starts a transaction that can be committed, saving all updates, or aborted to abandon all updates.
        /// </summary>
        /// <returns>Returns a YetaWF.Core.DataProvider.DataProviderTransaction object.</returns>
        DataProviderTransaction StartTransaction(DataProviderImpl ownerDP, params DataProviderImpl[] dps);
        /// <summary>
        /// Commits a transaction, saving all updates.
        /// </summary>
        Task CommitTransactionAsync();
        /// <summary>
        /// Aborts a transaction, abandoning all updates.
        /// </summary>
        void AbortTransaction();

        Task<bool> AddAsync(OBJTYPE obj); // returns false if key already exists
        Task<UpdateStatusEnum> UpdateAsync(KEYTYPE origKey, KEYTYPE newKey, OBJTYPE obj);
        Task<bool> RemoveAsync(KEYTYPE key);// returns false if not found
        Task<int> RemoveRecordsAsync(List<DataProviderFilterInfo> filters); // returns # of records removed

        Task<OBJTYPE> GetAsync(KEYTYPE key); // returns null if not found
        Task<OBJTYPE> GetOneRecordAsync(List<DataProviderFilterInfo> filters, List<JoinData> Joins = null); // returns null if not found
        Task<DataProviderGetRecords<OBJTYPE>> GetRecordsAsync(int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filters, List<JoinData> Joins = null);

        /// <summary>
        /// Returns whether the data provider is installed and available.
        /// </summary>
        /// <returns>true if the data provider is installed and available, false otherwise.</returns>
        Task<bool> IsInstalledAsync();
        /// <summary>
        /// Installs all data models (files, tables, etc.) for the data provider.
        /// </summary>
        /// <param name="errorList">A collection of error strings in user displayable format.</param>
        /// <returns>true if the models were created successfully, false otherwise.
        /// If the models could not be created, <paramref name="errorList"/> contains the reason for the failure.</returns>
        /// <remarks>
        /// While a package is installed, all data models are installed by calling the InstallModelAsync method.</remarks>
        Task<bool> InstallModelAsync(List<string> errorList);
        /// <summary>
        /// Uninstalls all data models (files, tables, etc.) for the data provider.
        /// </summary>
        /// <param name="errorList">A collection of error strings in user displayable format.</param>
        /// <returns>true if the models were removed successfully, false otherwise.
        /// If the models could not be removed, <paramref name="errorList"/> contains the reason for the failure.</returns>
        /// <remarks>
        /// While a package is uninstalled, all data models are uninstalled by calling the UninstallModelAsync method.</remarks>
        Task<bool> UninstallModelAsync(List<string> errorList);
        /// <summary>
        /// Adds data for a new site.
        /// </summary>
        /// <remarks>
        /// When a new site is created the AddSiteDataAsync method is called for all data providers.
        /// Data providers can then add site-specific data as the new site is added.</remarks>
        Task AddSiteDataAsync();
        /// <summary>
        /// Removes data when a site is deleted.
        /// </summary>
        /// <remarks>
        /// When a site is deleted the RemoveSiteDataAsync method is called for all data providers.
        /// Data providers can then remove site-specific data as the site is removed.</remarks>
        Task RemoveSiteDataAsync();
        /// <summary>
        /// Imports data into the data provider.
        /// </summary>
        /// <param name="chunk">The zero-based chunk number as data is imported. The first call when importing begins specifies 0 as chunk number.</param>
        /// <param name="fileList">A collection of files to be imported. Files are automatically imported, so the data provider doesn't have to process this collection.</param>
        /// <param name="obj">The data to be imported.</param>
        /// <remarks>
        /// The ImportChunkAsync method is called to import data for site restores, page and module imports.
        ///
        /// When a data provider is called to import data, it is called repeatedly until no more data is available.
        /// Each time it is called, it is expected to import the chunk of data defined by <paramref name="obj"/>.
        /// Each time ImportChunkAsync method is called, the zero-based chunk number <paramref name="chunk"/> is incremented.
        ///
        /// The <paramref name="obj"/> parameter is provided without type but should be cast to
        /// YetaWF.Core.Serializers.SerializableList&lt;OBJTYPE&gt; as it is a collection of records to import. All records in the collection must be imported.
        /// </remarks>
        Task ImportChunkAsync(int chunk, SerializableList<SerializableFile> fileList, object obj);
        /// <summary>
        /// Exports data from the data provider.
        /// </summary>
        /// <param name="chunk">The zero-based chunk number as data is exported. The first call when exporting begins specifies 0 as chunk number.</param>
        /// <param name="fileList">A collection of files. The data provider can add files to be exported to this collection when ExportChunkAsync is called.</param>
        /// <returns>Returns a YetaWF.Core.DataProvider.DataProviderExportChunk object describing the data exported.</returns>
        /// <remarks>
        /// The ExportChunkAsync method is called to export data for site backups, page and module exports.
        ///
        /// When a data provider is called to export data, it is called repeatedly until YetaWF.Core.DataProvider.DataProviderExportChunk.More is returned as false.
        /// Each time it is called, it is expected to export a chunk of data. The amount of data, i.e., the chunk size, is determined by the data provider.
        ///
        /// Each time ExportChunkAsync method is called, the zero-based chunk number <paramref name="chunk"/> is incremented.
        /// The data provider returns data in an instance of the DataProviderExportChunk object.
        ///
        /// Files to be exported can be added to the <paramref name="fileList"/> collection.
        /// Only data records need to be added to the returned YetaWF.Core.DataProvider.DataProviderExportChunk object.
        /// </remarks>
        Task<DataProviderExportChunk> ExportChunkAsync(int chunk, SerializableList<SerializableFile> fileList);
    }
    /// <summary>
    /// This interface is implemented by low-level data providers that offer access to record-based data with up to two primary keys and a record identity.
    /// </summary>
    /// <typeparam name="KEYTYPE">The type of the primary key property.</typeparam>
    /// <typeparam name="KEY2TYPE">The type of the second primary key property. If only one key is used, specify "object".</typeparam>
    /// <typeparam name="OBJTYPE">The type of the object (one record) in the dataset.</typeparam>
    public interface IDataProviderIdentity<KEYTYPE, KEY2TYPE, OBJTYPE> {

        /// <summary>
        /// Starts a transaction that can be committed, saving all updates, or aborted to abandon all updates.
        /// </summary>
        /// <returns>Returns a YetaWF.Core.DataProvider.DataProviderTransaction object.</returns>
        DataProviderTransaction StartTransaction(DataProviderImpl ownerDP, params DataProviderImpl[] dps);
        /// <summary>
        /// Commits a transaction, saving all updates.
        /// </summary>
        Task CommitTransactionAsync();
        /// <summary>
        /// Aborts a transaction, abandoning all updates.
        /// </summary>
        void AbortTransaction();

        Task<bool> AddAsync(OBJTYPE obj); // returns false if key already exists
        Task<UpdateStatusEnum> UpdateAsync(KEYTYPE origKey, KEY2TYPE origKey2, KEYTYPE newKey, KEY2TYPE newKey2, OBJTYPE obj);
        Task<UpdateStatusEnum> UpdateByIdentityAsync(int id, OBJTYPE obj);
        Task<bool> RemoveAsync(KEYTYPE key, KEY2TYPE key2);// returns false if not found
        Task<bool> RemoveByIdentityAsync(int id);// returns false if not found
        Task<int> RemoveRecordsAsync(List<DataProviderFilterInfo>? filters); // returns # of records removed

        Task<OBJTYPE> GetAsync(KEYTYPE key, KEY2TYPE key2); // returns null if not found
        Task<OBJTYPE> GetByIdentityAsync(int id); // returns null if not found
        Task<OBJTYPE> GetOneRecordAsync(List<DataProviderFilterInfo> filters, List<JoinData> Joins = null); // returns null if not found
        Task<DataProviderGetRecords<OBJTYPE>> GetRecordsAsync(int skip, int take, List<DataProviderSortInfo>? sort, List<DataProviderFilterInfo>? filters, List<JoinData>? Joins = null);

        /// <summary>
        /// Returns whether the data provider is installed and available.
        /// </summary>
        /// <returns>true if the data provider is installed and available, false otherwise.</returns>
        Task<bool> IsInstalledAsync();
        /// <summary>
        /// Installs all data models (files, tables, etc.) for the data provider.
        /// </summary>
        /// <param name="errorList">A collection of error strings in user displayable format.</param>
        /// <returns>true if the models were created successfully, false otherwise.
        /// If the models could not be created, <paramref name="errorList"/> contains the reason for the failure.</returns>
        /// <remarks>
        /// While a package is installed, all data models are installed by calling the InstallModelAsync method.</remarks>
        Task<bool> InstallModelAsync(List<string> errorList);
        /// <summary>
        /// Uninstalls all data models (files, tables, etc.) for the data provider.
        /// </summary>
        /// <param name="errorList">A collection of error strings in user displayable format.</param>
        /// <returns>true if the models were removed successfully, false otherwise.
        /// If the models could not be removed, <paramref name="errorList"/> contains the reason for the failure.</returns>
        /// <remarks>
        /// While a package is uninstalled, all data models are uninstalled by calling the UninstallModelAsync method.</remarks>
        Task<bool> UninstallModelAsync(List<string> errorList);
        /// <summary>
        /// Adds data for a new site.
        /// </summary>
        /// <remarks>
        /// When a new site is created the AddSiteDataAsync method is called for all data providers.
        /// Data providers can then add site-specific data as the new site is added.</remarks>
        Task AddSiteDataAsync();
        /// <summary>
        /// Removes data when a site is deleted.
        /// </summary>
        /// <remarks>
        /// When a site is deleted the RemoveSiteDataAsync method is called for all data providers.
        /// Data providers can then remove site-specific data as the site is removed.</remarks>
        Task RemoveSiteDataAsync();
        /// <summary>
        /// Imports data into the data provider.
        /// </summary>
        /// <param name="chunk">The zero-based chunk number as data is imported. The first call when importing begins specifies 0 as chunk number.</param>
        /// <param name="fileList">A collection of files to be imported. Files are automatically imported, so the data provider doesn't have to process this collection.</param>
        /// <param name="obj">The data to be imported.</param>
        /// <remarks>
        /// The ImportChunkAsync method is called to import data for site restores, page and module imports.
        ///
        /// When a data provider is called to import data, it is called repeatedly until no more data is available.
        /// Each time it is called, it is expected to import the chunk of data defined by <paramref name="obj"/>.
        /// Each time ImportChunkAsync method is called, the zero-based chunk number <paramref name="chunk"/> is incremented.
        ///
        /// The <paramref name="obj"/> parameter is provided without type but should be cast to
        /// YetaWF.Core.Serializers.SerializableList&lt;OBJTYPE&gt; as it is a collection of records to import. All records in the collection must be imported.
        /// </remarks>
        Task ImportChunkAsync(int chunk, SerializableList<SerializableFile> fileList, object obj);
        /// <summary>
        /// Exports data from the data provider.
        /// </summary>
        /// <param name="chunk">The zero-based chunk number as data is exported. The first call when exporting begins specifies 0 as chunk number.</param>
        /// <param name="fileList">A collection of files. The data provider can add files to be exported to this collection when ExportChunkAsync is called.</param>
        /// <returns>Returns a YetaWF.Core.DataProvider.DataProviderExportChunk object describing the data exported.</returns>
        /// <remarks>
        /// The ExportChunkAsync method is called to export data for site backups, page and module exports.
        ///
        /// When a data provider is called to export data, it is called repeatedly until YetaWF.Core.DataProvider.DataProviderExportChunk.More is returned as false.
        /// Each time it is called, it is expected to export a chunk of data. The amount of data, i.e., the chunk size, is determined by the data provider.
        ///
        /// Each time ExportChunkAsync method is called, the zero-based chunk number <paramref name="chunk"/> is incremented.
        /// The data provider returns data in an instance of the YetaWF.Core.DataProvider.DataProviderExportChunk object.
        ///
        /// Files to be exported can be added to the <paramref name="fileList"/> collection.
        /// Only data records need to be added to the returned YetaWF.Core.DataProvider.DataProviderExportChunk object.
        /// </remarks>
        Task<DataProviderExportChunk> ExportChunkAsync(int chunk, SerializableList<SerializableFile> fileList);
    }
}
