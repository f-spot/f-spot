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
		readonly PhotoQuery query;
		readonly FolderQueryWidget folderQueryWidget;

		HBox box;
		Label label;
		Label untagged;
		Label rated;
		Label comma1_label;
		Label comma2_label;
		Label rollfilter;
		HBox warning_box;
		Button clear_button;
		Button refresh_button;

		public LogicWidget Logic { get; }

		protected QueryWidget (IntPtr raw) : base (raw) {}

		public QueryWidget (PhotoQuery query, Db db) : base (new HBox())
		{
			box = Child as HBox;
			box.Spacing = 6;
			box.BorderWidth = 2;

			this.query = query;
			query.Changed += HandleChanged;

			label = new Label (Catalog.GetString ("Find: "));
			label.Show ();
			label.Ypad = 9;
			box.PackStart (label, false, false, 0);

			untagged = new Label (Catalog.GetString ("Untagged photos")) {
				Visible = false
			};
			box.PackStart (untagged, false, false, 0);

			comma1_label = new Label (", ") {
				Visible = false
			};
			box.PackStart (comma1_label, false, false, 0);

			rated = new Label (Catalog.GetString ("Rated photos")) {
				Visible = false
			};
			box.PackStart (rated, false, false, 0);

			comma2_label = new Label (", ") {
				Visible = false
			};
			box.PackStart (comma2_label, false, false, 0);

			// Note for translators: 'Import roll' is no command, it means 'Roll that has been imported'
			rollfilter = new Label (Catalog.GetString ("Import roll")) {
				Visible = false
			};
			box.PackStart (rollfilter, false, false, 0);

			folderQueryWidget = new FolderQueryWidget (query) {
				Visible = false
			};
			box.PackStart (folderQueryWidget, false, false, 0);

			Logic = new LogicWidget (query, db.Tags);
			Logic.Show ();
			box.PackStart (Logic, true, true, 0);

			warning_box = new HBox ();
			warning_box.PackStart (new Label (string.Empty));

			Image warning_image = new Image ("gtk-info", IconSize.Button);
			warning_image.Show ();
			warning_box.PackStart (warning_image, false, false, 0);

			clear_button = new Button {
				new Image ("gtk-close", IconSize.Button)
			};
			clear_button.Clicked += HandleClearButtonClicked;
			clear_button.Relief = Gtk.ReliefStyle.None;
			clear_button.TooltipText = Catalog.GetString("Clear search");
			box.PackEnd (clear_button, false, false, 0);

			refresh_button = new Button {
				new Image ("gtk-refresh", IconSize.Button)
			};
			refresh_button.Clicked += HandleRefreshButtonClicked;
			refresh_button.Relief = ReliefStyle.None;
			refresh_button.TooltipText = Catalog.GetString("Refresh search");
			box.PackEnd (refresh_button, false, false, 0);

			Label warning = new Label (Catalog.GetString ("No matching photos found"));
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
			Logic.Clear ();
			Logic.UpdateQuery ();

			folderQueryWidget.Clear ();
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
				Logic.Clear();

			if ( ! Logic.IsClear
			    || query.Untagged
			    || (query.RollSet != null)
			    || (query.RatingRange != null)
			    || ! folderQueryWidget.Empty)
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
			Logic.PhotoTagsChanged (tags);
		}

		public void Include (Tag [] tags)
		{
			Logic.Include (tags);
		}

		public void UnInclude (Tag [] tags)
		{
			Logic.UnInclude (tags);
		}

		public void Require (Tag [] tags)
		{
			Logic.Require (tags);
		}

		public void UnRequire (Tag [] tags)
		{
			Logic.UnRequire (tags);
		}

		public bool TagIncluded (Tag tag)
		{
			return Logic.TagIncluded (tag);
		}

		public bool TagRequired (Tag tag)
		{
			return Logic.TagRequired (tag);
		}

		public void SetFolders (IEnumerable<SafeUri> uriList)
		{
			folderQueryWidget.SetFolders (uriList);
			query.RequestReload ();
		}
	}
}
