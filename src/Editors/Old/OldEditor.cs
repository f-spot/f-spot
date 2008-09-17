
using System;
using Gtk;
using Cairo;
using Tao.OpenGl;
using FSpot.Utils;

namespace FSpot.Editors {
	public abstract class OldEditor {
		protected PhotoImageView view;
		protected Gtk.Window controls;

		public event EventHandler Done;

		public OldEditor (PhotoImageView view)
		{
			SetView (view);
		}

		protected virtual string GetTitle ()
		{
			return String.Empty;
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
				Window win = new Window (String.Format ("{0}", GetTitle ()));
				win.TransientFor = (Gtk.Window) view.Toplevel;
				win.Add (w);
				win.ShowAll ();
				win.DeleteEvent += delegate { Destroy (); };
				controls = win;
#endif
			}

		}

		protected virtual Widget CreateControls ()
		{
			return null;
		}

		public void Destroy ()
		{
			Close ();
		}

		protected virtual void Close ()
		{

			if (controls != null)
				controls.Destroy ();

			if (Done != null)
				Done (this, EventArgs.Empty);

			SetView (null);
		}
	}

	public class GlEditor : OldEditor {
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
			if (view != null) {
				view.ExposeEvent -= ExposeEvent;
				view.QueueDraw ();
			}

			base.SetView (value);

			if (value == null)
				return;

			view.ExposeEvent += ExposeEvent;
			view.QueueDraw ();
		}

		[GLib.ConnectBefore]
		public virtual void ExposeEvent (object sender, ExposeEventArgs args)
		{
			view.Glx.MakeCurrent (view.GdkWindow);
			Gl.glEnable (Gl.GL_CONVOLUTION_2D);
			Gdk.Color c = view.Style.Background (view.State);
			Gl.glClearColor (c.Red / (float) ushort.MaxValue,
					 c.Blue / (float) ushort.MaxValue,
					 c.Green / (float) ushort.MaxValue,
					 1.0f);

			if (texture == null) {
				float [] kernel = new float [] { .25f, .5f, .25f,
								 .5f, 1f, .5f,
								 .25f, .5f, .25f};

#if false
				bool supported = GlExtensionLoader.LoadExtension ("GL_ARB_imaging");
				if (!supported) {
					System.Console.WriteLine ("GL_ARB_imaging not supported");
					return;
				}
#else
				GlExtensionLoader.LoadAllExtensions ();
#endif

				Gl.glConvolutionParameteri (Gl.GL_CONVOLUTION_2D,
							    Gl.GL_CONVOLUTION_BORDER_MODE,
							    Gl.GL_REPLICATE_BORDER);

				Gl.glConvolutionFilter2D (Gl.GL_CONVOLUTION_2D,
							  Gl.GL_INTENSITY,
							  3,
							  3,
							  Gl.GL_INTENSITY,
							  Gl.GL_FLOAT,
							  kernel);

				texture = new Texture (view.CompletePixbuf ());
			}

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
			Gl.glDisable (Gl.GL_CONVOLUTION_2D);
		}

		private void HandleValueChanged (object sender, System.EventArgs args)
		{
			transition.Percent = (float) scale.Value;
			view.QueueDraw ();
		}

		protected override void Close ()
		{
			if (texture != null && view != null && view.Glx != null && view.GdkWindow != null) {
				view.Glx.MakeCurrent (view.GdkWindow);
				texture.Dispose ();
			}

			base.Close ();
		}
	}

	public class EffectEditor : OldEditor {
		protected IEffect effect;
		protected Widgets.ImageInfo info;
		bool double_buffer;

		public EffectEditor (PhotoImageView view) : base (view)
		{
		}

		protected override void SetView (PhotoImageView value)
		{
			if (view != null) {
				view.ExposeEvent -= ExposeEvent;
				view.QueueDraw ();
				view.DoubleBuffered = double_buffer;
			}

			base.SetView (value);

			if (view == null)
				return;

			info = new Widgets.ImageInfo (view.CompletePixbuf ());

			double_buffer = (view.WidgetFlags & WidgetFlags.DoubleBuffered) == WidgetFlags.DoubleBuffered;
			view.DoubleBuffered = true;
			view.ExposeEvent += ExposeEvent;
			view.QueueDraw ();
		}

		[GLib.ConnectBefore]
		public virtual void ExposeEvent (object sender, ExposeEventArgs args)
		{
			Context ctx = Gdk.CairoHelper.Create (view.GdkWindow);
			Gdk.Color c = view.Style.Background (view.State);
			ctx.Source = new SolidPattern (c.Red / (float) ushort.MaxValue,
						       c.Blue / (float) ushort.MaxValue,
						       c.Green / (float) ushort.MaxValue);

			ctx.Paint ();

			effect.OnExpose (ctx, view.Allocation);

			args.RetVal = true;
		}

		protected override void Close ()
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
