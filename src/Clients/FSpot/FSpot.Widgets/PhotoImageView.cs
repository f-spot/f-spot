//
// PhotoImageView.cs
//
// Author:
//   Larry Ewing <lewing@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2004-2007 Larry Ewing
// Copyright (C) 2008-2010 Ruben Vermeersch
// Copyright (C) 2007-2009 Stephane Delcroix
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
using FSpot.Loaders;
using FSpot.Settings;

using Hyena;

using Gdk;

using TagLib.Image;

namespace FSpot.Widgets
{
	public class PhotoImageView : ImageView
	{
		#region public API

		protected PhotoImageView (IntPtr raw) : base (raw) { }

		public PhotoImageView (IBrowsableCollection query) : this (new BrowsablePointer (query, -1))
		{
		}

		public PhotoImageView (BrowsablePointer item)
		{
			Preferences.SettingChanged += OnPreferencesChanged;

			this.item = item;
			item.Changed += HandlePhotoItemChanged;
		}

		public BrowsablePointer Item {
			get { return item; }
		}

		public IBrowsableCollection Query {
			get { return item.Collection; }
		}

		public Loupe Loupe {
			get { return loupe; }
		}

		public Gdk.Pixbuf CompletePixbuf ()
		{
			//FIXME: this should be an async call
			if (loader != null)
				while (loader.Loading)
					Gtk.Application.RunIteration ();
			return this.Pixbuf;
		}

		public void Reload ()
		{
			if (Item == null || !Item.IsValid)
				return;

			HandlePhotoItemChanged (this, null);
		}

		// Zoom scaled between 0.0 and 1.0
		public double NormalizedZoom {
			get { return (Zoom - MIN_ZOOM) / (MAX_ZOOM - MIN_ZOOM); }
			set { Zoom = (value * (MAX_ZOOM - MIN_ZOOM)) + MIN_ZOOM; }
		}

		public event EventHandler PhotoChanged;
		public event EventHandler PhotoLoaded;
		#endregion

		#region Gtk widgetry
		protected override void OnStyleSet (Gtk.Style previous)
		{
			CheckPattern = new CheckPattern (this.Style.Backgrounds [(int)Gtk.StateType.Normal]);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if ((evnt.State & (ModifierType.Mod1Mask | ModifierType.ControlMask)) != 0)
				return base.OnKeyPressEvent (evnt);

			bool handled = true;

			// Scroll if image is zoomed in (scrollbars are visible)
			Gtk.ScrolledWindow scrolled_w = this.Parent as Gtk.ScrolledWindow;
			bool scrolled = scrolled_w != null && !this.Fit;

			// Go to the next/previous photo when not zoomed (no scrollbars)
			switch (evnt.Key) {
			case Gdk.Key.Up:
			case Gdk.Key.KP_Up:
			case Gdk.Key.Left:
			case Gdk.Key.KP_Left:
			case Gdk.Key.h:
			case Gdk.Key.H:
			case Gdk.Key.k:
			case Gdk.Key.K:
				if (scrolled)
					handled = false;
				else
					Item.MovePrevious ();
				break;
			case Gdk.Key.Page_Up:
			case Gdk.Key.KP_Page_Up:
			case Gdk.Key.BackSpace:
			case Gdk.Key.b:
			case Gdk.Key.B:
				Item.MovePrevious ();
				break;
			case Gdk.Key.Down:
			case Gdk.Key.KP_Down:
			case Gdk.Key.Right:
			case Gdk.Key.KP_Right:
			case Gdk.Key.j:
			case Gdk.Key.J:
			case Gdk.Key.l:
			case Gdk.Key.L:
				if (scrolled)
					handled = false;
				else
					Item.MoveNext ();
				break;
			case Gdk.Key.Page_Down:
			case Gdk.Key.KP_Page_Down:
			case Gdk.Key.space:
			case Gdk.Key.KP_Space:
			case Gdk.Key.n:
			case Gdk.Key.N:
				Item.MoveNext ();
				break;
			case Gdk.Key.Home:
			case Gdk.Key.KP_Home:
				Item.Index = 0;
				break;
			case Gdk.Key.r:
			case Gdk.Key.R:
				Item.Index = new Random ().Next (0, Query.Count - 1);
				break;
			case Gdk.Key.End:
			case Gdk.Key.KP_End:
				Item.Index = Query.Count - 1;
				break;
			default:
				handled = false;
				break;
			}

			return handled || base.OnKeyPressEvent (evnt);
		}

		protected override void OnDestroyed ()
		{
			if (loader != null) {
				loader.AreaUpdated -= HandlePixbufAreaUpdated;
				loader.AreaPrepared -= HandlePixbufPrepared;
				loader.Dispose ();
			}
			base.OnDestroyed ();
		}
		#endregion

		#region loader
		uint timer;
		IImageLoader loader;
		void Load (SafeUri uri)
		{
			timer = Log.DebugTimerStart ();
			if (loader != null)
				loader.Dispose ();

			loader = ImageLoader.Create (uri);
			loader.AreaPrepared += HandlePixbufPrepared;
			loader.AreaUpdated += HandlePixbufAreaUpdated;
			loader.Completed += HandleDone;
			loader.Load (uri);
		}

		void HandlePixbufPrepared (object sender, AreaPreparedEventArgs args)
		{
			var loader = sender as IImageLoader;
			if (loader != this.loader)
				return;

			if (!ShowProgress)
				return;

			Gdk.Pixbuf prev = this.Pixbuf;
			this.Pixbuf = loader.Pixbuf;
			if (prev != null)
				prev.Dispose ();

			ZoomFit (args.ReducedResolution);
		}

		void HandlePixbufAreaUpdated (object sender, AreaUpdatedEventArgs args)
		{
			var loader = sender as IImageLoader;
			if (loader != this.loader)
				return;

			if (!ShowProgress)
				return;

			Gdk.Rectangle area = ImageCoordsToWindow (args.Area);
			QueueDrawArea (area.X, area.Y, area.Width, area.Height);
		}

		void HandleDone (object sender, EventArgs args)
		{
			Log.DebugTimerPrint (timer, "Loading image took {0}");
			IImageLoader loader = sender as IImageLoader;
			if (loader != this.loader)
				return;

			Pixbuf prev = this.Pixbuf;
			if (Pixbuf != loader.Pixbuf)
				Pixbuf = loader.Pixbuf;

			if (Pixbuf == null) {
				// FIXME: Do we have test cases for this ???

				// FIXME in some cases the image passes completely through the
				// pixbuf loader without properly loading... I'm not sure what to do about this other
				// than try to load the image one last time.
				try {
					Log.Warning ("Falling back to file loader");
					Pixbuf = PhotoLoader.Load (item.Collection, item.Index);
				} catch (Exception e) {
					LoadErrorImage (e);
				}
			}

			if (loader.Pixbuf == null) //FIXME: this test in case the photo was loaded with the direct loader
				PixbufOrientation = ImageOrientation.TopLeft;
			else
				// Accelerometer was lost, but keep this for future reference:
				// PixbufOrientation = Accelerometer.GetViewOrientation (loader.PixbufOrientation);
				PixbufOrientation = loader.PixbufOrientation;

			if (Pixbuf == null)
				LoadErrorImage (null);
			else
				ZoomFit ();

			progressive_display = true;

			if (prev != this.Pixbuf && prev != null)
				prev.Dispose ();

			PhotoLoaded?.Invoke (this, EventArgs.Empty);
		}
		#endregion

		protected BrowsablePointer item;
		protected Loupe loupe;
		protected Loupe sharpener;

		bool progressive_display = true;
		bool ShowProgress {
			get { return progressive_display; }
		}

		void LoadErrorImage (Exception e)
		{
			// FIXME we should check the exception type and do something
			// like offer the user a chance to locate the moved file and
			// update the db entry, but for now just set the error pixbuf
			Pixbuf old = Pixbuf;
			Pixbuf = new Pixbuf (PixbufUtils.ErrorPixbuf, 0, 0,
						 PixbufUtils.ErrorPixbuf.Width,
						 PixbufUtils.ErrorPixbuf.Height);

			if (old != null)
				old.Dispose ();

			PixbufOrientation = ImageOrientation.TopLeft;
			ZoomFit (false);
		}

		void HandlePhotoItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			// If it is just the position that changed fall out
			if (args != null &&
				args.PreviousItem != null &&
				Item.IsValid &&
				(args.PreviousIndex != item.Index) &&
				(Item.Current.DefaultVersion.Uri == args.PreviousItem.DefaultVersion.Uri))
				return;

			// Don't reload if the image didn't change at all.
			if (args != null && args.Changes != null &&
				!args.Changes.DataChanged &&
				args.PreviousItem != null &&
				Item.IsValid &&
				Item.Current.DefaultVersion.Uri == args.PreviousItem.DefaultVersion.Uri)
				return;

			// Same image, don't load it progressively
			if (args != null &&
				args.PreviousItem != null &&
				Item.IsValid &&
				Item.Current.DefaultVersion.Uri == args.PreviousItem.DefaultVersion.Uri)
				progressive_display = false;

			try {
				if (Item.IsValid)
					Load (Item.Current.DefaultVersion.Uri);
				else
					LoadErrorImage (null);
			} catch (Exception e) {
				Log.DebugException (e);
				LoadErrorImage (e);
			}

			Selection = Gdk.Rectangle.Zero;

			PhotoChanged?.Invoke (this, EventArgs.Empty);
		}

		void HandleLoupeDestroy (object sender, EventArgs args)
		{
			if (sender == loupe)
				loupe = null;

			if (sender == sharpener)
				sharpener = null;
		}

		public void ShowHideLoupe ()
		{
			if (loupe == null) {
				loupe = new Loupe (this);
				loupe.Destroyed += HandleLoupeDestroy;
				loupe.Show ();
			} else {
				loupe.Destroy ();
			}

		}

		public void ShowSharpener ()
		{
			if (sharpener == null) {
				sharpener = new Sharpener (this);
				sharpener.Destroyed += HandleLoupeDestroy;
			}

			sharpener.Show ();
		}

		void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE:
				Reload ();
				break;
			}
		}

		protected override void ApplyColorTransform (Pixbuf pixbuf)
		{
			Cms.Profile screen_profile;
			if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile))
				FSpot.ColorManagement.ApplyProfile (pixbuf, screen_profile);
		}

		bool crop_helpers = true;
		public bool CropHelpers {
			get { return crop_helpers; }
			set {
				if (crop_helpers == value)
					return;
				crop_helpers = value;
				QueueDraw ();
			}
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (!base.OnExposeEvent (evnt))
				return false;

			if (!CanSelect || !CropHelpers || Selection == Rectangle.Zero)
				return false;

			using (Cairo.Context ctx = CairoHelper.Create (GdkWindow)) {
				ctx.SetSourceRGBA (.7, .7, .7, .8);
				ctx.SetDash (new double [] { 10, 15 }, 0);
				ctx.LineWidth = .8;
				for (int i = 1; i < 3; i++) {
					Point s = ImageCoordsToWindow (new Point (Selection.X + Selection.Width / 3 * i, Selection.Y));
					Point e = ImageCoordsToWindow (new Point (Selection.X + Selection.Width / 3 * i, Selection.Y + Selection.Height));
					ctx.MoveTo (s.X, s.Y);
					ctx.LineTo (e.X, e.Y);
					ctx.Stroke ();
				}
				for (int i = 1; i < 3; i++) {
					Point s = ImageCoordsToWindow (new Point (Selection.X, Selection.Y + Selection.Height / 3 * i));
					Point e = ImageCoordsToWindow (new Point (Selection.X + Selection.Width, Selection.Y + Selection.Height / 3 * i));
					ctx.MoveTo (s.X, s.Y);
					ctx.LineTo (e.X, e.Y);
					ctx.Stroke ();
				}
			}
			return true;
		}
	}
}
