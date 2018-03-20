/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

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
        public object ObjectList { get; set; }
        public bool More { get; set; }
    }

    public interface IDataProviderTransactions {
        DataProviderTransaction StartTransaction();
        Task CommitTransactionAsync();
        void AbortTransaction();
    }

    public interface IDataProvider<KEYTYPE, OBJTYPE> {

        DataProviderTransaction StartTransaction();
        Task CommitTransactionAsync();
        void AbortTransaction();

        Task<bool> AddAsync(OBJTYPE obj); // returns false if key already exists
        Task<UpdateStatusEnum> UpdateAsync(KEYTYPE origKey, KEYTYPE newKey, OBJTYPE obj);
        Task<bool> RemoveAsync(KEYTYPE key);// returns false if not found
        Task<int> RemoveRecordsAsync(List<DataProviderFilterInfo> filters); // returns # of records removed

        Task<OBJTYPE> GetAsync(KEYTYPE key); // returns null if not found
        Task<OBJTYPE> GetOneRecordAsync(List<DataProviderFilterInfo> filters, List<JoinData> Joins = null); // returns null if not found
        Task<DataProviderGetRecords<OBJTYPE>> GetRecordsAsync(int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filters, List<JoinData> Joins = null);        

        Task<bool> IsInstalledAsync();
        Task<bool> InstallModelAsync(List<string> errorList);
        Task<bool> UninstallModelAsync(List<string> errorList);
        Task AddSiteDataAsync();
        Task RemoveSiteDataAsync();
        Task ImportChunkAsync(int chunk, SerializableList<SerializableFile> fileList, object obj);
        Task<DataProviderExportChunk> ExportChunkAsync(int chunk, SerializableList<SerializableFile> fileList);
    }
    public interface IDataProviderIdentity<KEYTYPE, KEY2TYPE, OBJTYPE> {

        DataProviderTransaction StartTransaction();
        Task CommitTransactionAsync();
        void AbortTransaction();

        Task<bool> AddAsync(OBJTYPE obj); // returns false if key already exists
        Task<UpdateStatusEnum> UpdateAsync(KEYTYPE origKey, KEY2TYPE origKey2, KEYTYPE newKey, KEY2TYPE newKey2, OBJTYPE obj);
        Task<UpdateStatusEnum> UpdateByIdentityAsync(int id, OBJTYPE obj);
        Task<bool> RemoveAsync(KEYTYPE key, KEY2TYPE key2);// returns false if not found
        Task<bool> RemoveByIdentityAsync(int id);// returns false if not found
        Task<int> RemoveRecordsAsync(List<DataProviderFilterInfo> filters); // returns # of records removed

        Task<OBJTYPE> GetAsync(KEYTYPE key, KEY2TYPE key2); // returns null if not found
        Task<OBJTYPE> GetByIdentityAsync(int id); // returns null if not found
        Task<OBJTYPE> GetOneRecordAsync(List<DataProviderFilterInfo> filters, List<JoinData> Joins = null); // returns null if not found
        Task<DataProviderGetRecords<OBJTYPE>> GetRecordsAsync(int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filters, List<JoinData> Joins = null);

        Task<bool> IsInstalledAsync();
        Task<bool> InstallModelAsync(List<string> errorList);
        Task<bool> UninstallModelAsync(List<string> errorList);
        Task AddSiteDataAsync();
        Task RemoveSiteDataAsync();
        Task ImportChunkAsync(int chunk, SerializableList<SerializableFile> fileList, object obj);
        Task<DataProviderExportChunk> ExportChunkAsync(int chunk, SerializableList<SerializableFile> fileList);
    }
}
