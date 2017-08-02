 /* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF_Core.TemplateDate {

    export class TemplateClass {

        private getGrid(ctrlId: string): HTMLElement {
            var el: HTMLElement | null = document.getElementById(ctrlId);
            if (el == null) throw `Grid element ${ctrlId} not found`;/*DEBUG*/
            return el;
        }
        private getControl(ctrlId: string): HTMLElement {
            var el: HTMLElement | null = document.getElementById(ctrlId);
            if (el == null) throw `Element ${ctrlId} not found`;/*DEBUG*/
            return el;
        }
        private getHidden(ctrl: HTMLElement): HTMLElement {
            var hidden: HTMLElement | null = ctrl.querySelector("input[type=\"hidden\"]") as HTMLElement;
            if (hidden == null) throw "Couldn't find hidden field";/*DEBUG*/
            return hidden;
        }
        private setHidden(hidden: HTMLElement, dateVal: Date): void {
            var s: string = "";
            if (dateVal != null) {
                var utcDate: Date = new Date(Date.UTC(dateVal.getFullYear(), dateVal.getMonth(), dateVal.getDate(), 0, 0, 0));
                s = utcDate.toUTCString();
            }
            hidden.setAttribute("value", s);
        }
        private setHiddenText(hidden: HTMLElement, dateVal: string|null): void {
            hidden.setAttribute("value", dateVal ? dateVal : "");
        }
        private getDate(ctrl: HTMLElement): HTMLElement {
            var date: HTMLElement = ctrl.querySelector("input[name=\"dtpicker\"]") as HTMLElement;
            if (date == null) throw "Couldn't find date field";/*DEBUG*/
            return date;
        }

        /**
         * Initializes one instance of a Date template control.
         * @param ctrlId - The HTML id of the date template control.
         */
        public init(ctrlId: string): void {
            var thisObj: TemplateClass = this;
            var ctrl: HTMLElement = this.getControl(ctrlId);
            var hidden: HTMLElement = this.getHidden(ctrl);
            var date: HTMLElement = this.getDate(ctrl);
            var sd: Date = new Date(1900, 1 - 1, 1);
            var d: string | null = date.getAttribute("data-min-y");
            if (d != null) sd = new Date(Number(date.getAttribute("data-min-y")), Number(date.getAttribute("data-min-m")) - 1, Number(date.getAttribute("data-min-d")));
            d = date.getAttribute("data-max-y");
            var ed: Date = new Date(2199, 12 - 1, 31);
            if (d != null) ed = new Date(Number(date.getAttribute("data-max-y")), Number(date.getAttribute("data-max-m")) - 1, Number(date.getAttribute("data-max-d")));
            $(date).kendoDatePicker({
                animation: false,
                format: YVolatile.Date.DateFormat,
                min: sd, max: ed,
                culture: YConfigs.Basics.Language,
                change: function (ev: kendo.ui.DatePickerEvent): void {
                    var kdPicker: kendo.ui.DatePicker = ev.sender;
                    var val: Date = kdPicker.value();
                    if (val == null)
                        thisObj.setHiddenText(hidden, kdPicker.element[0].getAttribute("value"));
                    else
                        thisObj.setHidden(hidden, val);
                    YetaWF_Core.Forms.ValidateElement(hidden);
                }
            });
            var kdPicker: kendo.ui.DatePicker = $(date).data("kendoDatePicker") as kendo.ui.DatePicker;
            this.setHidden(hidden, kdPicker.value());

            function changeHandler(event: Event): void {
                var val: Date = kdPicker.value();
                if (val == null)
                    thisObj.setHiddenText(hidden, (event.target as HTMLInputElement).value);
                else
                    thisObj.setHidden(hidden, val);
                YetaWF_Core.Forms.ValidateElement(hidden);
            }

            date.addEventListener("change", changeHandler, false);
            date.addEventListener("keyup", changeHandler, false);
        }

        /**
         * Renders a date picker in the jqGrid filter toolbar.
         * @param gridId - The id of the grid containing the date picker.
         * @param elem - The element containing the date value.
         */
        public renderjqGridFilter(gridId: string, elem: HTMLElement):void {
            var grid: HTMLElement = this.getGrid(gridId);
            // Build a kendo date picker
            // We have to add it next to the jqgrid provided input field elem
            // We can't use the jqgrid provided element as a kendodatepicker because jqgrid gets confused and
            // uses the wrong sorting option. So we add the datepicker next to the "official" input field (which we hide)
            var dtPick: HTMLElement = <input name="dtpicker" />;
            elem.insertAdjacentElement("afterend", dtPick);
            // Hide the jqgrid provided input element (we update the date in this hidden element)
            elem.style.display = "none";

            // init date picker
            $(dtPick).kendoDatePicker({
                animation: false,
                format: YVolatile.Date.DateFormat,
                //sb.Append("min: sd, max: ed,");
                culture: YConfigs.Basics.Language,
                change: function (ev: kendo.ui.DatePickerEvent): void {
                    var kdPicker: kendo.ui.DatePicker = ev.sender;
                    var val: Date = kdPicker.value();
                    var s: string = "";
                    if (val !== null) {
                        var utcDate:Date = new Date(Date.UTC(val.getFullYear(), val.getMonth(), val.getDate(), 0, 0, 0));
                        s = utcDate.toUTCString();
                    }
                    elem.setAttribute("value", s);
                }
            });
            /**
             * Handles Return key in Date picker, part of jqGrid filter toolbar.
             * @param event
             */
            function keydownHandler(event: KeyboardEvent): void {
                if (event.keyCode === 13)
                    (grid as any).triggerToolbar();
            }
            dtPick.addEventListener("keydown", keydownHandler, false);
        }
    }
}

