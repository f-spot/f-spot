/*
 * FSpot.Extensions.PopupCommands
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

using System;
using GLib;
using FSpot.Widgets;

namespace FSpot.Extensions
{
	public class Copy : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			App.Instance.Organizer.HandleCopy (o, e);
		}
	}

	public class Rotate270 : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			App.Instance.Organizer.HandleRotate270Command (o, e);
		}
	}

	public class Rotate90 : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			App.Instance.Organizer.HandleRotate90Command (o, e);
		}
	}

	public class Remove : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			App.Instance.Organizer.HandleRemoveCommand (o, e);
		}
	}

	public class Delete : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			App.Instance.Organizer.HandleDeleteCommand (o, e);
		}
	}

	public class OpenWith : IMenuGenerator
	{
		private Widgets.OpenWithMenu owm;

		public Gtk.Menu GetMenu ()
		{
			owm = new Widgets.OpenWithMenu (App.Instance.Organizer.SelectedMimeTypes, "f-spot");
			owm.ApplicationActivated += App.Instance.Organizer.HandleOpenWith;
			return (Gtk.Menu) owm;
		}

		public void OnActivated (object o, EventArgs e)
		{
			if (owm != null)
				owm.Populate (o, e);
		}
	}

	public class RemoveTag : IMenuGenerator
	{
		public Gtk.Menu GetMenu ()
		{
			PhotoTagMenu tag_menu = new PhotoTagMenu ();
			tag_menu.TagSelected += App.Instance.Organizer.HandleRemoveTagMenuSelected;
			return (Gtk.Menu) tag_menu;
		}

		public void OnActivated (object o, EventArgs e)
		{
			App.Instance.Organizer.HandleTagMenuActivate (o, e);
		}
	}

	public class Rate : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			App.Instance.Organizer.HandleRatingMenuSelected ((o as Widgets.Rating).Value);
		}
	}
}
