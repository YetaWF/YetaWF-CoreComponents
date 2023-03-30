/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredAttribute : ExprAttribute {
        public RequiredAttribute() : base(OpEnum.Required) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfAttribute : ExprAttribute {
        public RequiredIfAttribute(string prop1, object? val1) : base(OpEnum.RequiredIf, prop1, OpCond.Eq, val1) { }
        public RequiredIfAttribute(string prop1, object? val1, string prop2, object? val2) : base(OpEnum.RequiredIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public RequiredIfAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3) : base(OpEnum.RequiredIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfNotAttribute : ExprAttribute {
        public RequiredIfNotAttribute(string prop1, object? val1) : base(OpEnum.RequiredIfNot, prop1, OpCond.Eq, val1) { }
        public RequiredIfNotAttribute(string prop1, object? val1, string prop2, object? val2) : base(OpEnum.RequiredIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public RequiredIfNotAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3) : base(OpEnum.RequiredIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
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
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SelectionRequiredAttribute : ExprAttribute {
        public SelectionRequiredAttribute() : base(OpEnum.SelectionRequired) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SelectionRequiredIfAttribute : ExprAttribute {
        public SelectionRequiredIfAttribute(string prop1, object? val1) : base(OpEnum.SelectionRequiredIf, prop1, OpCond.Eq, val1) { }
        public SelectionRequiredIfAttribute(string prop1, object? val1, string prop2, object? val2) : base(OpEnum.SelectionRequiredIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public SelectionRequiredIfAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3) : base(OpEnum.SelectionRequiredIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SelectionRequiredIfNotAttribute : ExprAttribute {
        public SelectionRequiredIfNotAttribute(string prop1, object? val1) : base(OpEnum.SelectionRequiredIfNot, prop1, OpCond.Eq, val1) { }
        public SelectionRequiredIfNotAttribute(string prop1, object? val1, string prop2, object? val2) : base(OpEnum.SelectionRequiredIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public SelectionRequiredIfNotAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3) : base(OpEnum.SelectionRequiredIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
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
        public SuppressIfAttribute(string prop1, object? val1) : base(OpEnum.SuppressIf, prop1, OpCond.Eq, val1) { }
        public SuppressIfAttribute(string prop1, object? val1, string prop2, object? val2) : base(OpEnum.SuppressIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public SuppressIfAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3) : base(OpEnum.SuppressIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SuppressIfNotAttribute : ExprAttribute {
        public SuppressIfNotAttribute(string prop1, object? val1) : base(OpEnum.SuppressIfNot, prop1, OpCond.Eq, val1) { }
        public SuppressIfNotAttribute(string prop1, object? val1, string prop2, object? val2) : base(OpEnum.SuppressIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public SuppressIfNotAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3) : base(OpEnum.SuppressIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
        public SuppressIfNotAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3, string prop4, object? val4) : base(OpEnum.SuppressIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3, prop4, OpCond.Eq, val4) { }
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
        public ProcessIfAttribute(string prop1, object? val1) : base(OpEnum.ProcessIf, prop1, OpCond.Eq, val1) { }
        public ProcessIfAttribute(string prop1, object? val1, string prop2, object? val2) : base(OpEnum.ProcessIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public ProcessIfAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3) : base(OpEnum.ProcessIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ProcessIfNotAttribute : ExprAttribute {
        public ProcessIfNotAttribute(string prop1, object? val1) : base(OpEnum.ProcessIfNot, prop1, OpCond.Eq, val1) { }
        public ProcessIfNotAttribute(string prop1, object? val1, string prop2, object? val2) : base(OpEnum.ProcessIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public ProcessIfNotAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3) : base(OpEnum.ProcessIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
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
        public HideIfAttribute(string prop1, object? val1) : base(OpEnum.HideIf, prop1, OpCond.Eq, val1) { }
        public HideIfAttribute(string prop1, object? val1, string prop2, object? val2) : base(OpEnum.HideIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public HideIfAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3) : base(OpEnum.HideIf, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class HideIfNotAttribute : ExprAttribute {
        public HideIfNotAttribute(string prop1, object? val1) : base(OpEnum.HideIfNot, prop1, OpCond.Eq, val1) { }
        public HideIfNotAttribute(string prop1, object? val1, string prop2, object? val2) : base(OpEnum.HideIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2) { }
        public HideIfNotAttribute(string prop1, object? val1, string prop2, object? val2, string prop3, object? val3) : base(OpEnum.HideIfNot, prop1, OpCond.Eq, val1, prop2, OpCond.Eq, val2, prop3, OpCond.Eq, val3) { }
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

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

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
        /// </remarks>
        public bool Disable { get; set; }
        /// <summary>
        /// Defines whether the property value is cleared when the property is disabled.
        /// The default is to clear the property value.
        /// </summary>
        public bool ClearOnDisable { get; set; }

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
        public static bool IsRequired(List<ExprAttribute> exprAttributes, object container, out bool found) {
            found = false;
            foreach (ExprAttribute e in exprAttributes) {
                if (e.IsRequiredAttribute) {
                    found = true;
                    if (e.IsRequiredValid(container))
                        return true;
                }
            }
            return false;
        }
        public static bool IsSelectionRequired(List<ExprAttribute> exprAttributes, object container, out bool found) {
            found = false;
            foreach (ExprAttribute e in exprAttributes) {
                if (e.IsSelectionRequiredAttribute) {
                    found = true;
                    if (e.IsSelectionRequiredValid(container))
                        return true;
                }
            }
            return false;
        }
        public static bool IsSuppressed(List<ExprAttribute> exprAttributes, object container, out bool found) {
            found = false;
            foreach (ExprAttribute e in exprAttributes) {
                if (e.IsSuppressAttribute) {
                    found = true;
                    if (e.IsSuppressedValid(container))
                        return true;
                }
            }
            return false;
        }

        protected bool IsRequiredValid(object? container) {
            if (container == null) return false;
            foreach (Expr expr in ExprList) {
                switch (Op) {
                    case OpEnum.RequiredIf:
                        if (!IsExprValid(expr, container)) return false;
                        break;
                    case OpEnum.RequiredIfNot:
                        if (IsExprValid(expr, container)) return false;
                        break;
                    case OpEnum.RequiredIfSupplied:
                        if (!IsExprSupplied(expr, container)) return false;
                        break;
                    case OpEnum.RequiredIfNotSupplied:
                        if (IsExprSupplied(expr, container)) return false;
                        break;
                    default:
                        throw new InternalError($"Unexpected Op value {Op}");
                }
            }
            return true;// plain Required attribute or all expressions match
        }
        protected bool IsSelectionRequiredValid(object? container) {
            if (container == null) return false;
            foreach (Expr expr in ExprList) {
                switch (Op) {
                    case OpEnum.SelectionRequiredIf:
                        if (!IsExprValid(expr, container)) return false;
                        break;
                    case OpEnum.SelectionRequiredIfNot:
                        if (IsExprValid(expr, container)) return false;
                        break;
                    case OpEnum.SelectionRequiredIfSupplied:
                        if (!IsExprSupplied(expr, container)) return false;
                        break;
                    case OpEnum.SelectionRequiredIfNotSupplied:
                        if (IsExprSupplied(expr, container)) return false;
                        break;
                    default:
                        throw new InternalError($"Unexpected Op value {Op}");
                }
            }
            return true;// plain SelectionRequired attribute or all expressions match
        }
        protected bool IsSuppressedValid(object? value) {
            if (value == null) return false;
            foreach (Expr expr in ExprList) {
                switch (Op) {
                    case OpEnum.SuppressIf:
                        if (!IsExprValid(expr, value)) return false;
                        break;
                    case OpEnum.SuppressIfNot:
                        if (IsExprValid(expr, value)) return false;
                        break;
                    case OpEnum.SuppressIfSupplied:
                        if (!IsExprSupplied(expr, value)) return false;
                        break;
                    case OpEnum.SuppressIfNotSupplied:
                        if (IsExprSupplied(expr, value)) return false;
                        break;
                    default:
                        throw new InternalError($"Unexpected Op value {Op}");
                }
            }
            return true;
        }

        //public override bool IsValid(object? value) {
        //    if (value == null) return false;
        //    foreach (Expr expr in ExprList) {
        //        switch (Op) {
        //            case OpEnum.RequiredIf:
        //            case OpEnum.SelectionRequiredIf:
        //            case OpEnum.SuppressIfNot:
        //                if (!IsExprValid(expr, value))
        //                    return true;
        //                break;
        //            case OpEnum.RequiredIfNot:
        //            case OpEnum.SelectionRequiredIfNot:
        //            case OpEnum.SuppressIf:
        //                if (IsExprValid(expr, value))
        //                    return true;
        //                break;
        //            case OpEnum.RequiredIfSupplied:
        //            case OpEnum.SelectionRequiredIfSupplied:
        //            case OpEnum.SuppressIfNotSupplied:
        //                if (!IsExprSupplied(expr, value))
        //                    return true;
        //                break;
        //            case OpEnum.RequiredIfNotSupplied:
        //            case OpEnum.SelectionRequiredIfNotSupplied:
        //            case OpEnum.SuppressIfSupplied:
        //                if (IsExprSupplied(expr, value))
        //                    return true;
        //                break;
        //            case OpEnum.ProcessIf:
        //            case OpEnum.ProcessIfNot:
        //            case OpEnum.ProcessIfSupplied:
        //            case OpEnum.ProcessIfNotSupplied:
        //            case OpEnum.HideIf:
        //            case OpEnum.HideIfNot:
        //            case OpEnum.HideIfSupplied:
        //            case OpEnum.HideIfNotSupplied:
        //                break;
        //            default:
        //                throw new InternalError($"Unexpected Op value {Op}");
        //        }
        //    }
        //    return false;
        //}

        protected override ValidationResult? IsValid(object? value, ValidationContext context) {
            //if (IsValid(context.ObjectInstance))
            //    return ValidationResult.Success;
            switch (Op) {
                case OpEnum.Required:
                case OpEnum.RequiredIf:
                case OpEnum.RequiredIfNot:
                case OpEnum.RequiredIfSupplied:
                case OpEnum.RequiredIfNotSupplied:
                    if (IsEmpty(value))
                        return new ValidationResult(__ResStr("requiredExpr", "The field '{0}' is required", context.DisplayName));
                    break;
                case OpEnum.SelectionRequired:
                case OpEnum.SelectionRequiredIf:
                case OpEnum.SelectionRequiredIfNot:
                case OpEnum.SelectionRequiredIfSupplied:
                case OpEnum.SelectionRequiredIfNotSupplied:
                    if (IsEmptyOrZero(value))
                        return new ValidationResult(__ResStr("requiredExpr", "The field '{0}' is required", context.DisplayName));
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

        public bool IsExprValid(Expr expr, object container) {
            object? leftVal = GetPropertyValue(container, expr.LeftProperty);
            object? rightVal;
            if (expr.IsRightProperty)
                rightVal = GetPropertyValue(container, expr.RightProperty);
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
        public bool IsExprSupplied(Expr expr, object container) {
            object? leftVal = GetPropertyValue(container, expr.LeftProperty);
            if (IsEmpty(leftVal))
                return false;
            return true;
        }
        protected object? GetPropertyValue(object container, string propName) {
            Type type = container.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, propName);
            return pi.GetValue(container, null);
        }
        private bool IsEmpty(object? value) {
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

            TypeConverter conv = TypeDescriptor.GetConverter(value.GetType());
            try {
                string? v = conv.ConvertToString(value);
                if (string.IsNullOrWhiteSpace(v))
                    return true;
            } catch (Exception) { }
            return false;
        }
        private bool IsEmptyOrZero(object? value) {
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

            TypeConverter conv = TypeDescriptor.GetConverter(value.GetType());
            string? v = conv.ConvertToString(value);
            if (string.IsNullOrWhiteSpace(v))
                return true;
            if (v == "0")
                return true;
            return false;
        }
        private bool IsEqual(object? val1, object? val2) {
            // null == null
            // allow null == ""
            if (val1 == null) {
                if (val2 == null)
                    return true;
                TypeConverter? conv = TypeDescriptor.GetConverter(val2.GetType());
                if (conv == null) return false;
                string? v = conv.ConvertToString(val2);
                if (v == null) return false;
                return v.Length == 0;
            } else if (val2 == null) {
                TypeConverter? conv = TypeDescriptor.GetConverter(val1.GetType());
                if (conv == null) return false;
                string? v = conv.ConvertToString(val1);
                if (v == null) return false;
                return v.Length == 0;
            }
            {
                TypeConverter? conv = TypeDescriptor.GetConverter(val1.GetType());
                if (conv == null) return false;
                string? v1 = conv.ConvertToString(val1);
                conv = TypeDescriptor.GetConverter(val2.GetType());
                if (conv == null) return false;
                string? v2 = conv.ConvertToString(val2);
                return v1 == v2;
            }
        }

        public ExprAttribute(OpEnum op) {
            Op = op;
            ExprList = new List<Expr>();
            ClearOnDisable = true;
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object? val1) {
            Op = op;
            ExprList = new List<Expr>();
            ExprList.Add(new Expr {
                LeftProperty = propLeft1, Cond = cond1, Value = val1
            });
            ClearOnDisable = true;
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object? val1, string propLeft2, OpCond cond2, object? val2) {
            Op = op;
            ExprList = new List<Expr>();
            ExprList.Add(new Expr {
                LeftProperty = propLeft1, Cond = cond1, Value = val1
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft2, Cond = cond2, Value = val2
            });
            ClearOnDisable = true;
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object? val1, string propLeft2, OpCond cond2, object? val2, string propLeft3, OpCond cond3, object? val3) {
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
            ClearOnDisable = true;
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object? val1, string propLeft2, OpCond cond2, object? val2, string propLeft3, OpCond cond3, object? val3,
                string propLeft4, OpCond cond4, object? val4) {
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
            ClearOnDisable = true;
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object? val1, string propLeft2, OpCond cond2, object? val2, string propLeft3, OpCond cond3, object? val3,
                string propLeft4, OpCond cond4, object? val4, string propLeft5, OpCond cond5, object? val5) {
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
            ClearOnDisable = true;
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object? val1, string propLeft2, OpCond cond2, object? val2, string propLeft3, OpCond cond3, object? val3,
                string propLeft4, OpCond cond4, object? val4, string propLeft5, OpCond cond5, object? val5, string propLeft6, OpCond cond6, object? val6) {
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
            ClearOnDisable = true;
        }

        public class Expr {
            [JsonIgnore]
            public string LeftProperty { get; set; } = null!;
            [JsonIgnore]
            public object? Value {
                get {
                    if (_Value == null) return null;
                    TypeConverter? conv = TypeDescriptor.GetConverter(_Value.GetType());
                    if (conv == null) throw new InternalError("No type converter");
                    string? v = conv.ConvertToString(_Value);
                    if (v != null && v.StartsWith(ValueOf))
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
                    TypeConverter? conv = TypeDescriptor.GetConverter(_Value.GetType());
                    if (conv == null) throw new InternalError("No type converter");
                    string? v = conv.ConvertToString(_Value);
                    if (v != null && v.StartsWith(ValueOf))
                        return true;
                    return false;
                }
            }
            [JsonIgnore]
            public string RightProperty {
                get {
                    if (_Value == null)
                        throw new InternalError("Property used when the attribute describes a value");
                    TypeConverter? conv = TypeDescriptor.GetConverter(_Value.GetType());
                    if (conv == null) throw new InternalError("No type converter");
                    string? v = conv.ConvertToString(_Value);
                    if (v == null || !v.StartsWith(ValueOf))
                        throw new InternalError("Property used when the attribute describes a value");
                    return v.Substring(ValueOf.Length);
                }
            }
            [JsonIgnore]
            public object? _Value { get; set; }
            //serialized for client-side
            public OpCond Cond { get; set; }
            public string _Left { get { return AttributeHelper.GetDependentPropertyName(LeftProperty); } }
            public string _Right { get { return IsRightProperty ? AttributeHelper.GetDependentPropertyName(RightProperty) : string.Empty; } }
            public string? _RightVal {
                get {
                    // normalize to string
                    if (_Value == null || IsRightProperty) return null;
                    Type valType = _Value.GetType();
                    if (valType == typeof(bool))
                        return (bool)_Value ? "true" : "false";
                    if (valType.IsEnum)
                        return ((int)_Value).ToString();
                    TypeConverter conv = TypeDescriptor.GetConverter(_Value.GetType());
                    return conv.ConvertToString(_Value);
                }
            }
        }

        public class ValidationRequiredExpr : ValidationBase {
            public OpEnum Op { get; set; }
            public string Expr { get; set; } = null!;
        }
        public ValidationBase? AddValidation(object container, PropertyData propData, string caption) {
            switch (Op) {
                default:
                    throw new InternalError($"Invalid Op {Op} in {nameof(AddValidation)}");
                case OpEnum.Required:
                case OpEnum.RequiredIf:
                case OpEnum.RequiredIfNot:
                case OpEnum.RequiredIfSupplied:
                case OpEnum.RequiredIfNotSupplied:
                    return new ValidationRequiredExpr {
                        Method = nameof(ExprAttribute),
                        Message = __ResStr("requiredExpr", "The field '{0}' is required", caption),
                        Op = Op,
                        Expr = Utility.JsonSerialize(ExprList),
                    };
                case OpEnum.SelectionRequired:
                case OpEnum.SelectionRequiredIf:
                case OpEnum.SelectionRequiredIfNot:
                case OpEnum.SelectionRequiredIfSupplied:
                case OpEnum.SelectionRequiredIfNotSupplied:
                    return new ValidationRequiredExpr {
                        Method = nameof(ExprAttribute),
                        Message = __ResStr("requiredSelExpr", "The field '{0}' requires a selection", caption),
                        Op = Op,
                        Expr = Utility.JsonSerialize(ExprList),
                    };
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
            return null;
        }
    }
}
