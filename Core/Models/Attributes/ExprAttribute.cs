/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredAttribute : ExprAttribute {
        public RequiredAttribute() : base(OpEnum.Required) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfAttribute : ExprAttribute {
        public RequiredIfAttribute(string prop1, object val1) : base(OpEnum.RequiredIf, prop1, OpCond.Eq, val1) { }
        public RequiredIfAttribute(string prop1, object val1, string prop2, object val2) : base(OpEnum.RequiredIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public RequiredIfAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3) : base(OpEnum.RequiredIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfNotAttribute : ExprAttribute {
        public RequiredIfNotAttribute(string prop1, object val1) : base(OpEnum.RequiredIfNot, prop1, OpCond.Eq, val1) { }
        public RequiredIfNotAttribute(string prop1, object val1, string prop2, object val2) : base(OpEnum.RequiredIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public RequiredIfNotAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3) : base(OpEnum.RequiredIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfSuppliedAttribute : ExprAttribute {
        public RequiredIfSuppliedAttribute(string prop1) : base(OpEnum.RequiredIfSupplied, prop1, OpCond.None, null) { }
        public RequiredIfSuppliedAttribute(string prop1, string prop2) : base(OpEnum.RequiredIfSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null) { }
        public RequiredIfSuppliedAttribute(string prop1, string prop2, string prop3) : base(OpEnum.RequiredIfSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null, prop3, OpCond.None, null) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfNotSuppliedAttribute : ExprAttribute {
        public RequiredIfNotSuppliedAttribute(string prop1) : base(OpEnum.RequiredIfNotSupplied, prop1, OpCond.None, null) { }
        public RequiredIfNotSuppliedAttribute(string prop1, string prop2) : base(OpEnum.RequiredIfNotSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null) { }
        public RequiredIfNotSuppliedAttribute(string prop1, string prop2, string prop3) : base(OpEnum.RequiredIfNotSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null, prop3, OpCond.None, null) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SelectionRequiredAttribute : ExprAttribute {
        public SelectionRequiredAttribute() : base(OpEnum.SelectionRequired) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SelectionRequiredIfAttribute : ExprAttribute {
        public SelectionRequiredIfAttribute(string prop1, object val1) : base(OpEnum.SelectionRequiredIf, prop1, OpCond.Eq, val1) { }
        public SelectionRequiredIfAttribute(string prop1, object val1, string prop2, object val2) : base(OpEnum.SelectionRequiredIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public SelectionRequiredIfAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3) : base(OpEnum.SelectionRequiredIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SelectionRequiredIfNotAttribute : ExprAttribute {
        public SelectionRequiredIfNotAttribute(string prop1, object val1) : base(OpEnum.SelectionRequiredIfNot, prop1, OpCond.Eq, val1) { }
        public SelectionRequiredIfNotAttribute(string prop1, object val1, string prop2, object val2) : base(OpEnum.SelectionRequiredIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public SelectionRequiredIfNotAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3) : base(OpEnum.SelectionRequiredIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SelectionRequiredIfSuppliedAttribute : ExprAttribute {
        public SelectionRequiredIfSuppliedAttribute(string prop1) : base(OpEnum.SelectionRequiredIfSupplied, prop1, OpCond.None, null) { }
        public SelectionRequiredIfSuppliedAttribute(string prop1, string prop2) : base(OpEnum.SelectionRequiredIfSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null) { }
        public SelectionRequiredIfSuppliedAttribute(string prop1, string prop2, string prop3) : base(OpEnum.SelectionRequiredIfSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null, prop3, OpCond.None, null) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SelectionRequiredIfNotSuppliedAttribute : ExprAttribute {
        public SelectionRequiredIfNotSuppliedAttribute(string prop1) : base(OpEnum.SelectionRequiredIfNotSupplied, prop1, OpCond.None, null) { }
        public SelectionRequiredIfNotSuppliedAttribute(string prop1, string prop2) : base(OpEnum.SelectionRequiredIfNotSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null) { }
        public SelectionRequiredIfNotSuppliedAttribute(string prop1, string prop2, string prop3) : base(OpEnum.SelectionRequiredIfNotSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null, prop3, OpCond.None, null) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SuppressIfAttribute : ExprAttribute {
        public SuppressIfAttribute(string prop1, object val1) : base(OpEnum.SuppressIf, prop1, OpCond.Eq, val1) { }
        public SuppressIfAttribute(string prop1, object val1, string prop2, object val2) : base(OpEnum.SuppressIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public SuppressIfAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3) : base(OpEnum.SuppressIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SuppressIfNotAttribute : ExprAttribute {
        public SuppressIfNotAttribute(string prop1, object val1) : base(OpEnum.SuppressIfNot, prop1, OpCond.Eq, val1) { }
        public SuppressIfNotAttribute(string prop1, object val1, string prop2, object val2) : base(OpEnum.SuppressIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public SuppressIfNotAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3) : base(OpEnum.SuppressIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
        public SuppressIfNotAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3, string prop4, object val4) : base(OpEnum.SuppressIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3, prop4, OpCond.Eq, val4) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SuppressIfSuppliedAttribute : ExprAttribute {
        public SuppressIfSuppliedAttribute(string prop1) : base(OpEnum.SuppressIfSupplied, prop1, OpCond.None, null) { }
        public SuppressIfSuppliedAttribute(string prop1, string prop2) : base(OpEnum.SuppressIfSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null) { }
        public SuppressIfSuppliedAttribute(string prop1, string prop2, string prop3) : base(OpEnum.SuppressIfSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null, prop3, OpCond.None, null) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SuppressIfNotSuppliedAttribute : ExprAttribute {
        public SuppressIfNotSuppliedAttribute(string prop1) : base(OpEnum.SuppressIfNotSupplied, prop1, OpCond.None, null) { }
        public SuppressIfNotSuppliedAttribute(string prop1, string prop2) : base(OpEnum.SuppressIfNotSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null) { }
        public SuppressIfNotSuppliedAttribute(string prop1, string prop2, string prop3) : base(OpEnum.SuppressIfNotSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null, prop3, OpCond.None, null) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ProcessIfAttribute : ExprAttribute {
        public ProcessIfAttribute(string prop1, object val1) : base(OpEnum.ProcessIf, prop1, OpCond.Eq, val1) { }
        public ProcessIfAttribute(string prop1, object val1, string prop2, object val2) : base(OpEnum.ProcessIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public ProcessIfAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3) : base(OpEnum.ProcessIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ProcessIfNotAttribute : ExprAttribute {
        public ProcessIfNotAttribute(string prop1, object val1) : base(OpEnum.ProcessIfNot, prop1, OpCond.Eq, val1) { }
        public ProcessIfNotAttribute(string prop1, object val1, string prop2, object val2) : base(OpEnum.ProcessIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public ProcessIfNotAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3) : base(OpEnum.ProcessIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ProcessIfSuppliedAttribute : ExprAttribute {
        public ProcessIfSuppliedAttribute(string prop1) : base(OpEnum.ProcessIfSupplied, prop1, OpCond.None, null) { }
        public ProcessIfSuppliedAttribute(string prop1, string prop2) : base(OpEnum.ProcessIfSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null) { }
        public ProcessIfSuppliedAttribute(string prop1, string prop2, string prop3) : base(OpEnum.ProcessIfSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null, prop3, OpCond.None, null) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ProcessIfNotSuppliedAttribute : ExprAttribute {
        public ProcessIfNotSuppliedAttribute(string prop1) : base(OpEnum.ProcessIfNotSupplied, prop1, OpCond.None, null) { }
        public ProcessIfNotSuppliedAttribute(string prop1, string prop2) : base(OpEnum.ProcessIfNotSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null) { }
        public ProcessIfNotSuppliedAttribute(string prop1, string prop2, string prop3) : base(OpEnum.ProcessIfNotSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null, prop3, OpCond.None, null) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class HideIfAttribute : ExprAttribute {
        public HideIfAttribute(string prop1, object val1) : base(OpEnum.HideIf, prop1, OpCond.Eq, val1) { }
        public HideIfAttribute(string prop1, object val1, string prop2, object val2) : base(OpEnum.HideIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public HideIfAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3) : base(OpEnum.HideIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class HideIfNotAttribute : ExprAttribute {
        public HideIfNotAttribute(string prop1, object val1) : base(OpEnum.HideIfNot, prop1, OpCond.Eq, val1) { }
        public HideIfNotAttribute(string prop1, object val1, string prop2, object val2) : base(OpEnum.HideIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public HideIfNotAttribute(string prop1, object val1, string prop2, object val2, string prop3, object val3) : base(OpEnum.HideIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class HideIfSuppliedAttribute : ExprAttribute {
        public HideIfSuppliedAttribute(string prop1) : base(OpEnum.HideIfSupplied, prop1, OpCond.None, null) { }
        public HideIfSuppliedAttribute(string prop1, string prop2) : base(OpEnum.HideIfSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null) { }
        public HideIfSuppliedAttribute(string prop1, string prop2, string prop3) : base(OpEnum.HideIfSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null, prop3, OpCond.None, null) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class HideIfNotSuppliedAttribute : ExprAttribute {
        public HideIfNotSuppliedAttribute(string prop1) : base(OpEnum.HideIfNotSupplied, prop1, OpCond.None, null) { }
        public HideIfNotSuppliedAttribute(string prop1, string prop2) : base(OpEnum.HideIfNotSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null) { }
        public HideIfNotSuppliedAttribute(string prop1, string prop2, string prop3) : base(OpEnum.HideIfNotSupplied, prop1, OpCond.None, null, prop2, OpCond.None, null, prop3, OpCond.None, null) { }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ExprAttribute : ValidationAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public const string ValueOf = "ValOf+";

        public enum OpEnum {
            Required,
            RequiredIf,
            RequiredIfNot,
            RequiredIfSupplied,
            RequiredIfNotSupplied,
            SelectionRequired,
            SelectionRequiredIf,
            SelectionRequiredIfNot,
            SelectionRequiredIfSupplied,
            SelectionRequiredIfNotSupplied,
            SuppressIf,
            SuppressIfNot,
            SuppressIfSupplied,
            SuppressIfNotSupplied,

            ProcessIf,
            ProcessIfNot,
            ProcessIfSupplied,
            ProcessIfNotSupplied,

            HideIf,
            HideIfNot,
            HideIfSupplied,
            HideIfNotSupplied,
        };

        public enum OpCond {
            None,
            Eq,
            NotEq,
        }

        /// <summary>
        /// Defines whether the property is hidden or just disabled when it's not processed.
        /// The default is to hide the property.
        /// </summary>
        /// <remarks>This property is only used for Processxxx attributes and ignored for others.
        ///
        /// Only simple controls (input, select) can currently be used with the Disabled property.
        /// </remarks>
        public bool Disable { get; set; }

        public OpEnum Op { get; protected set; }
        public List<Expr> ExprList { get; protected set; }

        public bool IsSuppressAttribute {
            get {
                switch (Op) {
                    case OpEnum.SuppressIf:
                    case OpEnum.SuppressIfNot:
                    case OpEnum.SuppressIfSupplied:
                    case OpEnum.SuppressIfNotSupplied:
                        return true;
                    default:
                        break;
                }
                return false;
            }
        }
        public bool IsRequiredAttribute {
            get {
                switch (Op) {
                    case OpEnum.Required:
                    case OpEnum.RequiredIf:
                    case OpEnum.RequiredIfNot:
                    case OpEnum.RequiredIfSupplied:
                    case OpEnum.RequiredIfNotSupplied:
                        return true;
                    default:
                        break;
                }
                return false;
            }
        }
        public bool IsSelectionRequiredAttribute {
            get {
                switch (Op) {
                    case OpEnum.SelectionRequired:
                    case OpEnum.SelectionRequiredIf:
                    case OpEnum.SelectionRequiredIfNot:
                    case OpEnum.SelectionRequiredIfSupplied:
                    case OpEnum.SelectionRequiredIfNotSupplied:
                        return true;
                    default:
                        break;
                }
                return false;
            }
        }
        public bool IsProcessAttribute {
            get {
                switch (Op) {
                    case OpEnum.ProcessIf:
                    case OpEnum.ProcessIfNot:
                    case OpEnum.ProcessIfSupplied:
                    case OpEnum.ProcessIfNotSupplied:
                        return true;
                    default:
                        break;
                }
                return false;
            }
        }
        public bool IsHideAttribute {
            get {
                switch (Op) {
                    case OpEnum.HideIf:
                    case OpEnum.HideIfNot:
                    case OpEnum.HideIfSupplied:
                    case OpEnum.HideIfNotSupplied:
                        return true;
                    default:
                        break;
                }
                return false;
            }
        }
        public static bool IsRequired(List<ExprAttribute> exprAttributes, object model) {
            foreach (ExprAttribute e in exprAttributes) {
                if (e.IsRequiredAttribute && e.IsValid(model))
                    return true;
            }
            return false;
        }
        public static bool IsSelectionRequired(List<ExprAttribute> exprAttributes, object model) {
            foreach (ExprAttribute e in exprAttributes) {
                if (e.IsSelectionRequiredAttribute && e.IsValid(model))
                    return true;
            }
            return false;
        }
        public static bool IsSuppressed(List<ExprAttribute> exprAttributes, object model) {
            foreach (ExprAttribute e in exprAttributes) {
                if (e.IsSuppressAttribute && e.IsValid(model))
                    return true;// suppress this as requested
            }
            return false;
        }

        public override bool IsValid(object value) {
            foreach (Expr expr in ExprList) {
                switch (Op) {
                    case OpEnum.RequiredIf:
                    case OpEnum.SelectionRequiredIf:
                    case OpEnum.SuppressIfNot:
                        if (!IsExprValid(expr, value))
                            return true;
                        break;
                    case OpEnum.RequiredIfNot:
                    case OpEnum.SelectionRequiredIfNot:
                    case OpEnum.SuppressIf:
                        if (IsExprValid(expr, value))
                            return true;
                        break;
                    case OpEnum.RequiredIfSupplied:
                    case OpEnum.SelectionRequiredIfSupplied:
                    case OpEnum.SuppressIfNotSupplied:
                        if (!IsExprSupplied(expr, value))
                            return true;
                        break;
                    case OpEnum.RequiredIfNotSupplied:
                    case OpEnum.SelectionRequiredIfNotSupplied:
                    case OpEnum.SuppressIfSupplied:
                        if (IsExprSupplied(expr, value))
                            return true;
                        break;
                    case OpEnum.ProcessIf:
                    case OpEnum.ProcessIfNot:
                    case OpEnum.ProcessIfSupplied:
                    case OpEnum.ProcessIfNotSupplied:
                    case OpEnum.HideIf:
                    case OpEnum.HideIfNot:
                    case OpEnum.HideIfSupplied:
                    case OpEnum.HideIfNotSupplied:
                        break;
                    default:
                        throw new InternalError($"Unexpected Op value {Op}");
                }
            }
            return false;
        }
        protected override ValidationResult IsValid(object value, ValidationContext context) {
            if (IsValid(context.ObjectInstance))
                return ValidationResult.Success;
            switch (Op) {
                case OpEnum.Required:
                case OpEnum.RequiredIf:
                case OpEnum.RequiredIfNot:
                case OpEnum.RequiredIfSupplied:
                case OpEnum.RequiredIfNotSupplied:
                    if (IsEmpty(value))
                        return new ValidationResult(__ResStr("requiredExpr", "The '{0}' field is required", AttributeHelper.GetPropertyCaption(context)));
                    break;
                case OpEnum.SelectionRequired:
                case OpEnum.SelectionRequiredIf:
                case OpEnum.SelectionRequiredIfNot:
                case OpEnum.SelectionRequiredIfSupplied:
                case OpEnum.SelectionRequiredIfNotSupplied:
                    if (IsEmptyOrZero(value))
                        return new ValidationResult(__ResStr("requiredExpr", "The '{0}' field is required", AttributeHelper.GetPropertyCaption(context)));
                    break;
                case OpEnum.SuppressIf:
                case OpEnum.SuppressIfNot:
                case OpEnum.SuppressIfSupplied:
                case OpEnum.SuppressIfNotSupplied:
                case OpEnum.ProcessIf:
                case OpEnum.ProcessIfNot:
                case OpEnum.ProcessIfSupplied:
                case OpEnum.ProcessIfNotSupplied:
                case OpEnum.HideIf:
                case OpEnum.HideIfNot:
                case OpEnum.HideIfSupplied:
                case OpEnum.HideIfNotSupplied:
                    break;
                default:
                    throw new InternalError($"Unexpected Op value {Op}");
            }
            return ValidationResult.Success;
        }

        public bool IsExprValid(Expr expr, object model) {
            object leftVal = GetPropertyValue(model, expr.LeftProperty);
            object rightVal;
            if (expr.IsRightProperty)
                rightVal = GetPropertyValue(model, expr.RightProperty);
            else
                rightVal = expr.Value;

            switch (expr.Cond) {
                case OpCond.Eq:
                    return IsEqual(leftVal, rightVal);
                case OpCond.NotEq:
                    return !IsEqual(leftVal, rightVal);
                default:
                    throw new InternalError($"Unexpected condition {expr.Cond}");
            }
        }
        public bool IsExprSupplied(Expr expr, object model) {
            object leftVal = GetPropertyValue(model, expr.LeftProperty);
            if (IsEmpty(leftVal))
                return false;
            return true;
        }
        protected object GetPropertyValue(object model, string propName) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, propName);
            return pi.GetValue(model, null);
        }
        private bool IsEmpty(object value) {
            if (value is MultiString) {
                MultiString ms = (MultiString)value;
                string s = ms.ToString();
                if (s == null || s.Length == 0)
                    return true;
                return false;
            } else if (value is Guid) {
                if ((Guid)value == Guid.Empty)
                    return true;
                return false;
            } else if (value is ICollection) {
                ICollection coll = (ICollection)value;
                return coll.Count == 0;
            } else if (value == null)
                return true;
            string v = value.ToString();
            if (string.IsNullOrWhiteSpace(v))
                return true;
            return false;
        }
        private bool IsEmptyOrZero(object value) {
            if (value is MultiString) {
                MultiString ms = (MultiString)value;
                string s = ms.ToString();
                if (s == null || s.Length == 0)
                    return true;
                if (s == "0")
                    return true;
                return false;
            } else if (value is Guid) {
                if ((Guid)value == Guid.Empty)
                    return true;
                return false;
            } else if (value is ICollection) {
                ICollection coll = (ICollection)value;
                return coll.Count == 0;
            } else if (value == null)
                return true;
            string v = value.ToString();
            if (string.IsNullOrWhiteSpace(v))
                return true;
            if (v == "0")
                return true;
            return false;
        }
        private bool IsEqual(object val1, object val2) {
            if (val1 == null || val2 == null) {
                return val1 == val2;
            }
            Type type = val1.GetType();
            if (type != val2.GetType())
                return false;
            return val1.ToString() == val2.ToString();
        }

        public ExprAttribute(OpEnum op) {
            Op = op;
            ExprList = new List<Expr>();
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1) {
            Op = op;
            ExprList = new List<Expr>();
            ExprList.Add(new Expr {
                LeftProperty = propLeft1, Cond = cond1, Value = val1
            });
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2) {
            Op = op;
            ExprList = new List<Expr>();
            ExprList.Add(new Expr {
                LeftProperty = propLeft1, Cond = cond1, Value = val1
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft2, Cond = cond2, Value = val2
            });
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2, string propLeft3, OpCond cond3, object val3) {
            Op = op;
            ExprList = new List<Expr>();
            ExprList.Add(new Expr {
                LeftProperty = propLeft1, Cond = cond1, Value = val1
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft2, Cond = cond2, Value = val2
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft3, Cond = cond3, Value = val3
            });
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2, string propLeft3, OpCond cond3, object val3,
                string propLeft4, OpCond cond4, object val4) {
            Op = op;
            ExprList = new List<Expr>();
            ExprList.Add(new Expr {
                LeftProperty = propLeft1, Cond = cond1, Value = val1
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft2, Cond = cond2, Value = val2
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft3, Cond = cond3, Value = val3
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft4, Cond = cond4, Value = val4
            });
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2, string propLeft3, OpCond cond3, object val3,
                string propLeft4, OpCond cond4, object val4, string propLeft5, OpCond cond5, object val5) {
            Op = op;
            ExprList = new List<Expr>();
            ExprList.Add(new Expr {
                LeftProperty = propLeft1, Cond = cond1, Value = val1
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft2, Cond = cond2, Value = val2
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft3, Cond = cond3, Value = val3
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft4, Cond = cond4, Value = val4
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft5, Cond = cond5, Value = val5
            });
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2, string propLeft3, OpCond cond3, object val3,
                string propLeft4, OpCond cond4, object val4, string propLeft5, OpCond cond5, object val5, string propLeft6, OpCond cond6, object val6) {
            Op = op;
            ExprList = new List<Expr>();
            ExprList.Add(new Expr {
                LeftProperty = propLeft1, Cond = cond1, Value = val1
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft2, Cond = cond2, Value = val2
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft3, Cond = cond3, Value = val3
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft4, Cond = cond4, Value = val4
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft5, Cond = cond5, Value = val5
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft6, Cond = cond6, Value = val6
            });
        }

        public class Expr {
            [JsonIgnore]
            public string LeftProperty { get; set; }
            [JsonIgnore]
            public object Value {
                get {
                    if (_Value == null) return null;
                    string v = _Value.ToString();
                    if (v.StartsWith(ValueOf))
                        throw new InternalError("Value used when the attribute describes another property");
                    return _Value;
                }
                set {
                    _Value = value;
                }
            }
            [JsonIgnore]
            public bool IsRightProperty {
                get {
                    if (_Value == null) return false;
                    string v = _Value.ToString();
                    if (v.StartsWith(ValueOf))
                        return true;
                    return false;
                }
            }
            [JsonIgnore]
            public string RightProperty {
                get {
                    if (_Value == null)
                        throw new InternalError("Property used when the attribute describes a value");
                    string v = _Value.ToString();
                    if (v.StartsWith(ValueOf))
                        throw new InternalError("Property used when the attribute describes a value");
                    return v.Substring(ValueOf.Length);
                }
            }
            [JsonIgnore]
            public object _Value { get; set; }
            //serialized for client-side
            public OpCond Cond { get; set; }
            public string _Left { get { return AttributeHelper.GetDependentPropertyName(LeftProperty); } }
            public string _Right { get { return IsRightProperty ? AttributeHelper.GetDependentPropertyName(RightProperty) : ""; } }
            public object _RightVal { get { return !IsRightProperty ? _Value : null; } }
        }

        public void AddValidation(object container, PropertyData propData, YTagBuilder tag) {
            switch (Op) {
                default:
                    throw new InternalError($"Invalid Op {Op} in {nameof(AddValidation)}");
                case OpEnum.Required:
                case OpEnum.RequiredIf:
                case OpEnum.RequiredIfNot:
                case OpEnum.RequiredIfSupplied:
                case OpEnum.RequiredIfNotSupplied: {
                        string msg = __ResStr("requiredExpr", "The '{0}' field is required", propData.GetCaption(container));
                        tag.MergeAttribute("data-val-requiredexpr", msg);
                        tag.MergeAttribute("data-val-requiredexpr-op", ((int)Op).ToString());
                        tag.MergeAttribute("data-val-requiredexpr-json", YetaWFManager.JsonSerialize(ExprList));
                        tag.MergeAttribute("data-val", "true");
                        break;
                    }
                case OpEnum.SelectionRequired:
                case OpEnum.SelectionRequiredIf:
                case OpEnum.SelectionRequiredIfNot:
                case OpEnum.SelectionRequiredIfSupplied:
                case OpEnum.SelectionRequiredIfNotSupplied: {
                        string msg = __ResStr("requiredSelExpr", "The '{0}' field requires a selection", propData.GetCaption(container));
                        tag.MergeAttribute("data-val-requiredexpr", msg);
                        tag.MergeAttribute("data-val-requiredexpr-op", ((int)Op).ToString());
                        tag.MergeAttribute("data-val-requiredexpr-json", YetaWFManager.JsonSerialize(ExprList));
                        tag.MergeAttribute("data-val", "true");
                        break;
                    }
                case OpEnum.SuppressIf:
                case OpEnum.SuppressIfNot:
                case OpEnum.SuppressIfSupplied:
                case OpEnum.SuppressIfNotSupplied:
                case OpEnum.ProcessIf:
                case OpEnum.ProcessIfNot:
                case OpEnum.ProcessIfSupplied:
                case OpEnum.ProcessIfNotSupplied:
                case OpEnum.HideIf:
                case OpEnum.HideIfNot:
                case OpEnum.HideIfSupplied:
                case OpEnum.HideIfNotSupplied:
                    break;
            }
        }
    }
}
