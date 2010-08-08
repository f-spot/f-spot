/*
 * FSpot.PhotoView.cs
 *
 * Author(s)
 * 	Ettore Perazzoli
 * 	Larry Ewing
 *	Stephane Delcroix
 *
 * This is free software. See COPYING for details.
 */

using Gdk;
using GLib;
using Gtk;
using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using Mono.Unix;

using FSpot.Core;
using FSpot.Widgets;
using FSpot.Utils;
using Hyena;
using FSpot.UI.Dialog;

namespace FSpot {
	public class PhotoView : EventBox {
		Delay commit_delay;

		private PhotoImageView photo_view;
		private ScrolledWindow photo_view_scrolled;
		private EventBox background;

		private Filmstrip filmstrip;
		VBox inner_vbox;
		HBox inner_hbox;

		private Widgets.TagView tag_view;

		private Entry description_entry;
		private Widgets.Rating rating;

		// Public events.

		public delegate void PhotoChangedHandler (PhotoView me);
		public event PhotoChangedHandler PhotoChanged;

		public delegate void UpdateStartedHandler (PhotoView view);
		public event UpdateStartedHandler UpdateStarted;

		public delegate void UpdateFinishedHandler (PhotoView view);
		public event UpdateFinishedHandler UpdateFinished;

		public event EventHandler<BrowsableEventArgs> DoubleClicked;

		public Orientation FilmstripOrientation {
			get { return filmstrip.Orientation; }
		}

		public PhotoImageView View {
			get { return photo_view; }
		}

		public BrowsablePointer Item {
			get { return photo_view.Item; }
		}

		private IBrowsableCollection query;
		public IBrowsableCollection Query {
			get { return query; }
			set { query = value; }
		}

		public double Zoom {
			get { return photo_view.Zoom; }
			set { photo_view.Zoom = value; }
		}

		public double NormalizedZoom {
			get { return photo_view.NormalizedZoom; }
			set { photo_view.NormalizedZoom = value; }
		}

		public void Reload ()
		{
			photo_view.Reload ();
		}

		private void UpdateDescriptionEntry ()
		{
			description_entry.Changed -= HandleDescriptionChanged;
			if (Item.IsValid) {
				if (description_entry.Sensitive == false)
					description_entry.Sensitive = true;

				string desc = Item.Current.Description;
				if (description_entry.Text != desc) {
					description_entry.Text = desc == null ? String.Empty : desc;
				}
			} else {
				description_entry.Sensitive = false;
				description_entry.Text = String.Empty;
			}

			description_entry.Changed += HandleDescriptionChanged;
		}

		public void UpdateRating ()
		{
			if (Item.IsValid)
				UpdateRating ((int)Item.Current.Rating);
		}

		public void UpdateRating (int r)
		{
			rating.Changed -= HandleRatingChanged;
			rating.Value = r;
			rating.Changed += HandleRatingChanged;
		}

		private void Update ()
		{
			if (UpdateStarted != null)
				UpdateStarted (this);

			UpdateDescriptionEntry ();
			UpdateRating ();

			if (UpdateFinished != null)
				UpdateFinished (this);
		}

		public void ZoomIn ()
		{
			photo_view.ZoomIn ();
		}

		public void ZoomOut ()
		{
			photo_view.ZoomOut ();
		}

		// Event handlers.
		private void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Type == EventType.TwoButtonPress && args.Event.Button == 1 && DoubleClicked != null)
				DoubleClicked (this, null);
			if (args.Event.Type == EventType.ButtonPress
			    && args.Event.Button == 3) {
				PhotoPopup popup = new PhotoPopup ();
				popup.Activate (this.Toplevel, args.Event);
			}
		}

		protected override bool OnPopupMenu ()
		{
			PhotoPopup popup = new PhotoPopup ();
			popup.Activate (this.Toplevel);
			return true;
		}

		int changed_photo;
		private bool CommitPendingChanges ()
		{
			if (commit_delay.IsPending) {
				commit_delay.Stop ();
				((PhotoQuery)query).Commit (changed_photo);
			}
			return true;
		}

		private void HandleDescriptionChanged (object sender, EventArgs args)
		{
			if (!Item.IsValid)
				return;

			((Photo)Item.Current).Description = description_entry.Text;

			if (commit_delay.IsPending)
				if (changed_photo == Item.Index)
					commit_delay.Stop ();
				else
					CommitPendingChanges ();

			changed_photo = Item.Index;
			commit_delay.Start ();
		}

		private void HandleRatingChanged (object o, EventArgs e)
		{
			if (!Item.IsValid)
				return;

			((Photo)Item.Current).Rating = (uint)(o as Widgets.Rating).Value;

			if (commit_delay.IsPending)
				if (changed_photo == Item.Index)
					commit_delay.Stop();
				else
					CommitPendingChanges ();
			changed_photo = Item.Index;
			commit_delay.Start ();
		}

		public void UpdateTagView ()
		{
			tag_view.DrawTags ();
			tag_view.QueueDraw ();
		}

		void HandlePhotoChanged (object sender, EventArgs e)
		{
			if (query is PhotoQuery) {
				CommitPendingChanges ();
			}

			tag_view.Current = Item.Current;
			Update ();

			if (this.PhotoChanged != null)
				PhotoChanged (this);
		}

		private void HandleDestroy (object sender, System.EventArgs args)
		{
			CommitPendingChanges ();
			Dispose ();
		}

		private void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		private void LoadPreference (String key)
		{
			switch (key) {
			case Preferences.FILMSTRIP_ORIENTATION:
				PlaceFilmstrip ((Orientation) Preferences.Get<int> (key));
				break;
			}
		}

		public void PlaceFilmstrip (Orientation pos)
		{
			PlaceFilmstrip (pos, false);
		}

		public void PlaceFilmstrip (Orientation pos, bool force)
		{
			if (!force && filmstrip.Orientation == pos)
				return;
			filmstrip.Orientation = pos;

			System.Collections.IEnumerator widgets;
			switch (pos) {
			case Orientation.Horizontal:
				widgets = inner_hbox.AllChildren.GetEnumerator ();
				while (widgets.MoveNext ())
					if (widgets.Current == filmstrip) {
						inner_hbox.Remove (filmstrip);
						break;
					}
				inner_vbox.PackStart (filmstrip, false, false, 0);
				inner_vbox.ReorderChild (filmstrip, 0);
				break;
			case Orientation.Vertical:
				widgets = inner_vbox.AllChildren.GetEnumerator ();
				while (widgets.MoveNext ())
					if (widgets.Current == filmstrip) {
						inner_vbox.Remove (filmstrip);
						break;
					}
				inner_hbox.PackEnd (filmstrip, false, false, 0);
				break;
			}
			Preferences.Set (Preferences.FILMSTRIP_ORIENTATION, (int) pos);
		}

		public bool FilmStripVisibility {
			get { return filmstrip.Visible; }
			set { filmstrip.Visible = value; }
		}

		public PhotoView (IBrowsableCollection query)
			: base ()
		{
			this.query = query;

			commit_delay = new Delay (1000, new GLib.IdleHandler (CommitPendingChanges));
			this.Destroyed += HandleDestroy;

			Name = "ImageContainer";
			Box vbox = new VBox (false, 6);
			Add (vbox);

		        background = new EventBox ();
			Frame frame = new Frame ();
			background.Add (frame);

			frame.ShadowType = ShadowType.In;
			vbox.PackStart (background, true, true, 0);

			inner_vbox = new VBox (false , 2);
			inner_hbox = new HBox (false , 2);

			frame.Add (inner_hbox);

			BrowsablePointer bp = new BrowsablePointer (query, -1);
			photo_view = new PhotoImageView (bp);

			filmstrip = new Filmstrip (bp);
			filmstrip.ThumbOffset = 1;
			filmstrip.Spacing = 4;
			filmstrip.ThumbSize = 75;
			PlaceFilmstrip ((Orientation) Preferences.Get <int> (Preferences.FILMSTRIP_ORIENTATION), true);

			photo_view.PhotoChanged += HandlePhotoChanged;

			photo_view_scrolled = new ScrolledWindow (null, null);

			photo_view_scrolled.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
			photo_view_scrolled.ShadowType = ShadowType.None;
			photo_view_scrolled.Add (photo_view);
			photo_view_scrolled.Child.ButtonPressEvent += HandleButtonPressEvent;
			photo_view.AddEvents ((int) EventMask.KeyPressMask);
			inner_vbox.PackStart (photo_view_scrolled, true, true, 0);
			inner_hbox.PackStart (inner_vbox, true, true, 0);

			HBox lower_hbox = new HBox (false, 2);
			//inner_hbox.BorderWidth = 6;

			tag_view = new Widgets.TagView ();
			lower_hbox.PackStart (tag_view, false, true, 0);

			Label comment = new Label (Catalog.GetString ("Description:"));
			lower_hbox.PackStart (comment, false, false, 0);
			description_entry = new Entry ();
			lower_hbox.PackStart (description_entry, true, true, 0);
			description_entry.Changed += HandleDescriptionChanged;

			rating = new Widgets.Rating();
			lower_hbox.PackStart (rating, false, false, 0);
			rating.Changed += HandleRatingChanged;

			SetColors ();

			inner_vbox.PackStart (lower_hbox, false, true, 0);

			vbox.ShowAll ();

			Realized += delegate (object o, EventArgs e) {SetColors ();};
			Preferences.SettingChanged += OnPreferencesChanged;
		}

		~PhotoView ()
		{
			Hyena.Log.DebugFormat ("Finalizer called on {0}. Should be Disposed", GetType ());
			Dispose (false);
		}

		public override void Dispose ()
		{
			Dispose (true);
			base.Dispose ();
			System.GC.SuppressFinalize (this);
		}

		bool is_disposed = false;
		protected virtual void Dispose (bool disposing)
		{
			if (is_disposed)
				return;
			if (disposing) { //Free managed resources
				filmstrip.Dispose ();
			}

			is_disposed = true;
		}

		private void SetColors ()
		{
			GtkUtil.ModifyColors (filmstrip);
			GtkUtil.ModifyColors (tag_view);
			GtkUtil.ModifyColors (photo_view);
			GtkUtil.ModifyColors (background);
			GtkUtil.ModifyColors (photo_view_scrolled);
			GtkUtil.ModifyColors (rating);

			Gdk.Color dark = Style.Dark (Gtk.StateType.Normal);
			filmstrip.ModifyBg (Gtk.StateType.Normal, dark);
			tag_view.ModifyBg (Gtk.StateType.Normal, dark);
			photo_view.ModifyBg (Gtk.StateType.Normal, dark);
			background.ModifyBg (Gtk.StateType.Normal, dark);
			photo_view_scrolled.ModifyBg (Gtk.StateType.Normal, dark);
			rating.ModifyBg (Gtk.StateType.Normal, dark);
		}

		protected override void OnStyleSet (Style previous)
		{
			SetColors ();
		}
	}
}
