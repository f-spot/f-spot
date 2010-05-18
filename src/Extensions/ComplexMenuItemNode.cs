/*
 * FSpot.Extensions.ComplexMenuItemNode
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using Mono.Addins;
using FSpot.Widgets;
using System;

namespace FSpot.Extensions
{
	[ExtensionNode ("ComplexMenuItem")]
	public class ComplexMenuItemNode : MenuNode
	{
		[NodeAttribute]
		protected string widget_type;

		[NodeAttribute]
		protected string command_type;

		ICommand cmd;

		public override Gtk.MenuItem GetMenuItem (object parent)
		{
			ComplexMenuItem item = System.Activator.CreateInstance (Type.GetType (widget_type), parent) as ComplexMenuItem;
			cmd = (ICommand) Addin.CreateInstance (command_type);
			
			if (item != null)
				item.Changed += OnChanged;
			return item;
		}

		private void OnChanged (object o, EventArgs e)
		{
			if (cmd != null)
				cmd.Run (o, e);
		}
	}

}
