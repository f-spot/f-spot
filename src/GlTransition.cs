using System;
using Tao.OpenGl;
using Cairo;
using System.Runtime.InteropServices;

namespace FSpot {
	public abstract class GlTransition {
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
	
		public class Dissolve : GlTransition
		{
			float [] color = new float [] { 0, 0, 0, 0};

			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{
				int units;

				for (int i = 0; i < color.Length; i++)
					color [i] = percent;

				bool supported = GlExtensionLoader.LoadExtension ("GL_ARB_multitexture");
				System.Console.WriteLine ("MultiTexture supported = {0}", supported);
				if (!supported) {
					System.Console.WriteLine ("GL_ARB_multitexture not supported");
					return;
				}

				next.Bind ();
				previous.Bind ();

				Gl.glViewport (0, 0, viewport.Width, viewport.Height);
				Gl.glMatrixMode (Gl.GL_PROJECTION);
				Gl.glLoadIdentity ();
				Glu.gluOrtho2D (0, viewport.Width, 0, viewport.Height);
				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				Gl.glLoadIdentity ();

				Gl.glActiveTextureARB (Gl.GL_TEXTURE0);
				Gl.glEnable (Gl.GL_TEXTURE_RECTANGLE_ARB);

				next.Bind ();
				Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE);
				Fit (viewport, next);
   
				Gl.glActiveTextureARB (Gl.GL_TEXTURE1);
				Gl.glEnable (Gl.GL_TEXTURE_RECTANGLE_ARB);

				previous.Bind ();
				Fit (viewport, previous);
				Gl.glTexEnvfv (Gl.GL_TEXTURE_ENV, 
					       Gl.GL_TEXTURE_ENV_COLOR, 
					       color);

				Gl.glTexEnvi (Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_COMBINE);
				Gl.glTexEnvf (Gl.GL_TEXTURE_ENV, Gl.GL_COMBINE_RGB, Gl.GL_INTERPOLATE);

				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				
				Gl.glBegin (Gl.GL_QUADS);
				Gl.glMultiTexCoord2fARB (Gl.GL_TEXTURE0, 0, 0);
				Gl.glMultiTexCoord2fARB (Gl.GL_TEXTURE1, 0, 0);
				Gl.glVertex3f (0, viewport.Height, 0);
				Gl.glMultiTexCoord2fARB (Gl.GL_TEXTURE0, next.Width, 0);
				Gl.glMultiTexCoord2fARB (Gl.GL_TEXTURE1, previous.Width, 0);
				Gl.glVertex3f (viewport.Width, viewport.Height, 0);
				Gl.glMultiTexCoord2fARB (Gl.GL_TEXTURE0, next.Width, next.Height);
				Gl.glMultiTexCoord2fARB (Gl.GL_TEXTURE1, previous.Width, previous.Height);
				Gl.glVertex3f (viewport.Width, 0, 0);
				Gl.glMultiTexCoord2fARB (Gl.GL_TEXTURE0, 0, next.Height);
				Gl.glMultiTexCoord2fARB (Gl.GL_TEXTURE1, 0, previous.Height);
				Gl.glVertex3f (0, 0, 0);
				Gl.glEnd ();

				Gl.glDisable (Gl.GL_TEXTURE_RECTANGLE_ARB);
				Gl.glActiveTextureARB (Gl.GL_TEXTURE0);
			}
		}

		public class Flip : GlTransition
		{
			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{
				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				Gl.glLoadIdentity ();

				Glu.gluLookAt (0, 0, 3,
					       0, 0, 0,
					       0, 1, 1);
				
				Gl.glRotatef (90 * -percent, 0, 1, 0);

				Gl.glViewport (0, 0, viewport.Width, viewport.Height);
				
				Gl.glMatrixMode (Gl.GL_PROJECTION);
				Gl.glLoadIdentity ();
				Glu.gluPerspective (60, viewport.Width / (float) viewport.Height, .5, 15);
				
				next.Bind ();
				Fit (new Gdk.Rectangle (0, 0, 1, 1), next);
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
				Fit (new Gdk.Rectangle (0, 0, 1, 1), previous);
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

		public class Cover : Push
		{
			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{
				Gl.glViewport (0, 0, viewport.Width, viewport.Height);
				Gl.glMatrixMode (Gl.GL_PROJECTION);
				Gl.glLoadIdentity ();
				Glu.gluOrtho2D (0, viewport.Width, 0, viewport.Height);
				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				Gl.glLoadIdentity ();

				Gl.glPushMatrix ();
				Gl.glTranslatef (viewport.Width - viewport.Width * percent, 0, 0);
				RenderPlane (viewport, previous);				
				Gl.glPopMatrix ();

				Gl.glPushMatrix ();
				RenderPlane (viewport, next);
				Gl.glPopMatrix ();
			}
		}

		public class Reveal : Push
		{
			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{
				Gl.glViewport (0, 0, viewport.Width, viewport.Height);
				Gl.glMatrixMode (Gl.GL_PROJECTION);
				Gl.glLoadIdentity ();
				Glu.gluOrtho2D (0, viewport.Width, 0, viewport.Height);
				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				Gl.glLoadIdentity ();

				Gl.glPushMatrix ();				
				Gl.glTranslatef (- viewport.Width * percent, 0, 0);
				RenderPlane (viewport, next);
				Gl.glPopMatrix ();

				Gl.glPushMatrix ();
				RenderPlane (viewport, previous);				
				Gl.glPopMatrix ();
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
				Gl.glViewport (0, 0, viewport.Width, viewport.Height);
				Gl.glMatrixMode (Gl.GL_PROJECTION);
				Gl.glLoadIdentity ();
				Glu.gluPerspective (40, viewport.Width / (float) viewport.Height, 0.1, 50.0);

				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				Gl.glLoadIdentity ();
				
				Glu.gluLookAt (0.0, 0.0, 3.0,
					       0.0, 0.0, 0.0,
					       0.0, 1.0, 0.0);
				
				Gl.glTranslatef (0, 0, -3);
				Gl.glPushMatrix ();
				Gl.glTranslatef (0, 0, 0.5f);

				Texture t = previous;
				
				t.Bind ();
				RenderPlane (new Gdk.Rectangle (-2, -1, 2, 2), t);
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

		protected static void Fit (Gdk.Rectangle viewport, Texture texture)
		{
			float va = viewport.Width / (float) viewport.Height;
			float ta = texture.Width / (float) texture.Height;
			
			Gl.glMatrixMode (Gl.GL_TEXTURE);
			Gl.glLoadIdentity ();
			if (ta > va)
				Gl.glScalef (va/ta, 1, 0);
			else 
				Gl.glScalef (1, ta/va, 0);
				
		} 
		
		protected void RenderPlane (Gdk.Rectangle viewport, Texture previous)
		{
			previous.Bind ();
			Fit (viewport, previous);
			Gl.glMatrixMode (Gl.GL_MODELVIEW);
			
			Gl.glBegin (Gl.GL_QUADS);
			Gl.glTexCoord2f (0, 0);
			Gl.glVertex3f (0, viewport.Height, 0);
			Gl.glTexCoord2f (previous.Width, 0);
			Gl.glVertex3f (viewport.Width, viewport.Height, 0);
			Gl.glTexCoord2f (previous.Width, previous.Height);
			Gl.glVertex3f (viewport.Width, 0, 0);
			Gl.glTexCoord2f (0, previous.Height);
			Gl.glVertex3f (0, 0, 0);
			Gl.glEnd ();
		}
		
		public class Push : GlTransition
		{
			public override void Draw (Gdk.Rectangle viewport, Texture previous, Texture next)
			{
				Gl.glViewport (0, 0, viewport.Width, viewport.Height);
				Gl.glMatrixMode (Gl.GL_PROJECTION);
				Gl.glLoadIdentity ();
				Glu.gluOrtho2D (0, viewport.Width, 0, viewport.Height);
				Gl.glMatrixMode (Gl.GL_MODELVIEW);
				Gl.glLoadIdentity ();

				Gl.glPushMatrix ();
				Gl.glTranslatef (viewport.Width - viewport.Width * percent, 0, 0);
				RenderPlane (viewport, previous);				
				Gl.glPopMatrix ();

				Gl.glPushMatrix ();
				Gl.glTranslatef (-viewport.Width * percent, 0, 0);
				RenderPlane (viewport, next);
				Gl.glPopMatrix ();
			}
		}
	}
}
