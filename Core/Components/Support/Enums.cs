/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using YetaWF.Core.Localize;
using YetaWF.Core.Models;

namespace YetaWF.Core.Components {

    public class Enums {

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Enums), name, defaultValue, parms); }

        /// <summary>
        /// Given an enum type, returns a collection suitable for use with a DropDownList component.
        /// </summary>
        /// <param name="enumType">The type of the enum.</param>
        /// <param name="showSelect">Defines whether the first entry "(select)" should be generated.</param>
        /// <returns>Returns a collection suitable for use in a DropDownList component.</returns>
        public static List<SelectionItem<int>> GetEnumSelectionList(Type enumType, bool showSelect = false) {
            List<SelectionItem<int>> list = new List<SelectionItem<int>>();

            EnumData enumData = ObjectSupport.GetEnumData(enumType);
            bool showValues = UserSettings.GetProperty<bool>("ShowEnumValue");

            if (showSelect) {
                list.Add(new SelectionItem<int> {
                    Text = __ResStr("enumSelect", "(select)"),
                    Value = 0,
                    Tooltip = __ResStr("enumPlsSelect", "Please select one of the available options"),
                });
            }
            foreach (EnumDataEntry entry in enumData.Entries) {

                int enumVal = Convert.ToInt32(entry.Value);
                if (enumVal == 0 && showSelect) continue;

                string caption = entry.Caption;
                if (showValues)
                    caption = __ResStr("enumFmt", "{0} - {1}", enumVal, caption);

                list.Add(new SelectionItem<int> {
                    Text = caption,
                    Value = enumVal,
                    Tooltip = entry.Description,
                });
            }
            return list;
        }
    }
}
