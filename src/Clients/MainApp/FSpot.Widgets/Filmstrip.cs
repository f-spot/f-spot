//
// Filmstrip.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Ruben Vermeersch <ruben@savanne.be>
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2014 Daniel Köb
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2008-2009 Lorenzo Milesi
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

//TODO:
//	* only redraw required parts on ExposeEvents (low)
//	* Handle orientation changes (low) (require gtk# changes, so I can trigger an OrientationChanged event)

using System;
using System.Collections.Generic;
using FSpot.Bling;
using FSpot.Core;
using FSpot.Imaging;
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.Utils;
using Gdk;
using Gtk;
using Hyena;

namespace FSpot.Widgets
{
	public class Filmstrip : EventBox, IDisposable
	{
		bool disposed;

//		public event OrientationChangedHandler OrientationChanged;
		public event EventHandler PositionChanged;

		DoubleAnimation animation;

		bool extendable = true;
		public bool Extendable {
			get { return extendable; }
			set { extendable = value; }
		}

		Orientation orientation = Orientation.Horizontal;
		public Orientation Orientation {
			get { return orientation; }
			set {
				if (orientation == value)
					return;

				BackgroundPixbuf = null;
				orientation = value;
//				if (OrientationChanged != null) {
//					OrientationChangedArgs args = new OrientationChangedArgs ();
//					args.Orientation = value;
//					OrientationChanged (this, args);
//				}
			}
		}

		int spacing = 6;
		public int Spacing {
			get { return spacing; }
			set {
				if (value < 0)
					throw new ArgumentException ("Spacing is negative!");
				spacing = value;
			}
		}

		int thumb_offset = 17;
		public int ThumbOffset {
			get { return thumb_offset; }
			set {
				if (value < 0)
					throw new ArgumentException ("ThumbOffset is negative!");
				thumb_offset = value;
			}
		}

		int thumb_size = 67;
		public int ThumbSize {
			get { return thumb_size; }
			set {
				if (value < 0)
					throw new ArgumentException ("ThumbSize is negative!");
				thumb_size = value;
			}
		}

		public bool SquaredThumbs { get; set; }

		Pixbuf background_tile;
		public Pixbuf BackgroundTile {
			get {
				if (background_tile == null) {
					background_tile = new Pixbuf (Colorspace.Rgb, true, 8, 1, 77);
					background_tile.Fill (0x00000000);
				}

				if (Orientation == Orientation.Horizontal && background_tile.Height < background_tile.Width) {
					var temp_tile = background_tile.RotateSimple (PixbufRotation.Counterclockwise);
					background_tile.Dispose ();
					background_tile = temp_tile;
				} else if (Orientation == Orientation.Vertical && background_tile.Width < background_tile.Height) {
					var temp_tile = background_tile.RotateSimple (PixbufRotation.Clockwise);
					background_tile.Dispose ();
					background_tile = temp_tile;
				}
				return background_tile;
			}
			set {
				if (background_tile != value && background_tile != null)
					background_tile.Dispose ();
				background_tile = value;
				BackgroundPixbuf = null;
			}
		}

		int x_offset = 2;
		public int XOffset {
			get { return x_offset; }
			set {
				if (value < 0)
					throw new ArgumentException ("value is negative!");
				x_offset = value;
			}
		}

		int y_offset = 2;
		public int YOffset {
			get { return y_offset; }
			set {
				if (value < 0)
					throw new ArgumentException ("value is negative!");
				y_offset = value;
			}
		}

		float x_align = 0.5f, y_align = 0.5f;
		public float XAlign {
			get { return x_align; }
			set {
				if (value < 0.0 || value > 1.0)
					throw new ArgumentException ("value is not between 0.0 and 1.0");
				x_align = value;
			}
		}

		public float YAlign {
			get { return y_align; }
			set {
				if (value < 0.0 || value > 1.0)
					throw new ArgumentException ("value is not between 0.0 and 1.0");
				y_align = value;
			}
		}

		public int ActiveItem {
			get { return selection.Index; }
			set {
				if (value == selection.Index)
					return;
				if (value < 0)
					value = 0;
				if (value > selection.Collection.Count - 1)
					value = selection.Collection.Count - 1;

				selection.Index = value;
			}
		}

		double position;
		public double Position {
			get {
				return position;
			}
			set {
				if (value == position)
					return;
				if (value < 0)
					value = 0;
				if (value > selection.Collection.Count - 1)
					value = selection.Collection.Count - 1;

				animation.From = position;
				animation.To = value;
				animation.Restart ();

				if (PositionChanged != null)
					PositionChanged (this, EventArgs.Empty);
			}
		}

		readonly BrowsablePointer selection;
		DisposableCache<SafeUri, Pixbuf> thumb_cache;

		public Filmstrip (BrowsablePointer selection) : this (selection, true)
		{
		}

		public Filmstrip (BrowsablePointer selection, bool squaredThumbs)
		{
			CanFocus = true;
			this.selection = selection;
			this.selection.Changed += HandlePointerChanged;
			this.selection.Collection.Changed += HandleCollectionChanged;
			this.selection.Collection.ItemsChanged += HandleCollectionItemsChanged;
			SquaredThumbs = squaredThumbs;
			thumb_cache = new DisposableCache<SafeUri, Pixbuf> (30);
			App.Instance.Container.Resolve<IThumbnailLoader> ().OnPixbufLoaded += HandlePixbufLoaded;

			animation = new DoubleAnimation (0, 0, TimeSpan.FromSeconds (1.5), SetPositionCore, new CubicEase (EasingMode.EaseOut));
		}

		const int min_length = 400;
		const int min_height = 200;
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = (Orientation == Orientation.Horizontal ? min_length : BackgroundTile.Width) + 2 * x_offset;
			requisition.Height = (Orientation == Orientation.Vertical ? min_height : BackgroundTile.Height) + 2 * y_offset;
			switch (Orientation) {
			case Orientation.Horizontal:
				if (min_length % BackgroundTile.Width != 0)
					requisition.Width += BackgroundTile.Width - min_length % BackgroundTile.Width;
				break;
			case Orientation.Vertical:
				if (min_height % BackgroundTile.Height != 0)
					requisition.Height += BackgroundTile.Height - min_height % BackgroundTile.Height;
				break;
			}
		}

		Pixbuf background_pixbuf;
		protected Pixbuf BackgroundPixbuf {
			get {
				if (background_pixbuf == null) {
					int length = BackgroundTile.Width;
					int height = BackgroundTile.Height;
					switch (Orientation) {
					case Orientation.Horizontal:
						if (Allocation.Width < min_length || !extendable)
							length = min_length;
						else
							length = Allocation.Width;

						length = length - length % BackgroundTile.Width;
						break;
					case Orientation.Vertical:
						if (Allocation.Height < min_height || !extendable)
							height = min_height;
						else
							height = Allocation.Height;

						height = height - height % BackgroundTile.Height;
						break;
					}

					background_pixbuf = new Pixbuf (Colorspace.Rgb, true, 8, length, height);
					switch (Orientation) {
					case Orientation.Horizontal:
						for (int i = 0; i < length; i += BackgroundTile.Width) {
							BackgroundTile.CopyArea (0, 0, BackgroundTile.Width, BackgroundTile.Height,
									background_pixbuf, i, 0);
						}
						break;
					case Orientation.Vertical:
						for (int i = 0; i < height; i += BackgroundTile.Height) {
							BackgroundTile.CopyArea (0, 0, BackgroundTile.Width, BackgroundTile.Height,
									background_pixbuf, 0, i);
						}
						break;
					}
				}
				return background_pixbuf;
			}
			set {
				if (background_pixbuf != value && background_pixbuf != null) {
					background_pixbuf.Dispose ();
				}
				background_pixbuf = value;
			}
		}

		Dictionary<int,int> start_indexes;
		int filmstrip_start_pos;
		int filmstrip_end_pos;
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (evnt.Window != GdkWindow)
				return true;

			if (selection.Collection.Count == 0)
				return true;

			if (Orientation == Orientation.Horizontal && (extendable && Allocation.Width >= BackgroundPixbuf.Width + (2 * x_offset) + BackgroundTile.Width) ||
				Orientation == Orientation.Vertical && (extendable && Allocation.Height >= BackgroundPixbuf.Height + (2 * y_offset) + BackgroundTile.Height) )
				BackgroundPixbuf = null;

			if ( Orientation == Orientation.Horizontal && (extendable && Allocation.Width < BackgroundPixbuf.Width + (2 * x_offset) ) ||
				Orientation == Orientation.Vertical && ( extendable && Allocation.Height < BackgroundPixbuf.Height + (2 * y_offset) ))
				BackgroundPixbuf = null;

			int xpad = 0, ypad = 0;
			if (Allocation.Width > BackgroundPixbuf.Width + (2 * x_offset))
				xpad = (int) (x_align * (Allocation.Width - (BackgroundPixbuf.Width + (2 * x_offset))));

			if (Allocation.Height > BackgroundPixbuf.Height + (2 * y_offset))
				ypad = (int) (y_align * (Allocation.Height - (BackgroundPixbuf.Height + (2 * y_offset))));

			GdkWindow.DrawPixbuf (Style.BackgroundGC (StateType.Normal), BackgroundPixbuf,
					0, 0, x_offset + xpad, y_offset + ypad,
					BackgroundPixbuf.Width, BackgroundPixbuf.Height, RgbDither.None, 0, 0);

			//drawing the icons...
			start_indexes = new Dictionary<int, int> ();

			Pixbuf icon_pixbuf = null;
			if (Orientation == Orientation.Horizontal)
				icon_pixbuf = new Pixbuf (Colorspace.Rgb, true, 8, BackgroundPixbuf.Width, thumb_size);
			else if (Orientation == Orientation.Vertical)
				icon_pixbuf = new Pixbuf (Colorspace.Rgb, true, 8, thumb_size, BackgroundPixbuf.Height);
			icon_pixbuf.Fill (0x00000000);

			Pixbuf current = GetPixbuf ((int) Math.Round (Position));
			int ref_x = (int)(icon_pixbuf.Width / 2.0 - current.Width * (Position + 0.5f - Math.Round (Position))); //xpos of the reference icon
			int ref_y = (int)(icon_pixbuf.Height / 2.0 - current.Height * (Position + 0.5f - Math.Round (Position)));

			int start_x = Orientation == Orientation.Horizontal ? ref_x : 0;
			int start_y = Orientation == Orientation.Vertical ? ref_y : 0;
			for (int i = (int) Math.Round (Position); i < selection.Collection.Count; i++) {
				current = GetPixbuf (i, ActiveItem == i);
				if (Orientation == Orientation.Horizontal) {
					current.CopyArea (0, 0, Math.Min (current.Width, icon_pixbuf.Width - start_x) , current.Height, icon_pixbuf, start_x, start_y);
					start_indexes [start_x] = i;
					start_x += current.Width + spacing;
					if (start_x > icon_pixbuf.Width)
						break;
				} else if (Orientation == Orientation.Vertical) {
					current.CopyArea (0, 0, current.Width, Math.Min (current.Height, icon_pixbuf.Height - start_y), icon_pixbuf, start_x, start_y);
					start_indexes [start_y] = i;
					start_y += current.Height + spacing;
					if (start_y > icon_pixbuf.Height)
						break;
				}
			}
			filmstrip_end_pos = (Orientation == Orientation.Horizontal ? start_x : start_y);

			start_x = Orientation == Orientation.Horizontal ? ref_x : 0;
			start_y = Orientation == Orientation.Vertical ? ref_y : 0;
			for (int i = (int) Math.Round (Position) - 1; i >= 0; i--) {
				current = GetPixbuf (i, ActiveItem == i);
				if (Orientation == Orientation.Horizontal) {
					start_x -= (current.Width + spacing);
					current.CopyArea (Math.Max (0, -start_x), 0, Math.Min (current.Width, current.Width + start_x), current.Height, icon_pixbuf, Math.Max (start_x, 0), 0);
					start_indexes [Math.Max (0, start_x)] = i;
					if (start_x < 0)
						break;
				} else if (Orientation == Orientation.Vertical) {
					start_y -= (current.Height + spacing);
					current.CopyArea (0, Math.Max (0, -start_y), current.Width, Math.Min (current.Height, current.Height + start_y), icon_pixbuf, 0, Math.Max (start_y, 0));
					start_indexes [Math.Max (0, start_y)] = i;
					if (start_y < 0)
						break;
				}
			}
			filmstrip_start_pos = Orientation == Orientation.Horizontal ? start_x : start_y;

			GdkWindow.DrawPixbuf (Style.BackgroundGC (StateType.Normal), icon_pixbuf,
					0, 0, x_offset + xpad, y_offset + ypad + thumb_offset,
					icon_pixbuf.Width, icon_pixbuf.Height, RgbDither.None, 0, 0);

			icon_pixbuf.Dispose ();

			return true;
		}

		protected override bool OnScrollEvent (EventScroll evnt)
		{
			float shift = 1f;
			if ((evnt.State & ModifierType.ShiftMask) > 0)
				shift = 6f;

			switch (evnt.Direction) {
			case ScrollDirection.Up:
			case ScrollDirection.Right:
				Position = animation.To - shift;
				return true;
			case ScrollDirection.Down:
			case ScrollDirection.Left:
				Position = animation.To + shift;
				return true;
			}
			return false;
		}

		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			switch (evnt.Key) {
			case Gdk.Key.Page_Down:
			case Gdk.Key.Down:
			case Gdk.Key.Right:
				ActiveItem ++;
				return true;

			case Gdk.Key.Page_Up:
			case Gdk.Key.Up:
			case Gdk.Key.Left:
				ActiveItem --;
				return true;
			}
			return false;
		}

		protected virtual void SetPositionCore (double position)
		{
			if (this.position == position)
				return;
			if (position < 0)
				position = 0;
			if (position > selection.Collection.Count - 1)
				position = selection.Collection.Count - 1;


			this.position = position;
			QueueDraw ();
		}

		void HandlePointerChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			Position = ActiveItem;
		}

		void HandleCollectionChanged (IBrowsableCollection coll)
		{
			position = ActiveItem;
			QueueDraw ();
		}

		void HandleCollectionItemsChanged (IBrowsableCollection coll, BrowsableEventArgs args)
		{
			if (!args.Changes.DataChanged)
				return;
			foreach (int item in args.Items)
				thumb_cache.TryRemove ((selection.Collection [item]).DefaultVersion.Uri);

			//FIXME call QueueDrawArea
			QueueDraw ();
		}

		void HandlePixbufLoaded (IImageLoaderThread pl, RequestItem item) {
			if (!thumb_cache.Contains (item.Uri)) {
				return;
			}

			//FIXME use QueueDrawArea
			//FIXME only invalidate if displayed
			QueueDraw ();
		}

		protected override bool OnPopupMenu ()
		{
			DrawOrientationMenu (null);
			return true;
		}

		bool DrawOrientationMenu (EventButton args)
		{
			Menu placement_menu = new Menu ();
			GtkUtil.MakeCheckMenuItem (placement_menu,
							Mono.Unix.Catalog.GetString ("_Horizontal"),
							App.Instance.Organizer.HandleFilmstripHorizontal,
							true, Orientation == Orientation.Horizontal, true);
			GtkUtil.MakeCheckMenuItem (placement_menu,
							Mono.Unix.Catalog.GetString ("_Vertical"),
							App.Instance.Organizer.HandleFilmstripVertical,
							true, Orientation == Orientation.Vertical, true);

			if (args != null)
				placement_menu.Popup (null, null, null, args.Button, args.Time);
			else
				placement_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);

			return true;
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button == 3)
				return DrawOrientationMenu (evnt);

			if (evnt.Button != 1 || (
				(Orientation == Orientation.Horizontal && (evnt.X > filmstrip_end_pos || evnt.X < filmstrip_start_pos)) ||
				(Orientation == Orientation.Vertical && (evnt.Y > filmstrip_end_pos || evnt.Y < filmstrip_start_pos))
				))
				return false;
			HasFocus = true;
			int pos = -1;
			foreach (int key in start_indexes.Keys)
				if (key <= (Orientation == Orientation.Horizontal ? evnt.X : evnt.Y) && key > pos)
					pos = key;
			ActiveItem = start_indexes [pos];
			return true;
		}

		protected Pixbuf GetPixbuf (int i)
		{
			return GetPixbuf (i, false);
		}

		protected virtual Pixbuf GetPixbuf (int i, bool highlighted)
		{
			Pixbuf current = null;
			SafeUri uri = (selection.Collection [i]).DefaultVersion.Uri;
			try {
				var pixbuf = thumb_cache.Get (uri);
				if (pixbuf != null)
					current = pixbuf.ShallowCopy ();
			} catch (IndexOutOfRangeException) {
				current = null;
			}

			if (current == null) {
				var pixbuf = App.Instance.Container.Resolve<IThumbnailService> ().GetThumbnail (uri, ThumbnailSize.Large);
				if (pixbuf == null) {
					App.Instance.Container.Resolve<IThumbnailLoader> ().Request (uri, ThumbnailSize.Large, 0);
					current = FSpot.Settings.Global.IconTheme.LoadIcon ("gtk-missing-image", ThumbSize, (IconLookupFlags)0);
				} else {
					if (SquaredThumbs) {
						current = PixbufUtils.IconFromPixbuf (pixbuf, ThumbSize);
					} else {
						current = pixbuf.ScaleSimple (ThumbSize, ThumbSize, InterpType.Nearest);
					}
					pixbuf.Dispose ();
					thumb_cache.Add (uri, current);
				}
			}

			//FIXME: we might end up leaking a pixbuf here
			Cms.Profile screen_profile;
			if (ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile)) {
				Pixbuf t = current.Copy ();
				current = t;
				ColorManagement.ApplyProfile (current, screen_profile);
			}

			// Add a four pixel white border around the thumbnail
			// for some reason we cannot use "using" here, it looks like the pixbuf copy is not done properly
			Pixbuf whiteBorder = new Pixbuf (Colorspace.Rgb, true, 8, current.Width, current.Height);
			whiteBorder.Fill (0);
			current.CopyArea (1, 1, current.Width - 8, current.Height - 8, whiteBorder, 4, 4);
			current = whiteBorder;

			if (!highlighted)
				return current;

			Pixbuf highlight = new Pixbuf (Colorspace.Rgb, true, 8, current.Width, current.Height);

			highlight.Fill (ColorToInt (Style.Light (StateType.Selected)));

			// Add a two pixel highlight around the thumbnail
			current.CopyArea (2, 2, current.Width - 4, current.Height - 4, highlight, 2, 2);

			return highlight;
		}

		static uint ColorToInt(Color color) {
			return ((uint)color.Red / 256 << 24) + ((uint)color.Green / 256 << 16) + ((uint)color.Blue / 256 << 8) + 255;
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
				// free managed resources
				selection.Changed -= HandlePointerChanged;
				selection.Collection.Changed -= HandleCollectionChanged;
				selection.Collection.ItemsChanged -= HandleCollectionItemsChanged;
				App.Instance.Container.Resolve<IThumbnailLoader> ().OnPixbufLoaded -= HandlePixbufLoaded;
				if (background_pixbuf != null) {
					background_pixbuf.Dispose ();
					background_pixbuf = null;
				}
				if (background_tile != null) {
					background_tile.Dispose ();
					background_tile = null;
				}
				if (thumb_cache != null) {
					thumb_cache.Dispose ();
					thumb_cache = null;
				}
			}
			// free unmanaged resources
		}
	}
}
