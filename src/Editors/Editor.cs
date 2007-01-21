
using System;
using Gtk;
using Cairo;
using Tao.OpenGl;

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
			if (view == null)
				return;

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

		public virtual void Close ()
		{
			if (controls != null)
				controls.Destroy ();

			SetView (null);
		}
	}

	public class GlEditor : Editor {
		protected GlTransition transition;
		protected Scale scale;
		protected Texture texture;

		public GlEditor (PhotoImageView view) : base (view)
		{
			transition = new GlTransition.Flip ();
		}

		protected override Widget CreateControls ()
		{
			scale = new HScale (0, 1, 0.01);
			scale.ValueChanged += HandleValueChanged;
			scale.WidthRequest = 250;

			return scale;
		}

		protected override void SetView (PhotoImageView value)
		{
			if (view != null)
				view.ExposeEvent -= ExposeEvent;

			base.SetView (value);

			view.ExposeEvent += ExposeEvent;
			view.QueueDraw ();
		}

		[GLib.ConnectBefore]
		public virtual void ExposeEvent (object sender, ExposeEventArgs args)
		{
			view.Glx.MakeCurrent (view.GdkWindow);

			if (texture == null)
				texture = new Texture (view.CompletePixbuf ());

			Gl.glShadeModel(Gl.GL_FLAT);

			Gl.glColor3f(1.0f, 1.0f, 1.0f);
			
			Gl.glEnable (Gl.GL_DEPTH_TEST);
			Gl.glEnable (Gl.GL_NORMALIZE);
			Gl.glShadeModel (Gl.GL_FLAT);
			Gl.glEnable (Gl.GL_TEXTURE_RECTANGLE_ARB);
			Gl.glClear (Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

			

			transition.Draw (view.Allocation, texture, texture);
			
			view.Glx.SwapBuffers (view.GdkWindow);
			args.RetVal = true;
		}

		private void HandleValueChanged (object sender, System.EventArgs args)
		{
			transition.Percent = (float) scale.Value;
			view.QueueDraw ();
		}

		public override void Close ()
		{
			if (texture != null && view != null && view.Glx != null && view.GdkWindow != null) {
				view.Glx.MakeCurrent (view.GdkWindow);
				texture.Dispose ();
			}
			
			base.Close ();
		}
	}

	public class EffectEditor : Editor {
		protected IEffect effect;
		protected Widgets.ImageInfo info;

		public EffectEditor (PhotoImageView view) : base (view)
		{
		}
		
		protected override void SetView (PhotoImageView value)
		{
			if (view != null) {
				view.ExposeEvent -= ExposeEvent;
				view.QueueDraw ();
			}

			base.SetView (value);

			if (view == null)
				return;

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

		public override void Close ()
		{
			base.Close ();

			if (effect != null)
				effect.Dispose ();
			effect = null;
			
			if (info != null)
				info.Dispose ();
			info = null;
			
		}
	}
}

