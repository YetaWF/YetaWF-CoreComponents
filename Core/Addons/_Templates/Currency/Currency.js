/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF_Core;
(function (YetaWF_Core) {
    var TemplateCurrency;
    (function (TemplateCurrency) {
        var TemplateClass = (function () {
            function TemplateClass() {
            }
            /**
             * Initializes all currency fields in the specified tag.
             * @param tag - an element containing Currency template controls.
             */
            TemplateClass.prototype.initSection = function (tag) {
                var list = tag.querySelectorAll("input.yt_currency.t_edit");
                var len = list.length;
                for (var i = 0; i < len; ++i) {
                    var el = list[i];
                    var d = el.getAttribute("data-min");
                    var sd = d == null ? 0.0 : Number(d);
                    d = el.getAttribute("data-max");
                    var ed = d == null ? 99999999.99 : Number(d);
                    $(el).kendoNumericTextBox({
                        format: YVolatile.Currency.CurrencyFormat,
                        min: sd, max: ed,
                        culture: YConfigs.Basics.Language
                    });
                }
            };
            return TemplateClass;
        }());
        TemplateCurrency.TemplateClass = TemplateClass;
        // initializes new currency elements on demand
        YetaWF_Basics.addWhenReady(function (section) {
            new TemplateClass().initSection(section);
        });
    })(TemplateCurrency = YetaWF_Core.TemplateCurrency || (YetaWF_Core.TemplateCurrency = {}));
})(YetaWF_Core || (YetaWF_Core = {}));
//# sourceMappingURL=Currency.js.map