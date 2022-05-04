//
// Pulsator.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Gui.Theatrics
{
	public class Pulsator<T> where T : class
	{
		Stage<T> stage;
		public Stage<T> Stage {
			get { return stage; }
			set {
				if (stage == value) {
					return;
				}

				if (stage != null) {
					stage.ActorStep -= OnActorStep;
				}

				stage = value;

				if (stage != null) {
					stage.ActorStep += OnActorStep;
				}
			}
		}

		T target;
		public T Target {
			get { return target; }
			set { target = value; }
		}

		public double Percent {
			get { return IsPulsing ? stage[Target].Percent : 0; }
		}

		public bool IsPulsing {
			get { return stage != null && stage.Contains (Target); }
		}

		public bool Stopping {
			get { return !IsPulsing ? true : stage[Target].CanExpire; }
		}

#pragma warning disable 0067
		// FIXME: This is to mute gmcs: https://bugzilla.novell.com/show_bug.cgi?id=360455
		public event EventHandler Pulse;
#pragma warning restore 0067

		public Pulsator ()
		{
		}

		public Pulsator (Stage<T> stage)
		{
			Stage = stage;
		}

		public void StartPulsing ()
		{
			if (!Stage.Contains (Target)) {
				Stage.Add (Target);
			}

			Stage[Target].CanExpire = false;
		}

		public void StopPulsing ()
		{
			if (Stage.Contains (Target)) {
				Stage[Target].CanExpire = true;
			}
		}

		bool OnActorStep (Actor<T> actor)
		{
			if (actor.Target == target) {
				OnPulse ();
			}

			return true;
		}

		protected virtual void OnPulse ()
		{
			Pulse?.Invoke (this, EventArgs.Empty);
		}
	}
}
