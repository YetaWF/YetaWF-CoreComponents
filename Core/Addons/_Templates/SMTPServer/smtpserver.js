/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

var YetaWF_SMTPServer = {};

YetaWF_SMTPServer.init = function (id) {
    'use strict';

    var $control = $('#' + id);
    if ($control.length != 1) throw "SMTPServer control invalid";/*DEBUG*/

    var $server = $('input[name$=".Server"]', $control);
    if ($server.length != 1) throw "Server invalid";/*DEBUG*/
    var $auth = $('select[name$=".Authentication"]', $control);
    if ($auth.length != 1) throw "Authentication invalid";/*DEBUG*/
    var $button = $('.t_sendtestemail a', $control);
    if ($button.length != 1) throw "Button invalid";/*DEBUG*/

    function showFields(showAll) {
        if (showAll) $('.t_row.t_port', $control).removeAttr("disabled"); else $('.t_row.t_port', $control).attr("disabled", "disabled");
        if (showAll) $('.t_row.t_authentication', $control).removeAttr("disabled"); else $('.t_row.t_authentication', $control).attr("disabled", "disabled");
        if (showAll) $('.t_row.t_username', $control).removeAttr("disabled"); else $('.t_row.t_username', $control).attr("disabled", "disabled");
        if (showAll) $('.t_row.t_password', $control).removeAttr("disabled"); else $('.t_row.t_password', $control).attr("disabled", "disabled");
        if (showAll) $('.t_row.t_ssl', $control).removeAttr("disabled"); else $('.t_row.t_ssl', $control).attr("disabled", "disabled");
        if (showAll) $button.button("enable"); else $button.button("disable");
        if (!showAll) {
            $('input[name$=".Port"]', $control).val('25');
        }
    }

    $server.on('change keyup keydown', function () {
        showFields($server.val().trim().length != 0);
    })
    $auth.on('change select keyup keydown', function () {
        showFields($server.val().trim().length != 0);
    })

    $button.on('click', function () {
        var uri = $button.uri();
        uri.removeSearch('Server');
        uri.removeSearch('Port');
        uri.removeSearch('Authentication');
        uri.removeSearch('UserName');
        uri.removeSearch('Password');
        uri.removeSearch('SSL');

        uri.addSearch('Server', $server.val());
        var port = $('input[name$=".Port"]', $control).val();
        if (port.trim() == '') { port = 25; $('input[name$=".Port"]', $control).val(25); }
        uri.addSearch('Port', port);
        uri.addSearch('Authentication', $auth.val());
        uri.addSearch('UserName', $('input[name$=".UserName"]', $control).val());
        uri.addSearch('Password', $('input[name$=".Password"]', $control).val());
        uri.addSearch('SSL', $('input[name$=".SSL"]', $control).is(':checked'));
    });

    YetaWF_Basics.whenReady.push({
        callback: function ($tag) {
            if ($tag.has($server)) {
                showFields($server.val().trim().length != 0);
            }
        }
    });
};
