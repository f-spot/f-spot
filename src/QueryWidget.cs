/*
 * FSpot.QueryWidget.cs
 *
 * Author(s):
 *	Gabriel Burt
 *
 * This is free software. See COPYING for details.
 */


using System;
using System.Collections.Generic;

using Mono.Unix;

using Gtk;

using FSpot.Utils;
using FSpot.Query;
using FSpot.Widgets;



namespace FSpot {

	public class QueryWidget : HighlightedBox {
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
		Gtk.Tooltips tips = new Gtk.Tooltips ();

		public LogicWidget Logic {
			get { return logic_widget; }
		}

		public QueryWidget (PhotoQuery query, Db db) : base(new HBox())
		{
			box = Child as HBox;
			box.Spacing = 6;
			box.BorderWidth = 2;

			tips.Enable ();

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

			folder_query_widget = new FolderQueryWidget ();
			folder_query_widget.Visible = false;
			box.PackStart (folder_query_widget, false, false, 0);
			
			logic_widget = new LogicWidget (query, db.Tags);
			logic_widget.Show ();
			box.PackStart (logic_widget, true, true, 0);

			warning_box = new Gtk.HBox ();
			warning_box.PackStart (new Gtk.Label (System.String.Empty));
			
			Gtk.Image warning_image = new Gtk.Image ("gtk-info", Gtk.IconSize.Button);
			warning_image.Show ();
			warning_box.PackStart (warning_image, false, false, 0);
			
			clear_button = new Gtk.Button ();
			clear_button.Add (new Gtk.Image ("gtk-close", Gtk.IconSize.Button));
			clear_button.Clicked += HandleClearButtonClicked;
			clear_button.Relief = Gtk.ReliefStyle.None;
			box.PackEnd (clear_button, false, false, 0);
			tips.SetTip (clear_button, Catalog.GetString("Clear search"), null);
			
			refresh_button = new Gtk.Button ();
			refresh_button.Add (new Gtk.Image ("gtk-refresh", Gtk.IconSize.Button));
			refresh_button.Clicked += HandleRefreshButtonClicked;
			refresh_button.Relief = Gtk.ReliefStyle.None;
			box.PackEnd (refresh_button, false, false, 0);
			tips.SetTip (refresh_button, Catalog.GetString("Refresh search"), null);

			Gtk.Label warning = new Gtk.Label (Catalog.GetString ("No matching photos found"));
			warning_box.PackStart (warning, false, false, 0);
			warning_box.ShowAll ();
			warning_box.Spacing = 6;
			warning_box.Visible = false;

			box.PackEnd (warning_box, false, false, 0);
			
			warning_box.Visible = false;
		}
		
		public void HandleClearButtonClicked (object sender, System.EventArgs args)
		{
			Close ();
		}

 		public void HandleRefreshButtonClicked (object sender, System.EventArgs args)
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
			logic_widget.Clear = true;
			logic_widget.UpdateQuery ();
			
			folder_query_widget.Clear ();
			query.RequestReload ();
			
			HideBar ();
		}

		public void ShowBar ()
		{
			Show ();
			((Gtk.Label)MainWindow.Toplevel.FindByTag.Child).TextWithMnemonic = Catalog.GetString ("Hide _Find Bar");
		}

		public void HideBar ()
		{
			Hide ();
			((Gtk.Label)MainWindow.Toplevel.FindByTag.Child).TextWithMnemonic = Catalog.GetString ("Show _Find Bar");
		}

		public void HandleChanged (IBrowsableCollection collection) 
		{
			if (query.TagTerm == null)
				logic_widget.Clear = true;

			if ( ! logic_widget.Clear
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
		
		public void SetFolders (IEnumerable<Uri> uri_list)
		{
			folder_query_widget.SetFolders (uri_list);
			query.RequestReload ();
		}
	}
}
