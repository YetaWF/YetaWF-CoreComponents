/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_PropertyList = {};

// Show/hide controls based on control data derived from ProcessIf attribute in property lists
YetaWF_PropertyList.init = function (divId, controlData, inPartialView) {
    'use strict';

    var $div = $('#' + divId);
    if ($div.length != 1) throw "div not found";/*DEBUG*/

    function update(name, val) {
        controlData.Dependents.forEach(function (item, index) {
            if (name == item.ControlProp || name.endsWith('.' + item.ControlProp)) { // this entry is for the controlling item?
                var $row = $('.t_row.t_{0}'.format(item.Prop.toLowerCase()), $div); // the propertylist row affected
                var found = false, len = item.Values.length, i;
                for (i = 0 ; i < len ; ++i) {
                    if (item.Values[i] == val) {
                        found = true;
                        break;
                    }
                }
                if (item.Disable) {
                    if (found) {
                        $row.removeAttr("disabled");
                    } else {
                        $row.attr("disabled", "disabled");
                    }
                } else {
                    $row.toggle(found);
                    // init any controls that just became visible
                    $(document).trigger('YetaWF_PropertyList_PanelSwitched', $row);
                }
                if (found)
                    $('input,select,textarea', $row).removeClass('yNoValidate');
                else
                    $('input,select,textarea', $row).addClass('yNoValidate');
            }
        });
    }
    function changeSelect($this) {
        var name = $this.attr("name"); // name of controlling item (an enum)
        var val = $this.val(); // the current value
        update(name, val);
    }
    function changeInput($this) {
        var name = $this.attr("name"); // name of controlling item (an checkbox)
        var val = $this.is(':checked'); // the current checkbox state
        update(name, val);
    }
    // Handle change events
    controlData.Controls.forEach(function (item, index) {
        $('.t_row.t_{0} select[name$="{1}"]'.format(item.toLowerCase(), item), $div).on("change", function () {
            changeSelect($(this));
        });
        $('.t_row.t_{0} input[name$="{1}"]'.format(item.toLowerCase(), item), $div).on("change", function () {
            changeInput($(this));
        });
    });
    // Initialize initial form
    controlData.Controls.forEach(function (item, index) {
        var $s = $('.t_row.t_{0} select[name$="{1}"]'.format(item.toLowerCase(), item));
        if ($s.length > 0) changeSelect($s, $div);
        $s = $('.t_row.t_{0} input[name$="{1}"]'.format(item.toLowerCase(), item));
        if ($s.length > 0) changeInput($s, $div);
    });
};

YetaWF_PropertyList.tabInitjQuery = function (id, activeTab, activeTabId) {
    var $ctl = $('#{0}'.format(id));
    $ctl.addClass("t_jquery");
    $ctl.tabs({
        active: activeTab,
        activate: function(ev,ui) {
            if (ui.newPanel != undefined) {
                $ctl.trigger('YetaWF_PropertyList_PanelSwitched', ui.newPanel);
                if (activeTabId)
                    $('#{0}'.format(activeTabId)).val((ui.newTab.length > 0) ? ui.newTab.attr('data-tab') : -1);
            }
        }
    });
};
YetaWF_PropertyList.tabInitKendo = function (id, activeTab, activeTabId) {
    // mark the active tab with .k-state-active before initializing the tabstrip
    var $tabs = $('#{0}>ul>li'.format(id));
    $tabs.removeClass('k-state-active');
    $tabs.eq(activeTab).addClass('k-state-active');

    // init tab control
    var $ts = $('#{0}'.format(id));
    $ts.addClass("t_kendo");
    var tabStrip = $ts.kendoTabStrip({
        animation: false,
        activate: function(ev) {
            if (ev.contentElement != undefined) {
                $ts.trigger('YetaWF_PropertyList_PanelSwitched', $(ev.contentElement));
                if (activeTabId)
                    $('#{0}'.format(activeTabId)).val($(ev.item).attr('data-tab'));
            }
        }
    }).data('kendoTabStrip');
};

YetaWF_Basics.addClearDiv(function (tag) {
    var list = tag.querySelectorAll(".yt_propertylisttabbed.t_jquery");
    var len = list.length;
    for (var i = 0; i < len; ++i) {
        var el = list[i];
        var tabs = $(el);
        if (!tabs) throw "No jquery ui object found";/*DEBUG*/
        tabs.tabs("destroy");
    }
    var list = tag.querySelectorAll(".yt_propertylisttabbed.t_kendo");
    var len = list.length;
    for (var i = 0; i < len; ++i) {
        var el = list[i];
        var tabs = $(el).data('kendoTabStrip');
        if (!tabs) throw "No kendo object found";/*DEBUG*/
        tabs.destroy();
    }
});

// The property list needs a bit of special love when it's made visible. Because panels have no width/height
// while the propertylist is not visible (jquery implementation), when a propertylist is made visible using show(),
// the default panel is not sized correctly. If you explicitly show() a propertylist that has never been visible,
// send the following event to cause the propertylist to be resized correctly.
// $('body').trigger('YetaWF_PropertyList_Visible', $div);
// $div is any jquery object - all items (including child items) are checked for propertylists.

$(document).on('YetaWF_PropertyList_Visible', function (event, $div) {
    'use strict';
    // jquery tabs
    $('.ui-tabs', $div).each(function () {
        var $tabctl = $(this);
        var id = $tabctl.attr('id');
        if (id === undefined) throw "No id on tab control";/*DEBUG*/
        var tabid = $tabctl.tabs("option", "active");
        if (tabid >= 0) {
            var $panel = $('#{0}_tab{1}'.format(id, tabid), $tabctl);
            if ($panel.length == 0) throw "Tab panel {0} not found in tab control {1}".format(tabid, id);/*DEBUG*/
            $('body').trigger('YetaWF_PropertyList_PanelSwitched', $panel);
        }
    });
    // kendo tabs
    $('.k-widget.k-tabstrip', $div).each(function () {
        var $tabctl = $(this);
        var id = $tabctl.attr('id');
        if (id === undefined) throw "No id on tab control";/*DEBUG*/
        var ts = $tabctl.data("kendoTabStrip");
        var tabid = ts.select().attr("data-tab");
        if (tabid >= 0) {
            var $panel = $('#{0}-{1}'.format(id, +tabid + 1), $tabctl);
            if ($panel.length == 0) throw "Tab panel {0} not found in tab control {1}".format(tabid, id);/*DEBUG*/
            $('body').trigger('YetaWF_PropertyList_PanelSwitched', $panel);
        }
    });
});
