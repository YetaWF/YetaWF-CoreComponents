/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Models.Attributes {

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TimeSpanRangeAttribute : ValidationAttribute {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public TimeSpanRangeAttribute(string min, string max) {
            TimeSpan ts;
            if (!TimeSpan.TryParse(min, out ts))
                throw new InternalError($"Invalid minimum timespan value");
            Min = ts;
            if (!TimeSpan.TryParse(max, out ts))
                throw new InternalError($"Invalid maximum timespan value");
            Max = ts;
        }

        private TimeSpan Min { get; set; }
        private TimeSpan Max { get; set; }

        protected override ValidationResult IsValid(object value, ValidationContext context) {
            if (value == null) return ValidationResult.Success;
            TimeSpan dt = (TimeSpan)value;
            if (Min != null) {
                if (dt < Min) return new ValidationResult(__ResStr("badTimeSpan", "The valid range is {0} through {1}", Formatting.FormatTimeSpan(Min), Formatting.FormatTimeSpan(Max)));
            }
            if (Max != null) {
                if (dt > Max) return new ValidationResult(__ResStr("badTimeSpan", "The valid range is {0} through {1}", Formatting.FormatTimeSpan(Min), Formatting.FormatTimeSpan(Max)));
            }
            return ValidationResult.Success;
        }
    }
}
