//
// PhotoView.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2009 Lorenzo Milesi
// Copyright (C) 2008-2009 Stephane Delcroix
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
using FSpot.Core;
using FSpot.Settings;
using FSpot.Utils;
using FSpot.Widgets;
using Gdk;
using Gtk;
using Mono.Unix;

namespace FSpot
{
	public class PhotoView : EventBox
	{
		bool disposed;

		DelayedOperation commit_delay;

		ScrolledWindow photo_view_scrolled;
		EventBox background;

		Filmstrip filmstrip;
		VBox inner_vbox;
		HBox inner_hbox;

		TagView tag_view;

		Entry description_entry;
		RatingEntry rating;

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

		// was photo_view
		public PhotoImageView View { get; private set; }

		public BrowsablePointer Item {
                        get { return View.Item; }
		}

		public IBrowsableCollection Query { get; set; }

		public double Zoom {
			get { return View.Zoom; }
			set { View.Zoom = value; }
		}

		public double NormalizedZoom {
			get { return View.NormalizedZoom; }
			set { View.NormalizedZoom = value; }
		}

		public void Reload ()
		{
			View.Reload ();
		}

		void UpdateDescriptionEntry ()
		{
			description_entry.Changed -= HandleDescriptionChanged;
			if (Item.IsValid) {
				if (!description_entry.Sensitive)
					description_entry.Sensitive = true;

				string desc = Item.Current.Description;
				if (description_entry.Text != desc) {
					description_entry.Text = desc ?? string.Empty;
				}
			} else {
				description_entry.Sensitive = false;
				description_entry.Text = string.Empty;
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

		void Update ()
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
			View.ZoomIn ();
		}

		public void ZoomOut ()
		{
			View.ZoomOut ();
		}

		// Event handlers.
		void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Type == EventType.TwoButtonPress && args.Event.Button == 1 && DoubleClicked != null)
				    DoubleClicked (this, null);
			if (args.Event.Type == EventType.ButtonPress
			    && args.Event.Button == 3) {
				var popup = new PhotoPopup ();
				popup.Activate (Toplevel, args.Event);
			}
		}

		protected override bool OnPopupMenu ()
		{
			var popup = new PhotoPopup ();
			popup.Activate (Toplevel);
			return true;
		}

		int changed_photo;
		bool CommitPendingChanges ()
		{
			if (commit_delay.IsPending) {
				commit_delay.Stop ();
				((PhotoQuery)Query).Commit (changed_photo);
			}
			return true;
		}

		void HandleDescriptionChanged (object sender, EventArgs args)
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

		void HandleRatingChanged (object o, EventArgs e)
		{
			if (!Item.IsValid)
				return;

			((Photo)Item.Current).Rating = (uint)(o as RatingEntry).Value;

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
			if (Query is PhotoQuery) {
				CommitPendingChanges ();
			}

			tag_view.Current = Item.Current;
			Update ();

			if (PhotoChanged != null)
				PhotoChanged (this);
		}

		void HandleDestroy (object sender, EventArgs args)
		{
			CommitPendingChanges ();
		}

		void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void LoadPreference (string key)
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
		{
			Query = query;

			commit_delay = new DelayedOperation (1000, new GLib.IdleHandler (CommitPendingChanges));
			Destroyed += HandleDestroy;

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
			View = new PhotoImageView (bp);

			filmstrip = new Filmstrip (bp);
			filmstrip.ThumbOffset = 1;
			filmstrip.Spacing = 4;
			filmstrip.ThumbSize = 75;
			PlaceFilmstrip ((Orientation) Preferences.Get <int> (Preferences.FILMSTRIP_ORIENTATION), true);

			View.PhotoChanged += HandlePhotoChanged;

			photo_view_scrolled = new ScrolledWindow (null, null);

			photo_view_scrolled.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
			photo_view_scrolled.ShadowType = ShadowType.None;
			photo_view_scrolled.Add (View);
			photo_view_scrolled.Child.ButtonPressEvent += HandleButtonPressEvent;
			View.AddEvents ((int) EventMask.KeyPressMask);
			inner_vbox.PackStart (photo_view_scrolled, true, true, 0);
			inner_hbox.PackStart (inner_vbox, true, true, 0);

			HBox lower_hbox = new HBox (false, 2);
			//inner_hbox.BorderWidth = 6;

			tag_view = new TagView ();
			lower_hbox.PackStart (tag_view, false, true, 0);

			Label comment = new Label (Catalog.GetString ("Description:"));
			lower_hbox.PackStart (comment, false, false, 0);
			description_entry = new Entry ();
			lower_hbox.PackStart (description_entry, true, true, 0);
			description_entry.Changed += HandleDescriptionChanged;

			rating = new RatingEntry {
				HasFrame = false,
				AlwaysShowEmptyStars = true
			};
			lower_hbox.PackStart (rating, false, false, 0);
			rating.Changed += HandleRatingChanged;

			SetColors ();

			inner_vbox.PackStart (lower_hbox, false, true, 0);

			vbox.ShowAll ();

			Realized += (o, e) => SetColors();
			Preferences.SettingChanged += OnPreferencesChanged;
		}

		public override void Dispose ()
		{
			Dispose (true);
			base.Dispose (); // base calls GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing) {
				Preferences.SettingChanged -= OnPreferencesChanged;
				// free managed resources
				if (rating != null){
					rating.Dispose ();
					rating = null;
				}
				if (description_entry != null){
					description_entry.Dispose ();
					description_entry = null;
				}
				if (tag_view != null) {
					tag_view.Dispose ();
					tag_view = null;
				}
				if (photo_view_scrolled != null) {
					photo_view_scrolled.Dispose ();
					photo_view_scrolled = null;
				}
				if (filmstrip != null) {
					filmstrip.Dispose ();
					filmstrip = null;
				}
				if (inner_vbox != null) {
					inner_vbox.Dispose ();
					inner_vbox = null;
				}
				if (inner_hbox != null) {
					inner_hbox.Dispose ();
					inner_hbox = null;
				}
			}
			// free unmanaged resources
		}

		void SetColors ()
		{
			GtkUtil.ModifyColors (filmstrip);
			GtkUtil.ModifyColors (tag_view);
			GtkUtil.ModifyColors (View);
			GtkUtil.ModifyColors (background);
			GtkUtil.ModifyColors (photo_view_scrolled);
			GtkUtil.ModifyColors (rating);

			Color dark = Style.Dark (StateType.Normal);
			filmstrip.ModifyBg (StateType.Normal, dark);
			tag_view.ModifyBg (StateType.Normal, dark);
			View.ModifyBg (StateType.Normal, dark);
			background.ModifyBg (StateType.Normal, dark);
			photo_view_scrolled.ModifyBg (StateType.Normal, dark);
			rating.ModifyBg (StateType.Normal, dark);
		}

		protected override void OnStyleSet (Style previous_style)
		{
			SetColors ();
		}
	}
}
