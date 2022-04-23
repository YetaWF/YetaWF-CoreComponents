/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using PhoneNumbers;
using System;
using System.ComponentModel.DataAnnotations;
using YetaWF.Core.Components;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    /// <summary>
    /// Validates any phone number, national or international.
    /// </summary>
    /// <remarks>
    /// It uses the country defined in site settings (Site Settings, Site, Country property) as the current (national) country
    /// and allows entry of national numbers without country code. For all other international numbers the complete number as dialed is required or a "+" and the country code followed by the number.
    ///
    /// Examples of valid numbers (assuming US is the current country):
    /// (407) 555-1212
    /// 4075551212
    /// +14075551212 (includes country code)
    /// 011 41 44 833 nn nn  (as dialed)
    /// +41 44 833 nn nn   (country code)
    /// +4144833nnnn  (country code)
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class PhoneNumberValidationAttribute : ValidationAttribute {

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(PhoneNumberValidationAttribute), name, defaultValue, parms); }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PhoneNumberValidationAttribute() { }

        protected override ValidationResult? IsValid(object? value, ValidationContext context) {
            string? number = (string?)value;
            if (!string.IsNullOrWhiteSpace(number)) {
                if (!PhoneNumberValidationAttribute.Valid(number))
                    return new ValidationResult(__ResStr("inv", "{0} is an invalid phone number", number));
            }
            return ValidationResult.Success;
        }

        /// <summary>
        /// Returns whether a phone number is a valid (dialable) phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to validate.</param>
        /// <returns>Returns true if the phone number is valid, false otherwise.</returns>
        public static bool Valid(string phoneNumber) {
            PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();
            try {
                PhoneNumber p = phoneNumberUtil.Parse(phoneNumber, CountryCode);
                if (phoneNumberUtil.IsValidNumber(p))
                    return true;
            } catch (Exception) { }
            return false;
        }

        /// <summary>
        /// Returns a phone number in E164 ISO format.
        /// </summary>
        /// <param name="phoneNumber">The phone number to format in E164 ISO format.</param>
        /// <returns>The phone number formatted in E164 ISO format. null is returned if the phone number is invalid.</returns>
        public static string? GetE164(string? phoneNumber) {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;
            PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();
            try {
                PhoneNumber p = phoneNumberUtil.Parse(phoneNumber, CountryCode);
                if (!phoneNumberUtil.IsValidNumber(p))
                    return null;
                return phoneNumberUtil.Format(p, PhoneNumberFormat.E164);
            } catch (Exception) {
                return null;
            }
        }
        /// <summary>
        /// Returns a formatted user-displayable phone number (including spaces, parentheses, etc.)
        /// </summary>
        /// <param name="phoneNumber">The phone number to format.</param>
        /// <returns>Returns a formatted user-displayable phone number.</returns>
        /// <remarks>National numbers are formatted without country codes. International numbers include their country code.</remarks>
        public static string? GetDisplay(string? phoneNumber) {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;
            PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();
            try {
                PhoneNumber p = phoneNumberUtil.Parse(phoneNumber, CountryCode);
                if (!phoneNumberUtil.IsValidNumber(p))
                    return null;
                if (phoneNumberUtil.IsValidNumberForRegion(p, CountryCode))
                    return phoneNumberUtil.Format(p, PhoneNumberFormat.NATIONAL);
                return phoneNumberUtil.Format(p, PhoneNumberFormat.INTERNATIONAL);
            } catch (Exception) {
                return null;
            }
        }
        private static string CountryCode {
            get {
                return CountryISO3166.CountryToId(null, AllowMismatch: true);
            }
        }
    }
}
