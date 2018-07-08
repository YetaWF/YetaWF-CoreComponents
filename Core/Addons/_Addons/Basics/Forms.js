"use strict";
/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF;
(function (YetaWF) {
    var Forms = /** @class */ (function () {
        function Forms() {
            this.submitFormTimer = undefined;
            this.submitForm = null;
        }
        /**
         * Initialize a partial form.
         */
        Forms.prototype.initPartialFormTS = function (elem) {
            YetaWF_FormsImpl.initPartialFormTS(elem);
        };
        /**
         * Initialize a partial form.
         */
        Forms.prototype.initPartialForm = function ($elem) {
            YetaWF_FormsImpl.initPartialForm($elem);
        };
        // submit form on change
        /**
         * Handles submitonchange/applyonchange
         */
        Forms.prototype.initSubmitOnChange = function () {
            // submit
            var _this = this;
            $('body').on('keyup', '.ysubmitonchange select', function (e) {
                if (e.keyCode == 13) {
                    _this.submitForm = YetaWF_Forms /*$$$*/.getForm(e.currentTarget);
                    _this.submitFormOnChange();
                }
            });
            $('body').on('change', '.ysubmitonchange select,.ysubmitonchange input[type="checkbox"]', function (e) {
                clearInterval(_this.submitFormTimer);
                _this.submitForm = YetaWF_Forms /*$$$*/.getForm(e.currentTarget);
                _this.submitFormTimer = setInterval(function () { return _this.submitFormOnChange(); }, 1000); // wait 1 second and automatically submit the form
                YetaWF_Basics.setLoading(true);
            });
            // apply
            $('body').on('keyup', '.yapplyonchange select,.yapplyonchange input[type="checkbox"]', function (e) {
                if (e.keyCode == 13) {
                    _this.submitForm = YetaWF_Forms /*$$$*/.getForm(e.currentTarget);
                    _this.applyFormOnChange();
                }
            });
            $('body').on('change', '.yapplyonchange select', function (e) {
                clearInterval(_this.submitFormTimer);
                _this.submitForm = YetaWF_Forms /*$$$*/.getForm(e.currentTarget);
                _this.submitFormTimer = setInterval(function () { return _this.applyFormOnChange(); }, 1000); // wait 1 second and automatically submit the form
                YetaWF_Basics.setLoading(true);
            });
        };
        Forms.prototype.submitFormOnChange = function () {
            clearInterval(this.submitFormTimer);
            YetaWF_Forms /*$$$*/.submit(this.submitForm, false);
        };
        Forms.prototype.applyFormOnChange = function () {
            clearInterval(this.submitFormTimer);
            YetaWF_Forms /*$$$*/.submit(this.submitForm, false, YGlobals.Link_SubmitIsApply + "=y");
        };
        return Forms;
    }());
    YetaWF.Forms = Forms;
})(YetaWF || (YetaWF = {}));
var YetaWF_Forms = new YetaWF.Forms();
// initialize submit on change
YetaWF_Forms.initSubmitOnChange();
