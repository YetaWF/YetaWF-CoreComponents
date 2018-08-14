/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;

namespace YetaWF.Core.Components {

    public class DayTimeRange {

        [UIHint("DateTime")] // required so controller translates this to Utc
        public DateTime Date { get; set; }

        public TimeOfDay Start { get; set; }
        public TimeOfDay End { get; set; }
        public TimeOfDay Start2 { get; set; }
        public TimeOfDay End2 { get; set; }

        [Data_DontSave]
        public string ClosedFieldCaption { get; set; }
        [Data_DontSave]
        public string ClosedFieldDescription { get; set; }
        [Data_DontSave]
        public string AdditionalFieldCaption { get; set; }
        [Data_DontSave]
        public string AdditionalFieldDescription { get; set; }

        public DayTimeRange(DateTime date) {
            Date = date;
        }
        public DayTimeRange() {
            Date = Formatting.GetLocalDateTime(DateTime.UtcNow);// user's date with timezone
        }

        public DateTime GetStart() {
            return Formatting.GetUtcDateTime(new DateTime(Date.Year, Date.Month, Date.Day, Start.Hours, Start.Minutes, Start.Seconds, DateTimeKind.Local));
        }
        public DateTime GetEnd() {
            return Formatting.GetUtcDateTime(new DateTime(Date.Year, Date.Month, Date.Day, End.Hours, End.Minutes, End.Seconds, DateTimeKind.Local));
        }
        public DateTime GetStart2() {
            return Formatting.GetUtcDateTime(new DateTime(Date.Year, Date.Month, Date.Day, Start2.Hours, Start2.Minutes, Start2.Seconds, DateTimeKind.Local));
        }
        public DateTime GetEnd2() {
            return Formatting.GetUtcDateTime(new DateTime(Date.Year, Date.Month, Date.Day, End2.Hours, End2.Minutes, End2.Seconds, DateTimeKind.Local));
        }

        public bool IsClosedAllDay() {
            return (Start == null && End == null && Start2 == null && End2 == null);
        }
        public bool IsClosed(DateTime dt) {
            TimeOfDay tod = new TimeOfDay(dt);
            if (Start != null && End != null)
                if (tod >= Start && tod < End) return false;
            if (Start2 != null && End2 != null)
                if (tod >= Start2 && tod < End2) return false;
            return true;
        }
        public static DayTimeRange GetWorkDay() {
            return new DayTimeRange {
                Start = new TimeOfDay(9, 0, 0), // 9 am
                End = new TimeOfDay(17, 0, 0), // 5 pm
            };
        }
        public static DayTimeRange GetClosedDay() {
            return new DayTimeRange();
        }
    }
}