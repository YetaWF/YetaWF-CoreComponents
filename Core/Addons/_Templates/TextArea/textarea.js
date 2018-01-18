/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_TextArea = {};

// Override the built-in Save button to use our Form submit
// Need to wait for the ckeditor instance to finish initialization
// because CKEDITOR.instances.editor.commands is an empty object
// if you try to use it immediately after CKEDITOR.replace('editor');

if (typeof CKEDITOR !== 'undefined') { // CKEDITOR is only defined when an editable textarea is used
    CKEDITOR.on('instanceReady', function (ev) {

        // Replace the old save's exec function with the new one
        if (ev.editor.commands.save) {
            // Create a new command with the desired exec function
            var overridecmd = new CKEDITOR.command(ev.editor, {
                exec: function (editor) {
                    var $form = $(editor.element.$).closest('form.' + YConfigs.Forms.CssFormAjax);
                    if ($form.length != 1) throw "Couldn't find form";/*DEBUG*/
                    YetaWF_Forms.submit($form, false);
                }
            });

            ev.editor.commands.save.exec = overridecmd.exec;
        }
    });
}

// when a tab page is switched, resize all the ckeditors in the newly visible panel (custom event)
// when we're in a float div (property list or tabbed property list) the parent width isn't available until after the
// page has completely loaded, so we need to set it again.
// For other cases (outside float div) this does no harm and resizes to the current size.

$(document).on('YetaWF_PropertyList_PanelSwitched', function (event, $panel) {
    var $ckeds = $('.yt_textarea.t_edit', $panel);
    $ckeds.each(function () {
        var $cked = $(this);
        var ckEd = CKEDITOR.instances[$cked[0].id];
        ckEd.resize('100%', $cked.attr('data-height'), true);
    });
});

// A <div> is being emptied. Destroy all ckeditors the <div> may contain.
YetaWF_Basics.addClearDiv(function (tag) {
    var list = tag.querySelectorAll("textarea.yt_textarea");
    var len = list.length;
    for (var i = 0; i < len; ++i) {
        var el = list[i];
        if (CKEDITOR.instances[el.id])
            CKEDITOR.instances[el.id].destroy();
    }
});
