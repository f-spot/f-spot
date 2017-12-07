//
// PopupCommands.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Paul Werner Bou <paul@purecodes.org>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2010 Paul Werner Bou
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
		Widgets.OpenWithMenu owm;

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
			return tag_menu;
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
			App.Instance.Organizer.HandleRatingMenuSelected ((o as FSpot.Widgets.RatingMenuItem).Value);
		}
	}
}
