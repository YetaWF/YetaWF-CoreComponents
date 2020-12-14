/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using YetaWF.Core.Localize;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// An instance of this class represents a time of day, between 00:00:00 hours and 23:59:59.
    /// </summary>
    [TypeConverter(typeof(TimeOfDayConv))]
    public class TimeOfDay {
        /// <summary>
        /// Constructor.
        /// </summary>
        public TimeOfDay() { TOD = new TimeSpan(0, 0, 0); }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="hours">Defines the number of hours in the time of day.</param>
        /// <param name="minutes">Defines the number of minutes in the time of day.</param>
        /// <param name="seconds">Defines the number of seconds in the time of day.</param>
        public TimeOfDay(int hours, int minutes, int seconds) { TOD = new TimeSpan(hours, minutes, seconds); }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dt">A date and time. The time portion is used as time of day. The date portion is not used.</param>
        public TimeOfDay(DateTime dt) {
            if (dt.Kind != DateTimeKind.Utc) throw new InternalError($"DateTime has incorrect Kind {dt.Kind}");
            dt = Formatting.GetUserDateTime(dt);
            TOD = dt.TimeOfDay;
        }
        /// <summary>
        /// The defined time of day.
        /// </summary>
        public TimeSpan TOD { get; private set; }

        /// <summary>
        /// The number of hours in the defined time of day.
        /// </summary>
        public int Hours { get { return TOD.Hours; } }
        /// <summary>
        /// The number of minutes in the defined time of day.
        /// </summary>
        public int Minutes { get { return TOD.Minutes; } }
        /// <summary>
        /// The number of seconds in the defined time of day.
        /// </summary>
        public int Seconds { get { return TOD.Seconds; } }

        /// <summary>
        /// Compares two YetaWF.Core.Components.TimeOfDay instances whether the first instance is greater than the second instance.
        /// </summary>
        /// <param name="thisTime">The first YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <param name="thatTime">The second YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <returns>Returns true if the first instance is greater than the second instance, false otherwise.</returns>
        /// <remarks>TimeOfDay values are compared so earlier times are considered smaller. A null YetaWF.Core.Components.TimeOfDay instance is considered smaller (earlier) than a non-null value.</remarks>
        public static bool operator >(TimeOfDay? thisTime, TimeOfDay? thatTime) {
            return thisTime?.TOD > thatTime?.TOD;
        }
        /// <summary>
        /// Compares two YetaWF.Core.Components.TimeOfDay instances whether the first instance is greater than or equal to the second instance.
        /// </summary>
        /// <param name="thisTime">The first YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <param name="thatTime">The second YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <returns>Returns true if the first instance is greater than or equal to the second instance, false otherwise.</returns>
        /// <remarks>TimeOfDay values are compared so earlier times are considered smaller. A null YetaWF.Core.Components.TimeOfDay instance is considered smaller (earlier) than a non-null value.</remarks>
        public static bool operator >=(TimeOfDay? thisTime, TimeOfDay? thatTime) {
            return thisTime?.TOD >= thatTime?.TOD;
        }
        /// <summary>
        /// Compares two YetaWF.Core.Components.TimeOfDay instances whether the first instance is less than the second instance.
        /// </summary>
        /// <param name="thisTime">The first YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <param name="thatTime">The second YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <returns>Returns true if the first instance is less than the second instance, false otherwise.</returns>
        /// <remarks>TimeOfDay values are compared so earlier times are considered smaller. A null YetaWF.Core.Components.TimeOfDay instance is considered smaller (earlier) than a non-null value.</remarks>
        public static bool operator <(TimeOfDay? thisTime, TimeOfDay? thatTime) {
            return thisTime?.TOD < thatTime?.TOD;
        }
        /// <summary>
        /// Compares two YetaWF.Core.Components.TimeOfDay instances whether the first instance is less than or equal to the second instance.
        /// </summary>
        /// <param name="thisTime">The first YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <param name="thatTime">The second YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <returns>Returns true if the first instance is less than or equal to the second instance, false otherwise.</returns>
        /// <remarks>TimeOfDay values are compared so earlier times are considered smaller. A null YetaWF.Core.Components.TimeOfDay instance is considered smaller (earlier) than a non-null value.</remarks>
        public static bool operator <=(TimeOfDay? thisTime, TimeOfDay? thatTime) {
            return thisTime?.TOD <= thatTime?.TOD;
        }
        /// <summary>
        /// Compares two YetaWF.Core.Components.TimeOfDay instances for equality.
        /// </summary>
        /// <param name="thisTime">The first YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <param name="thatTime">The second YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <returns>Returns true if the two instances are equal, false otherwise.</returns>
        /// <remarks>TimeOfDay values are compared so earlier times are considered smaller. A null YetaWF.Core.Components.TimeOfDay instance is considered smaller (earlier) than a non-null value.</remarks>
        public static bool operator ==(TimeOfDay? thisTime, TimeOfDay? thatTime) {
            return thisTime?.TOD == thatTime?.TOD;
        }
        /// <summary>
        /// Compares two YetaWF.Core.Components.TimeOfDay instances for inequality.
        /// </summary>
        /// <param name="thisTime">The first YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <param name="thatTime">The second YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <returns>Returns true if the two instances are unequal, false otherwise.</returns>
        /// <remarks>TimeOfDay values are compared so earlier times are considered smaller. A null YetaWF.Core.Components.TimeOfDay instance is considered smaller (earlier) than a non-null value.</remarks>
        public static bool operator !=(TimeOfDay? thisTime, TimeOfDay? thatTime) {
            return thisTime?.TOD != thatTime?.TOD;
        }
        /// <inheritdoc/>
        public override bool Equals(object? obj) {
            return base.Equals(obj);
        }
        /// <inheritdoc/>
        public override int GetHashCode() {
            return base.GetHashCode();
        }
        /// <summary>
        /// Adds the specified number of hours to the time of day.
        /// </summary>
        /// <param name="hours">The number of hours to add to the time of day. A negative number can be used to subtract hours.</param>
        /// <remarks>No overflow checking is performed and adding/subtracting hours may result in an invalid time of day.</remarks>
        public void AddHours(int hours) { TOD = TOD.Add(new TimeSpan(hours, 0, 0)); }

        /// <summary>
        /// Returns the defined time of day with today's date.
        /// </summary>
        /// <returns>Returns the defined time of day (TOD property) with today's date.</returns>
        public DateTime AsDateTime() {
            DateTime today = DateTime.UtcNow.Date;
            DateTime dt = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Local);
            dt = dt.Add(TOD);
            dt = Formatting.GetUtcDateTime(dt);
            return dt;
        }
    }
    /// <summary>
    /// The TimeOfDayConv class is used to convert YetaWF.Core.Components.TimeOfDay instances to/from other types.
    /// Intended for internal use only.
    /// </summary>
    public class TimeOfDayConv : TypeConverter {
        /// <inheritdoc/>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            if (destinationType == typeof(string))
                return true;
            return base.CanConvertTo(context, destinationType);
        }
        /// <inheritdoc/>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType == typeof(string) && value != null) {
                return ((TimeOfDay)value).AsDateTime().ToString("R");
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
        /// <inheritdoc/>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }
        /// <inheritdoc/>
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
    /// <summary>
    /// Implements a type comparer that compares two YetaWF.Core.Components.TimeOfDay instances.
    /// TimeOfDay values are compared so earlier times are considered smaller. A null YetaWF.Core.Components.TimeOfDay instance is considered greater (later) than a non-null value.
    /// </summary>
    public class TimeOfDayNullLastComparer : IComparer<TimeOfDay> {
        /// <summary>
        /// Compares two YetaWF.Core.Components.TimeOfDay instances and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <param name="y">The second YetaWF.Core.Components.TimeOfDay instance to compare. May be null.</param>
        /// <returns>Returns less than 0 if x is smaller than y, 0 if they are equal, greater than 0 if x is greater than y.</returns>
        /// <remarks>TimeOfDay values are compared so earlier times are considered smaller. A null YetaWF.Core.Components.TimeOfDay instance is considered greater (later) than a non-null value.</remarks>
        public int Compare(TimeOfDay? x, TimeOfDay? y) {
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