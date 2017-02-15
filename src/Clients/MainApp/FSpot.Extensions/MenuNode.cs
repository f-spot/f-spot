//
// MenuNode.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2007-2008 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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

		IMenuGenerator menu_generator;

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
