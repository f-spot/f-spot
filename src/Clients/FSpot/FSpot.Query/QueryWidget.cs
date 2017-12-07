//
// QueryWidget.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2009 Mike Gemünde
// Copyright (C) 2006-2007 Gabriel Burt
// Copyright (C) 2010 Ruben Vermeersch
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
using System.Collections.Generic;

using FSpot.Core;
using FSpot.Database;
using FSpot.Widgets;

using Gtk;

using Hyena;

using Mono.Unix;

namespace FSpot.Query
{
	public class QueryWidget : HighlightedBox
	{
		PhotoQuery query;
		LogicWidget logic_widget;
		FolderQueryWidget folder_query_widget;

		Gtk.HBox box;
		Gtk.Label label;
		Gtk.Label untagged;
		Gtk.Label rated;
		Gtk.Label comma1_label;
		Gtk.Label comma2_label;
		Gtk.Label rollfilter;
		Gtk.HBox warning_box;
		Gtk.Button clear_button;
		Gtk.Button refresh_button;

		public LogicWidget Logic {
			get { return logic_widget; }
		}

		protected QueryWidget (IntPtr raw) : base (raw) {}

		public QueryWidget (PhotoQuery query, Db db) : base(new HBox())
		{
			box = Child as HBox;
			box.Spacing = 6;
			box.BorderWidth = 2;

			this.query = query;
			query.Changed += HandleChanged;

			label = new Gtk.Label (Catalog.GetString ("Find: "));
			label.Show ();
			label.Ypad = 9;
			box.PackStart (label, false, false, 0);

			untagged = new Gtk.Label (Catalog.GetString ("Untagged photos"));
			untagged.Visible = false;
			box.PackStart (untagged, false, false, 0);

			comma1_label = new Gtk.Label (", ");
			comma1_label.Visible = false;
			box.PackStart (comma1_label, false, false, 0);

			rated = new Gtk.Label (Catalog.GetString ("Rated photos"));
			rated.Visible = false;
			box.PackStart (rated, false, false, 0);

			comma2_label = new Gtk.Label (", ");
			comma2_label.Visible = false;
			box.PackStart (comma2_label, false, false, 0);

			// Note for translators: 'Import roll' is no command, it means 'Roll that has been imported'
			rollfilter = new Gtk.Label (Catalog.GetString ("Import roll"));
			rollfilter.Visible = false;
			box.PackStart (rollfilter, false, false, 0);

			folder_query_widget = new FolderQueryWidget (query);
			folder_query_widget.Visible = false;
			box.PackStart (folder_query_widget, false, false, 0);

			logic_widget = new LogicWidget (query, db.Tags);
			logic_widget.Show ();
			box.PackStart (logic_widget, true, true, 0);

			warning_box = new Gtk.HBox ();
			warning_box.PackStart (new Gtk.Label (string.Empty));

			Gtk.Image warning_image = new Gtk.Image ("gtk-info", Gtk.IconSize.Button);
			warning_image.Show ();
			warning_box.PackStart (warning_image, false, false, 0);

			clear_button = new Gtk.Button ();
			clear_button.Add (new Gtk.Image ("gtk-close", Gtk.IconSize.Button));
			clear_button.Clicked += HandleClearButtonClicked;
			clear_button.Relief = Gtk.ReliefStyle.None;
			clear_button.TooltipText = Catalog.GetString("Clear search");
			box.PackEnd (clear_button, false, false, 0);

			refresh_button = new Gtk.Button ();
			refresh_button.Add (new Gtk.Image ("gtk-refresh", Gtk.IconSize.Button));
			refresh_button.Clicked += HandleRefreshButtonClicked;
			refresh_button.Relief = Gtk.ReliefStyle.None;
			refresh_button.TooltipText = Catalog.GetString("Refresh search");
			box.PackEnd (refresh_button, false, false, 0);

			Gtk.Label warning = new Gtk.Label (Catalog.GetString ("No matching photos found"));
			warning_box.PackStart (warning, false, false, 0);
			warning_box.ShowAll ();
			warning_box.Spacing = 6;
			warning_box.Visible = false;

			box.PackEnd (warning_box, false, false, 0);

			warning_box.Visible = false;
		}

		public void HandleClearButtonClicked (object sender, EventArgs args)
		{
			Close ();
		}

		public void HandleRefreshButtonClicked (object sender, EventArgs args)
		{
			query.RequestReload ();
		}

		public void Close ()
		{
			query.Untagged = false;
			query.RollSet = null;

			if (query.Untagged)
				return;

			query.RatingRange = null;
			logic_widget.Clear ();
			logic_widget.UpdateQuery ();

			folder_query_widget.Clear ();
			query.RequestReload ();

			HideBar ();
		}

		public void ShowBar ()
		{
			Show ();
		}

		public void HideBar ()
		{
			Hide ();
		}

		public void HandleChanged (IBrowsableCollection collection)
		{
			if (query.TagTerm == null)
				logic_widget.Clear();

			if ( ! logic_widget.IsClear
			    || query.Untagged
			    || (query.RollSet != null)
			    || (query.RatingRange != null)
			    || ! folder_query_widget.Empty)
				ShowBar ();
			else
				HideBar ();

			untagged.Visible = query.Untagged;
			rated.Visible = (query.RatingRange != null);
			warning_box.Visible = (query.Count < 1);
			rollfilter.Visible = (query.RollSet != null);
			comma1_label.Visible = (untagged.Visible && rated.Visible);
			comma2_label.Visible = (!untagged.Visible && rated.Visible && rollfilter.Visible) ||
					       (untagged.Visible && rollfilter.Visible);

		}

		public void PhotoTagsChanged (Tag[] tags)
		{
			logic_widget.PhotoTagsChanged (tags);
		}

		public void Include (Tag [] tags)
		{
			logic_widget.Include (tags);
		}

		public void UnInclude (Tag [] tags)
		{
			logic_widget.UnInclude (tags);
		}

		public void Require (Tag [] tags)
		{
			logic_widget.Require (tags);
		}

		public void UnRequire (Tag [] tags)
		{
			logic_widget.UnRequire (tags);
		}

		public bool TagIncluded (Tag tag)
		{
			return logic_widget.TagIncluded (tag);
		}

		public bool TagRequired (Tag tag)
		{
			return logic_widget.TagRequired (tag);
		}

		public void SetFolders (IEnumerable<SafeUri> uriList)
		{
			folder_query_widget.SetFolders (uriList);
			query.RequestReload ();
		}
	}
}
