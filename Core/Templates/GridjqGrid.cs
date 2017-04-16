/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class Grid : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;
            string areaName = "Grid";

            scripts.AddConfigOption(areaName, "allRecords", GridDefinition.MaxPages);

            scripts.AddLocalization(areaName, "allRecords", this.__ResStr("allRecords", "All"));

            scripts.AddLocalization(areaName, "recordtext", this.__ResStr("recordtext", "{0} - {1} of {2} items"));//{0} is the index of the first record on the page, {1} - index of the last record on the page, {2} is the total amount of records
            scripts.AddLocalization(areaName, "emptyrecords", this.__ResStr("emptyrecords", "No items to display"));
            scripts.AddLocalization(areaName, "loadtext", this.__ResStr("loadtext", "Loading..."));
            scripts.AddLocalization(areaName, "pgtext", this.__ResStr("pgtext", "Page {0} of {1}"));//{0} is total amount of pages
            scripts.AddLocalization(areaName, "pgsearchTB", this.__ResStr("pgsearchTB", "Show/hide search toolbar"));
            scripts.AddLocalization(areaName, "pgfirst", this.__ResStr("pgfirst", "Display first page"));
            scripts.AddLocalization(areaName, "pglast", this.__ResStr("pglast", "Display last page"));
            scripts.AddLocalization(areaName, "pgnext", this.__ResStr("pgnext", "Display next page"));
            scripts.AddLocalization(areaName, "pgprev", this.__ResStr("pgprev", "Display previous page"));
            scripts.AddLocalization(areaName, "pgrecs", this.__ResStr("pgrecs", "Select number of items shown per page"));

            scripts.AddLocalization(areaName, "eq", this.__ResStr("eq", "Equal"));
            scripts.AddLocalization(areaName, "ne", this.__ResStr("ne", "Not equal"));
            scripts.AddLocalization(areaName, "lt", this.__ResStr("lt", "Less"));
            scripts.AddLocalization(areaName, "le", this.__ResStr("le", "Less or equal"));
            scripts.AddLocalization(areaName, "gt", this.__ResStr("gt", "Greater"));
            scripts.AddLocalization(areaName, "ge", this.__ResStr("ge", "Greater or equal"));
            scripts.AddLocalization(areaName, "bw", this.__ResStr("bw", "Begins with"));
            scripts.AddLocalization(areaName, "bn", this.__ResStr("bn", "Does not begin with"));
            scripts.AddLocalization(areaName, "inx", this.__ResStr("in", "Is in"));
            scripts.AddLocalization(areaName, "ni", this.__ResStr("ni", "Is not in"));
            scripts.AddLocalization(areaName, "ew", this.__ResStr("ew", "Ends with"));
            scripts.AddLocalization(areaName, "en", this.__ResStr("en", "Does not end with"));
            scripts.AddLocalization(areaName, "cn", this.__ResStr("cn", "Contains"));
            scripts.AddLocalization(areaName, "nc", this.__ResStr("nc", "Does not contain"));
            scripts.AddLocalization(areaName, "nu", this.__ResStr("nu", "Is null"));
            scripts.AddLocalization(areaName, "nn", this.__ResStr("nn", "Is not null"));
        }
    }
}

