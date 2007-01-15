using System;
using Tao.OpenGl;
using Cairo;

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

		public class Reveal : Push
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
	}
}
