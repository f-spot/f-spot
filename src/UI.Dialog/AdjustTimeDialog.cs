/*
 * FSpot.UI.Dialogs.AdjstTimeDialog.cs
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 *
 * Copyright (c) 2006-2009 Novell, Inc.
 *
 * This is free software. See COPYING for details.
 */

using System;
using Gtk;
using System.Collections;
using Mono.Unix;
using FSpot.Widgets;

namespace FSpot.UI.Dialog {
	public class AdjustTimeDialog : BuilderDialog 
	{
		[GtkBeans.Builder.Object] ScrolledWindow view_scrolled;
		[GtkBeans.Builder.Object] ScrolledWindow tray_scrolled;
		[GtkBeans.Builder.Object] Button ok_button;
		[GtkBeans.Builder.Object] Button cancel_button;
		[GtkBeans.Builder.Object] SpinButton photo_spin;
		[GtkBeans.Builder.Object] Label name_label;
		[GtkBeans.Builder.Object] Label old_label;
		[GtkBeans.Builder.Object] Label count_label;
		[GtkBeans.Builder.Object] Gnome.DateEdit date_edit;
		[GtkBeans.Builder.Object] Frame tray_frame;
		[GtkBeans.Builder.Object] Gtk.Entry entry;
		[GtkBeans.Builder.Object] Gtk.Entry offset_entry;
		[GtkBeans.Builder.Object] Gtk.CheckButton difference_check;
		[GtkBeans.Builder.Object] Gtk.CheckButton interval_check;
		[GtkBeans.Builder.Object] Gtk.Frame action_frame;
		[GtkBeans.Builder.Object] Gtk.Entry spacing_entry;
		[GtkBeans.Builder.Object] Gtk.Label starting_label;
		
		IBrowsableCollection collection;
		BrowsablePointer Item;
		FSpot.Widgets.IconView tray;
		PhotoImageView view;
		Db db;
		TimeSpan gnome_dateedit_sucks;

		public AdjustTimeDialog (Db db, IBrowsableCollection collection) : base ("AdjustTimeDialog.ui", "time_dialog")
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

			//forward_button.Clicked += HandleForwardClicked;
			//back_button.Clicked += HandleBackClicked;
			ok_button.Clicked += HandleOkClicked;
			cancel_button.Clicked += HandleCancelClicked;

			photo_spin.ValueChanged += HandleSpinChanged;
			photo_spin.SetIncrements (1.0, 1.0);
			photo_spin.Adjustment.StepIncrement = 1.0;
			photo_spin.Wrap = true;

			date_edit.TimeChanged += HandleTimeChanged;
			date_edit.DateChanged += HandleTimeChanged;
			Gtk.Entry entry = (Gtk.Entry) date_edit.Children [0];
			entry.Changed += HandleTimeChanged;
			entry = (Gtk.Entry) date_edit.Children [2];
			entry.Changed += HandleTimeChanged;
			offset_entry.Changed += HandleOffsetChanged;
			ShowAll ();
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

			starting_label.Text = "min.";
			difference_check.Label = String.Format (Catalog.GetString ("Shift all photos by {0}"),
							      Offset);
		}

		void HandleItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			//back_button.Sensitive = (Item.Index > 0 && collection.Count > 0);
			//forward_button.Sensitive = (Item.Index < collection.Count - 1);

			if (Item.IsValid) {
				IBrowsableItem item = Item.Current;
				
				name_label.Text = System.Uri.UnescapeDataString(item.Name);
				old_label.Text = item.Time.ToLocalTime ().ToString ();
				
				int i = collection.Count > 0 ? Item.Index + 1: 0;
				// Note for translators: This indicates the current photo is photo {0} of {1} out of photos
				count_label.Text = System.String.Format (Catalog.GetString ("{0} of {1}"), i, collection.Count);

				DateTime actual = item.Time;
				date_edit.Time = actual;
				gnome_dateedit_sucks = date_edit.Time - actual;
			}
			HandleTimeChanged (this, System.EventArgs.Empty);

			if (!tray.Selection.Contains (Item.Index)) {
				tray.Selection.Clear ();
				tray.Selection.Add (Item.Index);
			}

			photo_spin.Value = Item.Index + 1;
		}

		private void ShiftByDifference ()
		{
			TimeSpan span = Offset;
			Photo [] photos = new Photo [collection.Count];

			for (int i = 0; i < collection.Count; i++) {
				Photo p = (Photo) collection [i];
				DateTime time = p.Time;
				p.Time = time + span;
				photos [i] = p;
				System.Console.WriteLine ("XXXXX old: {0} new: {1} span: {2}", time, p.Time, span);
			}
			
			db.Photos.Commit (photos);
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
			
			TimeSpan accum = new TimeSpan (0);
			for (int j = Item.Index; j > 0; j--) {
				date -= span;
			}

			for (int i = 0; i < photos.Length; i++) {
				photos [i].Time = date + accum;
				accum += span;
			}
			
			db.Photos.Commit (photos);
		}

		void HandleSpinChanged (object sender, EventArgs args)
		{
			Item.Index = photo_spin.ValueAsInt - 1;
		}

		void HandleOkClicked (object sender, EventArgs args)
		{
			if (! Item.IsValid)
				throw new ApplicationException ("invalid item selected");

			Sensitive = false;
			
			if (difference_check.Active)
				ShiftByDifference ();
			else
				SpaceByInterval ();


			Destroy ();
		}

		void HandleOffsetChanged (object sender, EventArgs args)
		{
			System.Console.WriteLine ("offset = {0}", Offset);
			TimeSpan current = Offset;
			try {
				TimeSpan span = TimeSpan.Parse (offset_entry.Text);
				if (span != current)
					Offset = span;
			} catch (System.Exception) {
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
			Destroy ();
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
				view.Item.Index = ((FSpot.Widgets.IconView.SelectionCollection)sender).Ids[0];

			}
		}

		void HandleCollectionChanged (IBrowsableCollection collection)
		{
			bool multiple = collection.Count > 1;
			tray_frame.Visible = multiple;
			//forward_button.Visible = multiple;
			//back_button.Visible = multiple;
			count_label.Visible = multiple;
			photo_spin.Visible = multiple;
			action_frame.Visible = multiple;
			photo_spin.SetRange (1.0, (double) collection.Count);
		}
	}
}
