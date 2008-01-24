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

namespace FSpot.Extensions 
{
	public class CopyLocation : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			MainWindow.Toplevel.HandleCopyLocation (o, e);
		}		
	}

	public class Rotate270 : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			MainWindow.Toplevel.HandleRotate270Command (o, e);
		}
	}

	public class Rotate90 : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			MainWindow.Toplevel.HandleRotate90Command (o, e);
		}
	}

	public class Remove : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			MainWindow.Toplevel.HandleRemoveCommand (o, e);
		}
	}

	public class Delete : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			MainWindow.Toplevel.HandleDeleteCommand (o, e);
		}
	}

	public class OpenWith : IMenuGenerator
	{
		private OpenWithMenu owm;

		public Gtk.Menu GetMenu ()
		{
			owm = new OpenWithMenu (MainWindow.Toplevel.SelectedMimeTypes);
			owm.IgnoreApp = "f-spot";
			owm.ApplicationActivated += delegate (Gnome.Vfs.MimeApplication app) { MainWindow.Toplevel.HandleOpenWith (this, app); };
			return (Gtk.Menu) owm;
		}

		public void OnActivated (object o, EventArgs e)
		{
			if (owm != null)
				owm.Populate (o, e);
		}
	}

	public class AttachTag : IMenuGenerator
	{
		private TagMenu tag_menu;

		public Gtk.Menu GetMenu ()
		{
			tag_menu = new TagMenu (null, MainWindow.Toplevel.Database.Tags);
			tag_menu.NewTagHandler += delegate { MainWindow.Toplevel.HandleCreateTagAndAttach (this, null); };
			tag_menu.TagSelected += MainWindow.Toplevel.HandleAttachTagMenuSelected;
			return (Gtk.Menu) tag_menu;
		}

		public void OnActivated (object o, EventArgs e)
		{
			if (tag_menu != null)
				tag_menu.Populate ();
		}
	}

	public class RemoveTag : IMenuGenerator
	{
		public Gtk.Menu GetMenu ()
		{
			PhotoTagMenu tag_menu = new PhotoTagMenu ();
			tag_menu.TagSelected += MainWindow.Toplevel.HandleRemoveTagMenuSelected;
			return (Gtk.Menu) tag_menu;
		}

		public void OnActivated (object o, EventArgs e)
		{
			MainWindow.Toplevel.HandleTagMenuActivate (o, e);
		}
	}

	public class Rate : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			MainWindow.Toplevel.HandleRatingMenuSelected ((o as Widgets.Rating).Value);
		}
	}
}
