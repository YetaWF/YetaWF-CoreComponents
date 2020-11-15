/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Localize;
using System.IO;

namespace YetaWF.Core.Models.Attributes {

    // VALIDATION
    // VALIDATION
    // VALIDATION

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FolderPathValidationAttribute : ValidationAttribute {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(FolderPathValidationAttribute), name, defaultValue, parms); }

        public FolderPathValidationAttribute() : base() { }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) {

            string? folderPath = (string?)value;
            if (string.IsNullOrWhiteSpace(folderPath))
                return ValidationResult.Success;

            if (!Directory.Exists(folderPath))
                return new ValidationResult(__ResStr("notFound", "The folder {0} does not exist", folderPath));

            return ValidationResult.Success;
        }
    }
}
