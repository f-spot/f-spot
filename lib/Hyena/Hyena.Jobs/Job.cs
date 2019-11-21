//
// Job.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Hyena.Jobs
{
    public enum JobState {
        None,
        Scheduled,
        Running,
        Paused,
        Cancelled,
        Completed
    };

    public class Job
    {
        public event EventHandler Updated;
        public event EventHandler Finished;
        public event EventHandler CancelRequested;

        private int update_freeze_ref;
        private JobState state = JobState.None;

        private ManualResetEvent pause_event;
        private DateTime created_at = DateTime.Now;
        private TimeSpan run_time = TimeSpan.Zero;
        private Object sync = new Object ();

        public bool IsCancelRequested { get; private set; }

#region Internal Properties

        internal bool IsScheduled {
            get { return state == JobState.Scheduled; }
        }

        internal bool IsRunning {
            get { return state == JobState.Running; }
        }

        internal bool IsPaused {
            get { return state == JobState.Paused; }
        }

        public bool IsFinished {
            get {
                lock (sync) {
                    return state == JobState.Cancelled || state == JobState.Completed;
                }
            }
        }

        internal DateTime CreatedAt {
            get { return created_at; }
        }

        internal TimeSpan RunTime {
            get { return run_time; }
        }

#endregion

#region Scheduler Methods

        internal void Start ()
        {
            Log.Debug ("Starting", Title);
            lock (sync) {
                if (state != JobState.Scheduled && state != JobState.Paused) {
                    Log.DebugFormat ("Job {0} in {1} state is not runnable", Title, state);
                    return;
                }

                State = JobState.Running;

                if (pause_event != null) {
                    pause_event.Set ();
                }

                RunJob ();
            }
        }

        internal void Cancel ()
        {
            lock (sync) {
                if (!IsFinished) {
                    IsCancelRequested = true;
                    State = JobState.Cancelled;
                    EventHandler handler = CancelRequested;
                    if (handler != null) {
                        handler (this, EventArgs.Empty);
                    }
                }
            }
            Log.Debug ("Canceled", Title);
        }

        internal void Preempt ()
        {
            Log.Debug ("Preempting", Title);
            Pause (false);
        }

        internal bool Pause ()
        {
            Log.Debug ("Pausing ", Title);
            return Pause (true);
        }

        private bool Pause (bool unschedule)
        {
            lock (sync) {
                if (IsFinished) {
                    Log.DebugFormat ("Job {0} in {1} state is not pausable", Title, state);
                    return false;
                }

                State = unschedule ? JobState.Paused : JobState.Scheduled;
                if (pause_event != null) {
                    pause_event.Reset ();
                }
            }

            return true;
        }

#endregion

        private string title;
        private string status;
        private string [] icon_names;
        private double progress;

#region Public Properties

        public string Title {
            get { return title; }
            set {
                title = value;
                OnUpdated ();
            }
        }

        public string Status {
            get { return status; }
            set {
                status = value;
                OnUpdated ();
            }
        }

        public double Progress {
            get { return progress; }
            set {
                progress = Math.Max (0.0, Math.Min (1.0, value));
                OnUpdated ();
            }
        }

        public string [] IconNames {
            get { return icon_names; }
            set {
                if (value != null) {
                    icon_names = value;
                    OnUpdated ();
                }
            }
        }

        public bool IsBackground { get; set; }
        public bool CanCancel { get; set; }
        public string CancelMessage { get; set; }
        public bool DelayShow { get; set; }

        public PriorityHints PriorityHints { get; set; }

        // Causes runtime method-not-found error in mono 2.0.1
        //public IEnumerable<Resource> Resources { get; protected set; }
        internal Resource [] Resources;

        public JobState State {
            get { return state; }
            internal set {
                state = value;
                OnUpdated ();
            }
        }

        public void SetResources (params Resource [] resources)
        {
            Resources = resources ?? new Resource [0];
        }

#endregion

#region Constructor

        public Job () : this (null, PriorityHints.None)
        {
        }

        public Job (string title, PriorityHints hints, params Resource [] resources)
        {
            Title = title;
            PriorityHints = hints;
            SetResources (resources);
        }

#endregion

#region Abstract Methods

        protected virtual void RunJob ()
        {
        }

#endregion

#region Protected Methods

        public void Update (string title, string status, double progress)
        {
            Title = title;
            Status = status;
            Progress = progress;
        }

        protected void FreezeUpdate ()
        {
            System.Threading.Interlocked.Increment (ref update_freeze_ref);
        }

        protected void ThawUpdate (bool raiseUpdate)
        {
            System.Threading.Interlocked.Decrement (ref update_freeze_ref);
            if (raiseUpdate) {
                OnUpdated ();
            }
        }

        protected void OnUpdated ()
        {
            if (update_freeze_ref != 0) {
                return;
            }

            EventHandler handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public void YieldToScheduler ()
        {
            if (IsPaused || IsScheduled) {
                if (pause_event == null) {
                    pause_event = new ManualResetEvent (false);
                }

                pause_event.WaitOne ();
            }
        }

        protected void OnFinished ()
        {
            Log.Debug ("Finished", Title);
            pause_event = null;

            if (state != JobState.Cancelled) {
                State = JobState.Completed;
            }

            EventHandler handler = Finished;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

#endregion

        internal bool HasScheduler { get; set; }
    }
}
