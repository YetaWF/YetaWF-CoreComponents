/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

var YetaWF_GridKendo = {};

YetaWF_GridKendo._dataBoundEnabled = 1;

// Localization message for pager
// http://docs.telerik.com/kendo-ui/getting-started/web/grid/localization
YetaWF_GridKendo.getMessages_Pageable = function () {
    var msg = {
        display: YLocs.GridKendo.display,
        empty: YLocs.GridKendo.empty,
        page: YLocs.GridKendo.page,
        of: YLocs.GridKendo.of,
        itemsPerPage: YLocs.GridKendo.itemsPerPage,
        first: YLocs.GridKendo.first,
        previous: YLocs.GridKendo.previous,
        next: YLocs.GridKendo.next,
        last: YLocs.GridKendo.last,
        refresh: YLocs.GridKendo.refresh,
        morePages: YLocs.GridKendo.morePages
    };
    return msg;
}

YetaWF_GridKendo.GetReadParameterMap = function ($grid, moduleguid, data, operation, settingsModuleGuid) {
    var result = {};
    result["page"] = data.page;
    result["pageSize"] = data.pageSize;
    result["skip"] = data.skip;
    result["take"] = data.take;
    if (data.sort) {
        result.sort = {};
        for (var i = 0; i < data.sort.length; i++) {
            var srt = data.sort[i];
            for (var mem in srt) {
                if (mem == "field") {
                    result["sort[" + i + "].Field"] = srt[mem];
                } else if (mem == "dir") {
                    result["sort[" + i + "].Order"] = (srt[mem] == "asc" ? 0 : 1);
                }
                else throw "unknown member " + mem;/*DEBUG*/
            }
        }
    }

    function addFilters(dataFilter, counter, prefix)
    {
        if (dataFilter == undefined) return;
        if (dataFilter.hasOwnProperty('filters')) {
            if (!dataFilter.hasOwnProperty('logic')) throw "unknown member logic";/*DEBUG*/
            result[prefix + "[" + counter + "].Logic"] = dataFilter['logic'];
            addFilters(dataFilter['filters'], 0, prefix + "[" + counter + "].Filters");
            ++counter;
        } else {
            for (var i = 0 ; i < dataFilter.length; i++) {
                var flt = dataFilter[i];
                if (flt.hasOwnProperty('filters')) {
                    if (!flt.hasOwnProperty('logic')) throw "unknown member logic";/*DEBUG*/
                    result[prefix + "[" + counter + "].Logic"] = flt['logic'];
                    addFilters(flt['filters'], 0, prefix + "[" + counter + "].Filters");
                } else {
                    if (!flt.hasOwnProperty('field')) throw "unknown member field";/*DEBUG*/
                    if (!flt.hasOwnProperty('operator')) throw "unknown member operator";/*DEBUG*/
                    if (!flt.hasOwnProperty('value')) throw "unknown member value";/*DEBUG*/
                    result[prefix + "[" + counter + "].Field"] = flt['field'];
                    result[prefix + "[" + counter + "].Operator"] = flt['operator'];
                    result[prefix + "[" + counter + "].ValueAsString"] = flt['value'];
                }
                ++counter;
            }
        }
    }

    result.filter = {};
    addFilters(data.filter, 0, "filter");
    result["SettingsModuleGuid"] = settingsModuleGuid
    var info = YetaWF_Forms.getFormInfo($grid);
    result[YConfigs.Basics.ModuleGuid] = info.ModuleGuid;// the module handling this data
    result[YConfigs.Forms.RequestVerificationToken] = info.RequestVerificationToken;// antiforgery token from form
    result[YConfigs.Forms.UniqueIdPrefix] = info.UniqueIdPrefix;// uniqueidprefix from form

    // add any extra data if available
    var extradata = $grid.attr('data-extraproperty');
    if (extradata != undefined) {
        eval("extradata = " + extradata + ";");
        $.extend(result, extradata);
    }
    return result;
};

YetaWF_GridKendo.SaveSettingsColumnWidths = function (grid, url, settingsGuid) {
    if (!url || !settingsGuid) return;

    // save all relevant settings via ajax call
    var data = {};
    data["SettingsModuleGuid"] = settingsGuid;
    for (var i = 0; i < grid.columns.length; i++) {
        var field = grid.columns[i].field;
        data["Columns[" + field + "]"] = grid.columns[i].width;
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
            debugger;
            return false;
        }
    });
};

YetaWF_GridKendo.HandleInputUpdates = function ($grid, saveInDataSource) {
    var grid = $grid.data("kendoGrid");
    // handle input in grids on forms (save any new data in data source)
    // find the input field (name) and save the cell's html contents in the data source
    // we're using that html later to post to the data to the form
    $grid.on("change", 'input,select', function (e) {
        var $ctrl = $(e.target);
        // get the grid row
        var $row = $ctrl.closest("tr");
        if ($row == null) return;
        // get the grid item data
        var item = grid.dataItem($row);
        if (item == null) return;
        // get the name from the input field name attibute
        var name = $ctrl.attr('name');
        if (name.length == 0) throw "Invalid name attribute";/*DEBUG*/
        var parts = name.split(".");
        if (parts.length < 2) throw "Invalid name attribute - expected .";/*DEBUG*/
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
            item.set(parts[parts.length - 1], $cell.html());
        }
    });
};

// Add input fields from grid datasource to form as a hidden div (in order to return all local data)
YetaWF_GridKendo.HandleSubmitLocalData = function ($grid, $form) {
    var DATACLASS = "yetawf_grid_submitdata";
    //remove any existing hidden div
    $form.remove("div." + DATACLASS);
    // build a new div with all the input fields
    var div = "<div class='" + DATACLASS + "' style='display:none'>";

    var prefix = $grid.attr('data-fieldprefix');
    if (prefix == undefined) throw "Can't locate grid's field prefix";/*DEBUG*/

    var grid = $grid.data("kendoGrid");

    // collect all data from grid's datasource
    var ds = grid.dataSource;
    var total = ds.total();
    var colDefs = grid.columns;
    var colCount = colDefs.length;

    // prepare to replace variable[x] with variable[n] so data binding works for list<>
    var recordCount = 0;
    var re1 = new RegExp("'" + prefix + "\[[0-9]+\]\\.", "gim");
    var re2 = new RegExp("\"" + prefix + "\[[0-9]+\]\\.", "gim");
    var re3 = new RegExp("\\." + prefix + "\[[0-9]+\]\\.", "gim");

    var rowIndex = 0;
    for (var itemIndex = 0 ; itemIndex < total ; ++itemIndex) {
        var item = ds.at(itemIndex);
        if (item == undefined) continue; // happens when records are deleted
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
                    var search = /checked=('|")checked('|")/.exec(item[col.field]);
                    if (search == null)
                        break;//probably not checked
                    if (search.length != 3) throw "search failed";/*DEBUG*/
                    if (search[0].length == 0)
                        break;// not checked
                }
                var newtext = prefix + '[' + rowIndex + '].';
                var text = item[col.field];
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
YetaWF_GridKendo.HandleSubmitFields = function ($grid, $form) {
    var DATACLASS = "yetawf_gridkendo_submitdata";
    //remove any existing hidden div
    $form.remove("div." + DATACLASS);
    // build a new div with all the input fields
    var div = "<div class='" + DATACLASS + "' style='display:none'>";

    var prefix = $grid.attr('data-fieldprefix');
    if (prefix == undefined) throw "Can't locate grid's field prefix";/*DEBUG*/

    var grid = $grid.data("kendoGrid");

    // collect input fields from grid
    var $table = $('table', $grid);
    var $rows = $('tr', $table);
    var rowIndex = 0;
    // prepare to replace variable[x] with variable[n] so data binding works for list<>
    var re1 = new RegExp("'" + prefix + "\[[0-9]+\]\\.", "gim");
    var re2 = new RegExp("\"" + prefix + "\[[0-9]+\]\\.", "gim");
    var re3 = new RegExp("\\." + prefix + "\[[0-9]+\]\\.", "gim");
    var colDefs = grid.columns;
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

// execute javascript in grid
YetaWF_GridKendo.OnDataBound = function ($grid) {
    Y_KillTooltips();
    if (YetaWF_GridKendo._dataBoundEnabled) {
        var $scripts = $("script", $grid);
        $scripts.each(function (index) {
            eval($scripts[index].innerHTML);
        });
        _YetaWF_Basics.initButtons($grid);
    }
};

// update the grid in case there are no records shown
YetaWF_GridKendo.OnDataBound_NoRecords = function (e, $grid, idEmpty) {
    var gridVisible = e.sender.dataSource.view().length > 0;
    $('#' + idEmpty).toggle(!gridVisible);
    $grid.toggle(gridVisible);
};

// display/hide pager depending on how many records are shown (LOCAL data only)
YetaWF_GridKendo.ShowPager = function ($grid) {
    var grid = $grid.data("kendoGrid");
    var ds = grid.dataSource;
    var total = ds.total();
    if (grid.pager == undefined) return;// not ready yet
    var show = (total > grid.pager.pageSize());
    $('.k-pager-wrap.k-grid-pager', $grid).toggle(show);
};

// user clicked on delete action, remove from list
YetaWF_GridKendo.deleteAction = function(actionCtrl) {

    var $actionCtrl = $(actionCtrl);

    var $ctrl = $actionCtrl.closest('.yt_grid_addordelete');
    if ($ctrl.length != 1) throw "Can't find yt_grid_addordelete with new value control";/*DEBUG*/

    var $grid = $('.yt_gridkendo.k-grid.k-widget', $ctrl);
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

    var grid = $grid.data("kendoGrid");
    var ds = grid.dataSource;
    var total = ds.total();

    // find the value in the datasource
    for (var i = 0 ; i < total ; ++i) {
        var rec = ds.at(i);
        if (rec[propertyName] == value) {
            ds.remove(rec);
            if (rec[displayName] == undefined) throw "{0} property is missing".format(propertyName);/*DEBUG*/
            var fmt = $ctrl.attr('data-remmsg');
            if (fmt.length > 0)
                Y_Confirm(fmt.format(rec[displayName]));
            YetaWF_GridKendo.ShowPager($grid);
            return;
        }
    }
    Y_Error($ctrl.attr('data-notfoundmsg').format(value));
};

$(document).ready(function () {

    // CanAddOrDelete
    // handle all delete actions in grids
    $("body").on('click', '.yt_gridkendo img[name="DeleteAction"]', function () {
        YetaWF_GridKendo.deleteAction(this);
    });

    // CanAddOrDelete
    // intercept return in text box (used for add/delete) and click add button
    $("body").on("keydown", ".yt_grid_addordelete input[name$='.NewValue']", function (e) {
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

        var $attrVal = $('input[name$=".NewValue"]', $ctrl);
        if ($attrVal.length != 1) throw "Can't find new value control";/*DEBUG*/
        var attrVal = $attrVal.val();
        attrVal = attrVal.trim();
        if (attrVal == "") return;

        var $grid = $('.k-grid.k-widget', $ctrl);
        if ($grid.length != 1) throw "Can't find grid control for new value";/*DEBUG*/
        var propertyName = $grid.attr('data-deleteproperty');
        if (propertyName == undefined) throw "Can't get property name";/*DEBUG*/
        var displayName = $grid.attr('data-displayproperty');
        if (displayName == undefined) throw "Can't get property name";/*DEBUG*/

        // find the guid of the module being edited (if any)
        var $form = YetaWF_Forms.getForm($btnAdd);
        var editGuid = $('input[name="ModuleGuid"]', $form).val();

        var grid = $grid.data("kendoGrid");
        var ds = grid.dataSource;
        var total = ds.total();

        var prefix = $grid.attr('data-fieldprefix');
        if (prefix == undefined) throw "Can't locate grid's field prefix";/*DEBUG*/

        var ajaxurl = $btnAdd.attr('data-ajaxurl');
        if (ajaxurl == undefined) throw "Can't locate ajax url to validate and add attribute value";/*DEBUG*/

        // go to server to validate attribute value
        var postData = "NewValue=" + encodeURIComponent(attrVal)
                       + "&NewRecNumber=" + encodeURIComponent(ds.total())
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
                    var rec = ds.at(i);
                    if (rec[propertyName] == undefined) throw "{0} property is missing".format(propertyName);/*DEBUG*/
                    if (newAttrVal[propertyName] == undefined) throw "{0} property is missing".format(propertyName);/*DEBUG*/
                    if (rec[propertyName] == newAttrVal[propertyName]) {
                        if (rec[displayName] == undefined) throw "{0} property is missing".format(displayName);/*DEBUG*/
                        Y_Error($ctrl.attr('data-dupmsg').format(rec[displayName]));
                        return;
                    }
                }
                ds.add(newAttrVal);// add new user to grid datasource
                $attrVal.val('');// clear value name text box
                YetaWF_GridKendo.ShowPager($grid);
                Y_Confirm($ctrl.attr('data-addedmsg').format(attrVal));
            },
            error: function (jqXHR, textStatus, errorThrown) {
                Y_Alert(YLocs.Forms.AjaxError.format(jqXHR.status, jqXHR.statusText), YLocs.Forms.AjaxErrorTitle);
            }
        });
    });
});
