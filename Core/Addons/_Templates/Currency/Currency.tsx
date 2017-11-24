/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF_Core.TemplateCurrency {

    export class TemplateClass {
        /**
         * Initializes all currency fields in the specified tag.
         * @param tag - an element containing Currency template controls.
         */
        initSection(tag: HTMLElement):void {
            var list: NodeListOf<Element> = tag.querySelectorAll("input.yt_currency.t_edit");
            var len: number = list.length;
            for (var i: number = 0; i < len; ++i) {
                var el: HTMLElement = list[i] as HTMLElement;
                var d: string|null = el.getAttribute("data-min");
                var sd: number =  d == null ? 0.0 : Number(d);
                d = el.getAttribute("data-max");
                var ed: number = d == null ? 99999999.99 : Number(d);
                $(el).kendoNumericTextBox({
                    format: YVolatile.Currency.CurrencyFormat,
                    min: sd, max: ed,
                    culture: YConfigs.Basics.Language
                });
            }
        }
    }
    // initializes new currency elements on demand
    YetaWF_Basics.addWhenReady(function (section: HTMLElement): void {
        new TemplateClass().initSection(section);
    });
    // A <div> is being emptied. Destroy all kendoNumericTextBox the <div> may contain.
    YetaWF_Basics.addClearDiv(function (tag: HTMLElement): void {
        var list: NodeListOf<Element> = tag.querySelectorAll("input.yt_currency.t_edit");
        var len: number = list.length;
        for (var i: number = 0; i < len; ++i) {
            var el: HTMLElement = list[i] as HTMLElement;
            var numTextBox : kendo.ui.NumericTextBox = $(el).data("kendoNumericTextBox");
            if (numTextBox) numTextBox.destroy();
        }
    });
}

