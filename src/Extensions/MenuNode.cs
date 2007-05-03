/*
 * MenuNode.cs
 *
 * Author(s):
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 *
 */

using System;
using Mono.Addins;
using Mono.Unix;

namespace FSpot.Extensions
{
	[ExtensionNode ("Menu")]
	[ExtensionNodeChild (typeof (MenuItemNode))]
	[ExtensionNodeChild (typeof (ExportMenuItemNode))]
	[ExtensionNodeChild (typeof (ToolMenuItemNode))]
	[ExtensionNodeChild (typeof (MenuSeparatorNode))]
	[ExtensionNodeChild (typeof (SubmenuNode))]
	public class SubmenuNode : MenuNode
	{
		[NodeAttribute]
		string _label;

		bool changed;

		public override Gtk.MenuItem GetMenuItem ()
		{
			lock (this) {
				if (item == null || changed) {
					changed = false;
					item = new Gtk.MenuItem (_label != null ? Catalog.GetString (_label) : Id);
					Gtk.Menu submenu = new Gtk.Menu ();

					foreach (MenuNode node in ChildNodes)
						submenu.Insert (node.GetMenuItem (), -1);
					item.Submenu = submenu;
				}
			}
			return item;
		}

		protected override void OnChildrenChanged ()
		{
			lock (this) {
				changed = true;
			}
		}
	}

	[ExtensionNode ("MenuItem")]
	public class MenuItemNode : MenuNode
	{
		[NodeAttribute]
		string _label;

		public override Gtk.MenuItem GetMenuItem ()
		{
			if (item == null) {
				item = new Gtk.MenuItem (_label != null ? Catalog.GetString (_label) : Id);
				item.Activated += OnActivated;
			}
			return item;
		}

		protected virtual void OnActivated (object o, EventArgs e)
		{
			Console.WriteLine ("Item {0} activated", Id);
		}
	}

	[ExtensionNode ("MenuSeparator")]
	public class MenuSeparatorNode : MenuNode
	{
		public override Gtk.MenuItem GetMenuItem ()
		{
			if (item == null)
				item = new Gtk.SeparatorMenuItem ();

			return item;
		}
	}

	public abstract class MenuNode : ExtensionNode
	{
		protected Gtk.MenuItem item;

		public abstract Gtk.MenuItem GetMenuItem ();
	}
}
