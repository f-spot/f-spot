using System;
using Gtk;
using Tao.OpenGl;
using Cairo;
using System.Runtime.InteropServices;
using FSpot;
using FSpot.Widgets;

namespace FSpot {
	[Binding(Gdk.Key.Up, "Spin", 1)]
	[Binding(Gdk.Key.Down, "Spin", -1)]
	[Binding(Gdk.Key.Left, "Scale", .05f)]
	[Binding(Gdk.Key.Right, "Scale", -.05f)]

	public class TextureDisplay : Gtk.DrawingArea {
		BrowsablePointer item;
		GdkGlx.Context glx;
		float angle = 0.0f;
		Animator flip;

		public TextureDisplay (BrowsablePointer item)
		{
			this.item = item;
			DoubleBuffered = false;
			//AppPaintable = true;
			CanFocus = true;

			item.Changed += HandleItemChanged;
			flip = new Animator (4000, 4000, delegate { flip.Start (); item.MoveNext (true); }); 
			flip.RunWhenStarted = false;
		}

		GlTransition [] transitions = new GlTransition []
			{
				new GlTransition.Dissolve (),
				new GlTransition.Flip (),
				new GlTransition.Push (),
				new GlTransition.Reveal (),
				new GlTransition.Cover ()
			};
		int current_transition = 0;

		public GlTransition Transition {
			get { return transitions [current_transition]; }
		}

		public bool Spin (int amount)
		{
			current_transition += amount;

			if (current_transition >= transitions.Length)
				current_transition = 0;

			if (current_transition < 0)
				current_transition = transitions.Length -1;

			QueueDraw ();
			return true;
		}

		public bool Scale (float amount)
		{
			if (Transition != null)
				Transition.Percent += amount;

			QueueDraw ();
			return true;
		}
		
		private Texture CreateTexture ()
		{
			ImageFile img = ImageFile.Create (item.Current.DefaultVersionUri);
			Gdk.Pixbuf pixbuf = img.Load ();
			Texture tex = new Texture (pixbuf);
			pixbuf.Dispose ();
			return tex;
		}

		Animator animator;
		private void HandleItemChanged (BrowsablePointer p, BrowsablePointerChangedArgs args)
		{
			Console.WriteLine ("Begin previous = {0} texture = {1}", 
					   previous != null ? previous.Id.ToString () : "null", 
					   next != null ? next.Id.ToString () : "null");

			Next = null;

			Console.WriteLine ("End previous = {0} texture = {1}", 
					   previous != null ? previous.Id.ToString () : "null", 
					   next != null ? next.Id.ToString () : "null");

			Animator = new Animator (2000, 20, HandleTick);
			Animator.Start ();
		}

		private Animator Animator {
			get { return animator; }
			set {
				if (animator != null)
					animator.Stop ();

				if (value == null)
					flip.Start ();
				else 
					flip.Stop ();

				animator = value;
			}
		}

		public void HandleTick (object sender, EventArgs args)
		{
			if (Animator.Percent >= 1.0) {
				Transition.Percent = 1.0f;
				Animator = null;
			} else {
				Transition.Percent = Animator.Percent;
			}

			QueueDraw (); 
			GdkWindow.ProcessUpdates (false);
		}

		protected override void OnRealized ()
		{
#if FALSE
			if (CompositeUtils.IsComposited (Screen) && CompositeUtils.SetRgbaColormap (this))
				Console.WriteLine ("Set Rba");
#endif

			base.OnRealized ();

			if (glx != null)
				glx.Destroy ();
			
			int [] attr = new int [] {
				(int) GdkGlx.GlxAttribute.Rgba,
				(int) GdkGlx.GlxAttribute.DepthSize, 16,
				(int) GdkGlx.GlxAttribute.DoubleBuffer,
				(int) GdkGlx.GlxAttribute.None
			};
			
			glx = new GdkGlx.Context (GdkWindow, attr);
			flip.Start ();
		}
		
		protected override void OnUnrealized ()
		{
			Animator = null;
			flip.Stop ();

			base.OnUnrealized ();

			if (glx != null)
				glx.Destroy ();
			
			glx = null;
		}


		Texture previous;
		public Texture Previous {
			get {
				if (previous == null)
					previous = Next;
				
				return previous;
			}
			set {
				if (previous != next)
					previous.Dispose ();

				previous = value;
			}
		}

		Texture next;
		public Texture Next {
			get {
				if (next == null)
					next = CreateTexture ();

				return next;
			}
			set {
				Texture tmp = next;
				next = value;
				Previous = tmp;
			}
		}

		GlTransition transition;
		private void DrawTransition ()
		{
			Transition.Draw (Allocation, Next, Previous);
		}

		private void DrawPixels ()
		{
			Gl.glClear (Gl.GL_COLOR_BUFFER_BIT);
			Gl.glPixelStorei (Gl.GL_UNPACK_ALIGNMENT, 1);
			Gl.glRasterPos2i (0, Allocation.Height);
			Gl.glPixelZoom (Allocation.Width / (float) next.Width, 
					- Allocation.Height / (float) next.Height);
			/*
			Gl.glDrawPixels (texture.Width, texture.Height, 
					 Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, 
					 texture.Pixels);
			*/
		}


		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			QueueDraw ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			glx.MakeCurrent ();
			Gl.glShadeModel(Gl.GL_FLAT);
			
			//Gl.glMatrixMode(Gl.GL_MODELVIEW);
			//Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
			Gl.glColor3f(1.0f, 1.0f, 1.0f);
			
			Gl.glEnable (Gl.GL_DEPTH_TEST);
			Gl.glEnable (Gl.GL_NORMALIZE);
			//Gl.glEnable (Gl.GL_BLEND);
			//Gl.glBlendFunc (Gl.GL_ONE, Gl.GL_ONE);
			Gl.glShadeModel (Gl.GL_FLAT);
			Gl.glEnable (Gl.GL_TEXTURE_RECTANGLE_ARB);
			//Gl.glPolygonMode (Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
			//Gl.glDisable (Gl.GL_CULL_FACE);
			//Gl.glLightModelf (Gl.GL_LIGHT_MODEL_TWO_SIDE, 0);
			//Gl.glEnable (Gl.GL_LIGHTING);
			//Gl.glEnable (Gl.GL_LIGHT0);
			//Gl.glLightfv (Gl.GL_LIGHT0, Gl.GL_DIFFUSE, new float [] { 1.0f, 1.0f, 0.0f, 1.0f });
			
			//Gl.glTexEnvi (Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
			//Gl.glTexParameteri (Gl.GL_TEXTURE_RECTANGLE_ARB, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
			//Gl.glTexParameteri (Gl.GL_TEXTURE_RECTANGLE_ARB, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
			
			// Viewing transformation
			Gl.glClear (Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
			//Gl.glMaterialfv (Gl.GL_FRONT, Gl.GL_DIFFUSE, new float [] { 1f, 1f, 1f, 1f});
			//Gl.glMaterialfv (Gl.GL_BACK, Gl.GL_DIFFUSE, new float [] { 1f, 1f, 1f, 1f});

			//Gl.glRotatef (35, 0, 1, 0);
			
			DrawTransition ();
			//DrawTextureSplit ();
			
			Gl.glFlush ();
			glx.SwapBuffers ();
			
			return true;
		}
	}
}
