/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// Time of day, between 00:00:00 hours and 23:59:59.
    /// </summary>
    [TypeConverter(typeof(TimeOfDayConv))]
    public class TimeOfDay {
        public TimeOfDay() { TOD = new TimeSpan(0, 0, 0); }
        public TimeOfDay(int hours, int minutes, int seconds) { TOD = new TimeSpan(hours, minutes, seconds); }
        public TimeOfDay(DateTime dt) {
            if (dt.Kind != DateTimeKind.Utc) throw new InternalError($"DateTime has incorrect Kind {dt.Kind}");
            dt = Formatting.GetUserDateTime(dt);
            TOD = dt.TimeOfDay;
        }
        public TimeSpan TOD { get; private set; }

        public int Hours { get { return TOD.Hours; } }
        public int Minutes { get { return TOD.Minutes; } }
        public int Seconds { get { return TOD.Seconds; } }

        public static bool operator >(TimeOfDay thisTime, TimeOfDay thatTime) {
            return thisTime.TOD > thatTime.TOD;
        }
        public static bool operator >=(TimeOfDay thisTime, TimeOfDay thatTime) {
            return thisTime.TOD >= thatTime.TOD;
        }
        public static bool operator <(TimeOfDay thisTime, TimeOfDay thatTime) {
            return thisTime.TOD < thatTime.TOD;
        }
        public static bool operator <=(TimeOfDay thisTime, TimeOfDay thatTime) {
            return thisTime.TOD <= thatTime.TOD;
        }
        public static bool operator ==(TimeOfDay thisTime, TimeOfDay thatTime) {
            if ((object)thisTime == null) {
                if ((object)thatTime == null) return true;
                return false;
            }
            if ((object)thatTime == null) return false;
            return thisTime.TOD == thatTime.TOD;
        }
        public static bool operator !=(TimeOfDay thisTime, TimeOfDay thatTime) {
            if ((object)thisTime == null) {
                if ((object)thatTime == null) return false;
                return true;
            }
            if ((object)thatTime == null) return true;
            return thisTime.TOD == thatTime.TOD;
        }
        public override bool Equals(object obj) {
            return base.Equals(obj);
        }
        public override int GetHashCode() {
            return base.GetHashCode();
        }
        public void AddHours(int hours) { TOD = TOD.Add(new TimeSpan(hours, 0, 0)); }

        public DateTime AsDateTime() {
            DateTime today = DateTime.UtcNow.Date;
            DateTime dt = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Local);
            dt = dt.Add(TOD);
            dt = Formatting.GetUtcDateTime(dt);
            return dt;
        }
    }
    public class TimeOfDayConv : TypeConverter {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            if (destinationType == typeof(string))
                return true;
            return base.CanConvertTo(context, destinationType);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string) && value != null) {
                return ((TimeOfDay)value).AsDateTime().ToString("R");
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value != null && value.GetType() == typeof(string)) {
                DateTime dt;
                if (!DateTime.TryParse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces, out dt))
                    return DateTime.MinValue;
                return new TimeOfDay(dt);
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
    public class TimeOfDayNullLastComparer : IComparer<TimeOfDay> {
        public int Compare(TimeOfDay x, TimeOfDay y) {
            if (x == null) {
                if (y == null)
                    return 0;
                else
                    return 1;
            } else {
                if (y == null)
                    return -1;
                if (x.TOD < y.TOD) return -1;
                else if (x.TOD > y.TOD) return 1;
                return 0;
            }
        }
    }
}