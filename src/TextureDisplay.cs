using System;
using Gtk;
using Tao.OpenGl;
using Cairo;
using System.Runtime.InteropServices;
using FSpot;
using FSpot.Widgets;

namespace FSpot {
	[Binding(Gdk.Key.Up, "Spin", 5)]
	[Binding(Gdk.Key.Down, "Spin", -5)]
	[Binding(Gdk.Key.Left, "Scale", .1f)]
	[Binding(Gdk.Key.Right, "Scale", -.1f)]

	public class TextureDisplay : Gtk.DrawingArea {
		Delay delay;
		Texture texture;
		BrowsablePointer item;
		GdkGlx.Context glx;
		float scale = 1.0f;
		float angle = 0.0f;

		public TextureDisplay (BrowsablePointer item)
		{
			this.item = item;
			DoubleBuffered = false;
			//AppPaintable = true;
			CanFocus = true;

			delay = new Delay (50, delegate { 
				scale *= .95f;
				QueueDraw (); 
				return true;
			});

			item.Changed += HandleItemChanged;
		}

		public bool Spin (int amount)
		{
			//delay.Start ();
			//Console.WriteLine ("Up");
			angle += amount;
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

		private void HandleItemChanged (BrowsablePointer p, BrowsablePointerChangedArgs args)
		{
			if (texture != null)
				texture.Dispose ();
			
			texture = null;
			QueueDraw ();
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
		}
		
		protected override void OnUnrealized ()
		{
			delay.Stop ();
			base.OnUnrealized ();

			if (glx != null)
				glx.Destroy ();
			
			glx = null;
		}

		private void DrawZoom ()
		{
			Ortho ();
			
		}

		private void DrawCube ()
		{

			Gl.glMatrixMode (Gl.GL_MODELVIEW);
			Gl.glLoadIdentity ();

			Glu.gluLookAt (0, 0, 5,
				       0, 0, 0,
				       0, 1, 1);

			Gl.glRotatef (angle, 0, 1, 0);
			Gl.glScalef (scale, scale, scale);

			Gl.glViewport (0, 0, Allocation.Width, Allocation.Height);

			Gl.glMatrixMode (Gl.GL_PROJECTION);
			Gl.glLoadIdentity ();
			Glu.gluPerspective (60, Allocation.Width / (float) Allocation.Height, .5, 15);
			
			if (texture == null)
				texture = CreateTexture ();
			
			float aspect = texture.Width / texture.Height;

			Gl.glBindTexture (Gl.GL_TEXTURE_RECTANGLE_ARB, texture.Flush ());
			Gl.glBegin (Gl.GL_QUADS);
			Gl.glTexCoord2f (0, 0);
			Gl.glVertex3f (-1, 1, 1);
			Gl.glTexCoord2f (texture.Width, 0);
			Gl.glVertex3f (1, 1, 1);
			Gl.glTexCoord2f (texture.Width, texture.Height);
			Gl.glVertex3f (1, -1, 1);
			Gl.glTexCoord2f (0, texture.Height);
			Gl.glVertex3f (-1, -1, 1);

			Gl.glTexCoord2f (0, 0);
			Gl.glVertex3f (1, 1, 1);
			Gl.glTexCoord2f (texture.Width, 0);
			Gl.glVertex3f (1, 1, -1);
			Gl.glTexCoord2f (texture.Width, texture.Height);
			Gl.glVertex3f (1, -1, -1);
			Gl.glTexCoord2f (0, texture.Height);
			Gl.glVertex3f (1, -1, 1);
			Gl.glEnd ();
		}

		private void DrawTextureSplit ()
		{
			Gl.glMatrixMode (Gl.GL_MODELVIEW);
			Gl.glLoadIdentity ();
			
			Glu.gluLookAt (0.0, 0.0, 3.0,
				       0.0, 0.0, 0.0,
				       0.0, 1.0, 0.0);
		       
			Gl.glTranslatef (0, 0, -3);
			Gl.glViewport (0, 0, Allocation.Width, Allocation.Height);
			Gl.glMatrixMode (Gl.GL_PROJECTION);
			Gl.glLoadIdentity ();
			Glu.gluPerspective (40, Allocation.Width / (float) Allocation.Height, 0.1, 50.0);
			Gl.glPushMatrix ();
			Gl.glTranslatef (0, 0, 0.5f);

			if (texture == null)
				texture = CreateTexture ();

			Gl.glBindTexture (Gl.GL_TEXTURE_RECTANGLE_ARB, texture.Flush ());
			Gl.glBegin (Gl.GL_QUADS);
			Gl.glTexCoord2f (0, 0);
			Gl.glVertex3f (-2, -1, 0);
			Gl.glTexCoord2f (texture.Width, 0);
			Gl.glVertex3f (-2, 1, 0);
			Gl.glTexCoord2f (texture.Width, texture.Height);
			Gl.glVertex3f (0, 1, 0);
			Gl.glTexCoord2f (0, texture.Height);
			Gl.glVertex3f (0, -1, 0);
			
			Gl.glTexCoord2f (0, 0);
			Gl.glVertex3f (1, -1, 0);
			Gl.glTexCoord2f (texture.Width, 0);
			Gl.glVertex3f (1, 1, 0);
			Gl.glTexCoord2f (texture.Width, texture.Height);
			Gl.glVertex3f (2.41421f, 1, -1.41421f);
			Gl.glTexCoord2f (0, texture.Height);
			Gl.glVertex3f (2.41421f, -1, -1.41421f);
			Gl.glEnd ();
		}

		private void DrawPixels ()
		{
			if (texture == null)
				texture = CreateTexture ();

			Gl.glClear (Gl.GL_COLOR_BUFFER_BIT);
			Gl.glPixelStorei (Gl.GL_UNPACK_ALIGNMENT, 1);
			Gl.glRasterPos2i (0, Allocation.Height);
			Gl.glPixelZoom (Allocation.Width / (float) texture.Width, 
					- Allocation.Height / (float) texture.Height);
			Gl.glDrawPixels (texture.Width, texture.Height, 
					 Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, 
					 texture.Pixels);
		}


		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			QueueDraw ();
		}
#if true
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
			
			DrawCube ();
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
