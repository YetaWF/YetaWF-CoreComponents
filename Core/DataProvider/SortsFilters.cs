/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using YetaWF.Core.Models;
using YetaWF.Core.Support;
using YetaWF.Core.Views.Shared;

namespace YetaWF.Core.DataProvider {

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
            if (filters != null && filters.Count > 0) {
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

    public class DataProviderSortInfo {

        public DataProviderSortInfo() { }
        public DataProviderSortInfo(DataProviderSortInfo s) {
            Field = s.Field;
            Order = s.Order;
        }

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

        public DataProviderFilterInfo() { }
        public DataProviderFilterInfo(DataProviderFilterInfo f) {
            Field = f.Field;
            Filters = f.Filters;
            Logic = f.Logic;
            Operator = f.Operator;
            Value = f.Value;
            StringType = f.StringType;
        }

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
        /// <remarks>When a filter expression is received via querystring/form the value is always set using a string.
        /// </remarks>
        public string ValueAsString { get { return Value != null ? Value.ToString() : null; } set { Value = value; StringType = true; } }

        private bool StringType = false;

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
        public void NormalizeFilterProperty(Type type) {
            if (this.Field != null && StringType) { // only normalize string types, explicitly set values don't need to be changed in type
                string[] parts = Field.Split(new char[] { '.' });
                Type objType = type;
                PropertyData prop = null;
                foreach (string part in parts) {
                    prop = ObjectSupport.GetPropertyData(objType, part);
                    if (prop == null) throw new InternalError("Property {0} not found in type {1}", part, objType.Name);
                    objType = prop.PropInfo.PropertyType;
                }
                if (prop == null) throw new InternalError("Can't evaluate field {0} in type {1}", Field, objType.Name);
                if (objType != typeof(string)) {
                    if (objType.IsEnum) {
                        try { Value = Convert.ToInt32(Value); Value = Enum.ToObject(objType, Value); } catch (Exception) { }
                    } else if (objType == typeof(DateTime) || objType == typeof(DateTime?)) {
                        try { Value = Localize.Formatting.GetUtcDateTime(Convert.ToDateTime(Value)); } catch (Exception) { Value = DateTime.MinValue; }
                    } else if (objType == typeof(int) || objType == typeof(int?)) {
                        try { Value = Convert.ToInt32(Value); } catch (Exception) { Value = 0; }
                    } else if (objType == typeof(long) || objType == typeof(long?)) {
                        try { Value = Convert.ToInt64(Value); } catch (Exception) { Value = 0; }
                    } else if (objType == typeof(bool) || objType == typeof(bool?)) {
                        try { Value = Convert.ToBoolean(Value); } catch (Exception) { Value = true; }
                    } else if (objType == typeof(MultiString)) {
                        try { Value = new MultiString((string)Value); } catch (Exception) { Value = new MultiString(); }
                    } else {
                        // default to string and hope for the best
                    }
                }
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
                    if (Value == null)
                        return "(false)";
                    else if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                        return String.Format("({0} != null && {0}.ToLower().{1}(@{2}.ToLower()))", Field, command, index);
                    else
                        return String.Format("({0} != null && {0}.{1}(@{2}))", Field, command, index);
                } else if (command == "NotStartsWith") {
                    if (Value == null)
                        return "(false)";
                    else if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                        return String.Format("({0} == null || !{0}.ToLower().StartsWith(@{1}.ToLower()))", Field, index);
                    else
                        return String.Format("{0} == null || !{0}.StartsWith(@{1})", Field, index);
                } else if (command == "NotEndsWith") {
                    if (Value == null)
                        return "(false)";
                    else if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                        return String.Format("({0} == null || {0}.ToLower().EndsWith(@{1}.ToLower()))", Field, index);
                    else
                        return String.Format("{0} == null || !{0}.EndsWith(@{1})", Field, index);
                } else if (command == "NotContains") {
                    if (Value == null)
                        return "(false)";
                    else if (Value.GetType() == typeof(string) || Value.GetType() == typeof(MultiString))
                        return String.Format("({0} == null || {0}.ToLower().Contains(@{1}.ToLower()))", Field, index);
                    else
                        return String.Format("{0} == null || !{0}.Contains(@{1})", Field, index);
                } else {
                    if (Value == null) {
                        return String.Format("({0} {1} @{2})", Field, command, index);
                    } else if (Operator == "!=" || Operator == "<" || Operator == "<=") {
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
}
