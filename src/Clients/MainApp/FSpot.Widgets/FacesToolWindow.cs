//
// FacesToolWindow.cs
//
// TODO: Add authors and license.
//

using System;

using Gdk;

using Gtk;

using Mono.Unix;

using FSpot;

namespace FSpot.Widgets
{
	public class FaceEditionEventArgs : EventArgs
	{
		public string FaceName { get; set; }
	}

	public enum EditingPhase {
		ClickToEdit,
		NotEditing,
		CreatingDragging,
		CreatingEditing,
		Editing,
		DetectingFaces,
		DetectingFacesFinished
	}

	public class FacesToolWindow : Gtk.VBox
	{
		public event EventHandler FaceHidden;
		public event EventHandler<FaceEditionEventArgs> FaceEditRequested;
		public event EventHandler<FaceEditionEventArgs> FaceDeleteRequested;
		public event EventHandler DetectionCanceled;
		
		public Gtk.Button DetectionButton;
		public Gtk.Button OkButton;
		public Gtk.Button CancelButton;
		public Gtk.Button CancelDetectionButton;

		private Gtk.HBox help_layout = null;
		private Gtk.HBox response_layout = null;
		private Gtk.HSeparator buttons_text_separator = null;
		private Gtk.Label help_text = null;
		private Gtk.VBox face_widgets_layout = null;

		private EditingPhase editing_phase = EditingPhase.NotEditing;
		public EditingPhase CurrentEditingPhase {
			get {
				return editing_phase;
			}
		}

		public FacesToolWindow ()
			: base (false, FacesTool.CONTROL_SPACING)
		{
			BorderWidth = FacesTool.CONTROL_SPACING;

			DetectionButton = Gtk.Button.NewWithLabel (Catalog.GetString ("Detect faces"));
			DetectionButton.TooltipText = Catalog.GetString ("Detect faces on this photo");

			CancelDetectionButton = new Gtk.Button (Gtk.Stock.Cancel);
			CancelDetectionButton.TooltipText = Catalog.GetString ("Cancel face detection");
			CancelDetectionButton.ImagePosition = Gtk.PositionType.Left;
			CancelDetectionButton.Clicked += OnCancelDetection;

			CancelButton = new Gtk.Button (Gtk.Stock.Cancel);
			CancelButton.TooltipText = Catalog.GetString ("Close the Faces tool without saving changes");
			CancelButton.ImagePosition = Gtk.PositionType.Left;

			OkButton = new Gtk.Button (Gtk.Stock.Ok);
			OkButton.ImagePosition = Gtk.PositionType.Left;

			face_widgets_layout = new Gtk.VBox (false, FacesTool.CONTROL_SPACING);
			
			help_text = new Gtk.Label (Catalog.GetString ("Click and drag to tag a face"));
			help_layout = new Gtk.HBox (false, FacesTool.CONTROL_SPACING);
			help_layout.PackStart (help_text, true, true, 0);
			
			response_layout = new Gtk.HBox (false, FacesTool.CONTROL_SPACING);
			response_layout.Add (DetectionButton);
			response_layout.Add (CancelButton);
			response_layout.Add (OkButton);

			PackStart (face_widgets_layout, false, false, 0);
			PackStart (help_layout, false, false, 0);
			PackStart (new Gtk.HSeparator (), false, false, 0);
			PackStart (response_layout, false, false, 0);

			ShowAll ();

			// TODO: Implement face detection.
			DetectionButton.Hide ();
		}

		public void UpdateCurrentEditingPhase (EditingPhase new_phase, FaceShape face_shape = null)
		{
			if (editing_phase == EditingPhase.DetectingFaces && new_phase != EditingPhase.DetectingFacesFinished)
				return;
			
			switch (new_phase) {
			case EditingPhase.ClickToEdit:
				help_text.Markup = String.Format (
					Catalog.GetString ("Click to edit face <i>{0}</i>"),
					face_shape.Name);

				break;
			case EditingPhase.NotEditing:
				help_text.Text = Catalog.GetString ("Click and drag to tag a face");
				break;
			case EditingPhase.CreatingDragging:
				help_text.Text = Catalog.GetString ("Stop dragging to add your face and name it.");
				break;
			case EditingPhase.CreatingEditing:
				help_text.Text = Catalog.GetString ("Type a name for this face, then press Enter");
				break;
			case EditingPhase.Editing:
				help_text.Text = Catalog.GetString ("Move or modify the face shape or name and press Enter");
				break;
			case EditingPhase.DetectingFaces:
				help_text.Text = Catalog.GetString ("Detecting faces...");

				if (CancelDetectionButton.Parent == null)
					help_layout.PackStart (CancelDetectionButton, false, false, 0);

				DetectionButton.Sensitive = false;
				CancelDetectionButton.Sensitive = true;
				CancelDetectionButton.Show ();

				break;
			case EditingPhase.DetectingFacesFinished:
				help_text.Text = Catalog.GetString ("If you don't set the name of unknown faces they won't be saved.");
				break;
			}
			
			if (editing_phase == EditingPhase.DetectingFaces && editing_phase != new_phase) {
				CancelDetectionButton.Hide ();
				DetectionButton.Sensitive = true;
			}
			
			editing_phase = new_phase;
		}

		public void UpdateOkButtonSensitiveness (bool value)
		{
			if (value)
				OkButton.TooltipText = Catalog.GetString ("Save changes and close the Faces tool");
			else
				OkButton.TooltipText = Catalog.GetString ("No changes to save");

			OkButton.Sensitive = value;
		}
		
		public void AddFace (FaceShape face_shape)
		{
			FaceWidget face_widget = new FaceWidget (face_shape);
			face_widget.FaceHidden += OnFaceHidden;
			face_widget.EditButton.Clicked += OnEditFaceRequest;
			face_widget.DeleteButton.Clicked += OnDeleteFaceRequest;

			Gtk.EventBox event_box = new Gtk.EventBox ();
			event_box.Add (face_widget);
			event_box.AddEvents ((int) (Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask));
			event_box.EnterNotifyEvent += face_widget.OnEnterNotifyEvent;
			event_box.LeaveNotifyEvent += face_widget.OnLeaveNotifyEvent;

			face_widgets_layout.PackStart (event_box, false, false, 0);
			
			if (buttons_text_separator == null) {
				buttons_text_separator = new Gtk.HSeparator ();
				face_widgets_layout.PackEnd (buttons_text_separator, false, false, 0);
			}
			
			face_widgets_layout.ShowAll ();
		}
		
		private void OnEditFaceRequest (object sender, EventArgs e)
		{
			FaceWidget widget = (FaceWidget) ((Gtk.Button) sender).Parent;

			EventHandler<FaceEditionEventArgs> handler = FaceEditRequested;
			if (handler != null) {
				FaceEditionEventArgs args = new FaceEditionEventArgs ();
				args.FaceName = widget.Label.Text;

				handler (this, args);
			}
		}
		
		private void OnDeleteFaceRequest (object sender, EventArgs e)
		{
			FaceWidget widget = (FaceWidget) ((Gtk.Button) sender).Parent;
			
			EventHandler<FaceEditionEventArgs> handler = FaceDeleteRequested;
			if (handler != null) {
				FaceEditionEventArgs args = new FaceEditionEventArgs ();
				args.FaceName = widget.Label.Text;
				
				handler (this, args);
			}

			widget.Parent.Destroy ();

			if (face_widgets_layout.Children.Length == 1) {
				buttons_text_separator.Destroy ();
				buttons_text_separator = null;
			}
		}
		
		private void OnFaceHidden (object sender, EventArgs e)
		{
			EventHandler handler = FaceHidden;
			if (handler != null)
				handler (this, EventArgs.Empty);
		}
		
		private void OnCancelDetection (object sender, EventArgs e)
		{
			EventHandler handler = DetectionCanceled;
			if (handler != null)
				handler (this, EventArgs.Empty);
		}
	}
}