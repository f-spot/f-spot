//
// TransitionNode.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Transitions;

using Mono.Addins;

namespace FSpot.Extensions
{
	public class TransitionNode : ExtensionNode
	{
		[NodeAttribute ("transition_type", true)]
		protected string class_name;

		SlideShowTransition transition;
		public SlideShowTransition Transition {
			get {
				if (transition == null)
					transition = Addin.CreateInstance (class_name) as SlideShowTransition;
				return transition;
			}
		}
	}
}