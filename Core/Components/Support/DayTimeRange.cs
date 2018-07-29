/* Copyright © 2018 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Localize;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Serializers;

namespace YetaWF.Core.Components {

    public class WeeklyHours {

        public const int DaysInWeek = 7;
        public SerializableList<DayTimeRange> Days { get; set; }

        public WeeklyHours() {
            Days = new SerializableList<DayTimeRange> {
                DayTimeRange.GetClosedDay(), // Sunday
                DayTimeRange.GetClosedDay(), // Monday
                DayTimeRange.GetClosedDay(),
                DayTimeRange.GetClosedDay(),
                DayTimeRange.GetClosedDay(),
                DayTimeRange.GetClosedDay(),
                DayTimeRange.GetClosedDay(),
            };
        }
    }

    public class DayTimeRange {

        [UIHint("Time")] // required so controller translates this to Utc
        public DateTime? Start { get; set; }
        [UIHint("Time")]
        public DateTime? End { get; set; }
        [UIHint("Time")]
        public DateTime? Start2 { get; set; }
        [UIHint("Time")]
        public DateTime? End2 { get; set; }

        public DayTimeRange() { }
        public bool IsClosed() {
            return (Start == null && End == null && Start2 == null && End2 == null);
        }
        public static DayTimeRange GetWorkDay() {
            return new DayTimeRange {
                Start = Formatting.GetUtcDateTime(new DateTime(1, 1, 1, 9, 0, 0)), // local 9 am
                End = Formatting.GetUtcDateTime(new DateTime(1, 1, 1, 17, 0, 0)), // local 5 pm
            };
        }
        public static DayTimeRange GetClosedDay() {
            return new DayTimeRange();
        }
    }
}