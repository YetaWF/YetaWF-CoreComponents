/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */
var YetaWF_Core;
(function (YetaWF_Core) {
    var TemplateDecimal;
    (function (TemplateDecimal) {
        var TemplateClass = /** @class */ (function () {
            function TemplateClass() {
            }
            /**
             * Initializes all decimal fields in the specified tag.
             * @param tag - an element containing Decimal template controls.
             */
            TemplateClass.prototype.initSection = function (tag) {
                var list = tag.querySelectorAll("input.yt_decimal.t_edit");
                var len = list.length;
                for (var i = 0; i < len; ++i) {
                    var el = list[i];
                    var d = el.getAttribute("data-min");
                    var sd = d == null ? 0.0 : Number(d);
                    d = el.getAttribute("data-max");
                    var ed = d == null ? 99999999.99 : Number(d);
                    $(el).kendoNumericTextBox({
                        format: "0.00",
                        min: sd, max: ed,
                        culture: YConfigs.Basics.Language
                    });
                }
            };
            return TemplateClass;
        }());
        TemplateDecimal.TemplateClass = TemplateClass;
        // initializes new decimal elements on demand
        YetaWF_Basics.addWhenReady(function (section) {
            new TemplateClass().initSection(section);
        });
        // A <div> is being emptied. Destroy all kendoNumericTextBox the <div> may contain.
        YetaWF_Basics.addClearDiv(function (tag) {
            var list = tag.querySelectorAll("input.yt_decimal.t_edit");
            var len = list.length;
            for (var i = 0; i < len; ++i) {
                var el = list[i];
                var numTextBox = $(el).data("kendoNumericTextBox");
                if (numTextBox)
                    numTextBox.destroy();
            }
        });
    })(TemplateDecimal = YetaWF_Core.TemplateDecimal || (YetaWF_Core.TemplateDecimal = {}));
})(YetaWF_Core || (YetaWF_Core = {}));

//# sourceMappingURL=Decimal.js.map
