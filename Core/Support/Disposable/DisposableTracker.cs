/* Copyright © 2017 Softel vdm, Inc. - http://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace YetaWF.Core.Support {

    public class TrackedEntry {
        public object DisposableObject { get; set; }
        public DateTime Created { get; set; }
        public string CallStack { get; set; }
    }

    public static class DisposableTracker {

        private static object _lock = new object();

        private static Dictionary<object, TrackedEntry> DisposableObjects = new Dictionary<object, TrackedEntry>();

        public static bool UseTracker {
            get {
                if (_useTracker == null)
                    _useTracker = WebConfigHelper.GetValue<bool>("YetaWF_Core", "DisposableTracker");
                return (bool)_useTracker;
            }
        }
        private static bool? _useTracker = null;

        public static void AddObject(object o) {
            if (UseTracker) {
                lock (_lock) {
                    if (DisposableObjects.ContainsKey(o))
                        throw new InternalError("New disposable object which has already been added to the list of disposable objects - possible duplicate using() { }");

                    DisposableObjects.Add(o, new Support.TrackedEntry {
                        DisposableObject = o,
                        Created = DateTime.UtcNow,
                        CallStack = GetCallStack(),
                    });
                }
            }
        }
        public static void RemoveObject(object o) {
            if (UseTracker) {
                lock (_lock) {
                    if (!DisposableObjects.ContainsKey(o))
                        throw new InternalError("Disposing of disposable object which was not added to the list of disposable objects - possible duplicate Dispose() call");
                    DisposableObjects.Remove(o);
                }
            }
        }

        public static Dictionary<object, TrackedEntry> GetDisposableObjects() { return DisposableObjects; }

        private static string GetCallStack() {
            StringBuilder sb = new StringBuilder();
            StackTrace stackTrace = new StackTrace();
            for (int lvl = 1 ; lvl < stackTrace.FrameCount ; ++lvl) {
                StackFrame stackFrame = stackTrace.GetFrame(lvl);
                MethodBase methBase = stackFrame.GetMethod();
                if (methBase.DeclaringType != null)
                    sb.AppendFormat(" - {0} {1}", methBase.DeclaringType.Namespace, methBase.DeclaringType.Name);
                else
                    sb.Append(" - ?");
            }
            return sb.ToString();
        }
    }
}
