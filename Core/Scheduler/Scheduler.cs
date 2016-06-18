﻿/* Copyright © 2016 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using YetaWF.Core.Models.Attributes;
using YetaWF.Core.Packages;

namespace YetaWF.Core.Scheduler {

    public interface IScheduling {
        void RunItem(SchedulerItemBase evnt);
        SchedulerItemBase[] GetItems();
    }

    public class SchedulerSupport {

        public static Action<Package> Install { get; set; }
        public static Action<Package> Uninstall { get; set; }
        public static Action<string> RunItem { get; set; }
        public static bool Enabled { get; set; }

        static SchedulerSupport() {
            Install = DefaultInstaller;
            Uninstall = DefaultUninstaller;
            RunItem = DefaultRunItem;
            Enabled = false;
        }

        private static void DefaultInstaller(Package obj) {
            throw new NotImplementedException();
        }

        private static void DefaultUninstaller(Package obj) {
            throw new NotImplementedException();
        }

        private static void DefaultRunItem(string name) {
            throw new NotImplementedException();
        }
    }

    public class SchedulerItemBase {
        public string Name { get; set; }
        public string Description { get; set; }
        public string EventName { get; set; }
        public bool Enabled { get; set; }
        public bool EnableOnStartup { get; set; }// enable when the site is (re)started
        public bool RunOnce { get; set; }// run once, then disable
        public bool Startup { get; set; }// run at startup
        public bool SiteSpecific { get; set; } // this item runs for each site
        public SchedulerFrequency Frequency { get; set; }// run every ...
        public List<string> Log { get; set; }

        public SchedulerItemBase() {
            Frequency = new SchedulerFrequency();
            Log = new List<string>();
        }
    }

    public class SchedulerFrequency : IComparable {

        public enum TimeUnitEnum {
            [EnumDescription("Second(s)")]
            Seconds = 0,
            [EnumDescription("Minute(s)")]
            Minutes = 1,
            [EnumDescription("Hour(s)")]
            Hours = 2,
            [EnumDescription("Day(s)")]
            Days = 3,
            [EnumDescription("Week(s)")]
            Weeks = 4,
        }
        [Caption("Units"), Description("Value combined with TimeUnits defines the frequency with which the scheduler event is invoked")]
        [UIHint("Enum"), AdditionalMetadata("ShowEnumValue", false)]
        public TimeUnitEnum TimeUnits { get; set; }

        [Caption("Value"), Description("Value combined with TimeUnits defines the frequency with which the scheduler event is invoked")]
        [UIHint("IntValue4"), Range(1, 999)]
        public int Value { get; set; }

        public int CompareTo(object obj) {
            SchedulerFrequency sf = obj as SchedulerFrequency;
            if (sf == null) return -1;
            if ((int) TimeUnits < (int) sf.TimeUnits) return -1;
            if ((int) TimeUnits > (int) sf.TimeUnits) return 1;
            if (Value < sf.Value) return -1;
            if (Value > sf.Value) return 1;
            return 0;
        }

        public TimeSpan GetTimeSpan() {

            TimeSpan t;
            switch (TimeUnits) {
                case TimeUnitEnum.Weeks: t = new TimeSpan(7 * Value, 0, 0, 0, 0); break;
                case TimeUnitEnum.Days: t = new TimeSpan(Value, 0, 0, 0, 0); break;
                case TimeUnitEnum.Hours: t = new TimeSpan(Value, 0, 0); break;
                default:
                case TimeUnitEnum.Minutes: t = new TimeSpan(0, Value, 0); break;
                case TimeUnitEnum.Seconds: t = new TimeSpan(0, 0, Value); break;
            }
            return t;
        }
    }

    public class SchedulerEvent : IComparable {

        public const int MaxName = 100;
        public const int MaxImplementingAssembly = 150;
        public const int MaxImplementingType = 100;

        [Caption("Event"), Description("The event name")]
        [UIHint("Text40"), StringLength(MaxName)]
        public string Name { get; set; }

        [Caption("Assembly"), Description("The name of the assembly implementing this scheduler event")]
        [UIHint("String"), StringLength(MaxImplementingAssembly)]
        public string ImplementingAssembly { get; set; }

        [Caption("Type"), Description("The type of the assembly implementing this scheduler event")]
        [UIHint("String"), StringLength(MaxImplementingType)]
        public string ImplementingType { get; set; }

        [DontSave]// only used for UI purposes
        [Caption("Event Action"), Description("The action this event takes")]
        public string EventBuiltinDescription { get; set; }

        public int CompareTo(object obj) {
            SchedulerEvent se = obj as SchedulerEvent;
            if (se == null) return -1;
            int rc = se.Name.CompareTo(Name);
            if (rc != 0) return rc;
            rc = se.ImplementingAssembly.CompareTo(ImplementingAssembly);
            if (rc != 0) return rc;
            rc = se.ImplementingType.CompareTo(ImplementingType);
            return 0;
        }
    }
}
