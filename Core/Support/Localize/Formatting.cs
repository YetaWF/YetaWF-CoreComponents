/* Copyright © 2022 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Components;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Localize {

    public static class Formatting {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private static string __ResStr(string name, string defaultValue, params object?[] parms) { return ResourceAccess.GetResourceString(typeof(Formatting), name, defaultValue, parms); }

        public enum DateFormatEnum {
            [EnumDescription("Month/Day/Year")]
            MMDDYYYY = 0,
            [EnumDescription("Month-Day-Year")]
            MMDDYYYYdash = 1,
            [EnumDescription("Month.Day.Year")]
            MMDDYYYYdot = 2,
            [EnumDescription("Day/Month/Year")]
            DDMMYYYY = 10,
            [EnumDescription("Day-Month-Year")]
            DDMMYYYYdash = 11,
            [EnumDescription("Day.Month.Year")]
            DDMMYYYYdot = 12,
            [EnumDescription("Year/Month/Day")]
            YYYYMMDD = 20,
            [EnumDescription("Year-Month-Day")]
            YYYYMMDDdash = 21,
            [EnumDescription("Year.Month.Day")]
            YYYYMMDDdot = 22,
        }
        public enum TimeFormatEnum {
            [EnumDescription("hh:mm AM/PM")]
            HHMMAM = 0,
            [EnumDescription("hh.mm AM/PM")]
            HHMMAMdot = 1,
            [EnumDescription("hh:mm (hours shown as 0..23)")]
            HHMM = 10,
            [EnumDescription("hh.mm (hours shown as 0..23)")]
            HHMMdot = 11,
            [EnumDescription("hh:mm:ss AM/PM")]
            HHMMSSAM = 20,
            [EnumDescription("hh.mm.ss AM/PM")]
            HHMMSSAMdot = 21,
            [EnumDescription("hh:mm:ss (hours shown as 0..23)")]
            HHMMSS = 30,
            [EnumDescription("hh.mm.ss (hours shown as 0..23)")]
            HHMMSSdot = 31,
        }

        /// <summary>
        /// Format a date - Example "February 2010"
        /// </summary>
        public static string Date_Month_YYYY(DateTime dt)
        {
            dt = GetUserDateTime(dt);
            string month = GetMonthName(dt.Month);
            return __ResStr("strMonth_YYYY", "{0} {1}", month, dt.Year);
        }
        /// <summary>
        /// Format a date - Example "November 1"
        /// </summary>
        public static string Date_Month_Day(DateTime dt) {
            dt = GetUserDateTime(dt);
            return __ResStr("strMonth_Day", "{0} {1}", Formatting.GetMonthName(dt.Month), dt.Day);
        }
        /// <summary>
        /// Format a date - Example "November 1, 2020"
        /// </summary>
        public static string Date_Month_Day_Year(DateTime dt) {
            dt = GetUserDateTime(dt);
            string month = GetMonthName(dt.Month);
            return __ResStr("strMonth_Day_Year", "{0} {1}, {2}", month, dt.Day, dt.Year);
        }

        public static DateTime GetUserDateTime(DateTime dateTime, DateFormatEnum? dateFormat = null) {
            if (dateTime.Kind != DateTimeKind.Utc && dateTime.Kind != DateTimeKind.Unspecified) throw new InternalError($"DateTime has incorrect Kind {dateTime.Kind}");
            if (dateTime == DateTime.MinValue) return dateTime;
            TimeZoneInfo tzi = Manager.GetTimeZoneInfo();// user's timezone
            dateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, tzi);
            return dateTime;
        }

        public static string GetFormatDateFormat(DateFormatEnum? dateFormat = null) {
            DateFormatEnum df = dateFormat != null ? (DateFormatEnum) dateFormat : UserSettings.GetProperty<DateFormatEnum>("DateFormat");
            switch (df) {
                default:
                case DateFormatEnum.MMDDYYYY:
                    return @"MM/dd/yyyy";
                case DateFormatEnum.MMDDYYYYdash:
                    return @"MM-dd-yyyy";
                case DateFormatEnum.MMDDYYYYdot:
                    return @"MM.dd.yyyy";
                case DateFormatEnum.DDMMYYYY:
                    return @"dd/MM/yyyy";
                case DateFormatEnum.DDMMYYYYdash:
                    return @"dd-MM-yyyy";
                case DateFormatEnum.DDMMYYYYdot:
                    return @"dd.MM.yyyy";
                case DateFormatEnum.YYYYMMDD:
                    return @"yyyy/MM/dd";
                case DateFormatEnum.YYYYMMDDdash:
                    return @"yyyy-MM-dd";
                case DateFormatEnum.YYYYMMDDdot:
                    return @"yyyy.MM.dd";
            }
        }
        public static DateTime GetUtcDateTime(DateTime dateTime) {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;
            // dateTime is the user's time zone (NOT local)
            DateTime dt = new DateTime(dateTime.Ticks, DateTimeKind.Unspecified);
            TimeZoneInfo tzi = Manager.GetTimeZoneInfo();// user's timezone
            return TimeZoneInfo.ConvertTimeToUtc(dt, tzi);
        }
        /// <summary>
        /// Format date (the date has a time component (based on a timezone)
        /// </summary>
        public static string FormatDate(DateTime? dateTime, DateFormatEnum? dateFormat = null) {
            if (dateTime == null) return string.Empty;
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return string.Empty;
            dt = GetUserDateTime(dt);
            string fmt = GetFormatDateFormat(dateFormat);
            return dt.ToString(fmt);
        }
        /// <summary>
        /// Format date. The date does not have a time component (always UTC 00:00:00). If a time component is present, it is ignored.
        /// </summary>
        public static string FormatDateOnly(DateTime? dateTime, DateFormatEnum? dateFormat = null) {
            if (dateTime == null) return string.Empty;
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return string.Empty;
            dt = dt.Date;
            string fmt = GetFormatDateFormat(dateFormat);
            return dt.ToString(fmt);
        }

        public static string FormatTimeOfDay(TimeOfDay? timeOfDay, TimeFormatEnum? timeFormat = null) {
            if (timeOfDay == null) return string.Empty;
            TimeFormatEnum tf = UserSettings.GetProperty<TimeFormatEnum>("TimeFormat");
            switch (tf) {
                default:
                case TimeFormatEnum.HHMMAM:
                case TimeFormatEnum.HHMMSSAM: {
                        int hours = timeOfDay.Hours % 12;
                        if (hours == 0) hours += 12;
                        return $"{hours:D2}:{timeOfDay.Minutes:D2} {(timeOfDay.Hours < 12 ? "AM" : "PM")}";
                    }
                case TimeFormatEnum.HHMMAMdot:
                case TimeFormatEnum.HHMMSSAMdot: {
                        int hours = timeOfDay.Hours % 12;
                        if (hours == 0) hours += 12;
                        return $"{hours:D2}.{timeOfDay.Minutes:D2} {(timeOfDay.Hours < 12 ? "AM" : "PM")}";
                    }
                case TimeFormatEnum.HHMM:
                case TimeFormatEnum.HHMMSS:
                    return $"{timeOfDay.Hours:D2}:{timeOfDay.Minutes:D2}";
                case TimeFormatEnum.HHMMdot:
                case TimeFormatEnum.HHMMSSdot:
                    return $"{timeOfDay.Hours:D2}:{timeOfDay.Minutes:D2}";
            }
        }

        public static string GetFormatTimeFormat(TimeFormatEnum? timeFormat = null) {
            TimeFormatEnum tf = UserSettings.GetProperty<TimeFormatEnum>("TimeFormat");
            switch (tf) {
                default:
                case TimeFormatEnum.HHMMAM:
                    return @"hh:mm tt";
                case TimeFormatEnum.HHMMAMdot:
                    return @"hh.mm tt";
                case TimeFormatEnum.HHMM:
                    return @"HH:mm";
                case TimeFormatEnum.HHMMdot:
                    return @"HH.mm";
                case TimeFormatEnum.HHMMSSAM:
                    return @"hh:mm:ss tt";
                case TimeFormatEnum.HHMMSSAMdot:
                    return @"hh.mm.ss tt";
                case TimeFormatEnum.HHMMSS:
                    return @"HH:mm:ss";
                case TimeFormatEnum.HHMMSSdot:
                    return @"HH.mm.ss";
            }
        }
        public static string FormatTime(DateTime? dateTime, TimeFormatEnum? timeFormat = null) {
            if (dateTime == null) return string.Empty;
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return string.Empty;
            dt = GetUserDateTime(dt);
            string fmt = GetFormatTimeFormat(timeFormat);
            return dt.ToString(fmt);
        }
        public static string FormatTimeDetailed(DateTime? dateTime) {
            if (dateTime == null) return string.Empty;
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return string.Empty;
            dt = GetUserDateTime(dt);
            TimeFormatEnum tf = UserSettings.GetProperty<TimeFormatEnum>("TimeFormat");

            switch (tf) {
            default:
            case TimeFormatEnum.HHMMAM:
            case TimeFormatEnum.HHMMSSAM:
                return dt.ToString(@"hh:mm:ss.ffff tt");
            case TimeFormatEnum.HHMMAMdot:
            case TimeFormatEnum.HHMMSSAMdot:
                return dt.ToString(@"hh.mm.ss.ffff tt");
            case TimeFormatEnum.HHMM:
            case TimeFormatEnum.HHMMSS:
                return dt.ToString(@"HH:mm:ss.ffff");
            case TimeFormatEnum.HHMMdot:
            case TimeFormatEnum.HHMMSSdot:
                return dt.ToString(@"HH.mm.ss.ffff");
            }
        }
        public static string FormatDateTime(DateTime? dateTime) {
            if (dateTime == null) return string.Empty;
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return string.Empty;
            dt = GetUserDateTime(dt);
            string fmtTime = GetFormatTimeFormat();
            string fmtDate = GetFormatDateFormat();
            return dt.ToString(fmtDate) + " " + dt.ToString(fmtTime);
        }
        public static string GetFormatDateTimeFormat(DateFormatEnum? dateFormat = null, TimeFormatEnum? timeFormat = null) {
            string fmtDate = GetFormatDateFormat(dateFormat);
            string fmtTime = GetFormatTimeFormat(timeFormat);
            return fmtDate + " " + fmtTime;
        }
        public static string FormatDateTimeYear(DateTime? dateTime) {
            if (dateTime == null) return string.Empty;
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return string.Empty;
            dt = GetUserDateTime(dt);
            return dt.Year.ToString();
        }
        public static string FormatLongDate(DateTime? dateTime) {
            if (dateTime == null) return string.Empty;
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return string.Empty;
            dt = GetUserDateTime(dt);
            string day = GetDayName(dt.DayOfWeek);
            string month = GetMonthName(dt.Month);
            return __ResStr("longDate", "{0}, {1} {2}, {3}", day, month, dt.Day, dt.Year);
        }
        public static string FormatLongDateTime(DateTime? dateTime) {
            if (dateTime == null) return string.Empty;
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return string.Empty;
            dt = GetUserDateTime(dt);
            string day = GetDayName(dt.DayOfWeek);
            string month = GetMonthName(dt.Month);
            return __ResStr("longDateTime", "{0}, {1} {2}, {3} at ", day, month, dt.Day, dt.Year) + FormatTime(dateTime);
        }
        public static string GetDayName(DayOfWeek dow) {
            if (dow == DayOfWeek.Sunday) return __ResStr("Sunday", "Sunday");
            if (dow == DayOfWeek.Monday) return __ResStr("Monday", "Monday");
            if (dow == DayOfWeek.Tuesday) return __ResStr("Tuesday", "Tuesday");
            if (dow == DayOfWeek.Wednesday) return __ResStr("Wednesday", "Wednesday");
            if (dow == DayOfWeek.Thursday) return __ResStr("Thursday", "Thursday");
            if (dow == DayOfWeek.Friday) return __ResStr("Friday", "Friday");
            if (dow == DayOfWeek.Saturday) return __ResStr("Saturday", "Saturday");
            return "???";
        }
        public static string GetDayNamesArr() {
            return __ResStr("DayNames", "Sunday,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday");
        }
        public static string GetDayName2Chars(DayOfWeek dow) {
            if (dow == DayOfWeek.Sunday) return __ResStr("Sunday2", "Su");
            if (dow == DayOfWeek.Monday) return __ResStr("Monday2", "Mo");
            if (dow == DayOfWeek.Tuesday) return __ResStr("Tuesday2", "Tu");
            if (dow == DayOfWeek.Wednesday) return __ResStr("Wednesday2", "We");
            if (dow == DayOfWeek.Thursday) return __ResStr("Thursday2", "Th");
            if (dow == DayOfWeek.Friday) return __ResStr("Friday2", "Fr");
            if (dow == DayOfWeek.Saturday) return __ResStr("Saturday2", "Sa");
            return "???";
        }
        public static string GetDayName2CharsArr() {
            return __ResStr("DayNames2", "Su,Mo,Tu,We,Th,Fr,Sa");
        }
        public static string GetMonthName(int month) {
            if (month == 1) return __ResStr("January", "January");
            if (month == 2) return __ResStr("February", "February");
            if (month == 3) return __ResStr("March", "March");
            if (month == 4) return __ResStr("April", "April");
            if (month == 5) return __ResStr("May", "May");
            if (month == 6) return __ResStr("June", "June");
            if (month == 7) return __ResStr("July", "July");
            if (month == 8) return __ResStr("August", "August");
            if (month == 9) return __ResStr("September", "September");
            if (month == 10) return __ResStr("October", "October");
            if (month == 11) return __ResStr("November", "November");
            if (month == 12) return __ResStr("December", "December");
            return "???";
        }
        public static string GetMonthNamesArr() {
            return __ResStr("MonthNames", "January,February,March,April,May,June,July,August,September,October,November,December");
        }

        public static string FormatTimeSpan(TimeSpan? timeSpan) {
            if (timeSpan == null) return string.Empty;
            TimeSpan ts = (TimeSpan)timeSpan;
            if (ts.Days > 0)
                return __ResStr("timeSpanDays", "{0} Days {1:D2}:{2:D2}:{3:D2}", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
            else
                return __ResStr("timeSpan", "{1:D2}:{2:D2}:{3:D2}", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
        }
        public static string FormatTimeSpanHM(TimeSpan? timeSpan) {
            if (timeSpan == null) return string.Empty;
            TimeSpan ts = (TimeSpan)timeSpan;
            int hours = (int)ts.TotalHours;
            return __ResStr("timeSpanHM", "{0:D2}:{1:D2}", hours, ts.Minutes);
        }

        public static string LongMBDisplay(long value, bool detailed = false) {
            if (detailed) {
                return __ResStr("strFmtMBdet", "{0:##,#} Bytes", value);
            } else {
                const long oneMeg = 1024*1204;

                string strValue;
                if (value == 0)
                    strValue = __ResStr("str0MB", "0 MB");
                else if (value < oneMeg)
                    strValue = __ResStr("strLT1MB", "< 1 MB");
                else
                    strValue = __ResStr("strFmtMB", "{0} MB", (value+(oneMeg/2)) / oneMeg);
                return strValue;
            }
        }
        public static string LongKBDisplay(long value, bool detailed = false, bool Show0 = true) {
            if (detailed) {
                return __ResStr("strFmtKBdet", "{0:##,0} Bytes", value);
            } else {
                if (value < 0)
                    return __ResStr("unknownKB", "(unknown)");
                if (value > 0 && value < 1024)
                    return __ResStr("less1KB", "< 1K");
                if (value > 1024*1024*10) // 10 MB
                    return LongMBDisplay(value, detailed);
                if (value != 0 || Show0)
                    return __ResStr("strFmtKB", "{0} KB", (long)((value + 512)/1024));
                return string.Empty;
            }
        }
        public static string FormatFileSize(long value) {
            return LongKBDisplay(value);
        }
        public static string FormatFileFolderSize(long value) {
            return LongKBDisplay(value, Show0: false);
        }

        public static string GetFormatCurrencyFormat() {
            return Manager.CurrentSite.CurrencyFormat;
        }
        public static string FormatAmount(decimal amount) {
            return amount.ToString(GetFormatCurrencyFormat());
        }

        public static string FormatSeconds(long msecs) {
            return string.Format("{0:F3} s", ((double)msecs)/1000);
        }

        public static string FormatTimeSpanInWords(TimeSpan ts) {
            int absDays = Math.Abs(ts.Days);

            int years = absDays / 365;
            if (years > 0) {
                if (ts.Days > 0)
                    return years <= 1 ? __ResStr("inYear", "in a year", years) : __ResStr("inYears", "in {0} years", years);
                else
                    return years <= 1 ? __ResStr("agoYear", "a year ago", years) : __ResStr("agoYears", "{0} years ago", years);
            }
            int months = absDays / 30;
            if (months > 0) {
                if (ts.Days > 0)
                    return months <= 1 ? __ResStr("inMonth", "in a month", months) : __ResStr("inMonths", "in {0} months", months);
                else
                    return months <= 1 ? __ResStr("agoMonth", "a month ago", months) : __ResStr("agoMonths", "{0} months ago", months);
            }
            int weeks = absDays / 7;
            if (weeks > 0) {
                if (ts.Days > 0)
                    return weeks <= 1 ? __ResStr("inWeek", "next week", weeks) : __ResStr("inWeeks", "in {0} weeks", weeks);
                else
                    return weeks <= 1 ? __ResStr("agoWeek", "last week", weeks) : __ResStr("agoWeeks", "{0} weeks ago", weeks);
            }
            if (absDays > 0) {
                if (ts.Days > 0)
                    return absDays <= 1 ? __ResStr("inDay", "tomorrow", absDays) : __ResStr("inDays", "in {0} days", absDays);
                else
                    return absDays <= 1 ? __ResStr("agoDay", "yesterday", absDays) : __ResStr("agoDays", "{0} days ago", absDays);
            }
            int absHours = Math.Abs(ts.Hours);
            if (absHours > 0) {
                if (ts.Hours > 0)
                    return absHours <= 1 ? __ResStr("inHour", "in an hour", absHours) : __ResStr("inHours", "in {0} hours", absHours);
                else
                    return absHours <= 1 ? __ResStr("agoHour", "an hour ago", absHours) : __ResStr("agoHours", "{0} hours ago", absHours);
            }
            int absMins = Math.Abs(ts.Minutes);
            if (absMins > 0) {
                if (ts.Minutes > 0)
                    return absMins <= 1 ? __ResStr("inMin", "in a minute", absMins) : __ResStr("inMins", "in {0} minutes", absMins);
                else
                    return absMins <= 1 ? __ResStr("agoMin", "a minute ago", absMins) : __ResStr("agoMins", "{0} minutes ago", absMins);
            }
            int absSecs = Math.Abs(ts.Seconds);
            if (absSecs > 10) {
                if (ts.Seconds > 0) {
                    return __ResStr("inSecs", "in {0} seconds", absSecs);
                } else {
                    return __ResStr("agoSecs", "{0} seconds ago", absSecs);
                }
            }
            if (absSecs > 0) {
                if (ts.Seconds > 0) {
                    return __ResStr("inFewSecs", "in a few seconds", absSecs);
                } else {
                    return __ResStr("agoFewSecs", "a few seconds ago", absSecs);
                }
            }
            return __ResStr("now", "now", absSecs);
        }
    }
}
