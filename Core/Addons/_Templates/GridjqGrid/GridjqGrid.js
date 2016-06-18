/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_GridjqGrid = {};

YetaWF_GridjqGrid._dataBoundEnabled = 1;

jQuery.extend(jQuery.jgrid.defaults, {
    autowidth: true,
    hoverrows: false,
    sortable: false, // no column reordering
    height: '100%',
    // Localization message for pager
    // http://www.trirand.com/jqgridwiki/doku.php?id=wiki:pager
    recordtext: YLocs.GridjqGrid.recordtext,
    emptyrecords: YLocs.GridjqGrid.emptyrecords,
    loadtext: YLocs.GridjqGrid.loadtext,
    pgtext: YLocs.GridjqGrid.pgtext,
    pgfirst: YLocs.GridjqGrid.pgfirst,
    pglast: YLocs.GridjqGrid.pglast,
    pgnext: YLocs.GridjqGrid.pgnext,
    pgprev: YLocs.GridjqGrid.pgprev,
    pgrecs: YLocs.GridjqGrid.pgrecs,
    //showhide: "Toggle Expand Collapse Grid", // not used
    //savetext: "Saving...", // not used
});
jQuery.extend(jQuery.jgrid.search, {
    // caption: "Search...",// not used
    // Find: "Find",// not used
    // Reset: "Reset",// not used
    odata: [
        { oper: "eq", text: YLocs.GridjqGrid.eq },
        { oper: "ne", text: YLocs.GridjqGrid.ne },
        { oper: "lt", text: YLocs.GridjqGrid.lt },
        { oper: "le", text: YLocs.GridjqGrid.le },
        { oper: "gt", text: YLocs.GridjqGrid.gt },
        { oper: "ge", text: YLocs.GridjqGrid.ge },
        { oper: "bw", text: YLocs.GridjqGrid.bw },
        { oper: "bn", text: YLocs.GridjqGrid.bn },
        { oper: "in", text: YLocs.GridjqGrid.inx },
        { oper: "ni", text: YLocs.GridjqGrid.ni },
        { oper: "ew", text: YLocs.GridjqGrid.ew },
        { oper: "en", text: YLocs.GridjqGrid.en },
        { oper: "cn", text: YLocs.GridjqGrid.cn },
        { oper: "nc", text: YLocs.GridjqGrid.nc },
        { oper: "nu", text: YLocs.GridjqGrid.nu },
        { oper: "nn", text: YLocs.GridjqGrid.nn }
    ],
    //groupOps: [ // not used
    //    { op: "AND", text: "all" },
    //    { op: "OR", text: "any" }
    //],
    //addGroupTitle: "Add subgroup",// not used
    //deleteGroupTitle: "Delete group",// not used
    //addRuleTitle: "Add rule",// not used
    //deleteRuleTitle: "Delete rule",// not used
    operandTitle: "Select search operation",
    resetTitle: "Reset search value"
});
jQuery.extend(jQuery.jgrid.nav, {
    edit: false,
    add: false,
    del: false,
    view: false,
    search: false,
    refreshstate: 'current',
    refresh: false,
});
// modify url post data to retrieve data
YetaWF_GridjqGrid.modifySend = function ($grid, settingsModuleGuid, options, xhr, settings) {
    'use strict';

    var uri = new URI("?" + settings.data);
    var data = uri.search(true);
    var newData = { SettingsModuleGuid: settingsModuleGuid }
    var rows = parseInt(data.rows);
    options['requestedRecords'] = rows; // save this so we can calculate the # of pages when receiving data
    options['startingRecord'] = (parseInt(data.page) - 1) * rows; // save this so we can calculate the starting page
    newData['skip'] = options.startingRecord;
    newData['take'] = rows;
    if (data.sidx != null && data.sidx != "") {
        newData['sort[0].Field'] = data.sidx;
        newData['sort[0].Order'] = (data.sord == "asc" ? 0 : 1);
    }
    if (data._search == "true") {
        var filters;
        eval("filters = " + data.filters + ";");
        if (filters.groupOp != "AND") throw "Unexpected filters option"/*DEBUG*/
        var rules = filters.rules;
        for (var i = 0 ; i < rules.length; i++) {
            var rule = rules[i]
            newData['filters[{0}].Field'.format(i)] = rule.field
            newData['filters[{0}].Value'.format(i)] = rule.data;
            var newOp;
            switch (rule.op) {
                case 'eq': newOp = "=="; break
                case 'ne': newOp = "!="; break
                case 'lt': newOp = "<"; break
                case 'le': newOp = "<="; break
                case 'gt': newOp = ">"; break
                case 'ge': newOp = ">="; break
                case 'bw': newOp = "startswith"; break
                case 'bn': newOp = "notstartswith"; break
                //not support case 'in': newOp = "is in"; break
                //Not supported  case 'ni': newOp = "is not in"; break
                case 'ew': newOp = "endswith"; break
                case 'en': newOp = "notendswith"; break
                case 'cn': newOp = "contains"; break
                case 'nc': newOp = "notcontains"; break
                default:
                    throw "Unexpected operator {0}".format(rule.op)
            }
            newData['filters[{0}].Operator'.format(i)] = newOp
        }
    }

    var info = YetaWF_Forms.getFormInfo($grid);
    newData[YConfigs.Basics.ModuleGuid] = info.ModuleGuid;
    newData[YConfigs.Forms.RequestVerificationToken] = info.RequestVerificationToken;
    newData[YConfigs.Forms.UniqueIdPrefix] = info.UniqueIdPrefix;

    // add any extra data if available
    var extradata = $grid.attr('data-extraproperty');
    if (extradata != undefined) {
        eval("extradata = " + extradata + ";");
        $.extend(newData, extradata);
    }

    uri.search(newData);
    settings.data = uri.query();
    return true;
};

// modify received data to add page information
YetaWF_GridjqGrid.modifyReceive = function ($grid, options, data, status, xhr) {
    'use strict';
    var totalRecs = data.records;
    var startingRec = options.startingRecord;
    if (startingRec >= totalRecs) startingRec = totalRecs - 1;
    if (startingRec < 0) startingRec = 0;
    data['total'] = Math.ceil(totalRecs / options.requestedRecords);// total pages
    data['page'] = Math.floor(startingRec / options.requestedRecords) + 1;
};
// some error occurred during ajax
YetaWF_GridjqGrid.loadError = function ($grid, xhr, status, error) {
    YetaWF_Basics.processAjaxReturn(xhr.responseText, status, xhr);
}

// save column widths after user resizes
YetaWF_GridjqGrid.SaveSettingsColumnWidths = function ($grid, url, settingsGuid, options, newwidth, index) {
    'use strict';
    if (!url || !settingsGuid) return;
    // save new width in options
    options.colModel[index].width = newwidth;
    // save all relevant settings via ajax call
    var data = {};
    data["SettingsModuleGuid"] = settingsGuid;
    for (var i = 0; i < options.colModel.length; i++) {
        var field = options.colModel[i].index;
        data["Columns[" + field + "]"] = options.colModel[i].width;
    }
    // transmit all column info - save it if we can - we don't really care if this fails
    $.ajax({
        url: url,
        type: 'POST',
        data: data,
        cache: false,
        success: function (result, textStatus, jqXHR) {
            //Y_Error(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
            return false;
        },
        error: function (jqXHR, textStatus, errorThrown) {
            //Y_Error(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
            debugger;/*DEBUG*/
            return false;
        }
    });
};

YetaWF_GridjqGrid.HandleInputUpdates = function ($grid, saveInDataSource) {
    'use strict';
    // handle input in grids on forms (save any new data in data source)
    // find the input field (name) and save the cell's html contents in the data source
    // we're using that html later to post to the data to the form
    $grid.on("change", 'input,select', function (e) {
        var $ctrl = $(e.target);
        // get the grid row
        var $row = $ctrl.closest("tr");
        if ($row == null) return;
        // HACK - fix html() which doesn't reflect the new input value
        if ($ctrl[0].tagName == "INPUT" && $ctrl.attr('type') == "checkbox") {
            val = $ctrl.is(':checked');
            $ctrl.attr('value', val);
            if (val) $ctrl.attr('checked', 'checked'); else $ctrl.removeAttr('checked');
            //var $next = $ctrl.next("input[type='hidden']");
            //$next.val(val);
            //TODO:} else {
            //TODO: radiobutton?
        } else if ($ctrl[0].tagName == "SELECT") {
            var index = $ctrl[0].selectedIndex;
            $('option', $ctrl).removeAttr('selected');
            $('option', $ctrl).eq(index).attr('selected', 'selected');
        } else {
            var val = $ctrl.val();
            $ctrl.attr('value', val);
        }
        // get the cell containing the input field
        var $cell = $(e.target).closest("td");
        if ($cell == null) throw "No cell found";/*DEBUG*/
        // save new html in datasource
        if (saveInDataSource) {
            // get the grid item data
            var id = $row.attr("id");// record id
            // get the name from the input field name attibute
            var name = $ctrl.attr('name');
            if (name.length == 0) throw "Invalid name attribute";/*DEBUG*/
            var parts = name.split(".");
            if (parts.length < 2) throw "Invalid name attribute - expected";/*DEBUG*/
            name = parts[parts.length - 1];
            //var data = $grid.jqGrid('getCell', id, name);
            //if (data == null) return;
            $grid.jqGrid('setCell', id, name, $cell.html());
        }
    });
};

// Add input fields from grid datasource to form as a hidden div (in order to return all local data)
YetaWF_GridjqGrid.HandleSubmitLocalData = function ($grid, $form) {
    'use strict';
    var DATACLASS = "yetawf_grid_submitdata";
    //remove any existing hidden div
    $form.remove("div." + DATACLASS);
    // build a new div with all the input fields
    var div = "<div class='" + DATACLASS + "' style='display:none'>";

    var prefix = $grid.attr('data-fieldprefix');
    if (prefix == undefined) throw "Can't locate grid's field prefix";/*DEBUG*/

    // collect all data from grid's datasource
    var ds = $grid.jqGrid('getGridParam', 'data');
    var total = ds.length;
    var colDefs = $grid.jqGrid('getGridParam', 'colModel');
    var colCount = colDefs.length;

    // prepare to replace variable[x] with variable[n] so data binding works for list<>
    var recordCount = 0;
    var re1 = new RegExp("'" + prefix + "\[[0-9]+\]\\.", "gim");
    var re2 = new RegExp("\"" + prefix + "\[[0-9]+\]\\.", "gim");
    var re3 = new RegExp("\\." + prefix + "\[[0-9]+\]\\.", "gim");

    var rowIndex = 0;
    for (var itemIndex = 0 ; itemIndex < total ; ++itemIndex) {
        var item = ds[itemIndex];
        var haveData = false;
        var itemDiv = "";
        for (var colIndex = 0 ; colIndex < colCount ; ++colIndex) {
            var col = colDefs[colIndex];
            if (col.has_form_data) {
                if (col.no_sub_if_notchecked) {// if the checkbox in this cell is not checked we don't submit the data
                    // the checkbox is an unruly thing. if a checkbox is not checked, it's not sent as a form value.
                    // that's why the mvc checkbox also uses a hidden field (set to false) which is always sent
                    // so we have to figure out what the checkbox value is
                    haveData = false;
                    var search = /checked=('|")checked('|")/.exec(item[col.name]);
                    if (search == null)
                        break;//probably not checked
                    if (search.length != 3) throw "search failed";/*DEBUG*/
                    if (search[0].length == 0)
                        break;// not checked
                }
                var newtext = prefix + '[' + rowIndex + '].';
                var text = item[col.name];
                text = text.replace(re1, "'" + newtext);
                text = text.replace(re2, "\"" + newtext);
                text = text.replace(re3, "."+newtext);
                itemDiv += text;
                haveData = true;
            }
        }
        if (haveData) {
            div += itemDiv;
            rowIndex++;
        }
        ++recordCount;
    }
    // end the initial div and add it to the form
    div += "</div>";
    $form.append(div);
};

// Add all input fields from grid to form as a hidden div
YetaWF_GridjqGrid.HandleSubmitFields = function ($grid, $form) {
    'use strict';
    var DATACLASS = "yetawf_grid_submitdata";
    //remove any existing hidden div
    $form.remove("div." + DATACLASS);
    // build a new div with all the input fields
    var div = "<div class='" + DATACLASS + "' style='display:none'>";

    var prefix = $grid.attr('data-fieldprefix');
    if (prefix == undefined) throw "Can't locate grid's field prefix";/*DEBUG*/

    // collect input fields from grid
    var $table = $('table', $grid);
    var $rows = $('tr', $table);
    var rowIndex = 0;
    // prepare to replace variable[x] with variable[n] so data binding works for list<>
    var re1 = new RegExp("'" + prefix + "\[[0-9]+\]\\.", "gim");
    var re2 = new RegExp("\"" + prefix + "\[[0-9]+\]\\.", "gim");
    var re3 = new RegExp("\\." + prefix + "\[[0-9]+\]\\.", "gim");
    var colDefs = $grid.jqGrid('getGridParam', 'colModel');
    var colCount = colDefs.length;
    $rows.each(function (index) {
        var $row = $(this);
        if ($('th', $row).length == 0) {// ignore header
            for (var colIndex = 0 ; colIndex < colCount ; ++colIndex) {
                var col = colDefs[colIndex];
                if (col.has_form_data) {
                    var newtext = prefix + '[' + rowIndex + '].';
                    var text = $('td', $row).eq(colIndex).html();
                    text = text.replace(re1, "'" + newtext);
                    text = text.replace(re2, "\"" + newtext);
                    text = text.replace(re3, "." + newtext);
                    div += text;
                }
            }
            rowIndex++;
        }
    });
    // end the initial div and add it to the form
    div += "</div>";
    $form.append(div);
};

YetaWF_GridjqGrid.gridComplete = function ($grid, gridId) {
    'use strict';
    Y_KillTooltips();
    // execute javascript in grid
    if (YetaWF_GridjqGrid._dataBoundEnabled) {
        var $scripts = $("script", $grid);
        $scripts.each(function (index) {
            eval($scripts[index].innerHTML);
        });
        _YetaWF_Basics.initButtons($grid);
    }
    // highlight data rows with the __highlight property set to true
    var ds = $grid.jqGrid('getGridParam', 'data');
    var total = ds.length;
    for (var i = 0 ; i < total ; ++i) {
        var rec = ds[i];
        if (rec['__highlight'])
            $('tr#' + rec.id, $grid).addClass('yHighlightGridRow');
    }
    // Change pagelist dropdown to show All instead of MaxPages 999999999
    // inspired by http://www.trirand.com/blog/?page_id=393/feature-request/rowlist-all-results
    var $gbox = $('#gbox_{0}'.format(gridId));
    if ($gbox.length != 1) throw "Can't find main grid";/*DEBUG*/
    $("option[value={0}]".format(YConfigs.GridjqGrid.allRecords), $gbox).text(YLocs.GridjqGrid.allRecords);
};

// update the grid in case there are no records shown
YetaWF_GridjqGrid.gridComplete_NoRecords = function ($grid, idEmpty, emptyDiv) {
    // http://www.ok-soft-gmbh.com/jqGrid/EmptyMsgInBody.htm
    'use strict';
    var records = $grid.jqGrid('getGridParam', 'records');
    $('#' + idEmpty).remove();
    if (records === 0) {
        $grid.hide();
        $(emptyDiv).insertAfter($grid.parent());
    } else {
        $grid.show();
    }
};

// display/hide pager depending on how many records are shown (LOCAL data only)
YetaWF_GridjqGrid.ShowPager = function ($grid) {
    'use strict';
    var records = $grid.jqGrid('getGridParam', 'records');// total # of records
    var rowNum = $grid.jqGrid('getGridParam', 'rowNum');// # of records to view
    var id = $grid.attr('id');
    var pagerId = id + '_Pager';
    $('#' + pagerId).toggle(rowNum < records);
};

// user clicked on delete action, remove from list
YetaWF_GridjqGrid.deleteAction = function(actionCtrl) {
    'use strict';

    var $actionCtrl = $(actionCtrl);

    var $ctrl = $actionCtrl.closest('.yt_grid_addordelete');
    if ($ctrl.length != 1) throw "Can't find yt_grid_addordelete with new value control";/*DEBUG*/

    var $grid = $('.yt_gridjqgrid', $ctrl);
    if ($grid.length != 1) throw "Can't find grid control";/*DEBUG*/

    var propertyName = $grid.attr('data-deleteproperty');
    if (propertyName == undefined) throw "Can't get property name";/*DEBUG*/
    var displayName = $grid.attr('data-displayproperty');
    if (displayName == undefined) throw "Can't get display property name";/*DEBUG*/

    var $row = $actionCtrl.closest('td');
    if ($row.length != 1) throw "Can't find grid row";/*DEBUG*/

    var $value = $('input[type="hidden"][name$=".'+propertyName+'"]', $row);
    if ($value.length != 1) throw "Can't find grid row's hidden field {0}".format(propertyName);/*DEBUG*/
    var value = $value.val();
    if (value == undefined) throw "Can't find value to remove in {0}".format(propertyName);/*DEBUG*/

    var ds = $grid.jqGrid('getGridParam', 'data');
    var total = ds.length;

    // find the value in the datasource
    for (var i = 0 ; i < total ; ++i) {
        var rec = ds[i];
        if (rec[propertyName] == value) {
            $grid.jqGrid('delRowData', rec.id);
            if (rec[displayName] == undefined) throw "{0} property is missing".format(propertyName);/*DEBUG*/
            var fmt = $ctrl.attr('data-remmsg');
            if (fmt.length > 0)
                Y_Confirm(fmt.format(rec[displayName]));
            $grid.trigger('reloadGrid', [{page:1}]);
            YetaWF_GridjqGrid.ShowPager($grid);
            return;
        }
    }
    Y_Error($ctrl.attr('data-notfoundmsg').format(value));
};

// propagate all column css classes to grid headers (so we can manipulate headers in css)
YetaWF_GridjqGrid.fixHeaders = function ($grid) {
    'use strict';
    var $realGrid = $grid.closest('.ui-jqgrid');
    if ($realGrid.length == 0) throw "Grid not found";/*DEBUG*/

    // http://stackoverflow.com/questions/19975125/jqgrid-how-to-apply-extra-classes-to-header-columns
    var trHead = $("thead:first tr", $realGrid);
    var colModel = $grid.jqGrid('getGridParam', 'colModel');

    for (var iCol = 0; iCol < colModel.length; iCol++) {
        var columnInfo = colModel[iCol];
        if (columnInfo.classes) {
            var headDiv = $("th:eq(" + iCol + ")", trHead);
            headDiv.addClass(columnInfo.classes.replace('t_cell ', 't_header '));
        }
    }
};

YetaWF_GridjqGrid.toggleSearchToolbar = function ($grid, show) {
    'use strict';
    var $realGrid = $grid.closest('.ui-jqgrid');
    if ($realGrid.length == 0) throw "Grid not found";/*DEBUG*/
    $('.ui-search-toolbar', $realGrid).toggle(show);
};

YetaWF_GridjqGrid.clearSearchFilters = function (gridId) {
    // clear ken date(time)pickers because they're nto auomatically cleared by jqgrid
    $('#gbox_{0} .ui-search-toolbar input[name="dtpicker"]'.format(gridId)).val('');
};

$(document).ready(function () {
    'use strict';

    // when a tab page is switched, resize all the grids in the newly visible panel (custom event)
    // when we're in a float div (property list or tabbed property list) the parent width isn't available until after the
    // page has completely loaded, so we need to set it again. By then, jqgrid has added extra layers so we can't just
    // take $grid.parent()'s width.
    // For other cases (outside float div) this does no harm and resized to the current size.
    $("body").on('YetaWF_PropertyList_PanelSwitched', function(event, $panel) {
        var $grids = $('.yt_gridjqgrid', $panel);
        $grids.each(function () {
            var $grid = $(this);
            var $realGrid = $grid.closest('.ui-jqgrid');
            var width = $realGrid.parent().width();
            $grid.jqGrid('setGridWidth', width, true);
            $grid.trigger('reloadGrid');
        });
    });
    // If the browser window changes, it's possible that the grid's parent element is resized, so we're updating the grid width to match
    $(window).on('resize', function () {
        var $grids = $('.yt_gridjqgrid');
        $grids.each(function () {
            var $grid = $(this);
            var $realGrid = $grid.closest('.ui-jqgrid');
            var width = $realGrid.parent().width();
            $grid.jqGrid('setGridWidth', width, false);
        });
    });


    // CanAddOrDelete
    // handle all delete actions in grids
    $("body").on('click', '.yt_grid_addordelete .ui-jqgrid img[name="DeleteAction"]', function () {
        YetaWF_GridjqGrid.deleteAction(this);
    });

    // CanAddOrDelete
    // intercept return in text box (used for add/delete) and click add button
    $("body").on("keydown", ".yt_grid_addordelete input[name='txtNewValue']", function (e) {
        var $attrVal = $(this);
        var $ctrl = $attrVal.closest('.yt_grid_addordelete');
        if ($ctrl.length != 1) throw "Can't find yt_grid_addordelete with new value control";/*DEBUG*/
        var $addBtn = $('input[name="btnAdd"]', $ctrl);
        if ($addBtn.length != 1) throw "Can't find add button for new value";/*DEBUG*/

        if (e.which == 13) {
            e.preventDefault();
            $addBtn.trigger("click");
            return false;
        }
    });

    // CanAddOrDelete
    // user clicked add button, validate new value and add to list
    $("body").on('click', '.yt_grid_addordelete input[name="btnAdd"]', function () {

        var $btnAdd = $(this);
        var $ctrl = $btnAdd.closest('.yt_grid_addordelete');
        if ($ctrl.length != 1) throw "Can't find yt_grid_addordelete with new value control";/*DEBUG*/

        var $attrVal = $('input[name="txtNewValue"]', $ctrl);
        if ($attrVal.length != 1) throw "Can't find new value control";/*DEBUG*/
        var attrVal = $attrVal.val();
        attrVal = attrVal.trim();
        if (attrVal == "") return;

        var $grid = $('.yt_gridjqgrid', $ctrl);
        if ($grid.length != 1) throw "Can't find grid control for new value";/*DEBUG*/
        var propertyName = $grid.attr('data-deleteproperty');
        if (propertyName == undefined) throw "Can't get property name";/*DEBUG*/
        var displayName = $grid.attr('data-displayproperty');
        if (displayName == undefined) throw "Can't get property name";/*DEBUG*/

        // find the guid of the module being edited (if any)
        var $form = YetaWF_Forms.getForm($btnAdd);
        var editGuid = $('input[name="ModuleGuid"]', $form).val();

        var ds = $grid.jqGrid('getGridParam', 'data');
        var total = ds.length;

        var prefix = $grid.attr('data-fieldprefix');
        if (prefix == undefined) throw "Can't locate grid's field prefix";/*DEBUG*/

        var ajaxurl = $btnAdd.attr('data-ajaxurl');
        if (ajaxurl == undefined) throw "Can't locate ajax url to validate and add attribute value";/*DEBUG*/

        // go to server to validate attribute value
        var postData = "NewValue=" + encodeURIComponent(attrVal)
                       + "&NewRecNumber=" + encodeURIComponent(total)
                       + "&Prefix=" + encodeURIComponent(prefix)
                       + "&EditGuid=" + encodeURIComponent(editGuid)
                       + YetaWF_Forms.getFormInfo($btnAdd).QS;
        $.ajax({
            url: ajaxurl,
            data: postData, cache: false, type: 'POST',
            dataType: 'html',
            success: function (result, textStatus, jqXHR) {
                if (result.startsWith(YConfigs.Basics.AjaxJavascriptReturn)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptReturn.length);
                    eval(script);
                    return;
                } else if (result.startsWith(YConfigs.Basics.AjaxJavascriptErrorReturn)) {
                    var script = result.substring(YConfigs.Basics.AjaxJavascriptErrorReturn.length);
                    eval(script);
                    return;
                }
                // we got a new attribute value
                var newAttrVal = JSON.parse(result);
                // validate it's not a duplicate
                for (var i = 0 ; i < total ; ++i) {
                    var rec = ds[i];
                    if (rec[propertyName] == undefined) throw "{0} property is missing".format(propertyName);/*DEBUG*/
                    if (newAttrVal[propertyName] == undefined) throw "{0} property is missing".format(propertyName);/*DEBUG*/
                    if (rec[propertyName] == newAttrVal[propertyName]) {
                        if (rec[displayName] == undefined) throw "{0} property is missing".format(displayName);/*DEBUG*/
                        Y_Error($ctrl.attr('data-dupmsg').format(rec[displayName]));
                        return;
                    }
                }
                $grid.addRowData(total + 1, newAttrVal, 'last')// add new user to grid datasource
                $attrVal.val('');// clear value name text box
                $grid.trigger('reloadGrid');
                YetaWF_GridjqGrid.ShowPager($grid);
                Y_Confirm($ctrl.attr('data-addedmsg').format(attrVal));
            },
            error: function (jqXHR, textStatus, errorThrown) {
                Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
            }
        });
    });
});
