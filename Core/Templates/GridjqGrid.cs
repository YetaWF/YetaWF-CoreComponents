/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using YetaWF.Core.Localize;
using YetaWF.Core.Models;
using YetaWF.Core.Pages;
using YetaWF.Core.Support;

namespace YetaWF.Core.Addons.Templates {
    public class GridjqGrid : IAddOnSupport {

        public void AddSupport(YetaWFManager manager) {

            ScriptManager scripts = manager.ScriptManager;
            string areaName = "GridjqGrid";

            manager.ScriptManager.AddConfigOption(areaName, "allRecords", GridDefinition.MaxPages);

            manager.ScriptManager.AddLocalization(areaName, "allRecords", this.__ResStr("allRecords", "All"));

            manager.ScriptManager.AddLocalization(areaName, "recordtext", this.__ResStr("recordtext", "{0} - {1} of {2} items"));//{0} is the index of the first record on the page, {1} - index of the last record on the page, {2} is the total amount of records
            manager.ScriptManager.AddLocalization(areaName, "emptyrecords", this.__ResStr("emptyrecords", "No items to display"));
            manager.ScriptManager.AddLocalization(areaName, "loadtext", this.__ResStr("loadtext", "Loading..."));
            manager.ScriptManager.AddLocalization(areaName, "pgtext", this.__ResStr("pgtext", "Page {0} of {1}"));//{0} is total amount of pages
            manager.ScriptManager.AddLocalization(areaName, "pgsearchTB", this.__ResStr("pgsearchTB", "Show/hide search toolbar"));
            manager.ScriptManager.AddLocalization(areaName, "pgfirst", this.__ResStr("pgfirst", "Display first page"));
            manager.ScriptManager.AddLocalization(areaName, "pglast", this.__ResStr("pglast", "Display last page"));
            manager.ScriptManager.AddLocalization(areaName, "pgnext", this.__ResStr("pgnext", "Display next page"));
            manager.ScriptManager.AddLocalization(areaName, "pgprev", this.__ResStr("pgprev", "Display previous page"));
            manager.ScriptManager.AddLocalization(areaName, "pgrecs", this.__ResStr("pgrecs", "Select number of items shown per page"));

            manager.ScriptManager.AddLocalization(areaName, "eq", this.__ResStr("eq", "Equal"));
            manager.ScriptManager.AddLocalization(areaName, "ne", this.__ResStr("ne", "Not equal"));
            manager.ScriptManager.AddLocalization(areaName, "lt", this.__ResStr("lt", "Less"));
            manager.ScriptManager.AddLocalization(areaName, "le", this.__ResStr("le", "Less or equal"));
            manager.ScriptManager.AddLocalization(areaName, "gt", this.__ResStr("gt", "Greater"));
            manager.ScriptManager.AddLocalization(areaName, "ge", this.__ResStr("ge", "Greater or equal"));
            manager.ScriptManager.AddLocalization(areaName, "bw", this.__ResStr("bw", "Begins with"));
            manager.ScriptManager.AddLocalization(areaName, "bn", this.__ResStr("bn", "Does not begin with"));
            manager.ScriptManager.AddLocalization(areaName, "inx", this.__ResStr("in", "Is in"));
            manager.ScriptManager.AddLocalization(areaName, "ni", this.__ResStr("ni", "Is not in"));
            manager.ScriptManager.AddLocalization(areaName, "ew", this.__ResStr("ew", "Ends with"));
            manager.ScriptManager.AddLocalization(areaName, "en", this.__ResStr("en", "Does not end with"));
            manager.ScriptManager.AddLocalization(areaName, "cn", this.__ResStr("cn", "Contains"));
            manager.ScriptManager.AddLocalization(areaName, "nc", this.__ResStr("nc", "Does not contain"));
            manager.ScriptManager.AddLocalization(areaName, "nu", this.__ResStr("nu", "Is null"));
            manager.ScriptManager.AddLocalization(areaName, "nn", this.__ResStr("nn", "Is not null"));
        }
    }
}

