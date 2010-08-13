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
using System;
using System.Reflection;
using System.Collections;
using System.IO;

using Hyena.Gui;

using FSpot.Core;
using FSpot.Utils;
using FSpot.Platform;

namespace FSpot.Widgets
{
    public class StartDragArgs {
        public Event Event { get; set; }
        public uint Button { get; set; }

        public StartDragArgs (uint but, Event evt) {
            this.Button = but;
            this.Event = evt;
        }
    }

    public delegate void StartDragHandler (object o, StartDragArgs args);


	public class IconView : CellGridView
    {

		// Public properties.
		FSpot.PixbufCache cache;

#region Public Events

        // Public events.
        public event EventHandler<BrowsableEventArgs> DoubleClicked;
        public event EventHandler ZoomChanged;
        public event StartDragHandler StartDrag;

#endregion

#region Zooming and Thumbnail Size

        // fixed constants
        protected const double ZOOM_FACTOR = 1.2;
        protected const int MAX_THUMBNAIL_WIDTH = 256;
        protected const int MIN_THUMBNAIL_WIDTH = 64;

        // current with of the thumbnails. (height is calculated)
        private int thumbnail_width = 128;

        // current ratio of thumbnail width and height
        private double thumbnail_ratio = 4.0 / 3.0;

        public int ThumbnailWidth {
            get { return thumbnail_width; }
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

        public double ThumbnailRatio {
            get { return thumbnail_ratio; }
            set {
                thumbnail_ratio = value;
                QueueResize ();
            }
        }

        public int ThumbnailHeight {
            get { return (int) Math.Round ((double) thumbnail_width / ThumbnailRatio); }
        }


        public void ZoomIn ()
        {
            ThumbnailWidth = (int) (ThumbnailWidth * ZOOM_FACTOR);
        }

        public void ZoomOut ()
        {
            ThumbnailWidth = (int) (ThumbnailWidth / ZOOM_FACTOR);
        }

#endregion

#region Implement base class layout properties

        protected override int CellCount {
            get {
                if (collection == null)
                    return 0;

                return collection.Count;
            }
        }

        protected override int MinCellHeight {
            get {
                int cell_height = ThumbnailHeight + 2 * cell_border_width;

                cell_details = 0;

                if (DisplayTags || DisplayDates || DisplayFilenames)
                    cell_details += tag_icon_vspacing;
    
                if (DisplayTags)
                    cell_details += tag_icon_size;

                if (DisplayDates && Style != null) {
                    cell_details += Style.FontDescription.MeasureTextHeight (PangoContext);
                }
    
                if (DisplayFilenames && Style != null) {
                    cell_details += Style.FontDescription.MeasureTextHeight (PangoContext);
                }

                cell_height += cell_details;

                return cell_height;
            }
        }

        protected override int MinCellWidth {
            get { return ThumbnailWidth + 2 * cell_border_width; }
        }

#endregion


		public FSpot.PixbufCache Cache {
			get { return cache; }
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
				if (MinCellWidth > 100)
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
				if (MinCellWidth > 100)
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

		// Size of the frame that may be selected
        protected int cell_border_padding = 3;

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
		protected int cell_details;

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

		// Public API.
		public IconView (IntPtr raw) : base (raw) {}

		protected IconView () : base ()
		{
			cache = new FSpot.PixbufCache ();
			cache.OnPixbufLoaded += HandlePixbufLoaded;

			ButtonPressEvent += new ButtonPressEventHandler (HandleButtonPressEvent);
			ButtonReleaseEvent += new ButtonReleaseEventHandler (HandleButtonReleaseEvent);
			KeyPressEvent += new KeyPressEventHandler (HandleKeyPressEvent);
			ScrollEvent += new ScrollEventHandler(HandleScrollEvent);
			MotionNotifyEvent += new MotionNotifyEventHandler (HandleSelectMotionNotify);

			Destroyed += HandleDestroyed;

			AddEvents (
				(int) EventMask.KeyPressMask
				| (int) EventMask.KeyReleaseMask
				| (int) EventMask.PointerMotionMask
				| (int) EventMask.PointerMotionHintMask
				| (int) EventMask.ButtonPressMask
				| (int) EventMask.ButtonReleaseMask);

			CanFocus = true;
		}

		public IconView (IBrowsableCollection collection) : this ()
		{
			this.collection = collection;
			this.selection = new SelectionCollection (collection);

			Name = "ImageContainer";
			collection.Changed += HandleChanged;
			collection.ItemsChanged += HandleItemsChanged;

			selection.DetailedChanged += HandleSelectionChanged;
		}

		private void HandleSelectionChanged (IBrowsableCollection collection, int [] ids)
		{
			if (ids == null)
				QueueDraw ();
			else
				foreach (int id in ids)
					InvalidateCell (id);
		}

		private void HandleChanged (IBrowsableCollection sender)
		{
			// FIXME we should probably try to merge the selection forward
			// but it needs some thought to be efficient.
			//suppress_scroll = true;
			QueueResize ();
		}

		private void HandleItemsChanged (IBrowsableCollection sender, BrowsableEventArgs args)
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

		protected IBrowsableCollection collection;
		public IBrowsableCollection Collection {
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
			BitArray bit_array;
			int [] selection;
			IPhoto [] items;
			IPhoto [] old;

			public SelectionCollection (IBrowsableCollection collection)
			{
				this.selected_cells = new Hashtable ();
				this.parent = collection;
				this.bit_array = new BitArray (this.parent.Count);
				this.parent.Changed += HandleParentChanged;
				this.parent.ItemsChanged += HandleParentItemsChanged;
			}

			private void HandleParentChanged (IBrowsableCollection collection)
			{
				IPhoto [] local = old;
				selected_cells.Clear ();
				bit_array = new BitArray (parent.Count);
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

			public BitArray ToBitArray () {
				return bit_array;
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

			public IPhoto this [int index] {
				get {
					int [] ids = this.Ids;
					return parent [ids[index]];
				}
			}

			public IPhoto [] Items {
				get {
					if (items != null)
						return items;

					int [] ids = this.Ids;
					items = new IPhoto [ids.Length];
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
				bit_array.SetAll (false);
				SignalChange (ids);
			}

			public void Add (IPhoto item)
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

			public bool Contains (IPhoto item)
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

			public void Add (int num, bool notify)
			{
				if (num == -1)
					return;

				if (this.Contains (num))
					return;

				IPhoto item = parent [num];
				selected_cells [item] = num;
				bit_array.Set (num, true);

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

			public void Remove (int cell, bool notify)
			{
				IPhoto item = parent [cell];
				if (item != null)
					this.Remove (item, notify);

			}

			public void Remove (IPhoto item)
			{
				Remove (item, true);
			}

			public void Remove (int cell)
			{
				Remove (cell, true);
			}

			private void Remove (IPhoto item, bool notify)
			{
				if (item == null)
					return;

				int parent_index = (int) selected_cells [item];
				selected_cells.Remove (item);
				bit_array.Set (parent_index, false);

				if (notify)
					SignalChange (new int [] {parent_index});
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

			public int IndexOf (IPhoto item)
			{
				if (!this.Contains (item))
					return -1;

				int parent_index = (int) selected_cells [item];
				return System.Array.IndexOf (Ids, parent_index);
			}

			private void ToggleCell (int cell_num, bool notify)
			{
				if (Contains (cell_num))
					Remove (cell_num, notify);
				else
					Add (cell_num, notify);
			}

			public void ToggleCell (int cell_num)
			{
				ToggleCell (cell_num, true);
			}

			public void SelectionInvert ()
			{
				int [] changed_cell = new int[parent.Count];
				for (int i = 0; i < parent.Count; i++) {
					ToggleCell (i, false);
					changed_cell[i] = i;
				}

				SignalChange (changed_cell);
			}

			public void SelectRect (int start_row, int end_row, int start_line, int end_line, int cells_per_row)
			{
				for (int row = start_row; row < end_row; row++)
					for (int line = start_line; line < end_line; line++) {
						int index = line*cells_per_row + row;
						if (index < parent.Count)
							Add (index, false);
					}
			}

			public void ToggleRect (int start_row, int end_row, int start_line, int end_line, int cells_per_row)
			{
				for  (int row = start_row; row < end_row; row++)
					for (int line = start_line; line < end_line; line++) {
						int index = line*cells_per_row + row;
						if (index < parent.Count)
							ToggleCell (index, false);
					}
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

			public void SignalChange (int [] ids)
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
			IPhoto photo = collection [thumbnail_num];
			cache.Remove (photo.DefaultVersion.Uri);
			InvalidateCell (thumbnail_num);
		}


		// Private utility methods.
		public void SelectAllCells ()
		{
			selection.Add (0, collection.Count - 1);
		}

		// Layout and drawing.
/*		protected virtual void UpdateLayout ()
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
			cell_details = 0;

			if (DisplayTags || DisplayDates || DisplayFilenames)
				cell_details += tag_icon_vspacing;

			if (DisplayTags)
				cell_details += tag_icon_size;

            if (DisplayDates && Style != null) {
                cell_details += Style.FontDescription.MeasureTextHeight (PangoContext);
            }

            if (DisplayFilenames && Style != null) {
                cell_details += Style.FontDescription.MeasureTextHeight (PangoContext);
            }

			cell_height += cell_details;

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
		}*/

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

		// rectangle of dragging selection
		private Rectangle rect_select;

		System.Collections.Hashtable date_layouts = new Hashtable ();
		// FIXME Cache the GCs?
		protected override void DrawCell (int thumbnail_num, Gdk.Rectangle bounds, Gdk.Rectangle area)
		{
			if (!bounds.Intersect (area, out area))
				return;

			IPhoto photo = collection [thumbnail_num];

			FSpot.PixbufCache.CacheEntry entry = cache.Lookup (photo.DefaultVersion.Uri);
			if (entry == null)
				cache.Request (photo.DefaultVersion.Uri, thumbnail_num, ThumbnailWidth, ThumbnailHeight);
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
								entry.SetPixbufExtended (temp_thumbnail.ShallowCopy (), false);
								entry.Reload = true;
							}
						}
					} else {
						temp_thumbnail = thumbnail.ScaleSimple (region.Width, region.Height,
								InterpType.Bilinear);
					}
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
					Cms.Profile screen_profile;
					if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile)) {
						Pixbuf t = temp_thumbnail.Copy ();
						temp_thumbnail.Dispose ();
						temp_thumbnail = t;
						FSpot.ColorManagement.ApplyProfile (temp_thumbnail, screen_profile);
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
                var rating = new RatingRenderer () {
                    Value = (int) photo.Rating
                };
                using (var rating_pixbuf = rating.RenderPixbuf ()) {
                    rating_pixbuf.RenderToDrawable (BinWindow, Style.WhiteGC,
                                                    0, 0, region.X, region.Y, -1, -1, RgbDither.None, 0, 0);
                }
			}
			Gdk.Rectangle layout_bounds = Gdk.Rectangle.Zero;
			if (DisplayDates) {
				string date;
				try {
					if (MinCellWidth > 200) {
						date = photo.Time.ToString ();
					} else {
						date = photo.Time.ToShortDateString ();
					}
				} catch (Exception) {
					date = String.Empty;
				}

				Pango.Layout layout = (Pango.Layout)date_layouts [date];
				if (layout == null) {
					layout = new Pango.Layout (this.PangoContext);
					layout.SetText (date);
					date_layouts [date] = layout;
				}

				layout.GetPixelSize (out layout_bounds.Width, out layout_bounds.Height);

				layout_bounds.Y = bounds.Y + bounds.Height -
					cell_border_width - layout_bounds.Height + tag_icon_vspacing;
				layout_bounds.X = bounds.X + (bounds.Width - layout_bounds.Width) / 2;

				if (DisplayTags)
					layout_bounds.Y -= tag_icon_size;

				if (DisplayFilenames) {
					layout_bounds.Y -= Style.FontDescription.MeasureTextHeight (PangoContext);
				}

				if (layout_bounds.Intersect (area, out region)) {
					Style.PaintLayout (Style, BinWindow, cell_state,
							true, area, this, "IconView",
							layout_bounds.X, layout_bounds.Y,
							layout);
				}
			}

			if (DisplayFilenames) {

				string filename = System.IO.Path.GetFileName (photo.DefaultVersion.Uri.LocalPath);
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
					while (icon == null && tag_iter != App.Instance.Database.Tags.RootCategory && tag_iter != null) {
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

						Cms.Profile screen_profile;
						if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile))
							FSpot.ColorManagement.ApplyProfile (scaled_icon, screen_profile);

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

        protected override void PreloadCell (int cell_num)
        {
            var photo = collection [cell_num];
            var entry = cache.Lookup (photo.DefaultVersion.Uri);

            if (entry == null)
                cache.Request (photo.DefaultVersion.Uri, cell_num, ThumbnailWidth, ThumbnailHeight);
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



		// Event handlers.

		private void HandlePixbufLoaded (FSpot.PixbufCache cache, FSpot.PixbufCache.CacheEntry entry)
		{
			Gdk.Pixbuf result = entry.ShallowCopyPixbuf ();
			int order = (int) entry.Data;

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
				BinWindow.Background = Style.DarkColors [(int)State];
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

/*		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			scroll_value = (Vadjustment.Value)/ (Vadjustment.Upper);
			scroll = !suppress_scroll;
			suppress_scroll = false;
			UpdateLayout (allocation);
			base.OnSizeAllocated (allocation);
		}*/


		private bool isRectSelection = false;
		private bool isDragDrop = false;

		// initial click and scroll value
		private int start_select_x, start_select_y, start_select_vadj, start_select_hadj;
		// initial selection
		private int[] start_select_selection;
		// initial event used to detect drag&drop
		private EventButton start_select_event;
		// timer using when scrolling selection
		private uint scroll_timeout = 0;
		// initial click
	        private int start_press_x, start_press_y;

		// during pointer motion, select/toggle pictures between initial x/y (param)
		// and current x/y (get pointer)
		private void SelectMotion ()
		{
/*			int x2, y2;
			Gdk.ModifierType mod;
			Display.GetPointer (out x2, out y2, out mod);
			GetPointer (out x2, out y2);

			// check new coord
			int x1 = start_select_x;
			if (x1 < 0)
				x1 = 0;
			int y1 = start_select_y;
			if (y1 < 0)
				y1 = 0;
			if (y1 > Allocation.Height)
				y1 = (int) Allocation.Height;
			x1 += start_select_hadj;
			y1 += start_select_vadj;

			if (x2 < 0)
				x2 = 0;
			if (y2 < 0)
				y2 = 0;
			if (y2 > Allocation.Height)
				y2 = (int) Allocation.Height;
			x2 += (int) Hadjustment.Value;
			y2 += (int) Vadjustment.Value;

			int start_x = x1 < x2 ? x1 : x2;
			int end_x =   x1 > x2 ? x1 : x2;
			int start_y = y1 < y2 ? y1 : y2;
			int end_y =   y1 > y2 ? y1 : y2;

			// Restore initial selection
			BitArray initial_selection = selection.ToBitArray();
			selection.Clear (false);
			foreach (int i in start_select_selection)
				selection.Add (i, false);

			// Select or toggle according to modifiers
			int start_row  = (start_x - BORDER_SIZE) / cell_width;
			int start_line = (start_y - BORDER_SIZE) / cell_height;
			int end_row    = (end_x - BORDER_SIZE + cell_width - 1) / cell_width;
			int end_line   = (end_y - BORDER_SIZE + cell_height - 1) / cell_height;
			if (start_row > cells_per_row)
				start_row = cells_per_row;
			if (end_row > cells_per_row)
				end_row = cells_per_row;


			FocusCell = start_line * cells_per_row + start_row;

			if ((mod & ModifierType.ControlMask) == 0)
				selection.SelectRect (start_row, end_row, start_line, end_line, cells_per_row);
			else
				selection.ToggleRect (start_row, end_row, start_line, end_line, cells_per_row);

			// fire events for cells which have changed selection flag
			BitArray new_selection = selection.ToBitArray();
			BitArray selection_changed = initial_selection.Xor (new_selection);
			System.Collections.Generic.List<int> changed = new System.Collections.Generic.List<int>();
			for (int i = 0; i < selection_changed.Length; i++)
				if (selection_changed.Get(i))
					changed.Add (i);
			if (selection_changed.Length != 0)
				selection.SignalChange (changed.ToArray());

			// redraw selection box
			if (BinWindow != null) {
				BinWindow.InvalidateRect (rect_select, true); // old selection
				rect_select = new Rectangle (start_x, start_y, end_x - start_x, end_y - start_y);
				BinWindow.InvalidateRect (rect_select, true); // new selection
				BinWindow.ProcessUpdates (true);
			}*/
		}

		// if scroll is required, a timeout is fired
		// until the button is release or the pointer is
		// in window again
		private int deltaVscroll;
		private bool HandleMotionTimeout()
		{
			int new_x, new_y;
			ModifierType new_mod;
			Display.GetPointer (out new_x, out new_y, out new_mod);
			GetPointer (out new_x, out new_y);

			// do scroll
			double newVadj = Vadjustment.Value;
			if (deltaVscroll < 130)
				deltaVscroll += 15;

			if (new_y <= 0) {
				newVadj -= deltaVscroll;
				if (newVadj < 0)
					newVadj = 0;
			} else if ((new_y > Allocation.Height) &&
				   (newVadj < Vadjustment.Upper - Allocation.Height - deltaVscroll))
				newVadj += deltaVscroll;
			Vadjustment.Value = newVadj;
			Vadjustment.ChangeValue();

			// do again selection after scroll
			SelectMotion ();

			// stop firing timeout when no button pressed
			return (new_mod & (ModifierType.Button1Mask | ModifierType.Button3Mask)) != 0;
		}

		private void HandleSelectMotionNotify (object sender, MotionNotifyEventArgs args)
		{
			if ((args.Event.State & (ModifierType.Button1Mask | ModifierType.Button3Mask)) != 0 ) {
				if (Gtk.Drag.CheckThreshold (this, start_press_x, start_press_y,
							     (int) args.Event.X, (int) args.Event.Y))
					if (isRectSelection) {
						// scroll if out of window
						double d_x, d_y;
						deltaVscroll = 30;
						if (EventHelper.GetCoords (args.Event, out d_x, out d_y)) {
							int new_y = (int) d_y;
							if ((new_y <= 0) || (new_y >= Allocation.Height)) {
								if (scroll_timeout == 0)
									scroll_timeout = GLib.Timeout.Add (100, new GLib.TimeoutHandler (HandleMotionTimeout));
							}
						} else if (scroll_timeout != 0) {
							GLib.Source.Remove (scroll_timeout);
							scroll_timeout = 0;
						}

						// handle selection
						SelectMotion ();
					} else  {
						int cell_num = CellAtPosition (start_press_x, start_press_y, false);
						if (selection.Contains (cell_num)) {
							// on a selected cell : do drag&drop
							isDragDrop = true;
							if (StartDrag != null) {
								uint but;
								if ((args.Event.State & ModifierType.Button1Mask) != 0)
									but = 1;
								else
									but = 3;
								StartDrag (this, new StartDragArgs(but, start_select_event));
							}
						} else {
							// not on a selected cell : do rectangular select
							isRectSelection = true;
							start_select_hadj = (int) Hadjustment.Value;
							start_select_vadj = (int) Vadjustment.Value;
							start_select_x = start_press_x - start_select_hadj;
							start_select_y = start_press_y - start_select_vadj;

							// ctrl : toggle selected, shift : keep selected
							if ((args.Event.State & (ModifierType.ShiftMask | ModifierType.ControlMask)) == 0)
								selection.Clear ();

							start_select_selection = selection.Ids; // keep initial selection
							// no rect draw at beginning
							rect_select = new Rectangle ();

							args.RetVal = false;
						}
					}
			}
		}

		private void HandleButtonPressEvent (object obj, ButtonPressEventArgs args)
		{
			int cell_num = CellAtPosition ((int) args.Event.X, (int) args.Event.Y);

			args.RetVal = true;

			start_select_event = args.Event;
			start_press_x = (int) args.Event.X;
			start_press_y = (int) args.Event.Y;
			isRectSelection = false;
			isDragDrop = false;

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
				// on a cell : context menu if button 3
				// cell selection is done on button release
				if (args.Event.Button == 3){
					ContextMenu (args, cell_num);
					return;
				} else args.RetVal = false;

				break;

			default:
				args.RetVal = false;
				break;
			}
		}

		protected virtual void ContextMenu (ButtonPressEventArgs args, int cell_num)
		{
		}

		private void HandleButtonReleaseEvent (object sender, ButtonReleaseEventArgs args)
		{
			if (isRectSelection) {
				// remove scrolling and rectangular selection
				if (scroll_timeout != 0) {
					GLib.Source.Remove (scroll_timeout);
					scroll_timeout = 0;
				}
				SelectMotion ();

				isRectSelection = false;
				if (BinWindow != null) {
					BinWindow.InvalidateRect (rect_select, false);
					BinWindow.ProcessUpdates (true);
				}
				rect_select = new Rectangle();
			} else if (!isDragDrop) {
				int cell_num = CellAtPosition ((int) args.Event.X, (int) args.Event.Y, false);
				if (cell_num != -1) {
					if ((args.Event.State & ModifierType.ControlMask) != 0) {
						selection.ToggleCell (cell_num);
					} else if ((args.Event.State & ModifierType.ShiftMask) != 0) {
						selection.Add (FocusCell, cell_num);
					} else {
						selection.Clear ();
						selection.Add (cell_num);
					}
					FocusCell = cell_num;
				}
			}
			isDragDrop = false;
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
				FocusCell += VisibleColums;
				break;
			case Gdk.Key.Left:
			case Gdk.Key.H:
			case Gdk.Key.h:
				if (control && shift)
					FocusCell -= FocusCell % VisibleColums;
				else
					FocusCell--;
				break;
			case Gdk.Key.Right:
			case Gdk.Key.L:
			case Gdk.Key.l:
				if (control && shift)
					FocusCell += VisibleColums - (FocusCell % VisibleColums) - 1;
				else
					FocusCell++;
				break;
			case Gdk.Key.Up:
			case Gdk.Key.K:
			case Gdk.Key.k:
				FocusCell -= VisibleColums;
				break;
			case Gdk.Key.Page_Up:
				FocusCell -= VisibleColums * VisibleRows;
				break;
			case Gdk.Key.Page_Down:
				FocusCell += VisibleColums * VisibleRows;
				break;
			case Gdk.Key.Home:
				FocusCell = 0;
				break;
			case Gdk.Key.End:
				FocusCell = collection.Count - 1;
				break;
			case Gdk.Key.R:
			case Gdk.Key.r:
                                FocusCell = new Random().Next(0, collection.Count - 1);
                                break;
			case Gdk.Key.space:
				selection.ToggleCell (FocusCell);
				break;
			case Gdk.Key.Return:
				if (DoubleClicked != null)
					DoubleClicked (this, new BrowsableEventArgs (FocusCell, null));
				break;
			default:
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
