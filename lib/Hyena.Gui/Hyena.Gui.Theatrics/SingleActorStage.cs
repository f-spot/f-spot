//
// SingleActorStage.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Hyena.Gui.Theatrics
{
	public class SingleActorStage : Stage<object>
	{
		object target = new object ();

		public SingleActorStage () : base ()
		{
		}

		public SingleActorStage (uint actorDuration) : base (actorDuration)
		{
		}

		protected override bool OnActorStep (Actor<object> actor)
		{
			return true;
		}

		public void Reset ()
		{
			AddOrReset (target);
		}

		public void Reset (uint duration)
		{
			AddOrReset (target, duration);
		}

		public Actor<object> Actor {
			get { return this[target]; }
		}
	}
}
