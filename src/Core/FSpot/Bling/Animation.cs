//
// Animation.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

//TODO: send the progresschanged event

using System;
using System.ComponentModel;

using GLibBeans;

namespace FSpot.Bling
{
	public abstract class Animation<T>
	{
		enum AnimationState {
			NotRunning = 0,
			Running,
			Paused,
		}

		TimeSpan pausedafter;

		DateTimeOffset starttime;
		readonly Action<T> action;
		AnimationState state;
		readonly GLib.Priority priority = GLib.Priority.DefaultIdle;

		protected Animation ()
		{
			From = default;
			To = default;
			Duration = TimeSpan.Zero;
			action = null;
			state = AnimationState.NotRunning;
		}

		protected Animation (T from, T to, TimeSpan duration, Action<T> action) : this (from, to, duration, action, GLib.Priority.DefaultIdle)
		{
		}

		protected Animation (T from, T to, TimeSpan duration, Action<T> action, GLib.Priority priority)
		{
			From = from;
			To = to;
			Duration = duration;
			this.action = action;
			this.priority = priority;
			state = AnimationState.NotRunning;
		}

		public void Pause ()
		{
			if (state == AnimationState.Paused)
				return;
			if (state != AnimationState.NotRunning)
				throw new InvalidOperationException ("Can't Pause () a non running animation.");
			GLib.Idle.Remove (Handler);
			pausedafter = DateTimeOffset.Now - starttime;
			state = AnimationState.Paused;
		}

		public void Resume ()
		{
			if (state == AnimationState.Running)
				return;
			if (state != AnimationState.Paused)
				throw new InvalidOperationException ("Can't Resume () a non running animation.");
			starttime = DateTimeOffset.Now - pausedafter;
			state = AnimationState.Running;
			Sources.SetPriority (GLib.Timeout.Add (40, Handler), priority);
		}

		public void Start ()
		{
			if (state != AnimationState.NotRunning)
				throw new InvalidOperationException ("Can't Start () a running or paused animation.");
			starttime = DateTimeOffset.Now;
			state = AnimationState.Running;
			Sources.SetPriority (GLib.Timeout.Add (40, Handler), priority);
		}

		public void Stop ()
		{
			if (state == AnimationState.NotRunning)
				return;

			GLib.Idle.Remove (Handler);
			action (To);
			Completed?.Invoke (this, EventArgs.Empty);
			state = AnimationState.NotRunning;
		}

		public void Restart ()
		{
			if (state == AnimationState.NotRunning) {
				Start ();
				return;
			}
			if (state == AnimationState.Paused) {
				Resume ();
				return;
			}
			starttime = DateTimeOffset.Now;
		}

		protected virtual double Ease (double normalizedTime)
		{
			return normalizedTime;
		}

		bool Handler ()
		{
			double percentage = (double)(DateTimeOffset.Now - starttime).Ticks / (double)Duration.Ticks;
			ProgressChanged?.Invoke (this, new ProgressChangedEventArgs ((int)(100 * percentage), null));

			action (Interpolate (From, To, Ease (Math.Min (percentage, 1.0))));
			if (percentage >= 1.0) {
				Completed?.Invoke (this, EventArgs.Empty);
				state = AnimationState.NotRunning;
				return false;
			}
			return true;
		}

		protected abstract T Interpolate (T from, T to, double progress);

// Do not error out on this not being used
#pragma warning disable 67
		public event EventHandler Completed;
#pragma warning restore 67
		public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

		public bool IsRunning => state == AnimationState.Running;

		public bool IsPaused => state == AnimationState.Paused;

		public TimeSpan Duration { get; set; }

		public T From { get; set; }

		public T To { get; set; }
	}
}
