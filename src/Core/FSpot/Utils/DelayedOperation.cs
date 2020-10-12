//
// DelayedOperation.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Utils
{
    public class DelayedOperation
    {
        object syncHandle = new object ();

        public DelayedOperation (uint interval, GLib.IdleHandler op)
        {
            this.op = op;
            this.interval = interval;
        }

        public DelayedOperation (GLib.IdleHandler op)
        {
            this.op = op;
        }

        uint source;
        uint interval;

        GLib.IdleHandler op;

        bool HandleOperation ()
        {
            lock (syncHandle) {
                bool runagain = op ();
                if (!runagain)
                    source = 0;
                
                return runagain;
            }
        }

        public void Start ()
        {
            lock (syncHandle) {
                if (IsPending)
                    return;
                
                if (interval != 0)
                    source = GLib.Timeout.Add (interval, new GLib.TimeoutHandler (HandleOperation));
                else
                    source = GLib.Idle.Add (new GLib.IdleHandler (HandleOperation));
            }
        }

        public bool IsPending => (source != 0);

        public void Connect (Gtk.Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException (nameof (obj));
            obj.Destroyed += (s, e) => Stop();
        }

        public void Stop ()
        {
            lock (syncHandle) {
                if (IsPending) {
                    GLib.Source.Remove (source);
                    source = 0;
                }
            }
        }

        public void Restart ()
        {
            Stop ();
            Start ();
        }
    }
}
