/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using YetaWF.Core.Models;
using YetaWF.Core.Modules;
using YetaWF.Core.Support;
using YetaWF.Core.Upload;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.DataProvider {

    public class DataProviderSortInfo {

        public enum SortDirection { Ascending = 0, Descending };

        /// <summary>
        /// Gets or sets the name of the sorted field (property).
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the sort direction. Should be either "asc" or "desc".
        /// </summary>
        public SortDirection Order { get; set; }

        public string GetOrder() {
            switch (Order) {
                case SortDirection.Ascending:
                    return "asc";
                case SortDirection.Descending:
                    return "desc";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Converts to form required by Dynamic Linq e.g. "Field1 desc"
        /// </summary>
        public string ToExpression() {
            return Field + " " + GetOrder();
        }

        public static List<DataProviderSortInfo> Join(List<DataProviderSortInfo> sort, DataProviderSortInfo addSort) {
            if (sort == null || sort.Count == 0) return new List<DataProviderSortInfo> { addSort };
            sort.Add(addSort);
            return sort;
        }
    }

    public class DataProviderFilterInfo {
        /// <summary>
        /// Gets or sets the name of the sorted field (property). Set to <c>null</c> if the <c>Filters</c> property is set.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Gets or sets the filtering operator. Set to <c>null</c> if the <c>Filters</c> property is set.
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// Gets or sets the filtering value. Set to <c>null</c> if the <c>Filters</c> property is set.
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// Gets or sets the filtering value. Set to <c>null</c> if the <c>Filters</c> property is set.
        /// </summary>
        public string ValueAsString { get { return Value != null ? Value.ToString() : null; } set { Value = value; } }

        /// <summary>
        /// Gets or sets the filtering logic. Can be set to "||" or "&quot;&quot;". Set to <c>null</c> unless <c>Filters</c> is set.
        /// </summary>
        public string Logic { get; set; }

        /// <summary>
        /// Gets or sets the child filter expressions. Set to <c>null</c> if there are no child expressions.
        /// </summary>
        public List<DataProviderFilterInfo> Filters { get; set; }
        private bool IsComplex { get { return !string.IsNullOrWhiteSpace(Logic); } }

        private static readonly IDictionary<string, string> Operators = new Dictionary<string, string>
        {
            {"eq", "="},
            {"neq", "!="},
            {"lt", "<"},
            {"lte", "<="},
            {"gt", ">"},
            {"gte", ">="},
            {"startswith", "StartsWith"},
            {"notstartswith", "NotStartsWith"},
            {"endswith", "EndsWith"},
            {"notendswith", "NotEndsWith"},
            {"contains", "Contains"},
            {"notcontains", "NotContains"},
        };

        /// <summary>
        /// Get a flattened list of all child filter expressions.
        /// </summary>
        public static List<DataProviderFilterInfo> CollectAllFilters(List<DataProviderFilterInfo> filters) {
            List<DataProviderFilterInfo> flatList = new List<DataProviderFilterInfo>();
            if (filters != null && filters.Any()) {
                foreach (DataProviderFilterInfo f in filters)
                    f.Collect(flatList);
            }
            return flatList;
        }
        private void Collect(List<DataProviderFilterInfo> filters) {
            filters.Add(this);
            if (Filters != null && Filters.Any()) {
                foreach (DataProviderFilterInfo f in Filters)
                    f.Collect(filters);
            }
        }

        /// <summary>
        /// Converts the filter expression to a predicate suitable for Dynamic Linq e.g. "Field1 = @1 and Field2.Contains(@2)"
        /// </summary>
        /// <param name="filterList">A list of flattened filters.</param>
        public string ToExpression(IList<DataProviderFilterInfo> filterList) {
            if (Filters != null && Filters.Any()) {
                return "(" + String.Join(" " + Logic + " ", Filters.Select(filter => filter.ToExpression(filterList)).ToArray()) + ")";
            } else {
                int index = filterList.IndexOf(this);
                string command = Operator;
                if (Operators.ContainsKey(command))
                    command = Operators[command];
                if (command == "StartsWith" || command == "EndsWith" || command == "Contains") {
                    if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                        return String.Format("({0} != null && {0}.ToLower().{1}(@{2}.ToLower()))", Field, command, index);
                    else
                        return String.Format("({0} != null && {0}.{1}(@{2}))", Field, command, index);
                } else if (command == "NotStartsWith") {
                    if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                        return String.Format("({0} == null || !{0}.ToLower().StartsWith(@{1}.ToLower()))", Field, index);
                    else
                        return String.Format("{0} == null || !{0}.StartsWith(@{1})", Field, index);
                } else if (command == "NotEndsWith") {
                    if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                        return String.Format("({0} == null || {0}.ToLower().EndsWith(@{1}.ToLower()))", Field, index);
                    else
                        return String.Format("{0} == null || !{0}.EndsWith(@{1})", Field, index);
                } else if (command == "NotContains") {
                    if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                        return String.Format("({0} == null || {0}.ToLower().Contains(@{1}.ToLower()))", Field, index);
                    else
                        return String.Format("{0} == null || !{0}.Contains(@{1})", Field, index);
                } else {
                    if (Operator == "!=" || Operator == "<" || Operator == "<=") {
                        if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                            return String.Format("({0} == null || {0}.ToLower() {1} @{2}.ToLower())", Field, command, index);
                        else
                            return String.Format("({0} == null || {0} {1} @{2})", Field, command, index);
                    } else if (Operator == ">" || Operator == ">=") {
                        if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                            return String.Format("({0} != null && {0}.ToLower() {1} @{2}.ToLower())", Field, command, index);
                        else
                            return String.Format("({0} != null && {0} {1} @{2})", Field, command, index);
                    } else {
                        if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                            return String.Format("({0} != null && {0}.ToLower() {1} @{2}.ToLower())", Field, command, index);
                        else
                            return String.Format("{0} {1} @{2}", Field, command, index);
                    }
                }
            }
        }

        public static List<DataProviderFilterInfo> Join(List<DataProviderFilterInfo> filters, DataProviderFilterInfo addFilter, string SimpleLogic = null) {

            if (addFilter.IsComplex) {
                if (SimpleLogic != null)
                    throw new InternalError("SimpleLogic {0} can't be defined for complex filter", SimpleLogic);
            } else if (string.IsNullOrWhiteSpace(SimpleLogic))
                SimpleLogic = "&&";

            if (filters == null || filters.Count == 0) {
                // we have no filters
                return new List<DataProviderFilterInfo> { addFilter };
            }

            if (filters.Count > 1) {
                // we just have a list of filters (this happens when the caller builds the graph and is an implied &&)
                if (!addFilter.IsComplex) {
                    // adding another simple filter - this means the logic is assumed to be && for the current list
                    if (SimpleLogic != "&&") {
                        // adding a new simple filter but with different logic
                        DataProviderFilterInfo newRoot1 = new DataProviderFilterInfo {
                            Filters = new List<DataProviderFilterInfo> {
                                new DataProviderFilterInfo {
                                    Logic = "&&",
                                    Filters = filters,
                                },
                                addFilter,
                            },
                            Logic = SimpleLogic,
                        };
                        return new List<DataProviderFilterInfo> { newRoot1 };
                    } else {
                        // adding a new simple filter, also with &&, just append to current list
                        filters.Add(addFilter);
                        return filters;
                    }
                } else {
                    // adding a complex filter - this means the current filter list is an implied &&
                    if (addFilter.Logic != "&&") {
                        // adding a complex filter with new logic
                        DataProviderFilterInfo newRoot1 = new DataProviderFilterInfo {
                            Filters = new List<DataProviderFilterInfo> {
                                    new DataProviderFilterInfo {
                                        Logic = "&&",
                                        Filters = filters,
                                    },
                                },
                            Logic = addFilter.Logic,
                        };
                        newRoot1.Filters.AddRange(addFilter.Filters);
                        return new List<DataProviderFilterInfo> { newRoot1 };
                    } else {
                        // complex filter also uses && so just append new filters to current list (with implied &&)
                        filters.AddRange(addFilter.Filters);
                        return filters;
                    }
                }
            } else {/* filters.Count == 1*/
                // We have just one filter
                DataProviderFilterInfo root = filters.First();
                if (!root.IsComplex) {
                    // root is a simple filter (with implied &&)
                    if (!addFilter.IsComplex) {
                        // adding another simple filter - we now need to define the logic
                        DataProviderFilterInfo newRoot1 = new DataProviderFilterInfo {
                            Filters = new List<DataProviderFilterInfo> {
                                root,
                                addFilter
                            },
                            Logic = SimpleLogic,
                        };
                        return new List<DataProviderFilterInfo> { newRoot1 };
                    } else {
                        // adding a complex filter - we now need to define the logic
                        DataProviderFilterInfo newRoot1 = new DataProviderFilterInfo {
                            Filters = new List<DataProviderFilterInfo> {
                                root,
                            },
                            Logic = addFilter.Logic,
                        };
                        newRoot1.Filters.AddRange(addFilter.Filters);
                        return new List<DataProviderFilterInfo> { newRoot1 };
                    }
                } else {
                    // root is a complex filter
                    if (!addFilter.IsComplex) {
                        // adding a simple filter
                        if (SimpleLogic != root.Logic) {
                            // adding a new simple filter but with different logic
                            DataProviderFilterInfo newRoot1 = new DataProviderFilterInfo {
                                Filters = filters,
                                Logic = SimpleLogic,
                            };
                            newRoot1.Filters.Add(addFilter);
                            return new List<DataProviderFilterInfo> { newRoot1 };
                        } else {
                            // adding a new simple filter, with the same logic, just append to current list
                            root.Filters.Add(addFilter);
                            return filters;
                        }
                    } else {
                        // adding a complex filter to a complex root
                        if (addFilter.Logic != root.Logic) {
                            // adding a complex filter with new logic to the current complex filter
                            DataProviderFilterInfo newRoot1 = new DataProviderFilterInfo {
                                Filters = new List<DataProviderFilterInfo> {
                                    root,
                                },
                                Logic = addFilter.Logic,
                            };
                            newRoot1.Filters.AddRange(addFilter.Filters);
                            return new List<DataProviderFilterInfo> { newRoot1 };
                        } else {
                            // new complex filter uses same logic as root, so just append new filters to current list
                            root.Filters.AddRange(addFilter.Filters);
                            return filters;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Return a copy of the filter graph by copying each entry.
        /// </summary>
        /// <param name="filters">Filters to copy.</param>
        /// <returns>Returns a new filter graph that can be modified without affecting the original graph.</returns>
        public static List<DataProviderFilterInfo> Copy(List<DataProviderFilterInfo> filters) {
            List<DataProviderFilterInfo> newFilters = null;
            if (filters == null) return newFilters;
            newFilters = new List<DataProviderFilterInfo>();
            foreach (DataProviderFilterInfo f in filters) {
                newFilters.Add(new DataProvider.DataProviderFilterInfo {
                    Field = f.Field,
                    Logic = f.Logic,
                    Operator = f.Operator,
                    Value = f.Value,
                    Filters = Copy(f.Filters),
                });
            }
            return newFilters;
        }
    }

    public static class DataProviderImpl<OBJTYPE> {

        public static List<OBJTYPE> GetRecords(List<OBJTYPE> objects, int skip, int take, List<DataProviderSortInfo> sort, List<DataProviderFilterInfo> filters, out int total) {
            objects = Filter(objects, filters);
            objects = Sort(objects, sort);
            total = objects.Count;
            objects = objects.Skip(skip).Take(take).ToList();
            return objects;
        }
        public static List<OBJTYPE> Sort(List<OBJTYPE> list, List<DataProviderSortInfo> sort) {
            if (sort != null && sort.Any()) {
                IQueryable<OBJTYPE> queryable = list.AsQueryable<OBJTYPE>();
                string order = String.Join(",", sort.Select(s => s.ToExpression()));
                return queryable.OrderBy(order).ToList();
            }
            return list;
        }
        public static List<OBJTYPE> Filter(List<OBJTYPE> list, List<DataProviderFilterInfo> filters) {
            if (filters != null) {
                GridHelper.NormalizeFilters(typeof(OBJTYPE), filters);
                // get a flat list of all filters
                List<DataProviderFilterInfo> flatFilters = DataProviderFilterInfo.CollectAllFilters(filters);
                // get all filter values as array (needed by the Where method of Dynamic Linq)
                object[] parms = (from f in flatFilters select f.Value).ToArray();
                // create a predicate expression e.g. Field1 = @0 And Field2 > @1
                string[] select = filters.Select(s => s.ToExpression(flatFilters)).ToArray();
                // use the Where method of Dynamic Linq to filter the data
                list = list.AsQueryable().Where(string.Join(" && ", select), parms).ToList<OBJTYPE>();
            }
            return list;
        }
    }

    public abstract class DataProviderImpl : IDisposable {

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
                if (DataProviderObject != null)
                    DataProviderObject.Dispose();
                DataProviderObject = null;
            }
        }
        //~DataProviderImpl() { Dispose(false); }

        protected YetaWFManager Manager { get { return YetaWFManager.Manager; } }
        protected bool HaveManager { get { return YetaWFManager.HaveManager; } }

        public const string SQLConnectString = "SQLConnect";

        protected int SiteIdentity { get; set; }

        public static readonly string DefaultString = "Default";
        protected const string IOModeString = "IOMode";
        protected const string SQLDboString = "SQLDbo";

        protected string AreaName { get; set; }
        protected string SQLConn { get; private set; }
        protected string SQLDbo { get; private set; }

        protected void SetDataProvider(dynamic dp) { DataProviderObject = dp; }
        public dynamic DataProviderObject { get; protected set; }
        public string GetTableName() {
            if (DataProviderObject == null) throw new InternalError("DataProvider must be defined in constructor using SetDataProvider() - Only supported for SQL I/O");
            return DataProviderObject.GetTableName();
        }
        public string GetDbOwner() {
            if (DataProviderObject == null) throw new InternalError("DataProvider must be defined in constructor using SetDataProvider() - Only supported for SQL I/O");
            return SQLDbo;
        }
        public string ReplaceWithTableName(string text, string searchText) {
            return DataProviderObject.ReplaceWithTableName(text, searchText);
        }
        public string ReplaceWithLanguage(string text, string searchText) {
            return DataProviderObject.ReplaceWithLanguage(text, searchText);
        }
        public dynamic GetDatabase() {
            return DataProviderObject.GetDatabase();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        protected WebConfigHelper.IOModeEnum IOMode {
            get {
                if (_ioMode == WebConfigHelper.IOModeEnum.Determine)
                    throw new InternalError("I/O mode not available - GetIOMode not yet called");
                return _ioMode;
            }
            private set {
                _ioMode = value;
            }
        }
        WebConfigHelper.IOModeEnum _ioMode = WebConfigHelper.IOModeEnum.Determine;

        public static void GetSQLInfo(out string dbo, out string connString) {
            connString = WebConfigHelper.GetValue<string>(DefaultString, SQLConnectString);
            dbo = WebConfigHelper.GetValue<string>(DefaultString, SQLDboString);
        }
        protected WebConfigHelper.IOModeEnum GetIOMode(string areaName, bool DefaultOnly = false) {

            AreaName = areaName;

            SQLDbo = "dbo";

            string ioModeDefault = WebConfigHelper.GetValue<string>(DefaultString, IOModeString);
            if (string.IsNullOrWhiteSpace(ioModeDefault))
                throw new InternalError("Default IOMode is missing");
            string connDefault, sqlDboDefault;
            GetSQLInfo(out sqlDboDefault, out connDefault);

            string ioMode = null;
            if (!DefaultOnly)
                ioMode = WebConfigHelper.GetValue<string>(AreaName, IOModeString);
            if (string.IsNullOrWhiteSpace(ioMode))
                ioMode = ioModeDefault;

            WebConfigHelper.IOModeEnum mode = WebConfigHelper.IOModeEnum.File;
            switch (ioMode.ToLower()) {
                default:
                case "file":
                    mode = WebConfigHelper.IOModeEnum.File;
                    break;
                case "sql":
                    mode = WebConfigHelper.IOModeEnum.Sql;
                    if (!DefaultOnly) {
                        SQLConn = WebConfigHelper.GetValue<string>(AreaName, SQLConnectString);
                        SQLDbo = WebConfigHelper.GetValue<string>(AreaName, SQLDboString);
                    }
                    if (string.IsNullOrWhiteSpace(SQLConn))
                        SQLConn = connDefault;
                    if (string.IsNullOrWhiteSpace(SQLDbo))
                        SQLDbo = sqlDboDefault;
                    break;
            }

            if (mode == WebConfigHelper.IOModeEnum.Sql) {
                if (string.IsNullOrWhiteSpace(SQLConn))
                    throw new InternalError("{0} is missing for {1}", SQLConnectString, AreaName);
                if (string.IsNullOrWhiteSpace(SQLDbo))
                    throw new InternalError("{0} is missing for {1}", SQLDboString, AreaName);
            }
            _ioMode = mode;
            return mode;
        }

        public DataProviderTransaction StartTransaction() {
            if (DataProviderObject == null) throw new InternalError("DataProvider must be defined in constructor using SetDataProvider() - Only supported for SQL I/O");
            return DataProviderObject.StartTransaction();
        }
        protected void CommitTransaction() {
            if (DataProviderObject == null) throw new InternalError("DataProvider must be defined in constructor using SetDataProvider() - Only supported for SQL I/O");
            DataProviderObject.EndTransaction();
        }
        protected void AbortTransaction() {
            if (DataProviderObject == null) throw new InternalError("DataProvider must be defined in constructor using SetDataProvider() - Only supported for SQL I/O");
            DataProviderObject.AbortTransaction();
        }

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
