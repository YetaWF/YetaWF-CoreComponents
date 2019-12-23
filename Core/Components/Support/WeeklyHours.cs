/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Serializers;

namespace YetaWF.Core.Components {

    public class WeeklyHours {

        public const int DaysInWeek = 7;

        public SerializableList<DayTimeRange> Days { get; set; }

        [Data_DontSave]
        public DayTimeRange Mondays { get { return Days[(int)DayOfWeek.Monday]; } set { Days[(int)DayOfWeek.Monday] = value; } }
        [Data_DontSave]
        public DayTimeRange Tuesdays { get { return Days[(int)DayOfWeek.Tuesday]; } set { Days[(int)DayOfWeek.Tuesday] = value; } }
        [Data_DontSave]
        public DayTimeRange Wednesdays { get { return Days[(int)DayOfWeek.Wednesday]; } set { Days[(int)DayOfWeek.Wednesday] = value; } }
        [Data_DontSave]
        public DayTimeRange Thursdays { get { return Days[(int)DayOfWeek.Thursday]; } set { Days[(int)DayOfWeek.Thursday] = value; } }
        [Data_DontSave]
        public DayTimeRange Fridays { get { return Days[(int)DayOfWeek.Friday]; } set { Days[(int)DayOfWeek.Friday] = value; } }
        [Data_DontSave]
        public DayTimeRange Saturdays { get { return Days[(int)DayOfWeek.Saturday]; } set { Days[(int)DayOfWeek.Saturday] = value; } }
        [Data_DontSave]
        public DayTimeRange Sundays { get { return Days[(int)DayOfWeek.Sunday]; } set { Days[(int)DayOfWeek.Sunday] = value; } }

        [Data_DontSave]
        public string AdditionalFieldCaption { get; set; }
        [Data_DontSave]
        public string AdditionalFieldDescription { get; set; }

        [Data_DontSave]
        public string ClosedFieldCaption { get; set; }
        [Data_DontSave]
        public string ClosedFieldDescription { get; set; }

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
        public static WeeklyHours WorkWeek {
            get {
                WeeklyHours wk = new WeeklyHours();
                wk.Days = new SerializableList<DayTimeRange> {
                    DayTimeRange.GetClosedDay(), // Sunday
                    DayTimeRange.GetWorkDay(), // Monday
                    DayTimeRange.GetWorkDay(),
                    DayTimeRange.GetWorkDay(),
                    DayTimeRange.GetWorkDay(),
                    DayTimeRange.GetWorkDay(),
                    DayTimeRange.GetClosedDay(),// Saturday
                };
                return wk;
            }
        }
        public static WeeklyHours Always {
            get {
                DayTimeRange available = new DayTimeRange {
                    Start = new TimeOfDay(0, 0, 0),
                    End = new TimeOfDay(23, 59, 59),
                };
                WeeklyHours wk = new WeeklyHours();
                wk.Days = new SerializableList<DayTimeRange> {
                    available, // Sunday
                    available, // Monday
                    available,
                    available,
                    available,
                    available,
                    available,// Saturday
                };
                return wk;
            }
        }
        public DayTimeRange GetDayTimeRange(DayOfWeek dayOfWeek) {
            return Days[(int)dayOfWeek];
        }
        public bool IsClosedAllDay(DateTime dt) {
            return Days[(int)dt.DayOfWeek].IsClosedAllDay();
        }
        public bool IsClosed(DateTime dt) {
            return Days[(int)dt.DayOfWeek].IsClosed(dt);
        }
    }
}