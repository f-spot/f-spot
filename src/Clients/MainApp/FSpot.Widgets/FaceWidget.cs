//
// FaceWidget.cs
//
// TODO: Add authors and license.
//

using System;

using Gdk;

using Gtk;

using Pango;

using FSpot.Core;

namespace FSpot.Widgets
{
	public class FaceWidget : HBox {
		private static AttrList attrs_bold;
		private static AttrList attrs_normal;
		
		public event EventHandler FaceHidden;
		
		public Button EditButton;
		public Button DeleteButton;
		public Label Label;
		
		public WeakReference FaceShapeRef;
		
		static FaceWidget ()
		{
			attrs_bold = new AttrList ();
			attrs_bold.Insert (new AttrWeight (Weight.Bold));
			attrs_normal = new AttrList ();
			attrs_normal.Insert (new AttrWeight (Weight.Normal));
		}
		
		public FaceWidget (FaceShape face_shape)
			: base (false, FacesTool.CONTROL_SPACING)
		{
			EditButton = new Button (Stock.Edit);
			DeleteButton = new Button (Stock.Delete);
			
			Label = new Label (face_shape.Name);
			Label.SetAlignment (0f, 0.5f);
			Label.Ellipsize = EllipsizeMode.End;

			PackStart (Label, true, true, 0);
			PackStart (EditButton, false, false, 0);
			PackStart (DeleteButton, false, false, 0);
			
			FaceShapeRef = new WeakReference (face_shape);
			face_shape.Widget = this;
		}
		
		public void OnEnterNotifyEvent (object sender, EnterNotifyEventArgs e)
		{
			ActivateLabel ();

			FaceShape face_shape = FaceShapeRef.Target as FaceShape;
			if (face_shape == null || face_shape.Editable)
				return;

			if (!face_shape.Visible)
				face_shape.Show ();
			
			e.RetVal = true;
		}
		
		public void OnLeaveNotifyEvent (object sender, LeaveNotifyEventArgs e)
		{
			DeactivateLabel ();

			FaceShape face_shape = FaceShapeRef.Target as FaceShape;
			if (face_shape == null || face_shape.Editable)
				return;
			
			face_shape.Hide ();

			EventHandler handler = FaceHidden;
			if (handler != null)
				handler (this, EventArgs.Empty);
			
			e.RetVal = true;
		}
		
		public void ActivateLabel ()
		{
			Label.Attributes = attrs_bold;
		}
		
		public void DeactivateLabel ()
		{
			Label.Attributes = attrs_normal;
		}
	}
}