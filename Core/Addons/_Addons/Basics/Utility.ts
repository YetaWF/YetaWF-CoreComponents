/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

namespace YetaWF {

    export class Utility {

        public static formatDateTimeUTC(dateVal: Date | null): string {
            let s: string = "";
            if (dateVal)
                s = `${dateVal.getUTCFullYear()}-${Utility.zeroPad(dateVal.getUTCMonth()+1, 2)}-${Utility.zeroPad(dateVal.getUTCDate(), 2)}T${Utility.zeroPad(dateVal.getUTCHours(), 2)}:${Utility.zeroPad(dateVal.getUTCMinutes(), 2)}:00.000Z`;
            return s;
        }
        public static formatDateUTC(dateVal: Date | null): string {
            let s: string = "";
            if (dateVal)
                s = `${dateVal.getUTCFullYear()}-${this.zeroPad(dateVal.getUTCMonth()+1, 2)}-${this.zeroPad(dateVal.getUTCDate(), 2)}T00:00:00.000Z`;
            return s;
        }
        public static zeroPad(val: number, pos: number): string {
            if (val < 0) return val.toFixed();
            let s = val.toFixed(0);
            while (s.length < pos)
                s = "0" + s;
            return s;
        }
    }
}