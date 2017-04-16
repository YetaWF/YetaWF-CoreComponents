/* Copyright © 2017 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Localize {

    public static class Formatting {

        private static YetaWFManager Manager { get { return YetaWFManager.Manager; } }

        private static string __ResStr(string name, string defaultValue, params object[] parms) { return ResourceAccess.GetResourceString(typeof(Formatting), name, defaultValue, parms); }

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
        /// Format a date- Example "February 2010"
        /// </summary>
        public static string Date_Month_YYYY(DateTime dt)
        {
            string Months = __ResStr("strMonths", "January,February,March,April,May,June,July,August,September,October,November,December");
            string[] s = Months.Split(new char[] { ',' });
            if (s.Length != 12)
                return __ResStr("errInvMonths", "(Months string invalid)");
            return __ResStr("strMonth_YYYY", "{0} {1}", s[dt.Month-1], dt.Year);
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
            // dateTime is the user's time zone (NOT local)
            DateTime dt = new DateTime(dateTime.Ticks, DateTimeKind.Unspecified);
            TimeZoneInfo tzi = GetTimeZoneInfo();// user's timezone
            return TimeZoneInfo.ConvertTimeToUtc(dt, tzi);
        }
        /// <summary>
        /// Format date (the date has a time component (based on a timezone)
        /// </summary>
        public static string FormatDateOnlyFromUtc(DateTime? dateTime, DateFormatEnum? dateFormat = null) {
            if (dateTime == null) return "";
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return "";
            TimeZoneInfo tzi = GetTimeZoneInfo();// user's timezone
            dateTime = TimeZoneInfo.ConvertTimeFromUtc(dt, tzi);
            return FormatDate(dateTime);
        }
        /// <summary>
        /// Format date (the date has no time component and no timezone)
        /// </summary>
        public static string FormatDate(DateTime? dateTime, DateFormatEnum? dateFormat = null) {
            // dates do not use timezones. Date should always be utc normalized to midnight of the day of
            if (dateTime == null) return "";
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return "";

            string fmt = GetFormatDateFormat(dateFormat);
            return dt.ToString(fmt);
        }

        private static TimeZoneInfo GetTimeZoneInfo() {
            string tz = UserSettings.GetProperty<string>("TimeZone");
            TimeZoneInfo tzi = null;
            if (!string.IsNullOrWhiteSpace(tz))
                tzi = TimeZoneInfo.FindSystemTimeZoneById(tz);
            if (tzi == null)
                tzi = TimeZoneInfo.Local;
            return tzi;
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
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return "";
            TimeZoneInfo tzi = GetTimeZoneInfo();
            dt = TimeZoneInfo.ConvertTimeFromUtc(dt, tzi);
            string fmt = GetFormatTimeFormat(timeFormat);
            return dt.ToString(fmt);
        }
        public static string FormatTimeDetailed(DateTime? dateTime) {
            if (dateTime == null) return "";
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return "";
            TimeZoneInfo tzi = GetTimeZoneInfo();
            dt = TimeZoneInfo.ConvertTimeFromUtc(dt, tzi);
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
            if (dateTime == null) return "";
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return "";
            TimeZoneInfo tzi = GetTimeZoneInfo();
            dt = TimeZoneInfo.ConvertTimeFromUtc(dt, tzi);
            string fmt = GetFormatTimeFormat();
            return FormatDate(dt) + " " + dt.ToString(fmt);
        }
        public static string FormatDateTimeDetailed(DateTime? dateTime) {
            if (dateTime == null) return "";
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return "";
            return FormatDate(dt) + " " + FormatTimeDetailed(dt);
        }
        public static string GetFormatDateTimeFormat(DateFormatEnum? dateFormat = null, TimeFormatEnum? timeFormat = null) {
            string fmtDate = GetFormatDateFormat(dateFormat);
            string fmtTime = GetFormatTimeFormat(timeFormat);
            return fmtDate + " " + fmtTime;
        }
        public static string FormatDateTimeYear(DateTime? dateTime) {
            if (dateTime == null) return "";
            DateTime dt = (DateTime)dateTime;
            if (dt == DateTime.MinValue) return "";
            return dt.Year.ToString();
        }
        public static string FormatLongDate(DateTime? deliveryDate) {
            if (deliveryDate == null) return "";
            return string.Format("{0:D}", deliveryDate);
        }
        public static string FormatTimeSpan(TimeSpan? timeSpan) {
            if (timeSpan == null) return "";
            TimeSpan ts = (TimeSpan)timeSpan;
            if (ts.Days > 0)
                return __ResStr("timeSpanDays", "{0} Days {1:D2}:{2:D2}:{3:D2}", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
            else
                return __ResStr("timeSpan", "{1:D2}:{2:D2}:{3:D2}", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
        }

        public static string LongMBDisplay(long value, bool detailed = false) {
            if (detailed) {
                return string.Format(__ResStr("strFmtMBdet", "{0:##,#} Bytes"), value);
            } else {
                const long oneMeg = 1024*1204;

                string strValue;
                if (value == 0)
                    strValue = __ResStr("str0MB", "0 MB");
                else if (value < oneMeg)
                    strValue = __ResStr("strLT1MB", "< 1 MB");
                else
                    strValue = string.Format(__ResStr("strFmtMB", "{0} MB"), (value+(oneMeg/2)) / oneMeg);
                return strValue;
            }
        }
        public static string LongKBDisplay(long value, bool detailed = false) {
            if (detailed) {
                return string.Format(__ResStr("strFmtKBdet", "{0:##,0} Bytes"), value);
            } else {
                if (value < 0)
                    return __ResStr("unknownKB", "(unknown)");
                if (value > 0 && value < 1024)
                    return __ResStr("less1KB", "< 1K");
                if (value > 1024*1024*10) // 10 MB
                    return LongMBDisplay(value, detailed);
                return string.Format(__ResStr("strFmtKB", "{0} KB"), (long)((value + 512)/1024));
            }
        }
        public static string FormatFileSize(long value) {
            return LongKBDisplay(value);
        }

        public static string GetFormatCurrencyFormat() {
            string cf = Manager.CurrentSite.CurrencyFormat;
            if (string.IsNullOrWhiteSpace(cf))
                return Globals.DefaultCurrencyFormat;
            return cf;
        }
        public static string FormatAmount(decimal amount) {
            return amount.ToString(GetFormatCurrencyFormat());
        }

        public static string FormatSeconds(long msecs) {
            return string.Format("{0:F3} s", ((double)msecs)/1000);
        }
    }
}
