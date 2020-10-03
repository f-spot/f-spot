//
// ComplexMenuItemNode.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Mono.Addins;

using Hyena.Widgets;

namespace FSpot.Extensions
{
	[ExtensionNode ("ComplexMenuItem")]
	public class ComplexMenuItemNode : MenuNode
	{
		[NodeAttribute]
		protected string widget_type { get; set; }

		[NodeAttribute]
		protected string command_type { get; set; }

		ICommand cmd;

		public override Gtk.MenuItem GetMenuItem (object parent)
		{
			var item = Activator.CreateInstance (Type.GetType (widget_type), parent) as ComplexMenuItem;
			cmd = (ICommand)Addin.CreateInstance (command_type);

			if (item != null)
				item.Activated += OnActivated;
			return item;
		}

		void OnActivated (object o, EventArgs e)
		{
			if (cmd != null)
				cmd.Run (o, e);
		}
	}
}
