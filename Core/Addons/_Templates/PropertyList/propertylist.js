/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

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
    controlData.Controls.forEach(function(item, index) {
        $('#{0} select[name="{1}"]'.format(divId, item)).on("change", function () {
            change($(this));
        });
    });
    // Initialize initial form
    controlData.Controls.forEach(function (item, index) {
        change($('#{0} select[name="{1}"]'.format(divId, item)));
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

