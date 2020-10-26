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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

			Item = item ?? throw new ArgumentNullException (nameof (item));
			item.Changed += HandlePhotoItemChanged;
		}

		public IBrowsableCollection Query {
			get => Item.Collection;
		}

		public Pixbuf CompletePixbuf ()
		{
			//FIXME: this should be an async call
			if (loader != null)
				while (loader.Loading)
					Gtk.Application.RunIteration ();

			return Pixbuf;
		}

		public void Reload ()
		{
			if (Item == null || !Item.IsValid)
				return;

			HandlePhotoItemChanged (this, null);
		}

		// Zoom scaled between 0.0 and 1.0
		public double NormalizedZoom {
			get => (Zoom - MinZoom) / (MaxZoom - MinZoom);
			set { Zoom = (value * (MaxZoom - MinZoom)) + MinZoom; }
		}

		public event EventHandler PhotoChanged;
		public event EventHandler PhotoLoaded;
		#endregion

		#region Gtk widgetry
		protected override void OnStyleSet (Gtk.Style previous)
		{
			CheckPattern = new CheckPattern (Style.Backgrounds [(int)Gtk.StateType.Normal]);
		}

		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			if ((evnt.State & (ModifierType.Mod1Mask | ModifierType.ControlMask)) != 0)
				return base.OnKeyPressEvent (evnt);

			bool handled = true;

			// Scroll if image is zoomed in (scrollbars are visible)
			bool scrolled = Parent is Gtk.ScrolledWindow && !Fit;

			// Go to the next/previous photo when not zoomed (no scrollbars)
			switch (evnt.Key) {
			case Key.Up:
			case Key.KP_Up:
			case Key.Left:
			case Key.KP_Left:
			case Key.h:
			case Key.H:
			case Key.k:
			case Key.K:
				if (scrolled)
					handled = false;
				else
					Item.MovePrevious ();
				break;
			case Key.Page_Up:
			case Key.KP_Page_Up:
			case Key.BackSpace:
			case Key.b:
			case Key.B:
				Item.MovePrevious ();
				break;
			case Key.Down:
			case Key.KP_Down:
			case Key.Right:
			case Key.KP_Right:
			case Key.j:
			case Key.J:
			case Key.l:
			case Key.L:
				if (scrolled)
					handled = false;
				else
					Item.MoveNext ();
				break;
			case Key.Page_Down:
			case Key.KP_Page_Down:
			case Key.space:
			case Key.KP_Space:
			case Key.n:
			case Key.N:
				Item.MoveNext ();
				break;
			case Key.Home:
			case Key.KP_Home:
				Item.Index = 0;
				break;
			case Key.r:
			case Key.R:
				Item.Index = new Random ().Next (0, Query.Count - 1);
				break;
			case Key.End:
			case Key.KP_End:
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
			loader?.Dispose ();

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

			Pixbuf prev = Pixbuf;
			Pixbuf = loader.Pixbuf;
			prev?.Dispose ();

			ZoomFit (args.ReducedResolution);
		}

		void HandlePixbufAreaUpdated (object sender, AreaUpdatedEventArgs args)
		{
			var loader = sender as IImageLoader;
			if (loader != this.loader)
				return;

			if (!ShowProgress)
				return;

			Rectangle area = ImageCoordsToWindow (args.Area);
			QueueDrawArea (area.X, area.Y, area.Width, area.Height);
		}

		void HandleDone (object sender, EventArgs args)
		{
			Log.DebugTimerPrint (timer, "Loading image took {0}");
			var loader = sender as IImageLoader;
			if (loader != this.loader)
				return;

			Pixbuf prev = Pixbuf;
			if (Pixbuf != loader.Pixbuf)
				Pixbuf = loader.Pixbuf;

			if (Pixbuf == null) {
				// FIXME: Do we have test cases for this ???

				// FIXME in some cases the image passes completely through the
				// pixbuf loader without properly loading... I'm not sure what to do about this other
				// than try to load the image one last time.
				try {
					Log.Warning ("Falling back to file loader");
					Pixbuf = PhotoLoader.Load (Item.Collection, Item.Index);
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

			ShowProgress = true;

			if (prev != Pixbuf)
				prev?.Dispose ();

			PhotoLoaded?.Invoke (this, EventArgs.Empty);
		}
		#endregion

		public BrowsablePointer Item { get; protected set; }
		public Loupe Loupe { get; protected set; }
		protected Loupe Sharpener { get; set; }

		bool ShowProgress { get; set; } = true;

		void LoadErrorImage (Exception e)
		{
			// FIXME we should check the exception type and do something
			// like offer the user a chance to locate the moved file and
			// update the db entry, but for now just set the error pixbuf
			Pixbuf old = Pixbuf;
			Pixbuf = new Pixbuf (PixbufUtils.ErrorPixbuf, 0, 0,
			          PixbufUtils.ErrorPixbuf.Width,
						 PixbufUtils.ErrorPixbuf.Height);

			old?.Dispose ();

			PixbufOrientation = ImageOrientation.TopLeft;
			ZoomFit (false);
		}

		void HandlePhotoItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			// If it is just the position that changed fall out
			if (args?.PreviousItem != null && Item.IsValid &&
				(args.PreviousIndex != Item.Index) &&
				(Item.Current.DefaultVersion.Uri == args.PreviousItem.DefaultVersion.Uri))
				return;

			// Don't reload if the image didn't change at all.
			if (args?.Changes != null && !args.Changes.DataChanged &&
				args.PreviousItem != null && Item.IsValid &&
				Item.Current.DefaultVersion.Uri == args.PreviousItem.DefaultVersion.Uri)
				return;

			// Same image, don't load it progressively
			if (args?.PreviousItem != null && Item.IsValid &&
				Item.Current.DefaultVersion.Uri == args.PreviousItem.DefaultVersion.Uri)
				ShowProgress = false;

			try {
				if (Item.IsValid)
					Load (Item.Current.DefaultVersion.Uri);
				else
					LoadErrorImage (null);
			} catch (Exception e) {
				Log.DebugException (e);
				LoadErrorImage (e);
			}

			Selection = Rectangle.Zero;

			PhotoChanged?.Invoke (this, EventArgs.Empty);
		}

		void HandleLoupeDestroy (object sender, EventArgs args)
		{
			if (sender == Loupe)
				Loupe = null;

			if (sender == Sharpener)
				Sharpener = null;
		}

		public void ShowHideLoupe ()
		{
			if (Loupe == null) {
				Loupe = new Loupe (this);
				Loupe.Destroyed += HandleLoupeDestroy;
				Loupe.Show ();
			} else {
				Loupe.Destroy ();
			}
		}

		public void ShowSharpener ()
		{
			if (Sharpener == null) {
				Sharpener = new Sharpener (this);
				Sharpener.Destroyed += HandleLoupeDestroy;
			}

			Sharpener.Show ();
		}

		void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case Preferences.ColorManagementDisplayProfile:
				Reload ();
				break;
			}
		}

		protected override void ApplyColorTransform (Pixbuf pixbuf)
		{
			if (ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.ColorManagementDisplayProfile), out var screenProfile))
				ColorManagement.ApplyProfile (pixbuf, screenProfile);
		}

		bool cropHelpers = true;
		public bool CropHelpers {
			get => cropHelpers;
			set {
				if (cropHelpers == value)
					return;
				cropHelpers = value;
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
