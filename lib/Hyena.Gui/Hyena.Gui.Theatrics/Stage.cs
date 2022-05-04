//
// Stage.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Hyena.Gui.Theatrics
{
	public class Stage<T>
	{
		public delegate bool ActorStepHandler (Actor<T> actor);

		Dictionary<T, Actor<T>> actors = new Dictionary<T, Actor<T>> ();
		uint timeout_id;

		uint update_frequency = 30;
		uint default_duration = 1000;
		bool playing = true;

		public event ActorStepHandler ActorStep;

#pragma warning disable 0067
		// FIXME: This is to mute gmcs: https://bugzilla.novell.com/show_bug.cgi?id=360455
		public event EventHandler Iteration;
#pragma warning restore 0067

		public Stage ()
		{
		}

		public Stage (uint actorDuration)
		{
			default_duration = actorDuration;
		}

		public Actor<T> this[T target] {
			get {
				if (actors.ContainsKey (target)) {
					return actors[target];
				}

				return null;
			}
		}

		public bool Contains (T target)
		{
			return actors.ContainsKey (target);
		}

		public Actor<T> Add (T target)
		{
			lock (this) {
				return Add (target, default_duration);
			}
		}

		public Actor<T> Add (T target, uint duration)
		{
			lock (this) {
				if (Contains (target)) {
					throw new InvalidOperationException ("Stage already contains this actor");
				}

				var actor = new Actor<T> (target, duration);
				actors.Add (target, actor);

				CheckTimeout ();

				return actor;
			}
		}

		public Actor<T> AddOrReset (T target)
		{
			lock (this) {
				return AddOrResetCore (target, null);
			}
		}

		public Actor<T> AddOrReset (T target, uint duration)
		{
			lock (this) {
				return AddOrResetCore (target, duration);
			}
		}

		Actor<T> AddOrResetCore (T target, uint? duration)
		{
			lock (this) {
				if (Contains (target)) {
					Actor<T> actor = this[target];

					if (duration == null) {
						actor.Reset ();
					} else {
						actor.Reset (duration.Value);
					}

					CheckTimeout ();

					return actor;
				}

				return Add (target);
			}
		}

		public void Reset (T target)
		{
			lock (this) {
				ResetCore (target, null);
			}
		}

		public void Reset (T target, uint duration)
		{
			lock (this) {
				ResetCore (target, duration);
			}
		}

		void ResetCore (T target, uint? duration)
		{
			lock (this) {
				if (!Contains (target)) {
					throw new InvalidOperationException ("Stage does not contain this actor");
				}

				CheckTimeout ();

				if (duration == null) {
					this[target].Reset ();
				} else {
					this[target].Reset (duration.Value);
				}
			}
		}

		void CheckTimeout ()
		{
			if ((!Playing || actors.Count == 0) && timeout_id > 0) {
				GLib.Source.Remove (timeout_id);
				timeout_id = 0;
				return;
			} else if (Playing && actors.Count > 0 && timeout_id <= 0) {
				timeout_id = GLib.Timeout.Add (update_frequency, OnTimeout);
				return;
			}
		}

		bool OnTimeout ()
		{
			if (!Playing || this.actors.Count == 0) {
				timeout_id = 0;
				return false;
			}

			var actors = new Queue<Actor<T>> (this.actors.Values);
			while (actors.Count > 0) {
				Actor<T> actor = actors.Dequeue ();
				actor.Step ();

				if (!OnActorStep (actor) || actor.Expired) {
					this.actors.Remove (actor.Target);
				}
			}

			OnIteration ();

			return true;
		}

		protected virtual bool OnActorStep (Actor<T> actor)
		{
			ActorStepHandler handler = ActorStep;
			if (handler != null) {
				bool result = true;
				foreach (ActorStepHandler del in handler.GetInvocationList ()) {
					result &= del (actor);
				}
				return result;
			}
			return false;
		}

		protected virtual void OnIteration ()
		{
			Iteration?.Invoke (this, EventArgs.Empty);
		}

		public void Play ()
		{
			lock (this) {
				Playing = true;
			}
		}

		public void Pause ()
		{
			lock (this) {
				Playing = false;
			}
		}

		public void Exeunt ()
		{
			lock (this) {
				actors.Clear ();
				CheckTimeout ();
			}
		}

		public uint DefaultActorDuration {
			get { return default_duration; }
			set { lock (this) { default_duration = value; } }
		}

		public bool Playing {
			get { return playing; }
			set {
				lock (this) {
					if (playing == value) {
						return;
					}

					playing = value;
					CheckTimeout ();
				}
			}
		}

		public uint UpdateFrequency {
			get { return update_frequency; }
			set {
				lock (this) {
					bool _playing = Playing;
					update_frequency = value;
					Playing = _playing;
				}
			}
		}

		public int ActorCount {
			get { return actors.Count; }
		}
	}
}
