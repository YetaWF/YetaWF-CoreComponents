/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class MinimumDateAttribute : DataTypeAttribute {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public DateTime MinDate { get; set; }

        public MinimumDateAttribute(int year, int month, int day) : base(DataType.Date) {
            MinDate = new DateTime(year, month, day);
            ErrorMessage = __ResStr("valDateMin", "The date is invalid - it's before the allowed start date of {0}", Formatting.FormatDate(MinDate));
        }
        public override bool IsValid(object value) {
            if (value == null) return true;
            DateTime dt = (DateTime) value;
            return dt >= MinDate;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class MaximumDateAttribute : DataTypeAttribute {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public DateTime MaxDate { get; set; }

        public MaximumDateAttribute(int year, int month, int day)
            : base(DataType.Date) {
            MaxDate = new DateTime(year, month, day);
            ErrorMessage = __ResStr("valDateMax", "The date is invalid - it's after the allowed end date of {0}", Formatting.FormatDate(MaxDate));
        }
        public override bool IsValid(object value) {
            if (value == null) return true;
            DateTime dt = (DateTime) value;
            return dt <= MaxDate;
        }
    }
}
