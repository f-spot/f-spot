//
// FacesTool.cs
//
// TODO: Add authors and license.
//

using System;
using System.Collections.Generic;

using Gdk;

using Gtk;

using FSpot.Core;
using FSpot.Widgets;

using Hyena;

namespace FSpot
{
	public class FacesTool : IDisposable
	{
		public const int CONTROL_SPACING = 8;

		private IDictionary<string, FaceShape> face_shapes;
		private IDictionary<string, string> original_face_locations;
		private FaceShape editing_face_shape = null;
		private PhotoImageView view;
		private bool active = false;
		private bool loaded = false;
		private bool view_was_selectable;
		private Photo photo;

		private FacesToolWindow faces_tool_window;
		public FacesToolWindow Window {
			get {
				return faces_tool_window;
			}
		}

		public FacesTool () {}

		public void Dispose ()
		{
			Deactivate ();

			if (!loaded)
				return;

			if (faces_tool_window != null)
				faces_tool_window.Destroy ();

			foreach (FaceShape face_shape in face_shapes.Values)
				face_shape.Dispose ();

			System.GC.SuppressFinalize (this);
		}

		~FacesTool ()
		{
			Log.DebugFormat ("Finalizer called on {0}. Should be Disposed", GetType ());

			Deactivate ();

			if (!loaded)
				return;

			if (faces_tool_window != null)
				faces_tool_window.Destroy ();

			foreach (FaceShape face_shape in face_shapes.Values)
				face_shape.Dispose ();
		}

		public void Load ()
		{
			view = App.Instance.Organizer.PhotoView.View;
			
			photo = view.Item.Current as Photo;
			if (photo == null)
				return;
			
			face_shapes = new Dictionary<string, FaceShape> ();
			original_face_locations = new Dictionary<string, string> ();
			
			faces_tool_window = new FacesToolWindow ();
			
			FaceStore face_store = App.Instance.Database.Faces;
			Dictionary<uint, FaceLocation> face_locations =
				App.Instance.Database.FaceLocations.GetFaceLocationsByPhoto (photo);
			foreach (FaceLocation face_location in face_locations.Values) {
				FaceShape new_face_shape;
				try {
					new_face_shape = FaceShape.FromSerialized (this, face_location.Geometry);
				} catch (Exception e) {
					Log.DebugException (e);
					
					continue;
				}
				
				Face face = face_store.Get (face_location.FaceId);
				new_face_shape.Name = face.Name;
				
				AddFace (new_face_shape);
				original_face_locations [face.Name] = face_location.Geometry;
			}
			
			SetOkButtonSensitivity ();

			loaded = true;
		}

		public void Activate ()
		{
			if (active)
				return;

			active = true;

			if (loaded) {
				// TODO This could be improved to avoid unnecessary calls by checking if the
				// offset has changed instead of always doing it.
				foreach (FaceShape face_shape in face_shapes.Values)
					face_shape.OnOffsetChanged (this, EventArgs.Empty);
			} else
				Load ();

			view_was_selectable = view.CanSelect;
			view.CanSelect = false;

			BindViewHandlers ();
			BindWindowHandlers ();
		}

		public void Deactivate ()
		{
			if (!active)
				return;

			active = false;

			if (view != null) {
				view.CanSelect = view_was_selectable;
				
				UnbindViewHandlers ();
			}
			
			if (faces_tool_window != null)
				UnbindWindowHandlers ();
			
			if (editing_face_shape != null && !face_shapes.Values.Contains (editing_face_shape)) {
				editing_face_shape.Dispose ();
				editing_face_shape = null;
			}
		}

		private void BindViewHandlers ()
		{
			view.ButtonPressEvent += OnLeftClick;
			view.ButtonReleaseEvent += OnLeftReleased;
			view.MotionNotifyEvent += OnMotion;
			view.LeaveNotifyEvent += OnLeaveNotifyEvent;
			view.ExposeEvent += OnExpose;
			view.ZoomChanged += OnZoomChanged;

			view.Hadjustment.ValueChanged += OnScrollChanged;
			view.Vadjustment.ValueChanged += OnScrollChanged;
		}
		
		private void UnbindViewHandlers ()
		{
			view.ButtonPressEvent -= OnLeftClick;
			view.ButtonReleaseEvent -= OnLeftReleased;
			view.MotionNotifyEvent -= OnMotion;
			view.LeaveNotifyEvent -= OnLeaveNotifyEvent;
			view.ExposeEvent -= OnExpose;
			view.ZoomChanged -= OnZoomChanged;

			view.Hadjustment.ValueChanged -= OnScrollChanged;
			view.Vadjustment.ValueChanged -= OnScrollChanged;
		}
		
		private void BindWindowHandlers ()
		{
			faces_tool_window.KeyPressEvent += OnKeyPressed;
			faces_tool_window.OkButton.Clicked += OnFacesOk;
			faces_tool_window.FaceHidden += OnFaceHidden;
			faces_tool_window.FaceEditRequested += OnFaceEditRequested;
			faces_tool_window.FaceDeleteRequested += OnFaceDeleteRequested;
		}
		
		private void UnbindWindowHandlers ()
		{
			faces_tool_window.KeyPressEvent -= OnKeyPressed;
			faces_tool_window.OkButton.Clicked -= OnFacesOk;
			faces_tool_window.FaceHidden -= OnFaceHidden;
			faces_tool_window.FaceEditRequested -= OnFaceEditRequested;
			faces_tool_window.FaceDeleteRequested -= OnFaceDeleteRequested;
		}

		public void OnExpose (object sender, ExposeEventArgs args)
		{
			if (editing_face_shape != null)
				editing_face_shape.Show ();
		}

		public void OnZoomChanged (object sender, EventArgs e)
		{
			if (editing_face_shape != null)
				editing_face_shape.OnOffsetChanged (sender, e);

			foreach (FaceShape face_shape in face_shapes.Values)
				face_shape.OnOffsetChanged (sender, e);
		}

		public void OnScrollChanged (object sender, EventArgs e)
		{
			OnZoomChanged (sender, e);
		}

		public void OnKeyPressed (object sender, KeyPressEventArgs e)
		{
			string event_keyval = Keyval.Name (e.Event.KeyValue);
			if (event_keyval == "Return" || event_keyval == "KP_Enter") {
				OnFacesOk (sender, e);
				e.RetVal = true;
			}
		}

		public void OnLeftClick (object sender, ButtonPressEventArgs e)
		{
			if (e.Event.Button != 1)
				return;

			if (editing_face_shape != null) {
				editing_face_shape.OnLeftClick (sender, e);

				if (e.RetVal != null && (bool) e.RetVal)
					return;
			}
			
			foreach (FaceShape face_shape in face_shapes.Values) {
				if (face_shape.Visible && face_shape.CursorIsOver ((int) e.Event.X, (int) e.Event.Y)) {
					EditFaceShape (face_shape);
					face_shape.Editable = true;
					
					return;
				}
			}
			
			NewFaceShape ((int) e.Event.X, (int) e.Event.Y);
		}

		public void OnLeftReleased (object sender, ButtonReleaseEventArgs e)
		{
			if (e.Event.Button != 1)
				return;

			if (editing_face_shape == null)
				return;

			editing_face_shape.OnLeftReleased (sender, e);
			
			if (faces_tool_window.CurrentEditingPhase == EditingPhase.CreatingDragging)
				faces_tool_window.UpdateCurrentEditingPhase (EditingPhase.CreatingEditing);
		}

		public void OnMotion (object sender, MotionNotifyEventArgs e)
		{
			if (editing_face_shape != null) {
				editing_face_shape.OnMotion (sender, e);

				return;
			}

			int x = (int) e.Event.X;
			int y = (int) e.Event.Y;

			List<FaceShape> faces_under_cursor = new List<FaceShape> ();
			foreach (FaceShape face_shape in face_shapes.Values) {
				bool cursor_is_over = face_shape.CursorIsOver (x, y);
				if (cursor_is_over) {
					faces_under_cursor.Add (face_shape);
				} else if (!cursor_is_over && face_shape.Visible) {
					face_shape.Hide ();
					face_shape.Widget.DeactivateLabel ();
				}
			}

			if (faces_under_cursor.Count == 0)
				return;

			FaceShape to_show = null;

			double distance = 0;
			double new_distance;
			foreach (FaceShape face_shape in faces_under_cursor) {
				if (to_show == null) {
					to_show = face_shape;
					distance = face_shape.GetDistance (x, y);

					continue;
				}

				new_distance = face_shape.GetDistance (x, y);
				if (new_distance < distance) {
					distance = new_distance;
					to_show = face_shape;
				}
			}

			foreach (FaceShape face_shape in faces_under_cursor) {
				if (face_shape == to_show || !face_shape.Visible)
					continue;

				face_shape.Hide ();
				face_shape.Widget.DeactivateLabel ();
			}

			to_show.Show ();
			to_show.Widget.ActivateLabel ();
		}

		public void OnLeaveNotifyEvent (object sender, LeaveNotifyEventArgs e)
		{
			if (editing_face_shape != null)
				return;
			
			foreach (FaceShape face_shape in face_shapes.Values) {
				if (!face_shape.Visible)
					continue;
				
				face_shape.Hide ();
				face_shape.Widget.DeactivateLabel ();

				break;
			}
			
			faces_tool_window.UpdateCurrentEditingPhase (EditingPhase.NotEditing);
		}
		
		public ICollection<string> GetAddedFaceNames ()
		{
			return face_shapes.Keys;
		}

		private void NewFaceShape (int x, int y)
		{
			EditFaceShape (new FaceRectangle (this, x, y), true);
		}
		
		private void EditFaceShape (FaceShape face_shape, bool creating = false)
		{
			HideVisibleFace ();
			
			if (editing_face_shape != null && editing_face_shape != face_shape) {
				// We need to do this because it could be one of the already
				// created faces being edited, and if that is the case it
				// will not be destroyed.
				editing_face_shape.Hide ();
				editing_face_shape.Editable = false;
				
				// This is to allow the user to edit a FaceShape's shape
				// without pressing the Enter button.
				if (face_shapes.Values.Contains (editing_face_shape))
					SetOkButtonSensitivity ();

				editing_face_shape = null;
			}
			
			if (creating) {
				faces_tool_window.UpdateCurrentEditingPhase (EditingPhase.CreatingDragging);
			} else {
				face_shape.Show ();
				
				faces_tool_window.UpdateCurrentEditingPhase (EditingPhase.Editing);
			}
			
			if (editing_face_shape != face_shape) {
				editing_face_shape = face_shape;
				editing_face_shape.AddMeRequested += OnAddMeRequested;
				editing_face_shape.DeleteMeRequested += OnDeleteMeRequested;
			}
		}

		private void OnDeleteMeRequested (object sender, EventArgs e)
		{
			ReleaseFaceShape ();
		}

		private void ReleaseFaceShape ()
		{
			if (editing_face_shape == null)
				return;

			if (face_shapes.Values.Contains (editing_face_shape)) {
				editing_face_shape.Hide ();
				editing_face_shape.Editable = false;

				editing_face_shape.Widget.DeactivateLabel ();
			} else
				editing_face_shape.Dispose ();

			editing_face_shape = null;
			
			faces_tool_window.UpdateCurrentEditingPhase (EditingPhase.NotEditing);
		}
		
		private void HideVisibleFace ()
		{
			foreach (FaceShape face_shape in face_shapes.Values) {
				if (face_shape.Visible) {
					face_shape.Hide ();
					
					break;
				}
			}
		}

		private void OnFacesOk (object sender, EventArgs e)
		{
			original_face_locations = new Dictionary<string, string> ();
			Dictionary<Face, string> new_faces = new Dictionary<Face, string> ();
			foreach (FaceShape face_shape in face_shapes.Values) {
				if (!face_shape.Known)
					continue;

				Face new_face = App.Instance.Database.Faces.CreateFace (face_shape.Name);

				string serialized = face_shape.Serialize ();
				new_faces [new_face] = serialized;
				original_face_locations [new_face.Name] = serialized;
			}

			FaceLocation face_location;
			FaceLocationStore face_location_store = App.Instance.Database.FaceLocations;
			FaceStore face_store = App.Instance.Database.Faces;
			ICollection<Face> original_faces = face_store.GetFacesByPhoto (photo);

			// Remove any face that's in the original list but not the new one
			List<FaceLocation> to_remove = new List<FaceLocation> ();
			foreach (Face face in original_faces) {
				if (new_faces.ContainsKey (face))
					continue;

				face_location = face_location_store.Get (face, photo);
				if (face_location == null)
					continue;

				to_remove.Add (face_location);
			}

			if (to_remove.Count > 0)
				face_location_store.Remove (to_remove.ToArray ());

			// Add any face that's in the new list but not the original
			List<FaceLocation> to_update = new List<FaceLocation> ();
			foreach (KeyValuePair<Face, string> entry in new_faces) {
				if (original_faces.Contains (entry.Key)) {
					// If it is already in the original list we need to check if its
					// geometry has changed.
					face_location = face_location_store.Get (entry.Key, photo);
					if (face_location.Geometry.Equals (entry.Value, StringComparison.Ordinal))
						continue;
					
					face_location.Geometry = entry.Value;
					to_update.Add (face_location);
				} else
					face_location_store.CreateFaceLocation (entry.Key.Id, photo.Id, entry.Value);
			}

			if (to_update.Count > 0)
				face_location_store.Commit (to_update.ToArray ());

			faces_tool_window.UpdateOkButtonSensitiveness (false);
		}

		private void OnFaceHidden (object sender, EventArgs e)
		{
			if (editing_face_shape != null)
				editing_face_shape.Show ();
		}

		private void OnAddMeRequested (object sender, EventArgs e)
		{
			AddFace ((FaceShape) sender);
		}

		private void AddFace (FaceShape face_shape)
		{
			string face_name = face_shape.Name;
			if (face_shapes.Values.Contains (face_shape)) {
				foreach (string name in face_shapes.Keys) {
					if (face_shapes [name] == face_shape) {
						if (name == face_name)
							break;
						
						face_shapes [name] = face_shape;
						
						face_shape.Known = true;
					}
				}
			} else if (!face_shapes.ContainsKey (face_name)) {
				faces_tool_window.AddFace (face_shape);
				face_shapes [face_name] = face_shape;
			} else
				return;

			face_shape.Hide ();
			face_shape.Editable = false;

			SetOkButtonSensitivity ();
			ReleaseFaceShape ();
		}

		private void OnFaceEditRequested (object sender, FaceEditionEventArgs e)
		{
			FaceShape face_shape = face_shapes [e.FaceName];
			
			face_shape.Editable = true;
			EditFaceShape (face_shape);
		}
		
		private void OnFaceDeleteRequested (object sender, FaceEditionEventArgs e)
		{
			if (editing_face_shape != null &&
			    e.FaceName.Equals (editing_face_shape.Name, StringComparison.CurrentCultureIgnoreCase))
				editing_face_shape = null;

			face_shapes [e.FaceName].Dispose ();
			face_shapes.Remove (e.FaceName);

			// It is posible to have two visible faces at the same time, this happens
			// if you are editing one face and you move the pointer around the
			// FaceWidgets area in FacesToolWindow. And you can delete one of that
			// faces, so the other visible face must be repainted.
			foreach (FaceShape face_shape in face_shapes.Values) {
				if (face_shape.Visible) {
					face_shape.Hide ();
					face_shape.Show ();
					
					break;
				}
			}
			
			SetOkButtonSensitivity ();
		}

		private void SetOkButtonSensitivity ()
		{
			Dictionary<string, FaceShape> known_face_shapes = new Dictionary<string, FaceShape> ();
			foreach (KeyValuePair<string, FaceShape> name_and_shape in face_shapes) {
				if (name_and_shape.Value.Known)
					known_face_shapes [name_and_shape.Key] = name_and_shape.Value;
			}
			
			if (original_face_locations.Count != known_face_shapes.Count) {
				faces_tool_window.UpdateOkButtonSensitiveness (true);
				
				return;
			}
			
			foreach (KeyValuePair<string, FaceShape> name_and_shape in known_face_shapes) {
				bool found = false;
				foreach (KeyValuePair<string, string> name_and_serialized in original_face_locations) {
					if (!name_and_serialized.Key.Equals (name_and_shape.Key, StringComparison.Ordinal))
						continue;

					if (name_and_serialized.Value.Equals (name_and_shape.Value.Serialize (), StringComparison.Ordinal)) {
						found = true;

						break;
					} else {
						faces_tool_window.UpdateOkButtonSensitiveness (true);

						return;
					}
				}
				
				if (!found) {
					faces_tool_window.UpdateOkButtonSensitiveness (true);
					
					return;
				}
			}
			
			faces_tool_window.UpdateOkButtonSensitiveness (false);
		}
	}
	
	public abstract class FaceShape : IDisposable
	{
		public const string SHAPE_TYPE = null;
		
		protected const int FACE_WINDOW_MARGIN = 5;
		protected const int LABEL_MARGIN = 12;
		protected const int LABEL_PADDING = 9;
		
		public event EventHandler AddMeRequested;
		public event EventHandler DeleteMeRequested;
		
		protected EditingFaceToolWindow face_window;
		protected CursorType current_cursor_type = CursorType.BottomRightCorner;
		protected PhotoImageView view;
		protected string serialized = null;

		private WeakReference face_widget_ref;

		private bool editable = true;
		public bool Editable {
			get {
				return editable;
			}

			set {
				if (visible && value != editable) {
					Hide ();
					editable = value;
					Show ();
					
					return;
				}
				
				editable = value;
			}
		}
		
		private bool visible = true;
		public bool Visible {
			get {
				return visible;
			}

			set {
				if (value == visible)
					return;

				if (value)
					Show ();
				else
					Hide ();
			}
		}

		public bool Known = true;

		public string Name {
			get {
				string face_name = face_window.Entry.Text;

				return face_name == "" ? null : face_name;
			}

			set {
				face_window.Entry.Text = value;
			}
		}
		
		public FaceWidget Widget {
			get {
				if (face_widget_ref.Target == null)
					throw new Exception ("FaceWidget reference is null.");

				return face_widget_ref.Target as FaceWidget;
			}

			set {
				face_widget_ref.Target = value;
			}
		}
				
		public FaceShape (FacesTool faces_tool)
		{
			face_widget_ref = new WeakReference (null);

			view = App.Instance.Organizer.PhotoView.View;
			view.GdkWindow.Cursor = new Cursor (current_cursor_type);

			face_window = new EditingFaceToolWindow (faces_tool, App.Instance.Organizer.Window);
			face_window.KeyPressed += OnKeyPressed;
			face_window.NameSelected += OnNameSelected;

			face_window.ShowAll ();
			face_window.Hide ();
		}

		public virtual void Dispose ()
		{
			if (visible)
				Erase ();
			
			face_window.KeyPressed -= OnKeyPressed;
			face_window.NameSelected -= OnNameSelected;
			face_window.Destroy ();
			
			// make sure the cursor isn't set to a modify indicator
			view.GdkWindow.Cursor = new Cursor (CursorType.LeftPtr);

			System.GC.SuppressFinalize (this);
		}

		~FaceShape ()
		{
			if (visible)
				Erase ();
			
			face_window.KeyPressed -= OnKeyPressed;
			face_window.NameSelected -= OnNameSelected;
			face_window.Destroy ();
			
			// make sure the cursor isn't set to a modify indicator
			view.GdkWindow.Cursor = new Cursor (CursorType.LeftPtr);
		}

		public static FaceShape FromSerialized (FacesTool faces_tool, string serialized)
		{
			FaceShape face_shape;
			
			string [] args = serialized.Split (new string [] {";"}, StringSplitOptions.None);
			switch (args[0]) {
			case FaceRectangle.SHAPE_TYPE:
				face_shape = FaceRectangle.FromSerialized (faces_tool, args);
				
				break;
			default:
				throw new Exception ("Unrecognized FaceShape type.");
			}
			
			face_shape.serialized = serialized;
			
			return face_shape;
		}

		public void Hide ()
		{
			visible = false;
			Erase ();
			
			if (editable)
				face_window.Hide ();
			
			// make sure the cursor isn't set to a modify indicator
			App.Instance.Organizer.PhotoView.View.GdkWindow.Cursor = new Cursor (CursorType.LeftPtr);
		}
		
		public void Show ()
		{
			visible = true;
			Paint ();
			
			if (editable) {
				UpdateFaceWindowPosition ();
				face_window.Show ();
				face_window.Present ();

				if (!Known)
					face_window.Entry.SelectRegion (0, -1);
			}
		}
		
		public void OnKeyPressed (object sender, KeyPressEventArgs e)
		{
			EventHandler handler;
			switch (Keyval.Name (e.Event.KeyValue)) {
			case "Escape":
				handler = DeleteMeRequested;
				if (handler != null)
					handler (this, EventArgs.Empty);

				break;
			case "Return":
			case "KP_Enter":
				handler = AddMeRequested;
				if (handler != null)
					handler (this, EventArgs.Empty);

				break;
			default:
				return;
			}
			
			e.RetVal = true;
		}
		
		public void OnNameSelected (object sender, EventArgs e)
		{
			EventHandler handler = AddMeRequested;
			if (handler != null)
				handler (this, EventArgs.Empty);
		}

		protected void RaiseDeleteMeRequested ()
		{
			EventHandler handler = DeleteMeRequested;
			if (handler != null) {
				handler (this, EventArgs.Empty);
			}
		}

		public abstract string Serialize ();
		public abstract void UpdateFaceWindowPosition ();
		public abstract void OnMotion (object sender, MotionNotifyEventArgs e);
		public abstract void OnLeftReleased (object sender, ButtonReleaseEventArgs e);
		public abstract void OnLeftClick (object sender, ButtonPressEventArgs e);
		public abstract void OnOffsetChanged (object sender, EventArgs e);
		public abstract bool CursorIsOver (int x, int y);
		public abstract double GetDistance (int x, int y);
		
		protected abstract void Paint ();
		protected abstract void Erase ();
	}

	public enum CursorLocation {
		Outside,
		Inside,
		TopSide,
		LeftSide,
		RightSide,
		BottomSide,
		TopLeft,
		BottomLeft,
		TopRight,
		BottomRight
	}

	public static class RectangleExtensions {
		private const int THRESHOLD = 12;

		public static CursorLocation ApproxLocation (this Gdk.Rectangle rectangle, int x, int y)
		{
			bool near_width = NearInBetween (x, rectangle.Left, rectangle.Right);
			bool near_height = NearInBetween (y, rectangle.Top, rectangle.Bottom);
			
			if (InZone (x, rectangle.Left) && near_height) {
				if (InZone (y, rectangle.Top))
					return CursorLocation.TopLeft;
				else if (InZone (y, rectangle.Bottom))
					return CursorLocation.BottomLeft;
				else
					return CursorLocation.LeftSide;

			} else if (InZone (x, rectangle.Right) && near_height) {
				if (InZone (y, rectangle.Top))
					return CursorLocation.TopRight;
				else if (InZone (y, rectangle.Bottom))
					return CursorLocation.BottomRight;
				else
					return CursorLocation.RightSide;

			} else if (InZone (y, rectangle.Top) && near_width) {
				// if left or right was in zone, already caught top left & top right
				return CursorLocation.TopSide;
			} else if (InZone (y, rectangle.Bottom) && near_width) {
				// if left or right was in zone, already caught bottom left & bottom right
				return CursorLocation.BottomSide;
			} else if (InBetween (x, rectangle.Left, rectangle.Right) &&
			           InBetween (y, rectangle.Top, rectangle.Bottom)) {
				return CursorLocation.Inside;
			} else {
				return CursorLocation.Outside;
			}
		}

		private static bool InZone (double pos, int zone)
		{
			int top_zone = zone - THRESHOLD;
			int bottom_zone = zone + THRESHOLD;
			
			return InBetween (pos, top_zone, bottom_zone);
		}
		
		private static bool InBetween (double pos, int top, int bottom)
		{
			int ipos = (int) pos;
			
			return (ipos > top) && (ipos < bottom);
		}
		
		private static bool NearInBetween (double pos, int top, int bottom)
		{
			int ipos = (int) pos;
			int top_zone = top - THRESHOLD;
			int bottom_zone = bottom + THRESHOLD;
			
			return (ipos > top_zone) && (ipos < bottom_zone);
		}
	}
	
	public class FaceRectangle : FaceShape
	{
		public const string SHAPE_TYPE = "Rectangle";
		
		private const int FACE_MIN_SIZE = 8;
		private const int NULL_SIZE = 0;
		
		private Gdk.Rectangle box;
		private Gdk.Rectangle offset_box;
		private Gdk.Rectangle label_box = Gdk.Rectangle.Zero;
		private CursorLocation in_manipulation = CursorLocation.Outside;
		private int last_grab_x = -1;
		private int last_grab_y = -1;

		public FaceRectangle (FacesTool faces_tool, int x, int y, int half_width = NULL_SIZE, int half_height = NULL_SIZE)
			: base (faces_tool)
		{
			// If half_width is NULL_SIZE we are creating a new FaceShape,
			// otherwise we are only showing a previously created one.
			if (half_width == NULL_SIZE) {
				Gdk.Point image_point = view.WindowCoordsToImage (new Gdk.Point (x, y));
				
				box = Gdk.Rectangle.FromLTRB (image_point.X, image_point.Y,
				                              image_point.X, image_point.Y);
				
				in_manipulation = CursorLocation.BottomRight;
				last_grab_x = image_point.X;
				last_grab_y = image_point.Y;

				offset_box = Gdk.Rectangle.FromLTRB (x, y, x, y);
			} else {
				int right = x + half_width;
				int bottom = y + half_height;

				box = Gdk.Rectangle.FromLTRB (x - half_width, y - half_height, right, bottom);

				offset_box = view.ImageCoordsToWindow (box);
			}
		}

		public override void Dispose ()
		{
			if (!Editable)
				EraseLabel ();

			base.Dispose ();
		}

		~FaceRectangle ()
		{
			if (!Editable)
				EraseLabel ();
		}
		
		public static new FaceRectangle FromSerialized (FacesTool faces_tool, string [] args)
		{
			PhotoImageView view = App.Instance.Organizer.PhotoView.View;
			int x = (int) (view.Pixbuf.Width * Double.Parse (args[1]));
			int y = (int) (view.Pixbuf.Height * Double.Parse (args[2]));
			int half_width = (int) (view.Pixbuf.Width * Double.Parse (args[3]));
			int half_height = (int) (view.Pixbuf.Height * Double.Parse (args[4]));

			return new FaceRectangle (faces_tool, x, y, half_width, half_height);
		}
		
		public override void UpdateFaceWindowPosition ()
		{
			int x;
			int y;
			view.GdkWindow.GetOrigin (out x, out y);
			x += offset_box.Left + ((offset_box.Width - face_window.Allocation.Width) / 2);
			y += offset_box.Bottom + FACE_WINDOW_MARGIN;

			face_window.Move (x, y);
		}
		
		protected override void Paint ()
		{
			using (Cairo.Context ctx = CairoHelper.Create (view.GdkWindow)) {
				CairoHelper.SetSourceColor (ctx, new Gdk.Color (0, 0, 0));

				ctx.Rectangle (offset_box.X, offset_box.Y, offset_box.Width, offset_box.Height);
				ctx.Stroke ();

				CairoHelper.SetSourceColor (ctx, new Gdk.Color (255, 255, 255));
				Gdk.Rectangle deflated_box = new Gdk.Rectangle (offset_box.X, offset_box.Y,
				                                                offset_box.Width, offset_box.Height);
				deflated_box.Inflate (-1, -1);
				ctx.Rectangle (deflated_box.X, deflated_box.Y, deflated_box.Width, deflated_box.Height);
				ctx.Stroke ();
			}

			if (!Editable)
				PaintLabel ();
		}

		protected override void Erase ()
		{
			Gdk.Rectangle eraser_box = new Gdk.Rectangle (offset_box.X, offset_box.Y,
			                                              offset_box.Width, offset_box.Height);
			eraser_box.Inflate (1, 1);
			view.QueueDrawArea (eraser_box.X, eraser_box.Y, eraser_box.Width, eraser_box.Height);

			if (!Editable)
				EraseLabel ();
		}
		
		private void PaintLabel ()
		{
			using (Cairo.Context ctx = CairoHelper.Create (view.GdkWindow)) {
				Cairo.TextExtents text_extents = ctx.TextExtents (Name);
				
				int width = (int) text_extents.Width + LABEL_PADDING;
				int height = (int) text_extents.Height;
				int x = offset_box.Left + (offset_box.Width - width) / 2;
				int y = offset_box.Bottom + LABEL_MARGIN;
				
				label_box = Gdk.Rectangle.FromLTRB (x, y, x + width, y + height + LABEL_PADDING);
				
				ctx.Rectangle (x, y, width, height + LABEL_PADDING);
				ctx.SetSourceRGBA (0.0, 0.0, 0.0, 0.6);
				ctx.Fill ();
				
				ctx.SetSourceRGB (1.0, 1.0, 1.0);
				ctx.MoveTo (x + LABEL_PADDING / 2, y + height + LABEL_PADDING / 2);
				ctx.ShowText (Name);
			}
		}
		
		private void EraseLabel ()
		{
			if (label_box == Gdk.Rectangle.Zero)
				return;

			view.QueueDrawArea (label_box.Left, label_box.Top, label_box.Width, label_box.Height);

			label_box = Gdk.Rectangle.Zero;
		}
		
		public override string Serialize ()
		{
			if (serialized != null)
				return serialized;
			
			double x;
			double y;
			double half_width;
			double half_height;
			
			GetGeometry (out x, out y, out half_width, out half_height);
			
			serialized = String.Format ("{0};{1};{2};{3};{4}", SHAPE_TYPE,
			                            x.ToString ("R"), y.ToString ("R"),
			                            half_width.ToString ("R"), half_height.ToString ("R"));
			
			return serialized;
		}
		
		public void GetGeometry (out double x, out double y, out double half_width, out double half_height)
		{
			x = (box.Left + (box.Width / 2)) / (double) view.Pixbuf.Width;
			y = (box.Top + (box.Height / 2)) / (double) view.Pixbuf.Height;
			
			double width_left_end = box.Left / (double) view.Pixbuf.Width;
			double width_right_end = box.Right / (double) view.Pixbuf.Width;
			double height_top_end = box.Top / (double) view.Pixbuf.Height;
			double height_bottom_end = box.Bottom / (double) view.Pixbuf.Height;

			half_width = (width_right_end - width_left_end) / 2;
			half_height = (height_bottom_end - height_top_end) / 2;
		}
		
		private bool OnViewManipulation (object sender, MotionNotifyEventArgs e)
		{
			int x = (int) e.Event.X;
			int y = (int) e.Event.Y;

			int left = offset_box.Left;
			int top = offset_box.Top;
			int right = offset_box.Right;
			int bottom = offset_box.Bottom;

			int width;
			int height;

			Gdk.Rectangle offset = view.ImageCoordsToWindow (
				new Gdk.Rectangle (0, 0, view.Pixbuf.Width, view.Pixbuf.Height));

			switch (in_manipulation) {
			case CursorLocation.LeftSide:
				left = x;
				break;
				
			case CursorLocation.TopSide:
				top = y;
				break;
				
			case CursorLocation.RightSide:
				right = x;
				break;
				
			case CursorLocation.BottomSide:
				bottom = y;
				break;
				
			case CursorLocation.TopLeft:
				top = y;
				left = x;
				break;
				
			case CursorLocation.BottomLeft:
				bottom = y;
				left = x;
				break;
				
			case CursorLocation.TopRight:
				top = y;
				right = x;
				break;
				
			case CursorLocation.BottomRight:
				bottom = y;
				right = x;
				break;
				
			case CursorLocation.Inside:
				int delta_x = (x - last_grab_x);
				int delta_y = (y - last_grab_y);
				
				last_grab_x = x;
				last_grab_y = y;
				
				width = right - left + 1;
				height = bottom - top + 1;
				
				left += delta_x;
				top += delta_y;
				right += delta_x;
				bottom += delta_y;
				
				// bound offset_box inside of photo
				if (left < offset.X)
					left = offset.X;
				
				if (top < offset.Y + 1)
					top = offset.Y + 1;
				
				int max_right = offset.Width + offset.X;
				if (right >= max_right)
					right = max_right;
				
				int max_height = offset.Height + offset.Y;
				if (bottom >= max_height)
					bottom = max_height;

				int adj_width = right - left + 1;
				int adj_height = bottom - top + 1;
				
				// don't let adjustments affect the size of the offset_box
				if (adj_width != width) {
					if (delta_x < 0)
						right = left + width - 1;
					else left = right - width + 1;
				}
				
				if (adj_height != height) {
					if (delta_y < 0)
						bottom = top + height - 1;
					else top = bottom - height + 1;
				}
				break;
				
			default:
				// do nothing, not even a repaint
				return false;
			}
			
			// Check if the mouse has gone out of bounds, and if it has, make sure that the
			// face shape edges stay within the photo bounds.
			width = right - left + 1;
			height = bottom - top + 1;

			if (left < offset.X)
				left = offset.X;
			if (top < offset.Y)
				top = offset.Y;
			
			int photo_right_edge = offset.Width + offset.X;
			if (right > photo_right_edge)
				right = photo_right_edge;
			
			int photo_bottom_edge = offset.Height + offset.Y;
			if (bottom > photo_bottom_edge)
				bottom = photo_bottom_edge;

			width = right - left + 1;
			height = bottom - top + 1;
			
			switch (in_manipulation) {
			case CursorLocation.LeftSide:
			case CursorLocation.TopLeft:
			case CursorLocation.BottomLeft:
				if (width < FACE_MIN_SIZE)
					left = right - FACE_MIN_SIZE;
				break;
				
			case CursorLocation.RightSide:
			case CursorLocation.TopRight:
			case CursorLocation.BottomRight:
				if (width < FACE_MIN_SIZE)
					right = left + FACE_MIN_SIZE;
				break;
				
			default:
				break;
			}
			
			switch (in_manipulation) {
			case CursorLocation.TopSide:
			case CursorLocation.TopLeft:
			case CursorLocation.TopRight:
				if (height < FACE_MIN_SIZE)
					top = bottom - FACE_MIN_SIZE;
				break;
				
			case CursorLocation.BottomSide:
			case CursorLocation.BottomLeft:
			case CursorLocation.BottomRight:
				if (height < FACE_MIN_SIZE)
					bottom = top + FACE_MIN_SIZE;
				break;
				
			default:
				break;
			}

			Gdk.Rectangle new_offset_box = Gdk.Rectangle.FromLTRB (left, top, right, bottom);
			if (!offset_box.Equals (new_offset_box)) {
				Erase ();
				
				offset_box = new_offset_box;

				Gdk.Point image_lt = view.WindowCoordsToImage (new Gdk.Point(left, top));
				Gdk.Point image_rb = view.WindowCoordsToImage (new Gdk.Point(right, bottom));
				box = Gdk.Rectangle.FromLTRB (image_lt.X, image_lt.Y, image_rb.X, image_rb.Y);

				Paint ();
			}

			if (Editable)
				UpdateFaceWindowPosition ();
			
			serialized = null;
			
			return false;
		}

		private void UpdateCursor (int x, int y)
		{
			Gdk.CursorType cursor_type;
			switch (offset_box.ApproxLocation (x, y)) {
			case CursorLocation.LeftSide:
				cursor_type = Gdk.CursorType.LeftSide;
				break;
				
			case CursorLocation.TopSide:
				cursor_type = Gdk.CursorType.TopSide;
				break;
				
			case CursorLocation.RightSide:
				cursor_type = Gdk.CursorType.RightSide;
				break;
				
			case CursorLocation.BottomSide:
				cursor_type = Gdk.CursorType.BottomSide;
				break;
				
			case CursorLocation.TopLeft:
				cursor_type = Gdk.CursorType.TopLeftCorner;
				break;
				
			case CursorLocation.BottomLeft:
				cursor_type = Gdk.CursorType.BottomLeftCorner;
				break;
				
			case CursorLocation.TopRight:
				cursor_type = Gdk.CursorType.TopRightCorner;
				break;
				
			case CursorLocation.BottomRight:
				cursor_type = Gdk.CursorType.BottomRightCorner;
				break;
				
			case CursorLocation.Inside:
				cursor_type = Gdk.CursorType.Fleur;
				break;
				
			default:
				cursor_type = Gdk.CursorType.LeftPtr;
				break;
			}
			
			if (cursor_type != current_cursor_type) {
				view.GdkWindow.Cursor = new Gdk.Cursor (cursor_type);
				current_cursor_type = cursor_type;
			}
		}
		
		public override void OnMotion (object sender, MotionNotifyEventArgs e)
		{
			// only deal with manipulating the offset_box when click-and-dragging one of the edges
			// or the interior
			if (in_manipulation != CursorLocation.Outside)
				OnViewManipulation (sender, e);

			UpdateCursor ((int) e.Event.X, (int) e.Event.Y);
			
			e.RetVal = true;
		}
		
		public override void OnLeftClick (object sender, ButtonPressEventArgs e)
		{
			last_grab_x = (int) e.Event.X;
			last_grab_y = (int) e.Event.Y;

			in_manipulation = offset_box.ApproxLocation (last_grab_x, last_grab_y);

			e.RetVal = in_manipulation != CursorLocation.Outside;
		}

		public override void OnLeftReleased (object sender, ButtonReleaseEventArgs e)
		{
			if (offset_box.Width < FACE_MIN_SIZE) {
				RaiseDeleteMeRequested ();
				
				return;
			}
			
			if (Editable) {
				face_window.Show ();
				face_window.Present ();
			}
			
			// nothing to do if released outside of the offset_box
			if (in_manipulation == CursorLocation.Outside)
				return;
			
			// end manipulation
			in_manipulation = CursorLocation.Outside;
			last_grab_x = -1;
			last_grab_y = -1;
			
			UpdateCursor ((int) e.Event.X, (int) e.Event.Y);

			e.RetVal = true;
		}

		public override void OnOffsetChanged (object sender, EventArgs e)
		{
			offset_box = view.ImageCoordsToWindow (box);
		}

		public override bool CursorIsOver (int x, int y)
		{
			return offset_box.ApproxLocation (x, y) != CursorLocation.Outside;
		}
		
		public override double GetDistance (int x, int y)
		{
			double center_x = offset_box.Left + offset_box.Width / 2.0;
			double center_y = offset_box.Top + offset_box.Height / 2.0;
			
			return Math.Sqrt ((center_x - x) * (center_x - x) + (center_y - y) * (center_y - y));
		}
	}
}