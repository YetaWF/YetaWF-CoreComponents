/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_Grid = {};
var _YetaWF_Grid = {};

jQuery.extend(jQuery.jgrid.defaults, {
    autowidth: true,
    hoverrows: false,
    sortable: false, // no column reordering
    height: '100%',
    autoencode: false,
    // Localization message for pager
    // http://www.trirand.com/jqgridwiki/doku.php?id=wiki:pager
    recordtext: YLocs.Grid.recordtext,
    emptyrecords: YLocs.Grid.emptyrecords,
    loadtext: YLocs.Grid.loadtext,
    pgtext: YLocs.Grid.pgtext,
    pgfirst: YLocs.Grid.pgfirst,
    pglast: YLocs.Grid.pglast,
    pgnext: YLocs.Grid.pgnext,
    pgprev: YLocs.Grid.pgprev,
    pgrecs: YLocs.Grid.pgrecs,
    //showhide: "Toggle Expand Collapse Grid", // not used
    //savetext: "Saving...", // not used
});
jQuery.extend(jQuery.jgrid.search, {
    // caption: "Search...",// not used
    // Find: "Find",// not used
    // Reset: "Reset",// not used
    odata: [
        { oper: "eq", text: YLocs.Grid.eq },
        { oper: "ne", text: YLocs.Grid.ne },
        { oper: "lt", text: YLocs.Grid.lt },
        { oper: "le", text: YLocs.Grid.le },
        { oper: "gt", text: YLocs.Grid.gt },
        { oper: "ge", text: YLocs.Grid.ge },
        { oper: "bw", text: YLocs.Grid.bw },
        { oper: "bn", text: YLocs.Grid.bn },
        { oper: "in", text: YLocs.Grid.inx },
        { oper: "ni", text: YLocs.Grid.ni },
        { oper: "ew", text: YLocs.Grid.ew },
        { oper: "en", text: YLocs.Grid.en },
        { oper: "cn", text: YLocs.Grid.cn },
        { oper: "nc", text: YLocs.Grid.nc },
        { oper: "nu", text: YLocs.Grid.nu },
        { oper: "nn", text: YLocs.Grid.nn }
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
// translate column widths given in char width to pixels
YetaWF_Grid.setColumnWidths = function ($grid, options) {
    var colModel = options.colModel;
    var w = $grid.attr("data-charavgw");
    if (!w) throw "No data-charavgw attribute on grid";/*DEBUG*/
    for (var i = 0; i < colModel.length; i++) {
        var entry = colModel[i];
        if (entry.__charWidth) {
            var cw = entry.__charWidth;
            if (cw > 0) {
                entry.width = cw * w;
            }
        }
    }
}
// modify url post data to retrieve data
YetaWF_Grid.modifySend = function ($grid, settingsModuleGuid, options, xhr, settings) {
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
            newData['filters[{0}].ValueAsString'.format(i)] = rule.data;
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
YetaWF_Grid.modifyReceive = function ($grid, options, data, status, xhr) {
    'use strict';
    var totalRecs = data.records;
    var startingRec = options.startingRecord;
    if (startingRec >= totalRecs) startingRec = totalRecs - 1;
    if (startingRec < 0) startingRec = 0;
    data['total'] = Math.ceil(totalRecs / options.requestedRecords);// total pages
    data['page'] = Math.floor(startingRec / options.requestedRecords) + 1;
};
// some error occurred during ajax
YetaWF_Grid.loadError = function ($grid, xhr, status, error) {
    YetaWF_Basics.processAjaxReturn(xhr.responseText, status, xhr);
    $grid.trigger('YetaWF_Grid_LoadError');
}

// save column widths after user resizes
YetaWF_Grid.SaveSettingsColumnWidths = function ($grid, url, settingsGuid, options, newwidth, index) {
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

// We can't support complex controls in jqGrid - With local data sources we can't
// insure that any javascript is executed, particularly while paging through a local datasource.
// For ajax data we are able to run scripts, but it hardly seems worth it at this point.
YetaWF_Grid.HandleInputUpdates = function ($grid, saveInDataSource) {
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
            var id = $row.attr("id");// record id (0..n)
            // get the name from the input field name attribute
            var name = $ctrl.attr('name');
            if (name.length == 0) throw "Invalid name attribute";/*DEBUG*/
            var parts = name.split(".");
            if (parts.length < 2) throw "Invalid name attribute - expected";/*DEBUG*/
            name = parts[parts.length - 1];
            //var data = $grid.jqGrid('getCell', id, name);
            //if (data == null) return;
            $grid.jqGrid('setCell', id, name, $cell.html());
            if (name == '__Value') {
                // update key and display info for native values
                var propertyName = $grid.attr('data-deleteproperty');
                if (propertyName == undefined) throw "Can't get property name";/*DEBUG*/
                var displayName = $grid.attr('data-displayproperty');
                if (displayName == undefined) throw "Can't get display property name";/*DEBUG*/
                var rec = $grid.jqGrid('getRowData', id);
                rec[propertyName] = val;
                rec[displayName] = val;
                $grid.jqGrid('setRowData', id, rec);
            }
            // copy cell contents to data source
            //var cellText = $cell.html();
            //
            //ds[id][name] = cellText;
        }
    });
};

// Add input fields from grid datasource to form as a hidden div (in order to return all local data)
YetaWF_Grid.HandleSubmitLocalData = function ($grid, $form) {
    'use strict';
    // build a data div with all the input fields
    var div = "<div class='" + YetaWF_Forms.DATACLASS + "' style='display:none'>";

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
                text = text.replace(re3, "." + newtext);
                if (col.name == '__Value') { // remove the variable name for native types
                    var reName = new RegExp("\]\\." + col.name, "gim");
                    text = text.replace(reName, "\]");
                }
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
YetaWF_Grid.HandleSubmitFields = function ($grid, $form) {
    'use strict';
    // build a data div with all the input fields
    var div = "<div class='" + YetaWF_Forms.DATACLASS + "' style='display:none'>";

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

YetaWF_Grid.gridComplete = function ($grid, gridId) {
    'use strict';
    $grid.trigger('YetaWF_Grid_LoadSuccessful');
    Y_KillTooltips();
    // execute javascript in grid
    if ($grid.getGridParam("datatype") == 'json') {
        var $scripts = $("script", $grid);
        $scripts.each(function (index) {
            eval($scripts[index].innerHTML);
        });
        _YetaWF_Basics.initButtons($grid);
    }
    // highlight data rows with the __highlight property set to true
    $('tr.yHighlightGridRow', $grid).removeClass('yHighlightGridRow'); // remove all highlights
    $('tr td div.yHighlightGridRow', $grid).closest('tr').addClass('yHighlightGridRow');// add highlight to selected rows
    // lowlight data rows with the __lowlight property set to true
    $('tr.yLowlightGridRow', $grid).removeClass('yLowlightGridRow'); // remove all lowlights
    $('tr td div.yLowlightGridRow', $grid).closest('tr').addClass('yLowlightGridRow');// add lowlight to selected rows

    // Change pagelist dropdown to show All instead of MaxPages 999999999
    // inspired by http://www.trirand.com/blog/?page_id=393/feature-request/rowlist-all-results
    var $gbox = $('#gbox_{0}'.format(gridId));
    if ($gbox.length != 1) throw "Can't find main grid";/*DEBUG*/
    $("option[value={0}]".format(YConfigs.Grid.allRecords), $gbox).text(YLocs.Grid.allRecords);
    // restart validation for new data exposed (by paging)
    YetaWF_Forms.updateValidation($grid);
};

// update the grid in case there are no records shown
YetaWF_Grid.gridComplete_NoRecords = function ($grid, idEmpty, emptyDiv) {
    // http://www.ok-soft-gmbh.com/jqGrid/EmptyMsgInBody.htm
    'use strict';
    var records = $grid.jqGrid('getGridParam', 'records');
    $('#' + idEmpty).remove();
    if (records === 0) {
        $(emptyDiv).insertAfter($grid.parent());
    }
};

// display/hide pager depending on how many records are shown (LOCAL data only)
YetaWF_Grid.ShowPager = function ($grid) {
    'use strict';
    var records = $grid.jqGrid('getGridParam', 'records');// total # of records
    var rowNum = $grid.jqGrid('getGridParam', 'rowNum');// # of records to view
    var id = $grid.attr('id');
    var pagerId = id + '_Pager';
    $('#' + pagerId).toggle(rowNum < records);
};

// propagate all column css classes to grid headers (so we can manipulate headers in css)
YetaWF_Grid.fixHeaders = function ($grid) {
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

YetaWF_Grid.toggleSearchToolbar = function ($grid, show) {
    'use strict';
    var $realGrid = $grid.closest('.ui-jqgrid');
    if ($realGrid.length == 0) throw "Grid not found";/*DEBUG*/
    $('.ui-search-toolbar', $realGrid).toggle(show);
};

YetaWF_Grid.clearSearchFilters = function (gridId) {
    // clear kendo date(time)pickers because they're not automatically cleared by jqgrid
    $('#gbox_{0} .ui-search-toolbar input[name="dtpicker"]'.format(gridId)).val('');
};

// enable/disable add button in yt_grid_addordelete template
YetaWF_Grid.setAddButtonStatus = function ($elem) {
    'use strict';
    var $ctrl = $elem.closest('.yt_grid_addordelete');
    if ($ctrl.length != 1) throw "Can't find yt_grid_addordelete with new value control";/*DEBUG*/
    var disabled = $elem.val().trim() == "";
    if (disabled) {
        $('input[name="btnAdd"]', $ctrl).button("disable");
    } else {
        $('input[name="btnAdd"]', $ctrl).button("enable");
    }
    // mark input field as not to be validated
    var $inp = $('input[name$=".NewValue"]', $ctrl);
    if (!$inp.hasClass('yNoValidate'))
        $inp.addClass('yNoValidate');
};
_YetaWF_Grid.isDuplicate = function ($grid, value) {
    value = value.trim().toUpperCase();

    var ds = $grid.jqGrid('getGridParam', 'data');
    var total = ds.length;

    var propertyName = $grid.attr('data-deleteproperty');
    if (propertyName == undefined) throw "Can't get property name";/*DEBUG*/

    // validate it's not a duplicate
    for (var i = 0 ; i < total ; ++i) {
        var rec = ds[i];
        if (rec[propertyName] == undefined) throw "{0} property is missing".format(propertyName);/*DEBUG*/
        if (rec[propertyName].trim().toUpperCase() == value) {
            return true;
        }
    }
    return false;
};

// when a tab page is switched, resize all the grids in the newly visible panel (custom event)
// when we're in a float div (property list or tabbed property list) the parent width isn't available until after the
// page has completely loaded, so we need to set it again. By then, jqgrid has added extra layers so we can't just
// take $grid.parent()'s width.
// For other cases (outside float div) this does no harm and resizes to the current size.
$(document).on('YetaWF_PropertyList_PanelSwitched', function (event, $panel) {
    var $grids = $('.yt_grid', $panel);
    $grids.each(function () {
        var $grid = $(this);
        var $realGrid = $grid.closest('.ui-jqgrid');
        var width = $realGrid.parent().width();
        $grid.jqGrid('setGridWidth', width, false);
        //$grid.trigger('reloadGrid'); // reload removed for panels (removal test case: dashboard)
    });
});
// If the browser window changes, it's possible that the grid's parent element is resized, so we're updating the grid width to match
$(window).on('resize', function () {
    var $grids = $('.yt_grid');
    $grids.each(function () {
        var $grid = $(this);
        var $realGrid = $grid.closest('.ui-jqgrid');
        var width = $realGrid.parent().width();
        $grid.jqGrid('setGridWidth', width, false);
    });
});

// CanAddOrDelete
// handle all delete actions in grids
$(document).on('click', '.yt_grid_addordelete .ui-jqgrid img[name="DeleteAction"]', function () {
    var $actionCtrl = $(this);

    var $ctrl = $actionCtrl.closest('.yt_grid_addordelete');
    if ($ctrl.length != 1) throw "Can't find yt_grid_addordelete with new value control";/*DEBUG*/

    var $grid = $('.yt_grid', $ctrl);
    if ($grid.length != 1) throw "Can't find grid control";/*DEBUG*/

    var $row = $actionCtrl.closest('tr');
    if ($row.length != 1) throw "Can't find grid row";/*DEBUG*/
    var id = $row.attr("id");// record id (0..n)
    if (id == undefined) throw "Can't find record id";/*DEBUG*/
    var rec = $grid.jqGrid('getRowData', id);

    $grid.jqGrid('delRowData', id);
    var fmt = $ctrl.attr('data-remmsg');
    if (fmt.length > 0) {
        var displayName = $grid.attr('data-displayproperty');
        if (displayName == undefined) throw "Can't get display property name";/*DEBUG*/
        if (rec[displayName] == undefined) throw "{0} property is missing".format(propertyName);/*DEBUG*/
        Y_Confirm(fmt.format(rec[displayName]));
    }
    var total = $grid.getGridParam("reccount");// get total # of records on page
    //var rowsPerPage = $grid.getGridParam("rowNum");// get # of rows per page
    //var currPage = $grid.getGridParam('page');// get current page
    var lastPage = $grid.getGridParam('lastpage');// get last page
    if (total > 0) {
        // there are still records on the page, reload current position
        $grid.trigger('reloadGrid', [{ current: true }]);
    } else {
        if (lastPage > 0) {
            // we're on an empty, last page
            $grid.trigger('reloadGrid', [{ page: lastPage-1 }]);
        } else {
            // grid is empty now
            $grid.trigger('reloadGrid', [{ page: 1 }]);
        }
    }
    YetaWF_Grid.ShowPager($grid);
});

// CanAddOrDelete
$(document).on("propertychange change click keyup input paste", ".yt_grid_addordelete input[name$='.NewValue']", function (e) {
    YetaWF_Grid.setAddButtonStatus($(this));
});

// CanAddOrDelete
// intercept return in text box (used for add/delete) and click add button
$(document).on("keydown", ".yt_grid_addordelete input[name$='.NewValue']", function (e) {
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
$(document).on('click', '.yt_grid_addordelete input[name="btnAdd"]', function () {

    var $btnAdd = $(this);
    var $ctrl = $btnAdd.closest('.yt_grid_addordelete');
    if ($ctrl.length != 1) throw "Can't find yt_grid_addordelete with new value control";/*DEBUG*/

    var $grid = $('.yt_grid', $ctrl);
    if ($grid.length != 1) throw "Can't find grid control for new value";/*DEBUG*/
    var propertyName = $grid.attr('data-deleteproperty');
    if (propertyName == undefined) throw "Can't get property name";/*DEBUG*/
    var displayName = $grid.attr('data-displayproperty');
    if (displayName == undefined) throw "Can't get property name";/*DEBUG*/
    // get new value to add

    var $attrVal = $('input[name$=".NewValue"]', $ctrl);
    if ($attrVal.length != 1) throw "Can't find new value control";/*DEBUG*/
    var attrVal = $attrVal.val();
    attrVal = attrVal.trim();
    if (attrVal == "") return;
    if (_YetaWF_Grid.isDuplicate($grid, attrVal)) {
        Y_Error($ctrl.attr('data-dupmsg').format(attrVal));
        return;
    }

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
            Y_Loading(false);
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
            if (newAttrVal[propertyName] == undefined) throw "{0} property is missing".format(propertyName);/*DEBUG*/
            // validate it's not a duplicate (again, just in case)
            if (_YetaWF_Grid.isDuplicate($grid, newAttrVal[propertyName])) {
                if (newAttrVal[displayName] == undefined) throw "{0} property is missing".format(displayName);/*DEBUG*/
                Y_Error($ctrl.attr('data-dupmsg').format(newAttrVal[displayName]));
                return;
            }
            $grid.addRowData(total + 1, newAttrVal, 'last')// add new user to grid datasource
            $attrVal.val('');// clear value name text box
            $btnAdd.button("disable");
            $grid.trigger('reloadGrid');
            YetaWF_Grid.ShowPager($grid);

            YetaWF_Forms.updateValidation($grid);

            Y_Confirm($ctrl.attr('data-addedmsg').format(attrVal));
        },
        error: function (jqXHR, textStatus, errorThrown) {
            Y_Loading(false);
            Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
        }
    });
});

YetaWF_Basics.addClearDiv(function (tag) {
    var list = tag.querySelectorAll("table.yt_grid");
    var len = list.length;
    for (var i = 0; i < len; ++i) {
        var el = list[i];
        var grid = $(el);
        if (!grid) throw "No grid object found";/*DEBUG*/
        grid.jqGrid('GridUnload');
    }
});

