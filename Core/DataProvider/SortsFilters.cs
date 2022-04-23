/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Reflection;
using YetaWF.Core.Components;
using YetaWF.Core.Models;
using YetaWF.Core.Support;

namespace YetaWF.Core.DataProvider {

    public static class DataProviderImpl<OBJTYPE> {

        public static DataProviderGetRecords<OBJTYPE> GetRecords(List<object> objects, int skip, int take, List<DataProviderSortInfo>? sorts, List<DataProviderFilterInfo>? filters) {
            return GetRecords((from d in objects select (OBJTYPE)d).ToList(), skip, take, sorts, filters);
        }
        public static DataProviderGetRecords<OBJTYPE> GetRecords(List<OBJTYPE> objects, int skip, int take, List<DataProviderSortInfo>? sorts, List<DataProviderFilterInfo>? filters) {
            objects = Filter(objects, filters);
            objects = Sort(objects, sorts);
            int total = objects.Count;
            if (skip != 0 || take != 0)
                objects = objects.Skip(skip).Take(take).ToList();
            return new DataProviderGetRecords<OBJTYPE> {
                Data = objects,
                Total = total,
            };
        }
        public static List<OBJTYPE> Sort(List<OBJTYPE> list, List<DataProviderSortInfo>? sort) {
            if (sort != null && sort.Any()) {
                IQueryable<OBJTYPE> queryable = list.AsQueryable<OBJTYPE>();
                string order = String.Join(",", sort.Select(s => s.ToExpression()));
                return queryable.OrderBy(order).ToList();
            }
            return list;
        }
        public static List<OBJTYPE> Filter(List<OBJTYPE> list, List<DataProviderFilterInfo>? filters) {
            if (filters != null && filters.Count > 0) {
                DataProviderFilterInfo.NormalizeFilters(typeof(OBJTYPE), filters);
                // get a flat list of all filters
                List<DataProviderFilterInfo> flatFilters = DataProviderFilterInfo.CollectAllFilters(filters);
                // get all filter values as array (needed by the Where method of Dynamic Linq)
                object[] parms = (from f in flatFilters select f.Value).ToArray();
                // create a predicate expression e.g. Field1 = @0 And Field2 > @1
                string[] select = filters.Select(s => s.ToExpression(flatFilters)).ToArray();
                // use the Where method of Dynamic Linq to filter the data

                ParsingConfig config = new ParsingConfig {
                    CustomTypeProvider = new DynCustomTypeProvider()
                };
                string logic = filters[0].Logic ?? "&&";
                list = list.AsQueryable().Where(config, string.Join($" {logic} ", select), parms).ToList<OBJTYPE>();
            }
            return list;
        }
        // https://stackoverflow.com/questions/18313362/call-function-in-dynamic-linq/34301514
        // https://www.codefactor.io/repository/github/stefh/system.linq.dynamic.core/source/master/src-console/ConsoleAppEF2.1.1/Program.cs
        private class DynCustomTypeProvider : AbstractDynamicLinqCustomTypeProvider, IDynamicLinkCustomTypeProvider {

            public virtual HashSet<Type> GetCustomTypes() {
                if (_customTypes == null) {
                    _customTypes = new HashSet<Type>(FindTypesMarkedWithDynamicLinqTypeAttribute(new[] { GetType().GetTypeInfo().Assembly }));
                    //_customTypes.Add(typeof(MultiString));
                }
                return _customTypes;
            }
            private HashSet<Type>? _customTypes;

            public Dictionary<Type, List<MethodInfo>> GetExtensionMethods() {
                return new Dictionary<Type, List<MethodInfo>>();
            }
            public Type ResolveType(string typeName) {
                return ResolveType(Assemblies.GetLoadedAssemblies(), typeName);
            }
            public Type ResolveTypeBySimpleName(string typeName) {
                return ResolveTypeBySimpleName(Assemblies.GetLoadedAssemblies(), typeName);
            }
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
        public string Field { get; set; } = null!;

        /// <summary>
        /// Gets or sets the sort direction. Should be either "asc" or "desc".
        /// </summary>
        public SortDirection Order { get; set; }

        private string GetOrder() {
            switch (Order) {
                case SortDirection.Ascending:
                    return "asc";
                case SortDirection.Descending:
                    return "desc";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Converts to form required by Dynamic Linq e.g. "Field1 desc"
        /// </summary>
        public string ToExpression() {
            return $"@{Field} {GetOrder()}";
        }

        public static List<DataProviderSortInfo> Join(List<DataProviderSortInfo>? sort, DataProviderSortInfo addSort) {
            if (sort == null || sort.Count == 0) return new List<DataProviderSortInfo> { addSort };
            sort.Add(addSort);
            return sort;
        }

        /// <summary>
        /// Used to change a sort column when another column is used to sort rather than the displayed property.
        /// This method translates the displayed property name to the actual sort column name.
        /// </summary>
        /// <remarks>
        /// This is typically used when one property is displayed in a grid, but another property should be used for sort purposes.
        /// It is called before retrieving data from a data provider to translate the grid's column(s) to the column(s) used to sort/filter by the data provider.
        ///
        /// The parameter <paramref name="sort"/> only supports 1 sort field.
        /// </remarks>
        /// <param name="displayProperty">The name of the property as displayed by the grid.</param>
        /// <param name="realColumn">The name of the property that should be used by the data provider for sort/filter purposes.</param>
        /// <param name="filters">A collection describing the filtering criteria.</param>
        /// <param name="sort">A collection describing the sort order.</param>
        // sortas, sort as
        public static void UpdateAlternateSortColumn(List<DataProviderSortInfo>? sort, List<DataProviderFilterInfo>? filters, string displayProperty, string realColumn) {
            if (sort != null && sort.Count == 1) {
                DataProviderSortInfo sortInfo = sort.First();
                if (sortInfo.Field == displayProperty)
                    sortInfo.Field = realColumn;
            }
            UpdateNavigatedFilters(filters, displayProperty, realColumn);
        }

        private static void UpdateNavigatedFilters(List<DataProviderFilterInfo>? filters, string displayProperty, string realColumn) {
            if (filters == null) return;
            foreach (DataProviderFilterInfo f in filters) {
                if (f.Field == displayProperty) {
                    f.Field = realColumn;
                } else {
                    UpdateNavigatedFilters(f.Filters, displayProperty, realColumn);
                }
            }
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
        public string? Field { get; set; }

        /// <summary>
        /// Gets or sets the filtering operator. Set to <c>null</c> if the <c>Filters</c> property is set.
        /// </summary>
        public string Operator { get; set; } = null!;

        /// <summary>
        /// Gets or sets the filtering value. Set to <c>null</c> if the <c>Filters</c> property is set.
        /// </summary>
        public object? Value { get; set; }
        /// <summary>
        /// Gets or sets the filtering value. Set to <c>null</c> if the <c>Filters</c> property is set.
        /// </summary>
        /// <remarks>When a filter expression is received via querystring/form the value is always set using a string.
        /// </remarks>
        public string ValueAsString { get { return Value != null ? Value.ToString()! : string.Empty; } set { Value = value; StringType = true; } }

        private bool StringType = false;

        /// <summary>
        /// Gets or sets the filtering logic. Can be set to "||" or "&amp;&amp;". Set to <c>null</c> unless <c>Filters</c> is set.
        /// </summary>
        public string? Logic { get; set; }

        /// <summary>
        /// Gets or sets the child filter expressions. Set to <c>null</c> if there are no child expressions.
        /// </summary>
        public List<DataProviderFilterInfo>? Filters { get; set; }
        private bool IsComplex { get { return !string.IsNullOrWhiteSpace(Logic); } }

        private static readonly IDictionary<string, string> Operators = new Dictionary<string, string> {
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
        public static List<DataProviderFilterInfo> CollectAllFilters(List<DataProviderFilterInfo>? filters) {
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
        protected void NormalizeFilterProperty(Type type) {
            if (this.Field != null && StringType) { // only normalize string types, explicitly set values don't need to be changed in type
                string[] parts = Field.Split(new char[] { '.', '_' });
                Type objType = type;
                PropertyData? prop = null;
                foreach (string part in parts) {
                    prop = ObjectSupport.GetPropertyData(objType, part);
                    objType = prop.PropInfo.PropertyType;
                }
                if (prop == null) throw new InternalError("Can't evaluate field {0} in type {1}", Field, objType.Name);
                if (this.Operator == "Complex") {
                    ComplexFilterJSONBase filterBase = Utility.JsonDeserialize<ComplexFilterJSONBase>(ValueAsString);
                    DataProviderFilterInfo? newFilter = YetaWFComponentExtender.GetDataProviderFilterInfoFromUIHint(filterBase.UIHint, Field, ValueAsString);
                    if (newFilter == null)
                        throw new InternalError($"{nameof(YetaWFComponentExtender.GetDataProviderFilterInfoFromUIHint)} returned null for Complex filter ");
                    ObjectSupport.CopyData(newFilter, this); // use new filter values
                    NormalizeFilters(type, new List<DataProviderFilterInfo> { this });
                } else {
                    if (objType != typeof(string)) {
                        if (objType.IsEnum) {
                            object? final = null;
                            bool ok = false;
                            if (!ok) // handle by name
                                try { final = Enum.ToObject(objType, Value!); ok = true; } catch (Exception) { }
                            if (!ok) // handle by value
                                try { final = Convert.ToInt32(Value); ok = true; } catch (Exception) { }
                            if (!ok)
                                throw new InternalError($"Unable to convert value {Value} to {type.FullName}");
                            Value = final;
                        } else if (objType == typeof(DateTime) || (objType == typeof(DateTime?) && Value != null)) {
                            try { Value = Localize.Formatting.GetUtcDateTime(Convert.ToDateTime(Value)); } catch (Exception) { Value = DateTime.MinValue; }
                        } else if (objType == typeof(int) || (objType == typeof(int?) && Value != null)) {
                            try { Value = Convert.ToInt32(Value); } catch (Exception) { Value = 0; }
                        } else if (objType == typeof(long) || (objType == typeof(long?) && Value != null)) {
                            try { Value = Convert.ToInt64(Value); } catch (Exception) { Value = 0; }
                        } else if (objType == typeof(bool) || (objType == typeof(bool?) && Value != null)) {
                            try { Value = Convert.ToBoolean(Value); } catch (Exception) { Value = true; }
                        } else if (objType == typeof(MultiString)) {
                            try { Value = new MultiString((string)Value!); } catch (Exception) { Value = new MultiString(); }
                        } else if (objType == typeof(decimal) || (objType == typeof(decimal?) && Value != null)) {
                            try { Value = Convert.ToDecimal(Value); } catch (Exception) { Value = 0; }
                        } else if (objType == typeof(Guid) || (objType == typeof(Guid?) && Value != null)) {
                            Value = new GuidPartial { PartialString = ValueAsString.ToLower() };
                        } else {
                            // default to string and hope for the best
                        }
                    }
                }
            }
        }
        internal class GuidPartial {
            public string PartialString { get; set; } = null!;
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
                    else if (Value.GetType() == typeof(string))
                        return $"(@{Field} != null && @{Field}.ToLower().{command}(@{index}.ToLower()))";
                    else if (Value.GetType() == typeof(MultiString))
                        return $"(@{Field} != null && {nameof(MultiString)}.Dyn{command}(@{Field}, @{index}))";// Dynamically built
                    else if (Value.GetType() == typeof(GuidPartial))
                        return $"(@{Field} != null && @{Field}.ToString().ToLower().{command}(@{index}.PartialString))";
                    else
                        return $"(@{Field} != null && @{Field}.{command}(@{index}))";
                } else if (command == "NotStartsWith") {
                    if (Value == null)
                        return "(false)";
                    else if (Value.GetType() == typeof(string))
                        return $"(@{Field} == null || !@{Field}.ToLower().StartsWith(@{index}.ToLower()))";
                    else if (Value.GetType() == typeof(MultiString))
                        return $"(@{Field} == null || !{nameof(MultiString)}.{nameof(MultiString.DynStartsWith)}(@{Field}, @{index}))";
                    else
                        return $"@{Field} == null || !@{Field}.StartsWith(@{index})";
                } else if (command == "NotEndsWith") {
                    if (Value == null)
                        return "(false)";
                    else if (Value.GetType() == typeof(string))
                        return $"(@{Field} == null || !@{Field}.ToLower().EndsWith(@{index}.ToLower()))";
                    else if (Value.GetType() == typeof(MultiString))
                        return $"(@{Field} == null || !{nameof(MultiString)}.{nameof(MultiString.DynEndsWith)}(@{Field}, @{index}))";
                    else
                        return $"@{Field} == null || !@{Field}.EndsWith(@{index})";
                } else if (command == "NotContains") {
                    if (Value == null)
                        return "(false)";
                    else if (Value.GetType() == typeof(string))
                        return $"(@{Field} == null || !@{Field}.ToLower().Contains(@{index}.ToLower()))";
                    else if (Value.GetType() == typeof(MultiString))
                        return $"(@{Field} == null || !{nameof(MultiString)}.{nameof(MultiString.DynContains)}(@{Field}, @{index}))";
                    else
                        return $"@{Field} == null || !@{Field}.Contains(@{index})";
                } else {
                    if (Value == null) {
                        return $"(@{Field} {command} @{index})";
                    } else if (Operator == "!=" || Operator == "<" || Operator == "<=") {
                        if (Value.GetType() == typeof(string))
                            return $"(@{Field} == null || @{Field}.ToLower() {command} @{index}.ToLower())";
                        else if (Value.GetType() == typeof(MultiString))
                            return $"(@{Field} == null || {nameof(MultiString)}.{nameof(MultiString.DynCompare)}(@{Field}, \"{command}\", @{index}))";
                        else
                            return $"(@{Field} == null || @{Field} {command} @{index})";
                    } else if (Operator == ">" || Operator == ">=") {
                        if (Value.GetType() == typeof(string))
                            return $"(@{Field} != null && @{Field}.ToLower() {command} @{index}.ToLower())";
                        else if (Value.GetType() == typeof(MultiString))
                            return $"(@{Field} != null && {nameof(MultiString)}.{nameof(MultiString.DynCompare)}(@{Field}, \"{command}\", @{index}))";
                        else
                            return $"(@{Field} != null && @{Field} {command} @{index})";
                    } else {
                        if (Value.GetType() == typeof(string))
                            return $"(@{Field} != null && @{Field}.ToLower() {command} @{index}.ToLower())";
                        else if (Value.GetType() == typeof(MultiString))
                            return $"(@{Field} != null && {nameof(MultiString)}.{nameof(MultiString.DynCompare)}(@{Field}, \"{command}\", @{index}))";
                        else
                            return $"@{Field} {command} @{index}";
                    }
                }
            }
        }

        public static List<DataProviderFilterInfo> Join(List<DataProviderFilterInfo>? filters, DataProviderFilterInfo addFilter, string? SimpleLogic = null) {

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
                        newRoot1.Filters.AddRange(addFilter.Filters!);
                        return new List<DataProviderFilterInfo> { newRoot1 };
                    } else {
                        // complex filter also uses && so just append new filters to current list (with implied &&)
                        filters.AddRange(addFilter.Filters!);
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
                        newRoot1.Filters.AddRange(addFilter.Filters!);
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
                            root.Filters!.Add(addFilter);
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
                            newRoot1.Filters.AddRange(addFilter.Filters!);
                            return new List<DataProviderFilterInfo> { newRoot1 };
                        } else {
                            // new complex filter uses same logic as root, so just append new filters to current list
                            root.Filters!.AddRange(addFilter.Filters!);
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
        public static List<DataProviderFilterInfo>? Copy(List<DataProviderFilterInfo>? filters) {
            List<DataProviderFilterInfo>? newFilters = null;
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
        /// <summary>
        /// Normalize filters.
        /// RESEARCH! The purpose of this method is unclear.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="filters">A collection describing the filtering criteria.</param>
        public static void NormalizeFilters(Type type, List<DataProviderFilterInfo>? filters) {
            if (filters != null) {
                foreach (DataProviderFilterInfo f in filters) {
                    f.NormalizeFilterProperty(type);
                    NormalizeFilters(type, f.Filters);
                }
            }
        }
    }
    public static class GuidExtender {
        public static string ToLower(this Guid guid) {
            return guid.ToString().ToLower();
        }
    }
}
