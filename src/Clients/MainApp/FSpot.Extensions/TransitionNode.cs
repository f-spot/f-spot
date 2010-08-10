/*
 * FSpot.Extensions.TransitionNode.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * Copyright (c) 2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 *
 */

using System;
using Mono.Addins;
using Gdk;
using FSpot.Transitions;

namespace FSpot.Extensions
{
	public class TransitionNode : ExtensionNode
	{
		[NodeAttribute ("transition_type", true)]
		protected string class_name;

		SlideShowTransition transition = null;
		public SlideShowTransition Transition {
			get {
				if (transition == null)
					transition = Addin.CreateInstance (class_name) as SlideShowTransition;
				return transition;
			}
		}
	}
}
