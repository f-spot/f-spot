using System;
using Gtk;
using Gnome;
using System.Collections;
using Mono.Posix;

namespace FSpot {
	public class TimeChangedEventArgs : DbItemEventArgs {
		TimeSpan span;

		public TimeChangedEventArgs (DbItem [] items, TimeSpan span) : base (items)
		{
			this.span = span;
		}
	}

	public class TimeDialog : GladeDialog 
	{
		[Glade.Widget] ScrolledWindow view_scrolled;
		[Glade.Widget] ScrolledWindow tray_scrolled;

		[Glade.Widget] Button back_button;
		[Glade.Widget] Button forward_button;
		[Glade.Widget] Button ok_button;
		[Glade.Widget] Button cancel_button;

		[Glade.Widget] Label name_label;
		[Glade.Widget] Label old_label;
		[Glade.Widget] Label count_label;

		[Glade.Widget] DateEdit date_edit;

		[Glade.Widget] Frame tray_frame;
		
		[Glade.Widget] Gtk.Entry entry;
		[Glade.Widget] Gtk.Entry offset_entry;
		
		[Glade.Widget] Gtk.CheckButton difference_check;
		[Glade.Widget] Gtk.CheckButton interval_check;
		[Glade.Widget] Gtk.Frame action_frame;
		[Glade.Widget] Gtk.Entry spacing_entry;
		[Glade.Widget] Gtk.Label starting_label;
		
		IBrowsableCollection collection;
		BrowsablePointer Item;
		IconView tray;
		PhotoImageView view;
		Db db;
		TimeSpan gnome_dateedit_sucks;

		public TimeDialog (Db db, IBrowsableCollection collection) : base ("time_dialog")
		{
			this.db = db;
			this.collection = collection;

			tray = new TrayView (collection);
			tray_scrolled.Add (tray);
			tray.Selection.Changed += HandleSelectionChanged;

			view = new PhotoImageView (collection);
			view_scrolled.Add (view);
			Item = view.Item;
			Item.Changed += HandleItemChanged;
			Item.MoveFirst ();

			forward_button.Clicked += HandleForwardClicked;
			back_button.Clicked += HandleBackClicked;
			ok_button.Clicked += HandleOkClicked;
			cancel_button.Clicked += HandleCancelClicked;

			date_edit.TimeChanged += HandleTimeChanged;
			date_edit.DateChanged += HandleTimeChanged;
			Gtk.Entry entry = (Gtk.Entry) date_edit.Children [0];
			entry.Changed += HandleTimeChanged;
			entry = (Gtk.Entry) date_edit.Children [2];
			entry.Changed += HandleTimeChanged;
			offset_entry.Changed += HandleOffsetChanged;
			Dialog.ShowAll ();
			HandleCollectionChanged (collection);

			spacing_entry.Changed += HandleSpacingChanged;
			spacing_entry.Sensitive = ! difference_check.Active;
		      
			difference_check.Toggled += HandleActionToggled;
		}

		DateTime EditTime {
			get { return date_edit.Time - gnome_dateedit_sucks; }
		}

		TimeSpan Offset
		{
			get {
				System.Console.WriteLine ("{0} - {1} = {2}", date_edit.Time, Item.Current.Time, date_edit.Time - Item.Current.Time);
				return EditTime - Item.Current.Time;
			}
			set {
				date_edit.Time = Item.Current.Time - gnome_dateedit_sucks + value;
			}
		}

		void HandleTimeChanged (object sender, EventArgs args)
		{
			TimeSpan span = Offset;
			System.Console.WriteLine ("time changed {0}", span);
			if (! offset_entry.HasFocus)
				offset_entry.Text = span.ToString ();

			// The preceding text here is the second checkbutton in the Time dialog
			// that says "Space all photos by []"
			starting_label.Text = String.Format (Catalog.GetString ("min. Starting at {0}"),
							     EditTime);
			difference_check.Label = String.Format (Catalog.GetString ("Shift all photos by {0}"),
							      Offset);
		}

		void HandleItemChanged (BrowsablePointer pointer, BrowsablePointerChangedArgs args)
		{
			back_button.Sensitive = (Item.Index > 0 && collection.Count > 0);
			forward_button.Sensitive = (Item.Index < collection.Count - 1);

			if (Item.IsValid) {
				IBrowsableItem item = Item.Current;
				
				name_label.Text = item.Name;;
				old_label.Text = item.Time.ToString ();
				
				int i = collection.Count > 0 ? Item.Index + 1: 0;
				// This indicates the current photo is photo {0} of {1} out of photos
				count_label.Text = System.String.Format (Catalog.GetString ("{0} of {1}"), i, collection.Count);

				DateTime actual = item.Time.ToUniversalTime ();
				date_edit.Time = actual;
				gnome_dateedit_sucks = date_edit.Time - actual.ToLocalTime ();
			}
			HandleTimeChanged (this, System.EventArgs.Empty);

			if (!tray.Selection.Contains (Item.Index)) {
				tray.Selection.Clear ();
				tray.Selection.Add (Item.Index);
			}
		}

		private void ShiftByDifference ()
		{
			TimeSpan span = Offset;
			Photo [] photos = new Photo [collection.Count];

			for (int i = 0; i < collection.Count; i++) {
				Photo p = (Photo) collection [i];
				p.Time += span;
				photos [i] = p;
			}
			
			db.Photos.Commit (photos, new TimeChangedEventArgs (photos, span));
		}

		private void SpaceByInterval ()
		{
			DateTime date = EditTime;
		        long ticks = (long) (double.Parse (spacing_entry.Text) * TimeSpan.TicksPerMinute);
			TimeSpan span = new TimeSpan (ticks);
			Photo [] photos = new Photo [collection.Count];

			for (int i = 0; i < collection.Count; i++) {
				photos [i] = (Photo) collection [i];
			}
			
			Array.Sort (photos);

			TimeSpan accum = span;
			for (int i = 0; i < photos.Length; i++) {
				photos [i].Time = date + accum;
				accum += span;
			}
			
			db.Photos.Commit (photos, new TimeChangedEventArgs (photos, span));
		}

		void HandleOkClicked (object sender, EventArgs args)
		{
			if (! Item.IsValid)
				throw new ApplicationException ("invalid item selected");

			Dialog.Sensitive = false;
			
			if (difference_check.Active)
				ShiftByDifference ();
			else
				SpaceByInterval ();


			Dialog.Destroy ();
		}

		void HandleOffsetChanged (object sender, EventArgs args)
		{
			System.Console.WriteLine ("offset = {0}", Offset);
			TimeSpan current = Offset;
			try {
				TimeSpan span = TimeSpan.Parse (offset_entry.Text);
				if (span != current)
					Offset = current;
			} catch (System.Exception e) {
				System.Console.WriteLine ("unparsable span {0}", offset_entry.Text);
			}
		}

		void HandleSpacingChanged (object sender, EventArgs args)
		{
			if (! spacing_entry.Sensitive)
				return;

			try {
				double.Parse (spacing_entry.Text);
				ok_button.Sensitive = true;
			} catch {
				ok_button.Sensitive = false;
			}
		}

		void HandleActionToggled (object sender, EventArgs args)
		{
			spacing_entry.Sensitive = ! difference_check.Active;
			HandleSpacingChanged (sender, args);
		}

		void HandleCancelClicked (object sender, EventArgs args)
		{
			Dialog.Destroy ();
		}

		void HandleForwardClicked (object sender, EventArgs args)
		{
			view.Item.MoveNext ();
		}

		void HandleBackClicked (object sender, EventArgs args)
		{
			view.Item.MovePrevious ();
		}

		void HandleSelectionChanged (IBrowsableCollection sender)
		{
			if (sender.Count > 0) {
				view.Item.Index = ((IconView.SelectionCollection)sender).Ids[0];
			}
		}

		void HandleCollectionChanged (IBrowsableCollection collection)
		{
			bool multiple = collection.Count > 1;
			tray_frame.Visible = multiple;
			forward_button.Visible = multiple;
			back_button.Visible = multiple;
			count_label.Visible = multiple;
			action_frame.Visible = multiple;
		}
	}
}
