//
// IconView.cs:
//
// Authors:
//    Ettore Perazzoli
//    Larry Ewing <lewing@novell.com>
//
// (C) 2003 Novell, Inc.
//
using Gtk;
using Gdk;
using Gnome;
using GtkSharp;
using System;
using System.Reflection;
using System.Collections;
using System.IO;

public class IconView : Gtk.Layout {

	// Public properties.

	/* Width of the thumbnails. */
	private int thumbnail_width = 128;
	public int ThumbnailWidth {
		get {
			return thumbnail_width;
		}

		set {
			if (thumbnail_width != value) {
				thumbnail_width = value;
				QueueResize ();
			}
		}
	}

	private double thumbnail_ratio = 4.0 / 3.0;
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


	// Size of the frame around the thumbnail.
	private const int CELL_BORDER_WIDTH = 10;

	// Border around the scrolled area.
	private const int BORDER_SIZE = 6;

	// Thickness of the outline used to indicate selected items. 
	private const int SELECTION_THICKNESS = 5;

	// Size of the tag icon in the view.
	private const int TAG_ICON_SIZE = 12;

	// Horizontal spacing between the tag icons
	private const int TAG_ICON_HSPACING = 2;

	// Vertical spacing between the thumbnail and the row of tag icons.
	private const int TAG_ICON_VSPACING = 6;

	// The loader.
	private PixbufLoader pixbuf_loader;

	// Various other layout values.
	private int cells_per_row;
	private int cell_width;

	private int cell_height;

	// Query we are displaying.
	private PhotoQuery query;

	// The first pixel line that is currently on the screen (i.e. in the current
	// scroll region).  Used to compute the area that went offscreen in the "changed"
	// signal handler for the vertical GtkAdjustment.
	private int y_offset;

	// Hash of all the order number of the items that are selected.
	private Hashtable selected_cells;

	// Drag and drop bookkeeping. 
	private int click_x, click_y;

	// Focus Handling
	private int real_focus_cell;
	private int focus_cell {
		set {
			if (value != real_focus_cell) {
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

	// The pixbuf we use when we can't load a thumbnail.
	static Pixbuf error_pixbuf;

	// Public events.
	public delegate void DoubleClickedHandler (IconView view, int clicked_item);
	public event DoubleClickedHandler DoubleClicked;

	public delegate void MouseMotionHandler (IconView view, int clicked_item, int mouse_x, int mouse_y, Gdk.ModifierType state);
	public event MouseMotionHandler MouseMotion;

	public delegate void SelectionChangedHandler (IconView view);
	public event SelectionChangedHandler SelectionChanged;


	// Public API.

	public IconView () : base (null, null)
	{
		pixbuf_loader = new PixbufLoader (ThumbnailWidth);
		pixbuf_loader.OnPixbufLoaded += new PixbufLoader.PixbufLoadedHandler (HandlePixbufLoaded);

		selected_cells = new Hashtable ();

		ScrollAdjustmentsSet += new ScrollAdjustmentsSetHandler (HandleScrollAdjustmentsSet);
		SizeAllocated += new SizeAllocatedHandler (HandleSizeAllocated);
		
		ButtonPressEvent += new ButtonPressEventHandler (HandleButtonPressEvent);
		ButtonReleaseEvent += new ButtonReleaseEventHandler (HandleButtonReleaseEvent);
		KeyPressEvent += new KeyPressEventHandler (HandleKeyPressEvent);
		KeyReleaseEvent += new KeyReleaseEventHandler (HandleKeyReleaseEvent);

		DestroyEvent += new DestroyEventHandler (HandleDestroyEvent);

		AddEvents ((int) EventMask.KeyPressMask
			   | (int) EventMask.KeyReleaseMask 
			   | (int) EventMask.PointerMotionMask);
		
		CanFocus = true;
	}

	private void OnReload (PhotoQuery query)
	{
		// FIXME we should probably try to merge the selection forward
		// but it needs some thought to be efficient.
		UnselectAllCells ();
		QueueResize ();
	}

	public IconView (PhotoQuery query) : this ()
	{
		this.query = query;
		query.Reload += new PhotoQuery.ReloadHandler (OnReload);
	}

	//
	// IPhotoSelection
	//
	public PhotoQuery Query {
		get {
			return query;
		}
	}

	public int [] SelectedIdxs {
		get {
			int [] selection = new int [selected_cells.Count];

			int i = 0;
			foreach (int cell in selected_cells.Keys)
				selection [i ++] = cell;

			Array.Sort (selection);
			return selection;
		}
	}

	public int SelectedIdxCount 
	{
		get {
			return selected_cells.Count;
		}
	}
	
	public bool IdxIsSelected (int num)
	{
		return CellIsSelected (num);
	}

	public int CurrentIdx {
		get {
			if (selected_cells.Count == 1 && IdxIsSelected (focus_cell))
				return focus_cell;
			else 
				return -1;
		}
	}

	// Updating.

	public void UpdateThumbnail (int thumbnail_num)
	{
		Photo photo = query.Photos [thumbnail_num];
		string thumbnail_path = Thumbnail.PathForUri ("file://" + photo.DefaultVersionPath, ThumbnailSize.Large);
		ThumbnailCache.Default.RemoveThumbnailForPath (thumbnail_path);
		InvalidateCell (thumbnail_num);
	}


	// Cell Geometry

	public int CellAtPosition (int x, int y)
	{
		if (query == null)
			return -1;

		if (x < BORDER_SIZE || x >= BORDER_SIZE + cells_per_row * cell_width)
			return -1;
		if (y < BORDER_SIZE || y >= BORDER_SIZE + (query.Photos.Length / cells_per_row + 1) * cell_height)
			return -1;

		int column = (int) ((x - BORDER_SIZE) / cell_width);
		int row = (int) ((y - BORDER_SIZE) / cell_height);
		int cell_num = column + row * cells_per_row;

		if (cell_num < query.Photos.Length)
			return (int) cell_num;
		else
			return -1;
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

	static private Pixbuf ErrorPixbuf ()
	{
		if (IconView.error_pixbuf == null)
			IconView.error_pixbuf = PixbufUtils.LoadFromAssembly ("f-spot-question-mark.png");

		return IconView.error_pixbuf;
	}

	public void UnselectAllCells ()
	{
		if (selected_cells.Count == 0)
			return;

		if (selected_cells.Count < 100) {
			foreach (int cell in selected_cells.Keys) {
				InvalidateCell (cell);
			}
		} else {
			QueueDraw ();
		}
		selected_cells.Clear ();		


		if (SelectionChanged != null)
			SelectionChanged (this);
	}

	private bool CellIsSelected (int cell_num)
	{
		return selected_cells.ContainsKey (cell_num);
	}

	private void SelectCellNoNotify (int cell_num)
	{
		if (CellIsSelected (cell_num))
			return;

		selected_cells.Add (cell_num, cell_num);

		InvalidateCell (cell_num);
	}

	private void SelectCell (int cell_num)
	{
		SelectCellNoNotify (cell_num);

		if (SelectionChanged != null)
			SelectionChanged (this);
	}

	public void SelectAllCells ()
	{
		SelectCellRange (0, query.Photos.Length - 1);
	}

	private void SelectCellRange (int start, int end)
	{
		if (start == -1 || end == -1)
			return;

		int current = Math.Min (start, end);
		int final = Math.Max (start, end);				
	
		while (current <= final) {
			SelectCellNoNotify (current);
			current++;
		}

		if (SelectionChanged != null)
			SelectionChanged (this);
	}

	private void UnselectCell (int cell_num)
	{
		if (! CellIsSelected (cell_num))
			return;

		selected_cells.Remove (cell_num);

		InvalidateCell (cell_num);

		if (SelectionChanged != null)
			SelectionChanged (this);
	}

	private void ToggleCell (int cell_num)
	{
		if (CellIsSelected (cell_num))
			UnselectCell (cell_num);
		else
			SelectCell (cell_num);
	}


	// Layout and drawing.

	private void UpdateLayout ()
	{
		int available_width = Allocation.Width - 2 * BORDER_SIZE;

		cell_width = ThumbnailWidth + 2 * CELL_BORDER_WIDTH;
		cell_height = ThumbnailHeight + 2 * CELL_BORDER_WIDTH;

		if (DisplayTags)
			cell_height += TAG_ICON_SIZE + TAG_ICON_VSPACING;

		cells_per_row = (int) (available_width / cell_width);
		if (cells_per_row == 0)
			cells_per_row = 1;

		int num_thumbnails;
		if (query != null)
			num_thumbnails = query.Photos.Length;
		else
			num_thumbnails = 0;

		int num_rows = num_thumbnails / cells_per_row;
		if (num_thumbnails % cells_per_row != 0)
			num_rows ++;

		SetSize ((uint) Allocation.Width, (uint) (num_rows * cell_height + 2 * BORDER_SIZE));

		Vadjustment.StepIncrement = cell_height;
		Vadjustment.Change ();
	}

	// FIXME Cache the GCs?
	private void DrawCell (int thumbnail_num, int x, int y)
	{
		Gdk.GC gc = new Gdk.GC (BinWindow);
		gc.Copy (Style.ForegroundGC (StateType.Normal));
		gc.SetLineAttributes (1, LineStyle.Solid, CapStyle.NotLast, JoinStyle.Round);
		bool selected = CellIsSelected (thumbnail_num);

		Photo photo = query.Photos [thumbnail_num];

		string thumbnail_path = Thumbnail.PathForUri ("file://" + photo.DefaultVersionPath, ThumbnailSize.Large);
		Pixbuf thumbnail = ThumbnailCache.Default.GetThumbnailForPath (thumbnail_path);

		Gdk.Rectangle area = new Gdk.Rectangle (x, y, cell_width, cell_height);
		
#if false
		Style.PaintFlatBox (Style, BinWindow, selected ? (HasFocus ? StateType.Selected :StateType.Active) : StateType.Normal, 
				    ShadowType.Out, area, this, null, x, y, cell_width, cell_height);

#else
		Style.PaintBox (Style, BinWindow, selected ? (HasFocus ? StateType.Selected :StateType.Active) : StateType.Normal, 
				ShadowType.Out, area, this, "IconView", x, y, cell_width, cell_height);

#endif 
		if (HasFocus && thumbnail_num == focus_cell) {
			Style.PaintFocus(Style, BinWindow, StateType.Normal, area, 
					 this, null, x + 3, y + 3, cell_width - 6, cell_height - 6);
		}

		if (thumbnail == null) {
			pixbuf_loader.Request (thumbnail_path, thumbnail_num);
		} else {
			int width, height;
			PixbufUtils.Fit (thumbnail, ThumbnailWidth, ThumbnailHeight, true, out width, out height);

			int dest_x = (int) (x + (cell_width - width) / 2);
			int dest_y;
			if (DisplayTags)
				dest_y = (int) (y + (cell_height - height - (TAG_ICON_SIZE + TAG_ICON_VSPACING)) / 2);
			else
				dest_y = (int) (y + (cell_height - height) / 2);
			
			if (thumbnail_num == throb_cell) {
				double t = throb_state / (double) (throb_state_max - 1);
				double s;
				if (selected)
					s = Math.Cos (-2 * Math.PI * t);
				else
					s = 1 - Math.Cos (-2 * Math.PI * t);

				int scale = (int) (SELECTION_THICKNESS * s);
				dest_x -= scale;
				dest_y -= scale;		
				width += 2 * scale;
				height += 2 * scale;
			} else 	if (selected) { 
				dest_x -= SELECTION_THICKNESS;
				dest_y -= SELECTION_THICKNESS;
				width += 2 * SELECTION_THICKNESS;
				height += 2 * SELECTION_THICKNESS;
			}

			Pixbuf temp_thumbnail;
			if (width == thumbnail.Width) {
				temp_thumbnail = thumbnail;
			} else {
				temp_thumbnail = thumbnail.ScaleSimple (width, height, InterpType.Bilinear);
			}

			temp_thumbnail.RenderToDrawable (BinWindow, Style.WhiteGC,
							 0, 0, dest_x, dest_y, width, height, RgbDither.None, 0, 0);
			
			if (temp_thumbnail != thumbnail)
				temp_thumbnail.Dispose ();
		}

		if (DisplayTags) {
			Tag [] tags = photo.Tags;

			int tag_x, tag_y;

			tag_x = x + CELL_BORDER_WIDTH;
			tag_y = y + cell_height - CELL_BORDER_WIDTH - TAG_ICON_SIZE;
			foreach (Tag t in tags) {
				Pixbuf icon = null;

				if (t.Category.Icon == null) {
					if (t.Icon == null)
						continue;
					icon = t.Icon;
				} else {
					Category category = t.Category;
					while (category.Category.Icon != null)
						category = category.Category;
					icon = category.Icon;
				}

				Pixbuf scaled_icon;
				if (icon.Width == TAG_ICON_SIZE) {
					scaled_icon = icon;
				} else {
					scaled_icon = icon.ScaleSimple (TAG_ICON_SIZE, TAG_ICON_SIZE, InterpType.Bilinear);
				}

				scaled_icon.RenderToDrawable (BinWindow, Style.WhiteGC,
							      0, 0, tag_x, tag_y, TAG_ICON_SIZE, TAG_ICON_SIZE,
							      RgbDither.None, 0, 0);
				tag_x += TAG_ICON_SIZE + TAG_ICON_VSPACING;
			}
		}
	}

	private void DrawAllCells (int x, int y, int width, int height)
	{
		int start_cell_column = Math.Max ((x - BORDER_SIZE) / cell_width, 0);
		int start_cell_row = Math.Max ((y - BORDER_SIZE) / cell_height, 0);
		int start_cell_num = start_cell_column + start_cell_row * cells_per_row;

		int start_cell_x, cell_y;
		GetCellPosition (start_cell_num, out start_cell_x, out cell_y);

		int end_cell_column = Math.Max ((x + width - BORDER_SIZE) / cell_width, 0);
		int end_cell_row = Math.Max ((y + height - BORDER_SIZE) / cell_height, 0);

		int num_rows = end_cell_row - start_cell_row + 1;
		int num_cols = Math.Min (end_cell_column - start_cell_column + 1,
					 cells_per_row - start_cell_column);

		int i, cell_num;
		for (i = 0, cell_num = start_cell_num;
		     i < num_rows && cell_num < query.Photos.Length;
		     i ++) {
			int cell_x = start_cell_x;

			//Console.WriteLine ("Drawing row {0}", start_cell_row + i);
			for (int j = 0; j < num_cols && cell_num + j < query.Photos.Length; j ++) {
				//Console.WriteLine ("Drawing Cell {0}", cell_num + j);
				DrawCell (cell_num + j, cell_x, cell_y);
				cell_x += cell_width;
			}

			cell_y += cell_height;
			cell_num += cells_per_row;
		}

	}

	private void GetCellPosition (int cell_num, out int x, out int y)
	{
		int row = cell_num / cells_per_row;
		int col = cell_num % cells_per_row;

		x = col * cell_width + BORDER_SIZE;
		y = row * cell_height + BORDER_SIZE;
	}


	// Scrolling.  We do this in an idle loop so we can catch up if the user scrolls quickly.

	private uint scroll_on_idle_id;

	private int idle_count;	// FIXME

	private bool HandleScrollOnIdle ()
	{
		Adjustment adjustment = Vadjustment;

		if (y_offset == adjustment.Value)
			return false;

		int num_thumbnails = query.Photos.Length;
		int num_rows, start;

		if (y_offset < adjustment.Value) {
			int old_first_row = y_offset / cell_height;
			int new_first_row = (int) (adjustment.Value / cell_height);

			num_rows = new_first_row - old_first_row;
			start = old_first_row * cells_per_row;
		} else {
			int old_last_row = (y_offset + Allocation.Height) / cell_height;
			int new_last_row = ((int) adjustment.Value + Allocation.Height) / cell_height;

			num_rows = old_last_row - new_last_row;
			start = (new_last_row + 1) * cells_per_row;
		}

		for (int i = 0; i < cells_per_row * num_rows; i ++) {
			if (start + i >= num_thumbnails)
				break;

			Photo photo = query.Photos [start + i];
			string thumbnail_path = Thumbnail.PathForUri ("file://" + photo.DefaultVersionPath, ThumbnailSize.Large);
			pixbuf_loader.Cancel (thumbnail_path);
		}

		y_offset = (int) adjustment.Value;

		scroll_on_idle_id = 0;
		return false;
	}

	private void Scroll ()
	{
		if (scroll_on_idle_id == 0)
			scroll_on_idle_id = GLib.Idle.Add (new GLib.IdleHandler (HandleScrollOnIdle));
	}

	private void CancelScroll ()
	{
		if (scroll_on_idle_id != 0)
			GLib.Source.Remove (scroll_on_idle_id);
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
		//Console.WriteLine ("throb out {1} {0}", throb_cell, 1 - Math.Cos (throb_state));
		
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
		Adjustment adjustment = Vadjustment;
		int x;
		int y;

		GetCellPosition (cell_num, out x, out y);

		if (y < adjustment.Value)
			adjustment.Value = y; 
		else if (y + cell_height > adjustment.Value + adjustment.PageSize)
			adjustment.Value = y + cell_height - adjustment.PageSize;
		
		adjustment.ChangeValue();
	}


	// Event handlers.

	private void HandleAdjustmentValueChanged (object sender, EventArgs args)
	{
		Scroll ();
	}

	private void HandlePixbufLoaded (PixbufLoader loader, string path, int order, Pixbuf result)
	{
		if (result == null)
			result = ErrorPixbuf ();

		ThumbnailCache.Default.AddThumbnail (path, result);
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
		BinWindow.InvalidateRect (cell_area, false);
	}
			
	private void HandleScrollAdjustmentsSet (object sender, ScrollAdjustmentsSetArgs args)
	{
		if (args.Vadjustment != null)
			args.Vadjustment.ValueChanged += new EventHandler (HandleAdjustmentValueChanged);
	}

	private void HandleSizeAllocated (object sender, SizeAllocatedArgs args)
	{
		UpdateLayout ();
	}

	protected override bool OnExposeEvent (Gdk.EventExpose args)
	{
		DrawAllCells (args.Area.X, args.Area.Y,
			      args.Area.Width, args.Area.Height);

		return base.OnExposeEvent (args);
	}

 	private void HandleButtonPressEvent (object obj, ButtonPressEventArgs args)
 	{
		int cell_num = CellAtPosition ((int) args.Event.X, (int) args.Event.Y);

		args.RetVal = true;

		if (cell_num < 0) {
			args.RetVal = false;
			UnselectAllCells ();
			return;
		}

		switch (args.Event.Type) {
		case EventType.TwoButtonPress:
			if (args.Event.Button != 1
			    || (args.Event.State &  (ModifierType.ControlMask
						     | ModifierType.ShiftMask)) != 0)
				return;
			if (DoubleClicked != null)
				DoubleClicked (this, cell_num);
			return;

		case EventType.ButtonPress:
			GrabFocus ();
			if ((args.Event.State & ModifierType.ControlMask) != 0) {
				ToggleCell (cell_num);
			} else if ((args.Event.State & ModifierType.ShiftMask) != 0) {
				SelectCellRange (focus_cell, cell_num);
			} else if (!CellIsSelected (cell_num)) {
				UnselectAllCells ();
				SelectCell (cell_num);
			}

			if (args.Event.Button == 3){
				ContextMenu (args, cell_num);
				return;
			}
			
			if (args.Event.Button != 1)
				return;

			click_x = (int) args.Event.X;
			click_y = (int) args.Event.Y;

			focus_cell = cell_num;
			return;

		default:
			args.RetVal = false;
			return;
		}
	}

	void ContextMenu (ButtonPressEventArgs args, int cell_num)
	{
		PhotoPopup popup = new PhotoPopup ();
		popup.Activate (args.Event);
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
					(selected_cells.Count>1)) {
					UnselectAllCells ();
					SelectCell (cell_num);
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

		focus_old = focus_cell;
		switch (args.Event.Key) {
			case Gdk.Key.Down:
				focus_cell += cells_per_row;
				break;
			case Gdk.Key.Left:
				focus_cell--;
				break;
			case Gdk.Key.Right:
				focus_cell++;
				break;
			case Gdk.Key.Up:
				focus_cell -= cells_per_row;
				break;
			case Gdk.Key.Home:
				focus_cell = 0;
				break;
			case Gdk.Key.End:
				focus_cell = query.Photos.Length - 1; 
				break;
			case Gdk.Key.space:
				ToggleCell (focus_cell);
				break;
			default:	
				args.RetVal = false;
				return;		
		}
		
		if (focus_cell < 0 || focus_cell > query.Photos.Length - 1) {
			focus_cell = focus_old;
			args.RetVal = false;
		}	

		if (shift) {
			SelectCellRange (focus_old, focus_cell);
		} else if (!control) {
			UnselectAllCells ();
			SelectCell (focus_cell);
		} 
	
		ScrollTo (focus_cell);
	}

	private void HandleKeyReleaseEvent (object sender, KeyReleaseEventArgs args)
	{
		Console.WriteLine ("Release!");
	}

	private void HandleDestroyEvent (object sender, DestroyEventArgs args)
	{
		CancelScroll ();
		CancelThrob ();
	}
}
