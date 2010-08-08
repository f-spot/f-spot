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
	[ExtensionNodeChild (typeof (CommandMenuItemNode))]
	[ExtensionNodeChild (typeof (MenuSeparatorNode))]
	[ExtensionNodeChild (typeof (SubmenuNode))]
	[ExtensionNodeChild (typeof (MenuGeneratorNode))]
	[ExtensionNodeChild (typeof (ComplexMenuItemNode))]
	public class SubmenuNode : MenuNode
	{
		public override Gtk.MenuItem GetMenuItem (object parent)
		{
			Gtk.MenuItem item = base.GetMenuItem (parent);

			Gtk.Menu submenu = GetSubmenu (parent);

			if (item.Submenu != null)
				item.Submenu.Dispose ();

			item.Submenu = submenu;
			return item;
		}

		public Gtk.Menu GetSubmenu ()
		{
			return GetSubmenu (null);
		}

		public Gtk.Menu GetSubmenu (object parent)
		{
			Gtk.Menu submenu = new Gtk.Menu ();

			foreach (MenuNode node in ChildNodes)
				submenu.Insert (node.GetMenuItem (parent), -1);

			return submenu;
		}
	}

	[ExtensionNode ("MenuGenerator")]
	public class MenuGeneratorNode : MenuNode
	{
		[NodeAttribute ("generator_type", true)]
		protected string command_type;

		private IMenuGenerator menu_generator;

		public override Gtk.MenuItem GetMenuItem (object parent)
		{
			Gtk.MenuItem item = base.GetMenuItem (parent);
			menu_generator = (IMenuGenerator) Addin.CreateInstance (command_type);
			item.Submenu = menu_generator.GetMenu ();
			item.Activated += menu_generator.OnActivated;
			return item;
		}
	}

	[ExtensionNode ("MenuItem")]
	public class MenuItemNode : MenuNode
	{
		public override Gtk.MenuItem GetMenuItem (object parent)
		{
			Gtk.MenuItem item = base.GetMenuItem (parent);
			item.Activated += OnActivated;
			return item;
		}

		protected virtual void OnActivated (object o, EventArgs e)
		{
		}
	}

	[ExtensionNode ("MenuSeparator")]
	public class MenuSeparatorNode : MenuNode
	{
		public override Gtk.MenuItem GetMenuItem (object parent)
		{
			return new Gtk.SeparatorMenuItem ();
		}
	}

	public abstract class MenuNode : ExtensionNode
	{
		[NodeAttribute (Localizable=true)]
		protected string _label;

		[NodeAttribute]
		protected string icon;

		public virtual Gtk.MenuItem GetMenuItem (object parent)
		{
			Gtk.MenuItem item;
			if (icon == null)
				item = new Gtk.MenuItem (_label != null ? Catalog.GetString (_label) : Id);
			else {
				item = new Gtk.ImageMenuItem (_label != null ? Catalog.GetString (_label) : Id);
				(item as Gtk.ImageMenuItem).Image = Gtk.Image.NewFromIconName (icon, Gtk.IconSize.Menu);
			}
			return item;
		}
	}
}
