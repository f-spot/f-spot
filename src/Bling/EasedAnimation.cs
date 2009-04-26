//
// FSpot.Bling.EasedAnimation,cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// This is free software. See COPYING for details
//

//TODO: send the progresschanged event

using System;
using System.ComponentModel;

namespace FSpot.Bling
{
	public abstract class EasedAnimation<T>
	{
		enum AnimationState {
			NotRunning = 0,
			Running,
			Paused,
		}

		TimeSpan duration;
		TimeSpan pausedafter;

		DateTimeOffset starttime;
		T from;
		T to;
		Action<T> action;
		AnimationState state;
		EasingMode easingmode;

		public EasedAnimation ()
		{
			from = default (T);
			to = default (T);
			duration = TimeSpan.Zero;
			action = null;
			state = AnimationState.NotRunning;
			easingmode = EasingMode.In;
		}

		public EasedAnimation (T from, T to, TimeSpan duration, Action<T> action)
		{
			this.from = from;
			this.to = to;
			this.duration = duration;
			this.action = action;
			state = AnimationState.NotRunning;
			easingmode = EasingMode.In;
		}

		public void Pause ()
		{
			if (state == AnimationState.Paused)
				return;
			if (state != AnimationState.NotRunning)
				throw new InvalidOperationException ("Can't Pause () a non running animation");
			GLib.Idle.Remove (Handler);
			pausedafter = DateTimeOffset.Now - starttime;
			state = AnimationState.Paused;
		}

		public void Resume ()
		{
			if (state == AnimationState.Running)
				return;
			if (state != AnimationState.Paused)
				throw new InvalidOperationException ("Can't Resume a non running animation");
			starttime = DateTimeOffset.Now - pausedafter;
			state = AnimationState.Running;
			GLib.Idle.Add (Handler);
		}

		public void Start ()
		{
			if (state != AnimationState.NotRunning)
				throw new InvalidOperationException ("Can't Start() a running or paused animation");
			starttime = DateTimeOffset.Now;
			state = AnimationState.Running;
			GLib.Idle.Add (Handler);
		}

		public void Stop ()
		{
			if (state == AnimationState.NotRunning)
				return;
			else
				GLib.Idle.Remove (Handler);
			action (to);
			EventHandler h = Completed;
			if (h != null)
				Completed (this, EventArgs.Empty);
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

		public double Ease (double normalizedTime)
		{
			switch (easingmode) {
			case EasingMode.In:
				return EaseInCore (normalizedTime);
			case EasingMode.Out:
				return 1.0 - EaseInCore (1 - normalizedTime);
			case EasingMode.InOut:
				return (normalizedTime <= 0.5
					? EaseInCore (normalizedTime * 2) * 0.5
					: 1.0 - EaseInCore ((1 - normalizedTime) * 2) * 0.5);
			}
			throw new InvalidOperationException ("Unknown value for EasingMode");
		}

		bool Handler ()
		{
			double percentage = (double)(DateTimeOffset.Now - starttime ).Ticks / (double)duration.Ticks;
			action (Interpolate (from, to, Ease (Math.Min (percentage, 1.0))));
			if (percentage >= 1.0) {
				EventHandler h = Completed;
				if (h != null)
					Completed (this, EventArgs.Empty);
				state = AnimationState.NotRunning;
				return false;
			}
			return true;
		}

		protected abstract double EaseInCore (double percentage);
		protected abstract T Interpolate (T from, T to, double progress);

		public event EventHandler Completed;
		public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

		public EasingMode EasingMode {
			get { return easingmode; }
			set { easingmode = value; }
		}

		public bool IsRunning {
			get { return state == AnimationState.Running; }
		}

		public bool IsPaused {
			get { return state == AnimationState.Paused; }
		}

		public TimeSpan Duration {
			get { return duration; }
			set { duration = value; }
		}

		public T From {
			get { return from; }
			set { from = value; }
		}

		public T To {
			get { return to; }
			set { to = value; }
		}
	}
}
