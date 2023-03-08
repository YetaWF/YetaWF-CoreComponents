/* Copyright © 2023 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Text.Json.Serialization;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Support;

namespace YetaWF.Core.Components {

    /// <summary>
    /// An instance of the DayTimeRange class defines up to two start/end time ranges for a specific date.
    /// </summary>
    public class DayTimeRange {

        private readonly int BaseYear = 2000;
        private readonly int BaseMonth = 1;
        private readonly int BaseDay = 1;

        /// <summary>
        /// Defines the starting time of the first time start/end range.
        /// May be null if there is no first start/end range.
        /// </summary>
        public TimeOfDay? Start { get; set; }
        /// <summary>
        /// Defines the ending time of the first time start/end range.
        /// May be null if there is no first start/end range.
        /// </summary>
        public TimeOfDay? End { get; set; }
        /// <summary>
        /// Defines the starting time of the second time start/end range.
        /// May be null if there is no second start/end range.
        /// </summary>
        public TimeOfDay? Start2 { get; set; }
        /// <summary>
        /// Defines the ending time of the second time start/end range.
        /// May be null if there is no second start/end range.
        /// </summary>
        public TimeOfDay? End2 { get; set; }

        /// <summary>
        /// The caption for the "Closed" checkbox. If not specified, a default is provided.
        /// </summary>
        [Data_DontSave, JsonIgnore]
        public string? ClosedFieldCaption { get; set; }
        /// <summary>
        /// The description (tooltip) for the "Closed" checkbox. If not specified, a default is provided.
        /// </summary>
        [Data_DontSave, JsonIgnore]
        public string? ClosedFieldDescription { get; set; }
        /// <summary>
        /// The caption for the "Additional" checkbox. If not specified, a default is provided.
        /// </summary>
        [Data_DontSave, JsonIgnore]
        public string? AdditionalFieldCaption { get; set; }
        /// <summary>
        /// The description (tooltip) for the "Additional" checkbox. If not specified, a default is provided.
        /// </summary>
        [Data_DontSave, JsonIgnore]
        public string? AdditionalFieldDescription { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public DayTimeRange() { }

        /// <summary>
        /// Returns whether there are no start/end time ranges.
        /// </summary>
        /// <returns>true if there are no start/end time ranges, false otherwise.</returns>
        public bool IsClosedAllDay() {
            return Start == null && End == null && Start2 == null && End2 == null;
        }
        /// <summary>
        /// Given a date/time, returns whether the current time is outside one of the start/end time ranges.
        /// </summary>
        /// <param name="dt">The date/time to test.</param>
        /// <returns>true if the specified date/time is outside one of the start/end time ranges, false otherwise.</returns>
        public bool IsClosed(DateTime dt) {
            TimeOfDay tod = new TimeOfDay(dt);
            if (Start != null && End != null)
                if (tod >= Start && tod < End) return false;
            if (Start2 != null && End2 != null)
                if (tod >= Start2 && tod < End2) return false;
            return true;
        }
        /// <summary>
        /// Returns a DayTimeRange for a typical workday (9 am - 5 pm).
        /// </summary>
        /// <returns>Returns a DayTimeRange for a typical workday (9 am - 5 pm).</returns>
        public static DayTimeRange GetWorkDay() {
            return new DayTimeRange {
                Start = new TimeOfDay(9, 0, 0), // 9 am
                End = new TimeOfDay(17, 0, 0), // 5 pm
            };
        }
        /// <summary>
        /// Returns a DayTimeRange for a typical weekend day/holiday.
        /// </summary>
        /// <returns>Returns a DayTimeRange for a typical weekend day/holiday.</returns>
        public static DayTimeRange GetClosedDay() {
            return new DayTimeRange();
        }

        public TimeOfDay GetTimeStart() {
            DateTime dt = new DateTime(BaseYear, BaseMonth, BaseDay, Start?.Hours ?? 0, Start?.Minutes ?? 0, Start?.Seconds ?? 0, DateTimeKind.Utc);
            return new TimeOfDay(dt.Add(- YetaWFManager.Manager.GetTimeZoneInfo().BaseUtcOffset));
        }
        public TimeOfDay GetTimeEnd() {
            DateTime dt = new DateTime(BaseYear, BaseMonth, BaseDay, End?.Hours ?? 0, End?.Minutes ?? 0, End?.Seconds ?? 0, DateTimeKind.Utc);
            return new TimeOfDay(dt.Add(- YetaWFManager.Manager.GetTimeZoneInfo().BaseUtcOffset));
        }
        public TimeOfDay GetTimeStart2() {
            DateTime dt = new DateTime(BaseYear, BaseMonth, BaseDay, Start2?.Hours ?? 0, Start2?.Minutes ?? 0, Start2?.Seconds ?? 0, DateTimeKind.Utc);
            return new TimeOfDay(dt.Add(- YetaWFManager.Manager.GetTimeZoneInfo().BaseUtcOffset));
        }
        public TimeOfDay GetTimeEnd2() {
            DateTime dt = new DateTime(BaseYear, BaseMonth, BaseDay, End2?.Hours ?? 0, End2?.Minutes ?? 0, End2?.Seconds ?? 0, DateTimeKind.Utc);
            return new TimeOfDay(dt.Add(- YetaWFManager.Manager.GetTimeZoneInfo().BaseUtcOffset));
        }
    }
}