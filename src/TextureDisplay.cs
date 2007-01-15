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
		float scale = 0.0f;
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
				new Flip (),
				new Split (),
				new TexturePush (),
				new Dissolve (),
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
			scale += amount;
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

		protected override void OnDestroyed ()
		{
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

		public class GlTransition {
			protected float percent;

			public GlTransition ()
			{
			}

			public float Percent {
				get { return percent; }
				set { percent = value; }
			}

			public virtual void Draw (Gdk.Rectangle viewport, Texture start, Texture end)
			{
				throw new ApplicationException ("the world has come undone");
			}
		}

		public class Dissolve : GlTransition
		{
			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{
				Gl.glViewport (0, 0, viewport.Width, viewport.Height);
				Gl.glMatrixMode (Gl.GL_PROJECTION);
				Gl.glLoadIdentity ();
				Glu.gluOrtho2D (0, viewport.Width, 0, viewport.Height);
				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				Gl.glLoadIdentity ();

				
				next.Bind ();

				Gl.glBegin (Gl.GL_QUADS);
				Gl.glTexCoord2f (0, 0);
				Gl.glVertex3f (0, viewport.Height, 0);
				Gl.glTexCoord2f (next.Width, 0);
				Gl.glVertex3f (viewport.Width, viewport.Height, 0);
				Gl.glTexCoord2f (next.Width, next.Height);
				Gl.glVertex3f (viewport.Width, 0, 0);
				Gl.glTexCoord2f (0, next.Height);
				Gl.glVertex3f (0, 0, 1);
				Gl.glEnd ();

			}
		}

		public class Flip : GlTransition
		{
			public Flip ()
			{
			}

			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{
				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				Gl.glLoadIdentity ();

				Glu.gluLookAt (0, 0, 5,
					       0, 0, 0,
					       0, 1, 1);
				
				Gl.glRotatef (90 * -percent, 0, 1, 0);

				Gl.glViewport (0, 0, viewport.Width, viewport.Height);
				
				Gl.glMatrixMode (Gl.GL_PROJECTION);
				Gl.glLoadIdentity ();
				Glu.gluPerspective (60, viewport.Width / (float) viewport.Height, .5, 15);

				next.Bind ();
				Gl.glBegin (Gl.GL_QUADS);
				Gl.glTexCoord2f (0, 0);
				Gl.glVertex3f (-1, 1, 1);
				Gl.glTexCoord2f (next.Width, 0);
				Gl.glVertex3f (1, 1, 1);
				Gl.glTexCoord2f (next.Width, next.Height);
				Gl.glVertex3f (1, -1, 1);
				Gl.glTexCoord2f (0, next.Height);
				Gl.glVertex3f (-1, -1, 1);
				Gl.glEnd ();

				previous.Bind ();				
				Gl.glBegin (Gl.GL_QUADS);
				Gl.glTexCoord2f (0, 0);
				Gl.glVertex3f (1, 1, 1);
				Gl.glTexCoord2f (previous.Width, 0);
				Gl.glVertex3f (1, 1, -1);
				Gl.glTexCoord2f (previous.Width, previous.Height);
				Gl.glVertex3f (1, -1, -1);
				Gl.glTexCoord2f (0, previous.Height);
				Gl.glVertex3f (1, -1, 1);
				Gl.glEnd ();
			}
		}

		public class Reveal : TexturePush
		{
			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{

			}
		}
			
		public class Wipe : GlTransition
		{
			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{

			}
		}

		public class Split : GlTransition
		{
			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{
				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				Gl.glLoadIdentity ();
				
				Glu.gluLookAt (0.0, 0.0, 3.0,
					       0.0, 0.0, 0.0,
					       0.0, 1.0, 0.0);
				
				Gl.glTranslatef (0, 0, -3);
				Gl.glViewport (0, 0, viewport.Width, viewport.Height);
				Gl.glMatrixMode (Gl.GL_PROJECTION);
				Gl.glLoadIdentity ();
				Glu.gluPerspective (40, viewport.Width / (float) viewport.Height, 0.1, 50.0);
				Gl.glPushMatrix ();
				Gl.glTranslatef (0, 0, 0.5f);

				Texture t = previous;
				
				t.Bind ();
				Gl.glBegin (Gl.GL_QUADS);
				Gl.glTexCoord2f (0, 0);
				Gl.glVertex3f (-2, -1, 0);
				Gl.glTexCoord2f (t.Width, 0);
				Gl.glVertex3f (-2, 1, 0);
				Gl.glTexCoord2f (t.Width, t.Height);
				Gl.glVertex3f (0, 1, 0);
				Gl.glTexCoord2f (0, t.Height);
				Gl.glVertex3f (0, -1, 0);
				Gl.glEnd ();
				
				next.Bind ();
				Gl.glBegin (Gl.GL_QUADS);
				Gl.glTexCoord2f (0, 0);
				Gl.glVertex3f (1, -1, 0);
				Gl.glTexCoord2f (next.Width, 0);
				Gl.glVertex3f (1, 1, 0);
				Gl.glTexCoord2f (next.Width, next.Height);
				Gl.glVertex3f (2.41421f, 1, -1.41421f);
				Gl.glTexCoord2f (0, next.Height);
				Gl.glVertex3f (2.41421f, -1, -1.41421f);
				Gl.glEnd ();
			}
		}
		
		public class TexturePush : GlTransition
		{
			public TexturePush ()
			{

			} 
			
			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{
				Gl.glViewport (0, 0, viewport.Width, viewport.Height);
				Gl.glMatrixMode (Gl.GL_PROJECTION);
				Gl.glLoadIdentity ();
				Glu.gluOrtho2D (0, viewport.Width, 0, viewport.Height);
				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				Gl.glLoadIdentity ();

				float scale = Math.Max (viewport.Width / (float) previous.Width,
							 viewport.Height / (float) previous.Height);
			
				float x_offset = (viewport.Width  - previous.Width * scale) / 2.0f;
				float y_offset = (viewport.Height - previous.Height * scale) / 2.0f;

				Gl.glPushMatrix ();
				Gl.glTranslatef (-viewport.Width * percent, 0, 0);
				Gl.glScalef (scale, scale, scale);

				previous.Bind ();
				Gl.glBegin (Gl.GL_QUADS);
				Gl.glTexCoord2f (0, 0);
				Gl.glVertex3f (x_offset, y_offset, 0);
				Gl.glTexCoord2f (previous.Width, 0);
				Gl.glVertex3f (x_offset + previous.Width, y_offset, 0);
				Gl.glTexCoord2f (previous.Width, previous.Height);
				Gl.glVertex3f (x_offset + previous.Width, y_offset + previous.Height, 0);
				Gl.glTexCoord2f (0, previous.Height);
				Gl.glVertex3f (x_offset, y_offset + previous.Height, 0);
				Gl.glEnd ();
				Gl.glPopMatrix ();

				scale = Math.Max (viewport.Width / (float) next.Width,
							 viewport.Height / (float) next.Height);
			
				x_offset = (viewport.Width  - next.Width * scale) / 2.0f;
				y_offset = (viewport.Height - next.Height * scale) / 2.0f;


				Gl.glPushMatrix ();
				Gl.glTranslatef (viewport.Width - viewport.Width * percent, 0, 0);
				Gl.glScalef (scale, scale, scale);

				next.Bind ();
				Gl.glBegin (Gl.GL_QUADS);
				Gl.glTexCoord2f (0, 0);
				Gl.glVertex3f (x_offset, y_offset, 0);
				Gl.glTexCoord2f (next.Width, 0);
				Gl.glVertex3f (x_offset + next.Width, y_offset, 0);
				Gl.glTexCoord2f (next.Width, next.Height);
				Gl.glVertex3f (x_offset + next.Width, y_offset + next.Height, 0);
				Gl.glTexCoord2f (0, next.Height);
				Gl.glVertex3f (x_offset, y_offset + next.Height, 0);
				Gl.glPopMatrix ();
				Gl.glEnd ();
			}
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
			GlTransition transition = transitions [current_transition];

			if (Animator == null)
				transition.Percent = scale;

			transition.Draw (Allocation, Next, Previous);
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

		private void Project () 
		{
			Gl.glViewport (0, 0, Allocation.Width, Allocation.Height);
			Gl.glMatrixMode (Gl.GL_PROJECTION);
			Gl.glLoadIdentity ();
			Glu.gluPerspective (60, Allocation.Width / (float) Allocation.Height, 1, 30);
			Gl.glMatrixMode (Gl.GL_MODELVIEW);
			Gl.glLoadIdentity ();	
			Gl.glTranslatef (0.0f, 0.0f, -3.6f);
		}

		private void Ortho () 
		{
			Gl.glViewport (0, 0, Allocation.Width, Allocation.Height);
			Gl.glMatrixMode (Gl.GL_PROJECTION);
			Gl.glLoadIdentity ();
			Glu.gluOrtho2D (0, Allocation.Width, 0, Allocation.Height);
			Gl.glMatrixMode (Gl.GL_MODELVIEW);
			Gl.glLoadIdentity ();	
		}
	}
}
