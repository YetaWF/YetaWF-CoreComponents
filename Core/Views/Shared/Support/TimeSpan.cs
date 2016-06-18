/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Pages;

namespace YetaWF.Core.Views.Shared {

    public class TimeSpanUI {
        public TimeSpanUI() { }
        public TimeSpanUI(TimeSpan ts) { Span = ts; }

        private TimeSpan Span = new TimeSpan();

        [Caption("Days"), Description("Number of days")]
        [UIHint("IntValue4"), Required, Range(0,999999)]
        public int Days { get { return Span.Days; } set { } }
        [Caption("Hours"), Description("Number of hours")]
        [UIHint("IntValue2"), Required, Range(0, 23)]
        public int Hours { get { return Span.Hours; } set { } }
        [Caption("Minutes"), Description("Number of minutes")]
        [UIHint("IntValue2"), Required, Range(0, 59)]
        public int Minutes { get { return Span.Minutes; } set { } }
        [Caption("Seconds"), Description("Number of seconds")]
        [UIHint("IntValue2"), Required, Range(0, 59)]
        public int Seconds { get { return Span.Seconds; } set { } }
    }

    public class TimeSpan<TModel> : RazorTemplate<TModel> { }

    public static class TimeSpanHelper {

    }
}
