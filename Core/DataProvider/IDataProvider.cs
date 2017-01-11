/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.Packages;
using YetaWF.Core.Serializers;
using YetaWF.Core.Support;

namespace YetaWF.Core.DataProvider {

    public enum UpdateStatusEnum { OK = 0, NewKeyExists, RecordDeleted }

    public sealed class DataProviderTransaction : IDisposable {

        private Action abortTransaction;
        private Action commitTransaction;
        public DataProviderTransaction(Action commitTransaction, Action abortTransaction) {
            this.abortTransaction = abortTransaction;
            this.commitTransaction = commitTransaction;
            DisposableTracker.AddObject(this);
        }
        public void Commit() {
            commitTransaction();
        }
        public void Dispose() {
            DisposableTracker.RemoveObject(this);
            abortTransaction();
        }
    }


    public interface IDataProvider<KEYTYPE, OBJTYPE> {

        string ReplaceWithTableName(string text, string searchText);
        string ReplaceWithLanguage(string text, string searchText);
        string GetTableName();

        DataProviderTransaction StartTransaction();
        void CommitTransaction();
        void AbortTransaction();

        bool Add(OBJTYPE obj); // returns false if key already exists
        UpdateStatusEnum Update(KEYTYPE origKey, KEYTYPE newKey, OBJTYPE obj);
        bool Remove(KEYTYPE key);// returns false if not found
        int RemoveRecords(List<DataProviderFilterInfo> filters); // returns # of records removed

        OBJTYPE Get(KEYTYPE key); // returns null if not found
        OBJTYPE GetOneRecord(List<DataProviderFilterInfo> filters, List<JoinData> Joins = null); // returns null if not found
        List<OBJTYPE> GetRecords(int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filters, out int total, List<JoinData> Joins = null);
        // There is an inherent maximum that can be retrieved with this - use wisely
        List<KEYTYPE> GetKeyList();

        bool IsInstalled();
        bool InstallModel(List<string> errorList);
        bool UninstallModel(List<string> errorList);
        void AddSiteData();
        void RemoveSiteData();
        bool ExportChunk(int chunk, SerializableList<SerializableFile> fileList, out object obj);
        void ImportChunk(int chunk, SerializableList<SerializableFile> fileList, object obj);
    }
    public interface IDataProviderIdentity<KEYTYPE, KEY2TYPE, IDENTITYTYPE, OBJTYPE> {

        string ReplaceWithTableName(string text, string searchText);
        string ReplaceWithLanguage(string text, string searchText);
        string GetTableName();

        DataProviderTransaction StartTransaction();
        void CommitTransaction();
        void AbortTransaction();

        bool Add(OBJTYPE obj); // returns false if key already exists
        UpdateStatusEnum Update(KEYTYPE origKey, KEY2TYPE origKey2, KEYTYPE newKey, KEY2TYPE newKey2, OBJTYPE obj);
        UpdateStatusEnum UpdateByIdentity(IDENTITYTYPE id, OBJTYPE obj);
        bool Remove(KEYTYPE key, KEY2TYPE key2);// returns false if not found
        bool RemoveByIdentity(IDENTITYTYPE id);// returns false if not found
        int RemoveRecords(List<DataProviderFilterInfo> filters); // returns # of records removed

        OBJTYPE Get(KEYTYPE key, KEY2TYPE key2); // returns null if not found
        OBJTYPE GetByIdentity(IDENTITYTYPE id); // returns null if not found
        OBJTYPE GetOneRecord(List<DataProviderFilterInfo> filters, List<JoinData> Joins = null); // returns null if not found
        List<OBJTYPE> GetRecords(int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filters, out int total, List<JoinData> Joins = null);

        bool IsInstalled();
        bool InstallModel(List<string> errorList);
        bool UninstallModel(List<string> errorList);
        void AddSiteData();
        void RemoveSiteData();
        bool ExportChunk(int chunk, SerializableList<SerializableFile> fileList, out object obj);
        void ImportChunk(int chunk, SerializableList<SerializableFile> fileList, object obj);
    }
}
