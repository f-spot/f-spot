using System;
using Gtk;
using Tao.OpenGl;
using Cairo;
using System.Runtime.InteropServices;
using FSpot.Widgets;

namespace FSpot {
	public sealed class Texture : IDisposable {
		IntPtr pixels;
		Surface surface;
		int texture_id;
		int width;
		int height;
		
		public int Width {
			get { return width; }
		}

		public int Height {
			get { return height; }
		}
		
		public IntPtr Pixels {
			get { return pixels; }
		}

		public Cairo.Surface Surface {
			get { return surface; }
		}
		
		public Texture (Gdk.Pixbuf pixbuf) : this (pixbuf.Width, pixbuf.Height)
		{
			// FIXME this should really read directly from the pixbuf
			Cairo.Context ctx = new Cairo.Context (Surface);
			Surface image = CairoUtils.CreateSurface (pixbuf);
			Pattern p = new SurfacePattern (image);
			ctx.Source = new SolidPattern (1,1,0);
			ctx.Paint ();
			ctx.Source = p;
			ctx.Paint ();
			p.Destroy ();
			image.Destroy ();
			((IDisposable)ctx).Dispose ();
		}

		public Texture (int width, int height)
		{
			this.width = width;
			this.height = height;
			pixels = Marshal.AllocHGlobal (4 * width * height);
			
			surface = new ImageSurface (pixels, Format.Argb32, width, height, width * 4);
		
			Gl.glClear (Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
			Gl.glGenTextures (1, out texture_id);
			Gl.glBindTexture (Gl.GL_TEXTURE_RECTANGLE_ARB, texture_id);
		}
		
		public int Flush ()
		{
			Gl.glTexImage2D (Gl.GL_TEXTURE_RECTANGLE_ARB,
					 0,
					 Gl.GL_RGBA,
					 width,
					 height,
					 0,
					 Gl.GL_BGRA,
					 Gl.GL_UNSIGNED_BYTE,
					 pixels);
			return texture_id;
		}

		public void Dispose ()
		{
			surface.Destroy ();
			surface = null;
			Marshal.FreeHGlobal (pixels);
			pixels = IntPtr.Zero;

			GC.SuppressFinalize (this);
		}

		~Texture ()
		{
			Dispose ();
		}

	}
}
