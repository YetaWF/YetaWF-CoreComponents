/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace YetaWF.Core.Support {

    /// <summary>
    /// An instance of the TrackedEntry class describes one tracked object.
    /// This class is used by the DisposableTracker class.
    /// </summary>
    public class TrackedEntry {
        /// <summary>
        /// The object being tracked.
        /// </summary>
        public object DisposableObject { get; set; } = null!;
        /// <summary>
        /// The date/time the tracked object was created.
        /// </summary>
        public DateTime Created { get; set; }
        /// <summary>
        /// The callstack of the method that created the tracked object.
        /// </summary>
        public string? CallStack { get; set; }
    }

    /// <summary>
    /// Keeps track of objects that implement the IDisposable interface so objects that are not disposed can be located.
    ///
    /// Admin > Dashboard > Disposable Tracker (standard YetaWF site) can be used to view tracked objects.
    /// </summary>
    public static class DisposableTracker {

        private static readonly object _lock = new object();

        private static Dictionary<object, TrackedEntry> DisposableObjects = new Dictionary<object, TrackedEntry>();

        /// <summary>
        /// Returns whether objects are tracked. This is defined using AppSettings.json.
        /// </summary>
        public static bool UseTracker {
            get {
                if (_useTracker == null)
                    _useTracker = WebConfigHelper.GetValue<bool>("YetaWF_Core", "DisposableTracker");
                return (bool)_useTracker;
            }
        }
        private static bool? _useTracker = null;

        /// <summary>
        /// Adds an object to track.
        /// </summary>
        /// <param name="o">The object to track.</param>
        public static void AddObject(object o) {
            if (UseTracker) {
                lock (_lock) { // short-term lock to sync disposable objects (mainly a debug feature)
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
        /// <summary>
        /// Removes a tracked object, meaning it is being disposed.
        /// </summary>
        /// <param name="o">The object to remove from tracking.</param>
        public static void RemoveObject(object o) {
            if (UseTracker) {
                lock (_lock) { // short-term lock to sync disposable objects (mainly a debug feature)
                    if (!DisposableObjects.ContainsKey(o))
                        throw new InternalError("Disposing of disposable object which was not added to the list of disposable objects - possible duplicate Dispose() call");
                    DisposableObjects.Remove(o);
                }
            }
        }

        /// <summary>
        /// Returns a collection of all tracked objects that have not yet been disposed.
        /// </summary>
        /// <returns>Returns the collection of tracked objects that have not yet been disposed.</returns>
        public static List<TrackedEntry> GetDisposableObjects() {
            lock (_lock) {
                return (from d in DisposableObjects.Values select d).ToList();// return a copy
            }
        }

        private static string GetCallStack() {
            StringBuilder sb = new StringBuilder();
            StackTrace stackTrace = new StackTrace();
            for (int lvl = 1 ; lvl < stackTrace.FrameCount ; ++lvl) {
                StackFrame? stackFrame = stackTrace.GetFrame(lvl);
                if (stackFrame != null) {
                    MethodBase? methBase = stackFrame.GetMethod();
                    if (methBase != null) {
                        if (methBase.DeclaringType != null)
                            sb.AppendFormat(" - {0} {1}", methBase.DeclaringType.Namespace, methBase.DeclaringType.Name);
                        else
                            sb.Append(" - ?");
                    }
                }
            }
            return sb.ToString();
        }
    }
}
