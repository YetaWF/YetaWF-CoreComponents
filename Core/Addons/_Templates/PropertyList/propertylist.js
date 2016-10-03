﻿/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_PropertyList = {};

// Show/hide controls based on control data derived from ProcessIf attribute in property lists
YetaWF_PropertyList.init = function (divId, controlData, inPartialView) {
    'use strict';

    var $div = $('#' + divId);
    if ($div.length != 1) throw "div not found";/*DEBUG*/

    function change($this) {
        var name = $this.attr("name"); // name of controlling item (an enum)
        var val = $this.val(); // the current value
        controlData.Dependents.forEach(function (item, index) {
            if (name == item.ControlProp) { // this entry is for the controlling item?
                var $row = $('.t_row.t_{0}'.format(item.Prop.toLowerCase()), $div); // the propertylist row affected
                var found = false, len = item.Values.length, i;
                for (i = 0 ; i < len ; ++i) {
                    if (item.Values[i] == val) {
                        found = true;
                        break;
                    }
                }
                if (item.Disable) {
                    if (found)
                        $row.removeAttr("disabled").removeClass('yNoValidate');
                    else
                        $row.attr("disabled", "disabled").addClass('yNoValidate');
                } else {
                    $row.toggle(found);
                    if (found)
                        $row.removeClass('yNoValidate');
                    else
                        $row.addClass('yNoValidate');
                }
            }
        });
    }
    // Handle change events
    controlData.Controls.forEach(function (item, index) {
        $('.t_row.t_{0} select[name="{1}"]'.format(item.toLowerCase(), item), $div).on("change", function () {
            change($(this));
        });
    });
    // Initialize initial form
    controlData.Controls.forEach(function (item, index) {
        change($('.t_row.t_{0} select[name="{1}"]'.format(item.toLowerCase(), item)), $div);
    });

    // add a form presubmit handler so we can mark all hidden properties as not to be evaluated
    var $form = $div.closest('form');
    if ($form.length > 0 && typeof YetaWF_Forms !== 'undefined' && YetaWF_Forms != undefined) {
        YetaWF_Forms.addPreSubmitHandler(inPartialView, {
            form: $form,
            callback: function(entry) {
                // Before submitting, mark all hidden properties as not to be evaluated
                $('input,select,textarea', $div).removeClass('yNoValidate');
                controlData.Controls.forEach(function (ctlItem, index) {
                    controlData.Dependents.forEach(function (propItem, index) {
                        var $row = $('.t_row.t_{0}'.format(propItem.Prop.toLowerCase()), $div);
                        $('input,select,textarea', $row).addClass('yNoValidate');
                    });
                });
            },
        });
    }
};


// The property list needs a bit of special love when it's made visible. Because panels have no width/height
// while the propertylist is not visible (jquery implementation), when a propertylist is made visible using show(),
// the default panel is not sized correctly. If you explicitly show() a propertylist that has never been visible,
// send the following event to cause the propertylist to be resized correctly.
// $('body').trigger('YetaWF_PropertyList_Visible', $div);
// $div is any jquery object - all items (including child items) are checked for propertylists.

$(document).ready(function () {
    'use strict';
    $("body").on('YetaWF_PropertyList_Visible', function (event, $div) {
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
});