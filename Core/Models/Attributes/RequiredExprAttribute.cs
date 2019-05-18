/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;
using static YetaWF.Core.Models.Attributes.ExprAttribute;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfAttribute : RequiredIfExprAttribute {
        public RequiredIfAttribute(String propLeft1, object val1) : base(propLeft1, OpCond.Eq, val1) { }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RequiredIfNotAttribute : RequiredIfNotExprAttribute {
        public RequiredIfNotAttribute(String propLeft1, object val1) : base(propLeft1, OpCond.Eq, val1) { }
    }

    public class RequiredIfExprAttribute : RequiredExprAttribute {
        public RequiredIfExprAttribute(string propLeft1, OpCond cond1, object val1) :
            base(OpEnum.RequiredIf, propLeft1, cond1, val1) { }
        public RequiredIfExprAttribute(string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2) :
            base(OpEnum.RequiredIf, propLeft1, cond1, val1, propLeft2, cond2, val2) { }
        public RequiredIfExprAttribute(string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2, string propLeft3, OpCond cond3, object val3) :
            base(OpEnum.RequiredIf, propLeft1, cond1, val1, propLeft2, cond2, val2, propLeft3, cond3, val3) { }
    }
    public class RequiredIfNotExprAttribute : RequiredExprAttribute {
        public RequiredIfNotExprAttribute(string propLeft1, OpCond cond1, object val1) :
            base(OpEnum.RequiredIfNot, propLeft1, cond1, val1) { }
        public RequiredIfNotExprAttribute(string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2) :
            base(OpEnum.RequiredIfNot, propLeft1, cond1, val1, propLeft2, cond2, val2) { }
        public RequiredIfNotExprAttribute(string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2, string propLeft3, OpCond cond3, object val3) :
            base(OpEnum.RequiredIfNot, propLeft1, cond1, val1, propLeft2, cond2, val2, propLeft3, cond3, val3) { }
    }

    public class RequiredExprAttribute : ExprAttribute {
        public RequiredExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1) :
            base(op, propLeft1, cond1, val1) { }
        public RequiredExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2) :
            base(op, propLeft1, cond1, val1, propLeft2, cond2, val2) { }
        public RequiredExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2, string propLeft3, OpCond cond3, object val3) :
            base(op, propLeft1, cond1, val1, propLeft2, cond2, val2, propLeft3, cond3, val3) { }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ExprAttribute : ValidationAttribute, YIClientValidation {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public const string ValueOf = "ValOf+";

        public enum OpEnum {
            RequiredIf,
            RequiredIfNot,
            RequiredIfSupplied,
            RequiredIfNotSupplied,
            ProcessIf,
            ProcessIfNot,
            ProcessIfSupplied,
            ProcessIfNotSupplied,
            SuppressIf,
            SuppressIfNot,
            SuppressIfSupplied,
            SuppressIfNotSupplied,
            HideIfNotSupplied,
        };

        public enum OpCond {
            Eq,
            NotEq,
        }

        protected OpEnum Op;
        protected List<Expr> ExprList = new List<Expr>();

        protected override ValidationResult IsValid(object value, ValidationContext context) {
            foreach (Expr expr in ExprList) {
                switch (Op) {
                    case OpEnum.RequiredIf:
                        if (!IsExprValid(expr, context.ObjectInstance))
                            return ValidationResult.Success;
                        break;
                    case OpEnum.RequiredIfNot:
                        if (IsExprValid(expr, context.ObjectInstance))
                            return ValidationResult.Success;
                        break;
                    default:
                        throw new InternalError("Unexpected Op value {op}");
                }
            }
            return new ValidationResult(__ResStr("requiredExpr", "The '{0}' field is required", AttributeHelper.GetPropertyCaption(context)));
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
                    if (leftVal == null || rightVal == null)
                        return leftVal == rightVal;
                    return leftVal.Equals(rightVal);
                case OpCond.NotEq:
                    if (leftVal == null || rightVal == null)
                        return leftVal != rightVal;
                    return !leftVal.Equals(rightVal);
                default:
                    throw new InternalError($"Unexpected condition {expr.Cond}");
            }
            //$$ if (propType.IsEnum)
            //    return (int)propVal == (int)reqVal;
            //else
            //    return propVal.Equals(reqVal);
        }
        protected object GetPropertyValue(object model, string propName) {
            Type type = model.GetType();
            PropertyInfo pi = ObjectSupport.GetProperty(type, propName);
            return pi.GetValue(model, null);
        }


        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1) {
            Op = op;
            ExprList.Add(new Expr {
                LeftProperty = propLeft1, Cond = cond1, Value = val1
            });
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2) {
            Op = op;
            ExprList.Add(new Expr {
                LeftProperty = propLeft1, Cond = cond1, Value = val1
            });
            ExprList.Add(new Expr {
                LeftProperty = propLeft2, Cond = cond2, Value = val2
            });
        }
        public ExprAttribute(OpEnum op, string propLeft1, OpCond cond1, object val1, string propLeft2, OpCond cond2, object val2, string propLeft3, OpCond cond3, object val3) {
            Op = op;
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
            string msg = __ResStr("requiredExpr", "The '{0}' field is required", propData.GetCaption(container));
            tag.MergeAttribute("data-val-requiredexpr", msg);
            tag.MergeAttribute("data-val-requiredexpr-op", ((int)Op).ToString());
            tag.MergeAttribute("data-val-requiredexpr-json", YetaWFManager.JsonSerialize(ExprList));
            tag.MergeAttribute("data-val", "true");
        }
    }

    public class TestMe {//$$$$$
        public int Prop1 { get; set; }
        // Prop1 == 13 || Prop1 == Prop2
        [RequiredIfExpr(nameof(Prop1), OpCond.Eq, 13), RequiredIfExpr(nameof(Prop1), OpCond.Eq, ValueOf + nameof(Prop2))]
        public int Prop2 { get; set; }
        // Prop1 == 13 && Prop1 == Prop2
        [RequiredIfExpr(nameof(Prop1), OpCond.Eq, 13, nameof(Prop1), OpCond.Eq, ValueOf + nameof(Prop2))]
        public int Prop3 { get; set; }
        public int Prop4 { get; set; }
        public int Prop5 { get; set; }
    }
}
