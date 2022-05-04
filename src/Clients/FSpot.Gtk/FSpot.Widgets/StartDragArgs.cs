//
// SelectionCollectionGridView.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gdk;

namespace FSpot.Widgets
{
	public class StartDragArgs
	{
		public Event Event { get; private set; }

		public uint Button { get; private set; }

		public StartDragArgs (uint but, Event evt)
		{
			Button = but;
			Event = evt;
		}
	}
}
