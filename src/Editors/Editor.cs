
using System;
using Gtk;
using Cairo;

namespace FSpot.Editors {
	public abstract class Editor {
		protected PhotoImageView view;
		protected Gtk.Window controls;

		public Editor (PhotoImageView view)
		{
			SetView (view);
		}

		protected virtual void SetView (PhotoImageView view)
		{
			if (controls != null)
				controls.Destroy ();

			controls = null;

			this.view = view;
			
			Widget w = CreateControls ();

			if (w != null) {
#if false
				ControlOverlay c = new ControlOverlay (view);
				c.AutoHide = false;
				w.ShowAll ();
				c.Add (w);
				c.Visibility = ControlOverlay.VisibilityType.Full;
				controls = c;
#else
				Window win = new Window (String.Format ("{0}", this));
				win.TransientFor = (Gtk.Window) view.Toplevel;
				win.Add (w);
				win.ShowAll ();
				controls = win;
#endif
			}
			
		}

		protected virtual Widget CreateControls ()
		{
			return null;
		}
	}

	public class EffectEditor : Editor {
		protected IEffect effect;
		protected Widgets.ImageInfo info;

		public EffectEditor (PhotoImageView view) : base (view)
		{
		}
		
		protected override void SetView (PhotoImageView view)
		{
			if (view != null)
				view.ExposeEvent -= ExposeEvent;

			base.SetView (view);

			info = new Widgets.ImageInfo (view.CompletePixbuf ());
			view.ExposeEvent += ExposeEvent;
			view.QueueDraw ();
		}

		[GLib.ConnectBefore]
		public virtual void ExposeEvent (object sender, ExposeEventArgs args)
		{
			Context ctx = Widgets.CairoUtils.CreateContext (view.GdkWindow);
			effect.OnExpose (ctx, view.Allocation);
			
			args.RetVal = true;
		}
	}
}

