/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.DataProvider.Attributes;
using YetaWF.Core.Localize;

namespace YetaWF.Core.Components {

    /// <summary>
    /// An instance of the DayTimeRange class defines up to two start/end time ranges for a specific date.
    /// </summary>
    public class DayTimeRange {

        /// <summary>
        /// Defines the date.
        /// </summary>
        //[UIHint("DateTime")] // required so controller translates this to UTC - no longer needed.
        public DateTime Date { get; set; }

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
        [Data_DontSave]
        public string? ClosedFieldCaption { get; set; }
        /// <summary>
        /// The description (tooltip) for the "Closed" checkbox. If not specified, a default is provided.
        /// </summary>
        [Data_DontSave]
        public string? ClosedFieldDescription { get; set; }
        /// <summary>
        /// The caption for the "Additional" checkbox. If not specified, a default is provided.
        /// </summary>
        [Data_DontSave]
        public string? AdditionalFieldCaption { get; set; }
        /// <summary>
        /// The description (tooltip) for the "Additional" checkbox. If not specified, a default is provided.
        /// </summary>
        [Data_DontSave]
        public string? AdditionalFieldDescription { get; set; }

        /// <summary>
        /// Constructor, initializes the Date property.
        /// </summary>
        /// <param name="date"></param>
        public DayTimeRange(DateTime date) {
            Date = date;
        }
        /// <summary>
        /// Constructor, initializes the Date property with the current user's local time.
        /// </summary>
        /// <remarks>If no date/time is specified, the current date is used.</remarks>
        public DayTimeRange() {
            Date = Formatting.GetUserDateTime(DateTime.UtcNow);// user's date with timezone
        }

        /// <summary>
        /// Retrieves the starting time of the first time start/end range.
        /// </summary>
        /// <returns>Returns the starting time.</returns>
        public DateTime? GetStart() {
            if (Start == null) return null;
            return Formatting.GetUtcDateTime(new DateTime(Date.Year, Date.Month, Date.Day, Start.Hours, Start.Minutes, Start.Seconds, DateTimeKind.Local));
        }
        /// <summary>
        /// Retrieves the ending time of the first time start/end range.
        /// </summary>
        /// <returns>Returns the ending time.</returns>
        public DateTime? GetEnd() {
            if (End == null) return null;
            return Formatting.GetUtcDateTime(new DateTime(Date.Year, Date.Month, Date.Day, End.Hours, End.Minutes, End.Seconds, DateTimeKind.Local));
        }
        /// <summary>
        /// Retrieves the starting time of the second time start/end range.
        /// </summary>
        /// <returns>Returns the starting time.</returns>
        public DateTime? GetStart2() {
            if (Start2 == null) return null;
            return Formatting.GetUtcDateTime(new DateTime(Date.Year, Date.Month, Date.Day, Start2.Hours, Start2.Minutes, Start2.Seconds, DateTimeKind.Local));
        }
        /// <summary>
        /// Retrieves the ending time of the second time start/end range.
        /// </summary>
        /// <returns>Returns the ending time.</returns>
        public DateTime? GetEnd2() {
            if (End2 == null) return null;
            return Formatting.GetUtcDateTime(new DateTime(Date.Year, Date.Month, Date.Day, End2.Hours, End2.Minutes, End2.Seconds, DateTimeKind.Local));
        }

        /// <summary>
        /// Returns whether there are no start/end time ranges.
        /// </summary>
        /// <returns>true if there are no start/end time ranges, false otherwise.</returns>
        public bool IsClosedAllDay() {
            return (Start == null && End == null && Start2 == null && End2 == null);
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
    }
}