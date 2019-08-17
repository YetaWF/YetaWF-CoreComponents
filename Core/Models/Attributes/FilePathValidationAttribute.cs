/* Copyright © 2019 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Localize;
using System.IO;

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FilePathValidationAttribute : ValidationAttribute {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(FilePathValidationAttribute), name, defaultValue, parms); }

        public FilePathValidationAttribute() : base() { }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext) {

            string filePath = (string)value;
            if (string.IsNullOrWhiteSpace(filePath))
                return ValidationResult.Success;

            if (!File.Exists(filePath))
                return new ValidationResult(__ResStr("notFound", "The file {0} does not exist", filePath));

            return ValidationResult.Success;
        }
    }
}
