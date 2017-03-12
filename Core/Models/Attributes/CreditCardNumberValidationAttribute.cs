/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
#if MVC6
using Microsoft.AspNetCore.Mvc;
#else
using System.Web.Mvc;
#endif
using YetaWF.Core.Localize;

namespace YetaWF.Core.Models.Attributes {

    // Helper info
    // http://www.codeproject.com/Articles/20271/Ultimate-NET-Credit-Card-Utility-Class
    // http://www.codeproject.com/Tips/515367/Validate-credit-card-number-with-Mod-algorithm


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class CreditCardNumberValidationAttribute : DataTypeAttribute /*this stopped working after an upgrade of jQuery validation, YIClientValidatable*/ {

        [CombinedResources]
        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Resources), name, defaultValue, parms); }

        public CreditCardNumberValidationAttribute()
            : base(DataType.CreditCard) {
            ErrorMessage = __ResStr("valCCNum", "The credit card number is invalid");
        }

        private static Regex _regex = new Regex(@"^\s*([1-9][0-9]{11,15})\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        public override bool IsValid(object value) {
            string valueAsString = value as string;
            if (string.IsNullOrWhiteSpace(valueAsString)) return true;
            return IsValidNumber(valueAsString);
        }
        public static bool IsValidNumber(string cardNum) {
            cardNum = cardNum.Trim();
            CreditCardTypeType cardType = GetCardTypeFromNumber(cardNum);
            if (IsValidNumber(cardNum, cardType))
                return true;
            else
                return false;
        }
        public static bool IsValidNumber(string cardNum, CreditCardTypeType cardType) {
            if (cardType == CreditCardTypeType.Invalid)
                return false;
            cardNum = cardNum.Trim();
            if (_regex.Match(cardNum).Length > 0)
                return PassesLuhnTest(cardNum);
            else
                return false;
        }
#if MVC6
#else
        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {
            ErrorMessage = __ResStr("valCCNum", "The credit card number is invalid");
            yield return new ModelClientValidationRule {
                ErrorMessage = ErrorMessage,
                ValidationType = "creditcard",
            };
        }
#endif

        // very basic card type determination (close enough)
        public static CreditCardTypeType GetCardTypeFromNumber(string cardNum) {
            if (cardNum.StartsWith("3")) return CreditCardTypeType.Amex;
            else if (cardNum.StartsWith("4")) return CreditCardTypeType.Visa;
            else if (cardNum.StartsWith("5")) return CreditCardTypeType.MasterCard;
            else if (cardNum.StartsWith("6")) return CreditCardTypeType.Discover;
            else
                return CreditCardTypeType.Invalid;
        }

        public static bool PassesLuhnTest(string cardNumber) {
            //Clean the card number- remove dashes and spaces
            cardNumber = cardNumber.Replace("-", "").Replace(" ", "");

            //Convert card number into digits array
            int[] digits = new int[cardNumber.Length];
            for (int len = 0 ; len < cardNumber.Length ; len++) {
                digits[len] = Int32.Parse(cardNumber.Substring(len, 1));
            }

            //Luhn Algorithm
            //Adapted from code availabe on Wikipedia at
            //http://en.wikipedia.org/wiki/Luhn_algorithm
            int sum = 0;
            bool alt = false;
            for (int i = digits.Length - 1 ; i >= 0 ; i--) {
                int curDigit = digits[i];
                if (alt) {
                    curDigit *= 2;
                    if (curDigit > 9) {
                        curDigit -= 9;
                    }
                }
                sum += curDigit;
                alt = !alt;
            }

            //If Mod 10 equals 0, the number is good and this will return true
            return sum % 10 == 0;
        }
    }

    /// <summary>
    /// CreditCardTypeType copied for PayPal WebPayment Pro API
    /// (If you use the PayPal API, you do not need this definition)
    /// </summary>
    public enum CreditCardTypeType {
        Invalid  = -1,
        Visa,
        MasterCard,
        Discover,
        Amex,
        Switch,
        Solo
    }

}