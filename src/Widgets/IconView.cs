/*
* Widgets/IconView.cs
*
* Author(s):
* 	Etore Perazzoli
*	Larry Ewing <lewing@novell.com>
*	Stephane Delcroix <stephane@delcroix.org>
*
* This is free software. See COPYING for details.
*/

using Gtk;
using Gdk;
using Gnome;
using GtkSharp;
using System;
using System.Reflection;
using System.Collections;
using System.IO;

namespace FSpot.Widgets
{
	public class IconView : Gtk.Layout {

		// Public properties.
		FSpot.PixbufCache cache;

		/* preserve the scroll postion when possible */
		private bool scroll;
		private double scroll_value;

		/* suppress it sometimes */
		bool suppress_scroll = false;

		// Zooming factor.
		protected const double ZOOM_FACTOR = 1.2;

		/* Width of the thumbnails. */
		protected int thumbnail_width = 128;
		protected const int MAX_THUMBNAIL_WIDTH = 256;
		protected const int MIN_THUMBNAIL_WIDTH = 64;
		public int ThumbnailWidth {
			get {
				return thumbnail_width;
			}
			set {
				value = Math.Min(value, MAX_THUMBNAIL_WIDTH);
				value = Math.Max(value, MIN_THUMBNAIL_WIDTH);

				if (thumbnail_width != value) {
					thumbnail_width = value;
					QueueResize ();

					if (ZoomChanged != null)
						ZoomChanged (this, System.EventArgs.Empty);
				}
			}
		}

		public double Zoom {
			get {
				return ((double)(ThumbnailWidth - MIN_THUMBNAIL_WIDTH) / (double)(MAX_THUMBNAIL_WIDTH - MIN_THUMBNAIL_WIDTH));
			}
			set {
				ThumbnailWidth = (int) ((value) * (MAX_THUMBNAIL_WIDTH - MIN_THUMBNAIL_WIDTH)) + MIN_THUMBNAIL_WIDTH;
			}
		}

		protected double thumbnail_ratio = 4.0 / 3.0;
		public double ThumbnailRatio {
			get {
				return thumbnail_ratio;
			}
			set {
				thumbnail_ratio = value;
				QueueResize ();
			}
		}

		public int ThumbnailHeight {
			get {
				return (int) Math.Round ((double) thumbnail_width / ThumbnailRatio);
			}
		}

		public FSpot.PixbufCache Cache {
			get {
				return cache;
			}
		}

		private bool display_tags = true;
		public bool DisplayTags {
			get {
				return display_tags;
			}

			set {
				display_tags = value;
				QueueResize ();
			}
		}

		private bool display_dates = true;
		public bool DisplayDates {
			get {
				if (cell_width > 100)
					return display_dates;
				else
					return false;
			}

			set {
				display_dates = value;
				QueueResize ();
			}
		}

		private bool display_filenames = false;
		public bool DisplayFilenames {
			get { return display_filenames; }
			set {
				if (value != display_filenames) {
					display_filenames = value;
					QueueResize ();
				}
			}
		}

		private bool display_ratings = true;
		public bool DisplayRatings {
			get {
				if (cell_width > 100)
					return display_ratings;
				else
					return false;
			}

			set {
				display_ratings  = value;
				QueueResize ();
			}
		}

		// Size of the frame around the thumbnail.
		protected int cell_border_width = 10;

		// Border around the scrolled area.
		protected const int BORDER_SIZE = 6;

		// Thickness of the outline used to indicate selected items.
		private const int SELECTION_THICKNESS = 5;

		// Size of the tag icon in the view.
		protected int tag_icon_size = 16;

		// Horizontal spacing between the tag icons
		protected int tag_icon_hspacing = 2;

		// Vertical spacing between the thumbnail and additional infos (tags, dates, ...).
		protected int tag_icon_vspacing = 3;

		// Various other layout values.
		protected int cells_per_row;
		protected int cell_width;
		protected int cell_height;
		protected int displayed_rows; //for pgUp pgDn support

		// The first pixel line that is currently on the screen (i.e. in the current
		// scroll region).  Used to compute the area that went offscreen in the "changed"
		// signal handler for the vertical GtkAdjustment.
		private int y_offset;
		private int x_offset;

		// Focus Handling
		private int real_focus_cell;
		public int FocusCell {
			set {
				if (value != real_focus_cell) {
					value = Math.Max (value, 0);
					value = Math.Min (value, collection.Count - 1);
					InvalidateCell (value);
					InvalidateCell (real_focus_cell);
					real_focus_cell = value;
				}
			}
			get {
				return real_focus_cell;
			}
		}
		// Number of consecutive GDK_BUTTON_PRESS on the same cell, to
		// distinguish the GDK_2BUTTON_PRESS events that we actually care
		// about.
		private int click_count;

		// Public events.
		public delegate void DoubleClickedHandler (Widget widget, BrowsableEventArgs args);
		public event DoubleClickedHandler DoubleClicked;

		public delegate void ZoomChangedHandler (object sender, System.EventArgs args);
		public event ZoomChangedHandler ZoomChanged;

		// Public API.
		public IconView (IntPtr raw) : base (raw) {}

		protected IconView () : base (null, null)
		{
			cache = new FSpot.PixbufCache ();
			cache.OnPixbufLoaded += HandlePixbufLoaded;

			ScrollAdjustmentsSet += new ScrollAdjustmentsSetHandler (HandleScrollAdjustmentsSet);

			ButtonPressEvent += new ButtonPressEventHandler (HandleButtonPressEvent);
			ButtonReleaseEvent += new ButtonReleaseEventHandler (HandleButtonReleaseEvent);
			KeyPressEvent += new KeyPressEventHandler (HandleKeyPressEvent);
			ScrollEvent += new ScrollEventHandler(HandleScrollEvent);

			Destroyed += HandleDestroyed;

			AddEvents ((int) EventMask.KeyPressMask
			| (int) EventMask.KeyReleaseMask
			| (int) EventMask.PointerMotionMask);

			CanFocus = true;

			//FSpot.Global.ModifyColors (this);
		}

		public IconView (FSpot.IBrowsableCollection collection) : this ()
		{
			this.collection = collection;
			this.selection = new SelectionCollection (collection);

			Name = "ImageContainer";
			collection.Changed += HandleChanged;
			collection.ItemsChanged += HandleItemsChanged;

			selection.DetailedChanged += HandleSelectionChanged;
		}

		private void HandleSelectionChanged (FSpot.IBrowsableCollection collection, int [] ids)
		{
			if (ids == null)
				QueueDraw ();
			else
				foreach (int id in ids)
					InvalidateCell (id);
		}

		private void HandleChanged (FSpot.IBrowsableCollection sender)
		{
			// FIXME we should probably try to merge the selection forward
			// but it needs some thought to be efficient.
			suppress_scroll = true;
			QueueResize ();
		}

		private void HandleItemsChanged (FSpot.IBrowsableCollection sender, BrowsableEventArgs args)
		{
			foreach (int item in args.Items) {
				if (args.Changes.DataChanged)
					UpdateThumbnail (item);
				InvalidateCell (item);
			}
		}

		//
		// IPhotoSelection
		//

		protected FSpot.IBrowsableCollection collection;
		public FSpot.IBrowsableCollection Collection {
			get {
				return collection;
			}
		}

		protected SelectionCollection selection;
		public SelectionCollection Selection {
			get {
				return selection;
			}
		}

		// FIXME right now a selection change triggers a complete view redraw
		// This should be optimized away by directly notifyiing the view of changed
		// indexes rather than having the view connect to the collection.Changed event.
		public class SelectionCollection : IBrowsableCollection {
			IBrowsableCollection parent;
			Hashtable selected_cells;
			int [] selection;
			IBrowsableItem [] items;
			IBrowsableItem [] old;

			public SelectionCollection (IBrowsableCollection collection)
			{
				this.selected_cells = new Hashtable ();
				this.parent = collection;
				this.parent.Changed += HandleParentChanged;
				this.parent.ItemsChanged += HandleParentItemsChanged;
			}

			private void HandleParentChanged (IBrowsableCollection collection)
			{
				IBrowsableItem [] local = old;
				selected_cells.Clear ();
				ClearCached ();

				if (old != null) {
					int i = 0;

					for (i = 0; i < local.Length; i++) {
						int parent_index = parent.IndexOf (local [i]);
						if (parent_index >= 0)
							this.Add (parent_index, false);
					}
				}

				// Call the directly so that we don't reset old immediately this way the old selection
				// set isn't actually lost until we change it.
				if (this.Changed != null)
					Changed (this);

				if (this.DetailedChanged != null)
					DetailedChanged (this, null);

			}

			public void MarkChanged (int item, IBrowsableItemChanges changes)
			{
				// Forward the change event up to our parent
				// we'll fire the event when the parent calls us back
				parent.MarkChanged ((int) selected_cells [item], changes);
			}

			private void HandleParentItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args)
			{
				if (this.ItemsChanged == null)
					return;

				ArrayList local_ids = new ArrayList ();
				foreach (int parent_index in args.Items) {
					// If the item isn't part of the selection ignore it
					if (!this.Contains (collection [parent_index]))
						return;

					int local_index = this.IndexOf (parent_index);
					if (local_index >= 0)
						local_ids.Add (local_index);
				}

				if (local_ids.Count == 0)
					return;

				int [] items = (int [])local_ids.ToArray (typeof (int));
				ItemsChanged (this, new BrowsableEventArgs (items, args.Changes));
			}

			public int [] Ids {
				get {
					if (selection != null)
						return selection;

					selection = new int [selected_cells.Count];

					int i = 0;
					foreach (int cell in selected_cells.Values)
						selection [i ++] = cell;

					Array.Sort (selection);
					return selection;
				}
			}

			public IBrowsableItem this [int index] {
				get {
					int [] ids = this.Ids;
					return parent [ids[index]];
				}
			}

			public IBrowsableItem [] Items {
				get {
					if (items != null)
						return items;

					int [] ids = this.Ids;
					items = new IBrowsableItem [ids.Length];
					for (int i = 0; i < items.Length; i++) {
						items [i] = parent [ids[i]];
					}
					return items;
				}
			}

			public void Clear ()
			{
				Clear (true);
			}

			public void Clear (bool update)
			{
				int [] ids = Ids;
				selected_cells.Clear ();
				SignalChange (ids);
			}

			public void Add (IBrowsableItem item)
			{
				if (this.Contains (item))
					return;

				int index = parent.IndexOf (item);
				this.Add (index);
			}

			public int Count {
				get {
					return selected_cells.Count;
				}
			}

			public bool Contains (IBrowsableItem item)
			{
				return selected_cells.ContainsKey (item);
			}

			public bool Contains (int num)
			{
				if (num < 0 || num > parent.Count)
					return false;

				return this.Contains (parent [num]);
			}

			public void Add (int num)
			{
				this.Add (num, true);
			}

			private void Add (int num, bool notify)
			{
				if (num == -1)
					return;

				if (this.Contains (num))
					return;

				IBrowsableItem item = parent [num];
				selected_cells [item] = num;

				if (notify)
					SignalChange (new int [] {num});
			}

			public void Add (int start, int end)
			{
				if (start == -1 || end == -1)
					return;

				int current = Math.Min (start, end);
				int final = Math.Max (start, end);
				int count = final - current + 1;
				int [] ids = new int [count];

				for (int i = 0; i < count; i++) {
					this.Add (current, false);
					ids [i] = current;
					current++;
				}

				SignalChange (ids);
			}

			// Remove a range, except the start entry
			public void Remove (int start, int end)
			{
				if (start == -1 || end == -1)
					return;

				int current = Math.Min (start + 1, end);
				int final = Math.Max (start - 1, end);
				int count = final - current + 1;
				int [] ids = new int [count];

				for (int i = 0; i < count; i++) {
					this.Remove (current, false);
					ids [i] = current;
					current++;
				}

				SignalChange (ids);
			}

			public int IndexOf (int parent_index)
			{
				return System.Array.IndexOf (this.Ids, parent_index);
			}

			public int IndexOf (IBrowsableItem item)
			{
				if (!this.Contains (item))
					return -1;

				int parent_index = (int) selected_cells [item];
				return System.Array.IndexOf (Ids, parent_index);
			}

			public void Remove (int cell)
			{
				Remove (cell, true);
			}

			private void Remove (int cell, bool notify)
			{
				IBrowsableItem item = parent [cell];
				if (item != null)
					this.Remove (item, notify);

			}

			public void Remove (IBrowsableItem item)
			{
				Remove (item, true);
			}

			private void Remove (IBrowsableItem item, bool notify)
			{
				if (item == null)
					return;

				int parent_index = (int) selected_cells [item];
				selected_cells.Remove (item);

				if (notify)
					SignalChange (new int [] {parent_index});
			}

			public event IBrowsableCollectionChangedHandler Changed;
			public event IBrowsableCollectionItemsChangedHandler ItemsChanged;

			public delegate void DetailedCollectionChanged (IBrowsableCollection collection, int [] ids);
			public event DetailedCollectionChanged DetailedChanged;

			private void ClearCached ()
			{
				selection = null;
				items = null;
			}

			private void SignalChange (int [] ids)
			{
				ClearCached ();
				old = this.Items;


				if (Changed != null)
					Changed (this);

				if (DetailedChanged!= null)
					DetailedChanged (this, ids);
			}
		}

		// Updating.
		public void UpdateThumbnail (int thumbnail_num)
		{
			FSpot.IBrowsableItem photo = collection [thumbnail_num];
			string thumbnail_path = FSpot.ThumbnailGenerator.ThumbnailPath (photo.DefaultVersionUri);
			cache.Remove (thumbnail_path);
			InvalidateCell (thumbnail_num);
		}

		// Cell Geometry
		public int CellAtPosition (int x, int y)
		{
			return CellAtPosition (x, y, true);
		}

		public int CellAtPosition (int x, int y, bool crop_visible)
		{
			if (collection == null)
				return -1;

			if (crop_visible
				&& ((y < (int)Vadjustment.Value || y > (int)Vadjustment.Value + Allocation.Height)
			|| (x < (int)Hadjustment.Value || x > (int)Hadjustment.Value + Allocation.Width)))
			return -1;

			if (x < BORDER_SIZE || x >= BORDER_SIZE + cells_per_row * cell_width)
				return -1;
			if (y < BORDER_SIZE || y >= BORDER_SIZE + (collection.Count / cells_per_row + 1) * cell_height)
				return -1;

			int column = (int) ((x - BORDER_SIZE) / cell_width);
			int row = (int) ((y - BORDER_SIZE) / cell_height);
			int cell_num = column + row * cells_per_row;

			if (cell_num < collection.Count)
				return (int) cell_num;
			else
				return -1;
		}

		public int TopLeftVisibleCell ()
		{
			//return CellAtPosition(BORDER_SIZE, (int)Vadjustment.Value + BORDER_SIZE + 8);
			return CellAtPosition(BORDER_SIZE, (int) (Vadjustment.Value + Allocation.Height * (Vadjustment.Value / Vadjustment.Upper)) + BORDER_SIZE + 8);
		}

		public void GetCellCenter (int cell_num, out int x, out int y)
		{
			if (cell_num == -1) {
				x = -1;
				y = -1;
			}

			x = BORDER_SIZE + (cell_num % cells_per_row) * cell_width + cell_width / 2;
			y = BORDER_SIZE + (cell_num / cells_per_row) * cell_height + cell_height / 2;
		}

		public void GetCellSize (int cell_num, out int w, out int h)
		{
			// Trivial for now.
			w = cell_width;
			h = cell_height;
		}


		// Private utility methods.
		public void SelectAllCells ()
		{
			selection.Add (0, collection.Count - 1);
		}

		private void ToggleCell (int cell_num)
		{
			if (selection.Contains (cell_num))
				selection.Remove (cell_num);
			else
				selection.Add (cell_num);
		}


		// Layout and drawing.

		// FIXME I can't find a c# wrapper for the C PANGO_PIXELS () macro
		// So this Function is for that.
		protected static int PangoPixels (int val)
		{
			return val >= 0 ? (val + 1024 / 2) / 1024 :
				(val - 1024 / 2) / 1024;
		}

		protected virtual void UpdateLayout ()
		{
			UpdateLayout (Allocation);
		}

		protected virtual void UpdateLayout (Gdk.Rectangle allocation)
		{
			int available_width = allocation.Width - 2 * BORDER_SIZE;
			int available_height = allocation.Height - 2 * BORDER_SIZE;
			cell_width = ThumbnailWidth + 2 * cell_border_width;
			cell_height = ThumbnailHeight + 2 * cell_border_width;
			cells_per_row = Math.Max ((int) (available_width / cell_width), 1);
			cell_width += (available_width - cells_per_row * cell_width) / cells_per_row;

			if (DisplayTags || DisplayDates || DisplayFilenames)
				cell_height += tag_icon_vspacing;

			if (DisplayTags)
				cell_height += tag_icon_size;

			if (DisplayDates && this.Style != null) {
				Pango.FontMetrics metrics = this.PangoContext.GetMetrics (this.Style.FontDescription,
						Pango.Language.FromString ("en_US"));
				cell_height += PangoPixels (metrics.Ascent + metrics.Descent);
			}

			if (DisplayFilenames && this.Style != null) {
				Pango.FontMetrics metrics = this.PangoContext.GetMetrics (this.Style.FontDescription,
						Pango.Language.FromString ("en_US"));
				cell_height += PangoPixels (metrics.Ascent + metrics.Descent);
			}

			displayed_rows = (int)Math.Max (available_height / cell_height, 1);

			int num_thumbnails;
			if (collection != null)
				num_thumbnails = collection.Count;
			else
				num_thumbnails = 0;

			int num_rows = num_thumbnails / cells_per_row;
			if (num_thumbnails % cells_per_row != 0)
				num_rows ++;

			int height = num_rows * cell_height + 2 * BORDER_SIZE;

			Vadjustment.StepIncrement = cell_height;
			int x = (int)(Hadjustment.Value);
			int y = (int)(height * scroll_value);
			SetSize (x, y, (int) allocation.Width, (int) height);
		}

		void SetSize (int x, int y, int width, int height)
		{
			bool xchange = false;
			bool ychange = false;

			Hadjustment.Upper = System.Math.Max (Allocation.Width, width);
			Vadjustment.Upper = System.Math.Max (Allocation.Height, height);

			if (scroll) {
				xchange = (int)(Hadjustment.Value) != x;
				ychange = (int)(Vadjustment.Value) != y;
				scroll = false;
			}

			if (IsRealized)
				BinWindow.FreezeUpdates ();

			if (xchange || ychange) {
				if (IsRealized)
					BinWindow.MoveResize (-x, -y, (int)(Hadjustment.Upper), (int)(Vadjustment.Upper));
				Vadjustment.Value = y;
				Hadjustment.Value = x;
			}

			if (scroll)
				scroll = false;

			if (this.Width != Allocation.Width || this.Height != Allocation.Height)
				SetSize ((uint)Allocation.Width, (uint)height);

			if (xchange || ychange) {
				Vadjustment.ChangeValue ();
				Hadjustment.ChangeValue ();
			}

			if (IsRealized) {
				BinWindow.ThawUpdates ();
				BinWindow.ProcessUpdates (true);
			}
		}

		int ThrobExpansion (int cell, bool selected)
		{
			int expansion = 0;
			if (cell == throb_cell) {
				double t = throb_state / (double) (throb_state_max - 1);
				double s;
				if (selected)
					s = Math.Cos (-2 * Math.PI * t);
				else
					s = 1 - Math.Cos (-2 * Math.PI * t);

				expansion = (int) (SELECTION_THICKNESS * s);
			} else if (selected) {
				expansion = SELECTION_THICKNESS;
			}

			return expansion;
		}

		System.Collections.Hashtable date_layouts = new Hashtable ();
		// FIXME Cache the GCs?
		private void DrawCell (int thumbnail_num, Gdk.Rectangle area)
		{
			Gdk.Rectangle bounds = CellBounds (thumbnail_num);

			if (!bounds.Intersect (area, out area))
				return;

			FSpot.IBrowsableItem photo = collection [thumbnail_num];
			string thumbnail_path = FSpot.ThumbnailGenerator.ThumbnailPath (photo.DefaultVersionUri);

			FSpot.PixbufCache.CacheEntry entry = cache.Lookup (thumbnail_path);
			if (entry == null)
				cache.Request (thumbnail_path, thumbnail_num, ThumbnailWidth, ThumbnailHeight);
			else
				entry.Data = thumbnail_num;

			bool selected = selection.Contains (thumbnail_num);
			StateType cell_state = selected ? (HasFocus ? StateType.Selected : StateType.Active) : State;

			if (cell_state != State)
				Style.PaintBox (Style, BinWindow, cell_state,
					ShadowType.Out, area, this, "IconView",
					bounds.X, bounds.Y,
					bounds.Width - 1, bounds.Height - 1);

			Gdk.Rectangle focus = Gdk.Rectangle.Inflate (bounds, -3, -3);

			if (HasFocus && thumbnail_num == FocusCell) {
				Style.PaintFocus(Style, BinWindow,
						cell_state, area,
						this, null,
						focus.X, focus.Y,
						focus.Width, focus.Height);
			}

			Gdk.Rectangle region = Gdk.Rectangle.Zero;
			Gdk.Rectangle image_bounds = Gdk.Rectangle.Inflate (bounds, -cell_border_width, -cell_border_width);
			int expansion = ThrobExpansion (thumbnail_num, selected);

			Gdk.Pixbuf thumbnail = null;
			if (entry != null)
				thumbnail = entry.ShallowCopyPixbuf ();

			Gdk.Rectangle draw = Gdk.Rectangle.Zero;
			if (Gdk.Rectangle.Inflate (image_bounds, expansion + 1, expansion + 1).Intersect (area, out image_bounds) && thumbnail != null) {

				PixbufUtils.Fit (thumbnail, ThumbnailWidth, ThumbnailHeight,
						true, out region.Width, out region.Height);

				region.X = (int) (bounds.X + (bounds.Width - region.Width) / 2);
				region.Y = (int) bounds.Y + ThumbnailHeight - region.Height + cell_border_width;

				if (Math.Abs (region.Width - thumbnail.Width) > 1
					&& Math.Abs (region.Height - thumbnail.Height) > 1)
				cache.Reload (entry, thumbnail_num, thumbnail.Width, thumbnail.Height);

				region = Gdk.Rectangle.Inflate (region, expansion, expansion);
				Pixbuf temp_thumbnail;
				region.Width = System.Math.Max (1, region.Width);
				region.Height = System.Math.Max (1, region.Height);

				if (Math.Abs (region.Width - thumbnail.Width) > 1
					&& Math.Abs (region.Height - thumbnail.Height) > 1) {
					if (region.Width < thumbnail.Width && region.Height < thumbnail.Height) {
						/*
						temp_thumbnail = PixbufUtils.ScaleDown (thumbnail,
								region.Width, region.Height);
						*/
						temp_thumbnail = thumbnail.ScaleSimple (region.Width, region.Height,
								InterpType.Bilinear);


						lock (entry) {
							if (entry.Reload && expansion == 0 && !entry.IsDisposed) {
								entry.SetPixbufExtended (PixbufUtils.ShallowCopy (temp_thumbnail), false);
								entry.Reload = true;
							}
						}
					} else {
						temp_thumbnail = thumbnail.ScaleSimple (region.Width, region.Height,
								InterpType.Bilinear);
					}

					PixbufUtils.CopyThumbnailOptions (thumbnail, temp_thumbnail);
				} else
					temp_thumbnail = thumbnail;

				// FIXME There seems to be a rounding issue between the
				// scaled thumbnail sizes, we avoid this for now by using
				// the actual thumnail sizes here.
				region.Width = temp_thumbnail.Width;
				region.Height = temp_thumbnail.Height;

				draw = Gdk.Rectangle.Inflate (region, 1, 1);

				if (!temp_thumbnail.HasAlpha)
					Style.PaintShadow (Style, BinWindow, cell_state,
						ShadowType.Out, area, this,
						"IconView",
						draw.X, draw.Y,
						draw.Width, draw.Height);

				if (region.Intersect (area, out draw)) {
					//FIXME
					if (FSpot.ColorManagement.IsEnabled) {
						temp_thumbnail = temp_thumbnail.Copy();
						FSpot.ColorManagement.ApplyScreenProfile (temp_thumbnail);
					}
					temp_thumbnail.RenderToDrawable (BinWindow, Style.WhiteGC,
							draw.X - region.X,
							draw.Y - region.Y,
							draw.X, draw.Y,
							draw.Width, draw.Height,
							RgbDither.None,
							draw.X, draw.Y);
				}

				if (temp_thumbnail != thumbnail) {
					temp_thumbnail.Dispose ();
				}

			}

			if (thumbnail != null) {
				thumbnail.Dispose ();
			}
			if (DisplayRatings && photo.Rating > 0 && region.X == draw.X && region.X != 0) {
				FSpot.Widgets.RatingSmall rating;
				rating = new FSpot.Widgets.RatingSmall ((int) photo.Rating, false);
				rating.DisplayPixbuf.RenderToDrawable (BinWindow, Style.WhiteGC,
						0, 0, region.X, region.Y, -1, -1, RgbDither.None, 0, 0);
				
			}
			Gdk.Rectangle layout_bounds = Gdk.Rectangle.Zero;
			if (DisplayDates) {
				string date;
				if (cell_width > 200) {
					date = photo.Time.ToLocalTime ().ToString ();
				} else {
					date = photo.Time.ToLocalTime ().ToShortDateString ();
				}

				Pango.Layout layout = (Pango.Layout)date_layouts [date];
				if (layout == null) {
					layout = new Pango.Layout (this.PangoContext);
					layout.SetText (date);
					date_layouts [date] = layout;
				}

				layout.GetPixelSize (out layout_bounds.Width, out layout_bounds.Height);

				layout_bounds.Y = bounds.Y + bounds.Height - cell_border_width - layout_bounds.Height + tag_icon_vspacing;
				layout_bounds.X = bounds.X + (bounds.Width - layout_bounds.Width) / 2;

				if (DisplayTags)
					layout_bounds.Y -= tag_icon_size;

				if (DisplayFilenames) {
					Pango.FontMetrics metrics = this.PangoContext.GetMetrics (this.Style.FontDescription,
							Pango.Language.FromString ("en_US"));
					layout_bounds.Y -= PangoPixels (metrics.Ascent + metrics.Descent);
				}

				if (layout_bounds.Intersect (area, out region)) {
					Style.PaintLayout (Style, BinWindow, cell_state,
							true, area, this, "IconView",
							layout_bounds.X, layout_bounds.Y,
							layout);
				}
			}

			if (DisplayFilenames) {

				string filename = System.IO.Path.GetFileName (photo.DefaultVersionUri.LocalPath);
				Pango.Layout layout = new Pango.Layout (this.PangoContext);
				layout.SetText (filename);

				layout.GetPixelSize (out layout_bounds.Width, out layout_bounds.Height);

				layout_bounds.Y = bounds.Y + bounds.Height - cell_border_width - layout_bounds.Height + tag_icon_vspacing;
				layout_bounds.X = bounds.X + (bounds.Width - layout_bounds.Width) / 2;

				if (DisplayTags)
					layout_bounds.Y -= tag_icon_size;

				if (layout_bounds.Intersect (area, out region)) {
					Style.PaintLayout (Style, BinWindow, cell_state,
							true, area, this, "IconView",
							layout_bounds.X, layout_bounds.Y,
							layout);
				}

			}

			if (DisplayTags) {
				Tag [] tags = photo.Tags;
				Gdk.Rectangle tag_bounds;

				tag_bounds.X = bounds.X + (bounds.Width  + tag_icon_hspacing - tags.Length * (tag_icon_size + tag_icon_hspacing)) / 2;
				tag_bounds.Y = bounds.Y + bounds.Height - cell_border_width - tag_icon_size + tag_icon_vspacing;
				tag_bounds.Width = tag_icon_size;
				tag_bounds.Height = tag_icon_size;

				foreach (Tag t in tags) {
					if (t == null)
						continue;

					Pixbuf icon = t.Icon;

					Tag tag_iter = t.Category;
					while (icon == null && tag_iter != Core.Database.Tags.RootCategory && tag_iter != null) {
						icon = tag_iter.Icon;
						tag_iter = tag_iter.Category;
					}

					if (icon == null)
						continue;

					if (tag_bounds.Intersect (area, out region)) {
						Pixbuf scaled_icon;
						if (icon.Width == tag_bounds.Width) {
							scaled_icon = icon;
						} else {
							scaled_icon = icon.ScaleSimple (tag_bounds.Width,
									tag_bounds.Height,
									InterpType.Bilinear);
						}
						
						FSpot.ColorManagement.ApplyScreenProfile (scaled_icon);

						scaled_icon.RenderToDrawable (BinWindow, Style.WhiteGC,
								region.X - tag_bounds.X,
								region.Y - tag_bounds.Y,
								region.X, region.Y,
								region.Width, region.Height,
								RgbDither.None, region.X, region.Y);
						if (scaled_icon != icon) {
							scaled_icon.Dispose ();
						}
					}
					tag_bounds.X += tag_bounds.Width + tag_icon_hspacing;
				}
			}

		}

		private void DrawAllCells (Gdk.Rectangle area)
		{
			if (cell_width == 0 || cell_height == 0)
				return;

			int start_cell_column = Math.Max ((area.X - BORDER_SIZE) / cell_width, 0);
			int start_cell_row = Math.Max ((area.Y - BORDER_SIZE) / cell_height, 0);
			int start_cell_num = start_cell_column + start_cell_row * cells_per_row;

			int start_cell_x, cell_y;
			GetCellPosition (start_cell_num, out start_cell_x, out cell_y);

			int end_cell_column = Math.Max ((area.X + area.Width - BORDER_SIZE) / cell_width, 0);
			int end_cell_row = Math.Max ((area.Y + area.Height - BORDER_SIZE) / cell_height, 0);

			int num_rows = end_cell_row - start_cell_row + 1;
			int num_cols = Math.Min (end_cell_column - start_cell_column + 1,
					cells_per_row - start_cell_column);

			int i, cell_num;
			//Preload (area, false);

			for (i = 0, cell_num = start_cell_num;
				i < num_rows && cell_num < collection.Count;
			i ++) {
				int cell_x = start_cell_x;

				//Log.DebugFormat ("Drawing row {0}", start_cell_row + i);
				for (int j = 0; j < num_cols && cell_num + j < collection.Count; j ++) {
					DrawCell (cell_num + j, area);
					cell_x += cell_width;
				}

				cell_y += cell_height;
				cell_num += cells_per_row;
			}

		}

		private void GetCellPosition (int cell_num, out int x, out int y)
		{
			if (cells_per_row == 0) {
				x = 0;
				y = 0;
				return;
			}

			int row = cell_num / cells_per_row;
			int col = cell_num % cells_per_row;

			x = col * cell_width + BORDER_SIZE;
			y = row * cell_height + BORDER_SIZE;
		}


		// Scrolling.  We do this in an idle loop so we can catch up if the user scrolls quickly.

		private void Scroll ()
		{
			int ystep = (int)(Vadjustment.Value - y_offset);
			int xstep = (int)(Hadjustment.Value - x_offset);

			if (xstep > 0)
				xstep = Math.Max (xstep, Allocation.Width);
			else
				xstep = Math.Min (xstep, -Allocation.Width);

			if (ystep > 0)
				ystep = Math.Max (ystep, Allocation.Height);
			else
				ystep = Math.Min (ystep, -Allocation.Height);

			Gdk.Rectangle area;

			Gdk.Region offscreen = new Gdk.Region ();
			/*
			Log.DebugFormat ("step ({0}, {1}) allocation ({2},{3},{4},{5})",
					xstep, ystep, Hadjustment.Value, Vadjustment.Value,
					Allocation.Width, Allocation.Height);
			*/
			/*
			area = new Gdk.Rectangle (Math.Max ((int) (Hadjustment.Value + 4 * xstep), 0),
					Math.Max ((int) (Vadjustment.Value + 4 * ystep), 0),
					Allocation.Width,
					Allocation.Height);
			offscreen.UnionWithRect (area);
			area = new Gdk.Rectangle (Math.Max ((int) (Hadjustment.Value + 3 * xstep), 0),
					Math.Max ((int) (Vadjustment.Value + 3 * ystep), 0),
					Allocation.Width,
					Allocation.Height);
			offscreen.UnionWithRect (area);
			*/
			area = new Gdk.Rectangle (Math.Max ((int) (Hadjustment.Value + 2 * xstep), 0),
					Math.Max ((int) (Vadjustment.Value + 2 * ystep), 0),
					Allocation.Width,
					Allocation.Height);
			offscreen.UnionWithRect (area);
			area = new Gdk.Rectangle (Math.Max ((int) (Hadjustment.Value + xstep), 0),
					Math.Max ((int) (Vadjustment.Value + ystep), 0),
					Allocation.Width,
					Allocation.Height);
			offscreen.UnionWithRect (area);
			area = new Gdk.Rectangle ((int) Hadjustment.Value,
					(int) Vadjustment.Value,
					Allocation.Width,
					Allocation.Height);

			// always load the onscreen area last to make sure it
			// is first in the loading
			Gdk.Region onscreen = Gdk.Region.Rectangle (area);
			offscreen.Subtract (onscreen);

			PreloadRegion (offscreen, ystep);
			Preload (area, false);

			y_offset = (int) Vadjustment.Value;
			x_offset = (int) Hadjustment.Value;
		}

		private void PreloadRegion (Gdk.Region region, int step)
		{
			Gdk.Rectangle [] rects = region.GetRectangles ();

			if (step < 0)
				System.Array.Reverse (rects);

			foreach (Gdk.Rectangle preload in rects) {
				Preload (preload, false);
			}
		}

		private void Preload (Gdk.Rectangle area, bool back)
		{
			if (cells_per_row ==0)
				return;

			int start_cell_column = Math.Max ((area.X - BORDER_SIZE) / cell_width, 0);
			int start_cell_row = Math.Max ((area.Y - BORDER_SIZE) / cell_height, 0);
			int start_cell_num = start_cell_column + start_cell_row * cells_per_row;

			int end_cell_column = Math.Max ((area.X + area.Width - BORDER_SIZE) / cell_width, 0);
			int end_cell_row = Math.Max ((area.Y + area.Height - BORDER_SIZE) / cell_height, 0);

			int i;

			FSpot.IBrowsableItem photo;
			FSpot.PixbufCache.CacheEntry entry;
			string thumbnail_path;

			// Preload the cache with images aroud the expose area
			// FIXME the preload need to be tuned to the Cache size but this is a resonable start

			int cols = end_cell_column - start_cell_column + 1;
			int rows = end_cell_row - start_cell_row + 1;
			int len = rows * cols;
			int scell = start_cell_num;
			int ecell = scell + len;
			if (scell > collection.Count - len) {
				ecell = collection.Count;
				scell = System.Math.Max (0, scell - len);
			} else
				ecell = scell + len;

			int mid = (ecell - scell) / 2;
			for (i = 0; i < mid; i++)
				{
				int cell = back ? ecell - i - 1 : scell + mid + i;

				photo = collection [cell];
				thumbnail_path = FSpot.ThumbnailGenerator.ThumbnailPath (photo.DefaultVersionUri);

				entry = cache.Lookup (thumbnail_path);
				if (entry == null)
					cache.Request (thumbnail_path, cell, ThumbnailWidth, ThumbnailHeight);

				cell = back ? scell + i : scell + mid - i - 1;
				photo = collection [cell];
				thumbnail_path = FSpot.ThumbnailGenerator.ThumbnailPath (photo.DefaultVersionUri);

				entry = cache.Lookup (thumbnail_path);
				if (entry == null)
					cache.Request (thumbnail_path, cell, ThumbnailWidth, ThumbnailHeight);
			}
		}

		//
		// The throb interface
		//
		private uint throb_timer_id;
		private int throb_cell = -1;
		private int throb_state;
		private const int throb_state_max = 40;
		public void Throb (int cell_num)
		{
			throb_state = 0;
			throb_cell = cell_num;
			if (throb_timer_id == 0)
				throb_timer_id = GLib.Timeout.Add ((39000/throb_state_max)/100,
					new GLib.TimeoutHandler (HandleThrobTimer));

			InvalidateCell (cell_num);
		}

		private void CancelThrob ()
		{
			if (throb_timer_id != 0)
				GLib.Source.Remove (throb_timer_id);
		}

		private bool HandleThrobTimer ()
		{
			InvalidateCell (throb_cell);
			if (throb_state++ < throb_state_max) {
				return true;
			} else {
				throb_cell = -1;
				throb_timer_id = 0;
				return false;
			}
		}

		public void ScrollTo (int cell_num)
		{
			ScrollTo (cell_num, true);
		}

		public void ScrollTo (int cell_num, bool center)
		{
			if (!IsRealized)
				return;

			Adjustment adjustment = Vadjustment;
			int x;
			int y;

			GetCellPosition (cell_num, out x, out y);

			if (y + cell_height > adjustment.Upper)
				UpdateLayout ();

			if (center)
				adjustment.Value = y + cell_height / 2 - adjustment.PageSize / 2;
			else
				adjustment.Value = y;

			adjustment.ChangeValue ();

		}

		public void ZoomIn ()
		{
			ThumbnailWidth = (int) (ThumbnailWidth * ZOOM_FACTOR);
		}

		public void ZoomOut ()
		{
			ThumbnailWidth = (int) (ThumbnailWidth / ZOOM_FACTOR);
		}

		// Event handlers.

		[GLib.ConnectBefore]
		private void HandleAdjustmentValueChanged (object sender, EventArgs args)
		{
			Scroll ();
		}

		private void HandlePixbufLoaded (FSpot.PixbufCache cache, FSpot.PixbufCache.CacheEntry entry)
		{
			Gdk.Pixbuf result = entry.ShallowCopyPixbuf ();
			int order = (int) entry.Data;

			if (order >= 0 && order < collection.Count) {
				System.Uri uri = collection [order].DefaultVersionUri;

				if (result == null && !System.IO.File.Exists (FSpot.ThumbnailGenerator.ThumbnailPath (uri)))
					FSpot.ThumbnailGenerator.Default.Request (uri, 0, 256, 256);

				if (result == null)
					return;

				if (!FSpot.PhotoLoader.ThumbnailIsValid (uri, result))
					FSpot.ThumbnailGenerator.Default.Request (uri, 0, 256, 256);
			}

			if (result == null)
				return;

			// We have to do the scaling here rather than on load because we need to preserve the
			// Pixbuf option iformation to verify the thumbnail validity later
			int width, height;
			PixbufUtils.Fit (result, ThumbnailWidth, ThumbnailHeight, false, out width, out height);
			if (result.Width > width && result.Height > height) {
				//  Log.Debug ("scaling");
				Gdk.Pixbuf temp = PixbufUtils.ScaleDown (result, width, height);
				result.Dispose ();
				result = temp;
			} else if (result.Width < ThumbnailWidth && result.Height < ThumbnailHeight) {
				// FIXME this is a workaround to handle images whose actual size is smaller than
				// the thumbnail size, it needs to be fixed at a different level.
				Gdk.Pixbuf temp = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, ThumbnailWidth, ThumbnailHeight);
				temp.Fill (0x00000000);
				result.CopyArea (0, 0,
						result.Width, result.Height,
						temp,
						(temp.Width - result.Width)/ 2,
						temp.Height - result.Height);

				result.Dispose ();
				result = temp;
			}

			cache.Update (entry, result);
			InvalidateCell (order);
		}

		public Gdk.Rectangle CellBounds (int cell)
		{
			Rectangle bounds;
			GetCellPosition (cell, out bounds.X, out bounds.Y);
			bounds.Width = cell_width;
			bounds.Height = cell_height;
			return bounds;
		}

		public void InvalidateCell (int order)
		{
			Rectangle cell_area = CellBounds (order);
			// FIXME where are we computing the bounds incorrectly
			cell_area.Width -= 1;
			cell_area.Height -= 1;
			Gdk.Rectangle visible = new Gdk.Rectangle ((int)Hadjustment.Value,
					(int)Vadjustment.Value,
					Allocation.Width,
					Allocation.Height);

			if (BinWindow != null && cell_area.Intersect (visible, out cell_area))
				BinWindow.InvalidateRect (cell_area, false);
		}

		private void HandleScrollAdjustmentsSet (object sender, ScrollAdjustmentsSetArgs args)
		{
			if (args.Vadjustment != null)
				args.Vadjustment.ValueChanged += new EventHandler (HandleAdjustmentValueChanged);
		}

		private void HandleScrollEvent(object sender, ScrollEventArgs args)
		{
			// Activated only by Control + ScrollWheelUp/ScrollWheelDown
			if (ModifierType.ControlMask != (args.Event.State & ModifierType.ControlMask))
				return;

			if (args.Event.Direction == ScrollDirection.Up) {
				ZoomIn ();
				// stop event from propagating.
				args.RetVal = true;
			} else if (args.Event.Direction == ScrollDirection.Down ) {
				ZoomOut ();
				args.RetVal = true;
			}
		}

		private void SetColors ()
		{
			if (IsRealized) {
				BinWindow.Background = Style.BaseColors [(int)State];
				GdkWindow.Background = Style.BaseColors [(int)State];
			}
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			SetColors ();
		}

		protected override void OnStateChanged (StateType previous)
		{
			base.OnStateChanged (previous);
			SetColors ();
		}

		protected override void OnStyleSet (Style previous)
		{
			base.OnStyleSet (previous);
			SetColors ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			scroll_value = (Vadjustment.Value)/ (Vadjustment.Upper);
			scroll = !suppress_scroll;
			suppress_scroll = false;
			UpdateLayout (allocation);
			base.OnSizeAllocated (allocation);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			foreach (Rectangle area in args.Region.GetRectangles ()) {
				DrawAllCells (area);
			}
			return base.OnExposeEvent (args);
		}

		private void HandleButtonPressEvent (object obj, ButtonPressEventArgs args)
		{
			int cell_num = CellAtPosition ((int) args.Event.X, (int) args.Event.Y, false);

			args.RetVal = true;

			if (cell_num < 0) {
				args.RetVal = false;
				selection.Clear ();
				return;
			}

			switch (args.Event.Type) {
			case EventType.TwoButtonPress:
				if (args.Event.Button != 1 ||
						(args.Event.State &  (ModifierType.ControlMask | ModifierType.ShiftMask)) != 0)
					return;
				if (DoubleClicked != null)
					DoubleClicked (this, new BrowsableEventArgs (cell_num, null));
				return;

			case EventType.ButtonPress:
				GrabFocus ();
				if ((args.Event.State & ModifierType.ControlMask) != 0) {
					ToggleCell (cell_num);
				} else if ((args.Event.State & ModifierType.ShiftMask) != 0) {
					selection.Add (FocusCell, cell_num);
				} else if (!selection.Contains (cell_num)) {
					selection.Clear ();
					selection.Add (cell_num);
				}

				if (args.Event.Button == 3){
					ContextMenu (args, cell_num);
					return;
				}

				if (args.Event.Button != 1)
					return;

				FocusCell = cell_num;
				return;

			default:
				args.RetVal = false;
				return;
			}
		}

		protected virtual void ContextMenu (ButtonPressEventArgs args, int cell_num)
		{
		}

		private void HandleButtonReleaseEvent (object sender, ButtonReleaseEventArgs args)
		{
			int cell_num = CellAtPosition ((int) args.Event.X, (int) args.Event.Y);

			args.RetVal = true;

			if (cell_num < 0) {
				args.RetVal = false;
				return;
			}

			switch (args.Event.Type) {
			case EventType.ButtonRelease:
				if ((args.Event.State & ModifierType.ControlMask) == 0 &&
						(args.Event.State & ModifierType.ShiftMask  ) == 0 &&
						(selection.Count > 1)) {
					selection.Clear ();
					selection.Add (FocusCell);
				}

				break;

			default:
				args.RetVal = false;
				break;
			}

		}

		private void HandleKeyPressEvent (object sender, KeyPressEventArgs args)
		{
			int focus_old;
			args.RetVal = true;
			bool shift = ModifierType.ShiftMask == (args.Event.State & ModifierType.ShiftMask);
			bool control = ModifierType.ControlMask == (args.Event.State & ModifierType.ControlMask);

			focus_old = FocusCell;
			switch (args.Event.Key) {
			case Gdk.Key.Down:
			case Gdk.Key.J:
			case Gdk.Key.j:
				FocusCell += cells_per_row;
				break;
			case Gdk.Key.Left:
			case Gdk.Key.H:
			case Gdk.Key.h:
				if (control && shift)
					FocusCell -= FocusCell % cells_per_row;
				else
					FocusCell--;
				break;
			case Gdk.Key.Right:
			case Gdk.Key.L:
			case Gdk.Key.l:
				if (control && shift)
					FocusCell += cells_per_row - (FocusCell % cells_per_row) - 1;
				else
					FocusCell++;
				break;
			case Gdk.Key.Up:
			case Gdk.Key.K:
			case Gdk.Key.k:
				FocusCell -= cells_per_row;
				break;
			case Gdk.Key.Page_Up:
				FocusCell -= cells_per_row * displayed_rows;
				break;
			case Gdk.Key.Page_Down:
				FocusCell += cells_per_row * displayed_rows;
				break;
			case Gdk.Key.Home:
				FocusCell = 0;
				break;
			case Gdk.Key.End:
				FocusCell = collection.Count - 1;
				break;
			case Gdk.Key.space:
				ToggleCell (FocusCell);
				break;
			case Gdk.Key.Return:
				if (DoubleClicked != null)
					DoubleClicked (this, new BrowsableEventArgs (FocusCell, null));
				break;
			default:
				args.RetVal = false;
				return;
			}

			if (FocusCell == focus_old) {
				args.RetVal = false;
				return;
			}

			if (shift) {
				if (focus_old != FocusCell && selection.Contains (focus_old) && selection.Contains (FocusCell))
					selection.Remove (FocusCell, focus_old);
				else
					selection.Add (focus_old, FocusCell);
			} else if (!control) {
				selection.Clear ();
				selection.Add (FocusCell);
			}

			ScrollTo (FocusCell);
		}

		private void HandleDestroyed (object sender, System.EventArgs args)
		{
			cache.OnPixbufLoaded -= HandlePixbufLoaded;
			CancelThrob ();
		}
	}
}
