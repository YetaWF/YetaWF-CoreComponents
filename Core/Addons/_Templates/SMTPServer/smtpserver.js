/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */
'use strict';

var YetaWF_SMTPServer = {};

YetaWF_SMTPServer.init = function (id) {

    var $control = $('#' + id);
    if ($control.length != 1) throw "SMTPServer control invalid";/*DEBUG*/

    var $server = $('input[name$=".SMTP.Server"]', $control);
    if ($server.length != 1) throw "Server invalid";/*DEBUG*/
    var $auth = $('select[name$=".SMTP.Authentication"]', $control);
    if ($auth.length != 1) throw "Authentication invalid";/*DEBUG*/
    var $button = $('.t_sendtestemail a', $control);
    if ($button.length != 1) throw "Button invalid";/*DEBUG*/

    function showFields(showAll, showSignon) {
        showSignon = (showAll) ? showSignon : false;
        //$('.t_row.t_port', $control).toggle(showAll);
        //$('.t_row.t_authentication', $control).toggle(showAll);
        //$('.t_row.t_username', $control).toggle(showSignon);
        //$('.t_row.t_password', $control).toggle(showSignon);
        //$('.t_row.t_ssl', $control).toggle(showAll);

        if (showAll) $('.t_row.t_port', $control).removeAttr("disabled"); else $('.t_row.t_port', $control).attr("disabled", "disabled");
        if (showAll) $('.t_row.t_authentication', $control).removeAttr("disabled"); else $('.t_row.t_authentication', $control).attr("disabled", "disabled");
        if (showSignon) $('.t_row.t_username', $control).removeAttr("disabled"); else $('.t_row.t_username', $control).attr("disabled", "disabled");
        if (showSignon) $('.t_row.t_password', $control).removeAttr("disabled"); else $('.t_row.t_password', $control).attr("disabled", "disabled");
        if (showAll) $('.t_row.t_ssl', $control).removeAttr("disabled"); else $('.t_row.t_ssl', $control).attr("disabled", "disabled");
        if (showAll) $button.button("enable"); else $button.button("disable");

        if (!showAll) {
            $('input[name$=".SMTP.Port"]', $control).val('25');
        }
        if (!showSignon) {
            $('input[name$=".SMTP.UserName"]', $control).val('');
            $('input[name$=".SMTP.Password"]', $control).val('');
        }
    }

    showFields($server.val().trim().length != 0, $auth.val() != 0);

    $server.on('change keyup keydown', function () {
        showFields($server.val().trim().length != 0, $auth.val() != 0);
    })
    $auth.on('change select keyup keydown', function () {
        showFields($server.val().trim().length != 0, $auth.val() != 0);
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
        var port = $('input[name$=".SMTP.Port"]', $control).val();
        if (port.trim() == '') { port = 25; $('input[name$=".SMTP.Port"]', $control).val(25); }
        uri.addSearch('Port', port);
        uri.addSearch('Authentication', $auth.val());
        uri.addSearch('UserName', $('input[name$=".SMTP.UserName"]', $control).val());
        uri.addSearch('Password', $('input[name$=".SMTP.Password"]', $control).val());
        uri.addSearch('SSL', $('input[name$=".SMTP.SSL"]', $control).is(':checked'));
    });
};
