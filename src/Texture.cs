/* 
 * Texture.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */

using System;
using Gtk;
using Tao.OpenGl;
using Cairo;
using System.Runtime.InteropServices;
using FSpot.Widgets;

namespace FSpot {
	// FIXME this class is a hack to have get_data functionality
	// on cairo 1.0.x
	public sealed class MemorySurface : ImageSurface {
		[DllImport ("libfspot")]
		static extern IntPtr f_image_surface_create (Cairo.Format format, int width, int height);
		
		[DllImport ("libfspot")]
		static extern IntPtr f_image_surface_get_data (IntPtr surface);

		[DllImport ("libfspot")]
		static extern Cairo.Format f_image_surface_get_format (IntPtr surface);

		public MemorySurface (Cairo.Format format, int width, int height)
			: this (f_image_surface_create (format, width, height))
		{
		}

		public MemorySurface (IntPtr handle) : base (handle, true)
		{
			if (Pixels == IntPtr.Zero)
				throw new ApplicationException ("Missing image data");
		}

		public IntPtr Pixels {
			get {
				return f_image_surface_get_data (Handle);
			}
		}

		public Cairo.Format Format {
			get {
				return f_image_surface_get_format (Handle);
			}
		}
	}

	public class TextureException : System.Exception {
		public TextureException (string msg) : base (msg)
		{
		}
	}

	public sealed class Texture : IDisposable {
		int texture_id;
		int width;
		int height;
		
		public int Width {
			get { return width; }
		}

		public int Height {
			get { return height; }
		}
		
		public int Id {
			get { return texture_id; }
		}

		public MemorySurface CreateMatchingSurface ()
		{
			return new MemorySurface (Format.Argb32, width, height);
		}
		
		public Texture (MemorySurface surface) 
			: this (surface.Width, surface.Height)
		{
			CopyFromSurface (surface);
		}

		public Texture (Gdk.Pixbuf pixbuf) 
			: this (pixbuf.Width, pixbuf.Height)
		{
			MemorySurface surface = CairoUtils.CreateSurface (pixbuf);
			CopyFromSurface (surface);
			surface.Destroy ();
		}

		private Texture (int width, int height)
		{
			this.width = width;
			this.height = height;
			
			Gl.glGenTextures (1, out texture_id);
			//System.Console.WriteLine ("generated texture.{0} ({1}, {2})", texture_id, width, height); 
		}

		public void Bind ()
		{
			/*
			Console.WriteLine ("binding texture {0} is it a texture {1}", 
					   texture_id, 
					   Gl.glIsTexture (texture_id));
			*/
			Gl.glEnable (Gl.GL_TEXTURE_RECTANGLE_ARB);
			Gl.glBindTexture (Gl.GL_TEXTURE_RECTANGLE_ARB, texture_id);
		}

		public int CopyFromSurface (MemorySurface surface)
		{
			Bind ();

			int max_size;
			Gl.glGetIntegerv (Gl.GL_MAX_RECTANGLE_TEXTURE_SIZE_ARB, out max_size);
			float scale = (float)Math.Min (1.0, max_size / (double) Math.Max (width, height));
			System.Console.WriteLine ("max texture size {0} scaling to {1}", max_size, scale);
			
			if (surface.Pixels == IntPtr.Zero)
				throw new TextureException ("Surface has no data");

			if (surface.Format != Format.Rgb24 && surface.Format != Format.Argb32)
				throw new TextureException ("Unsupported format type");

			IntPtr pixels = surface.Pixels;
			IntPtr tmp = IntPtr.Zero;
			if (scale != 1.0) {
				int swidth = (int)(width * scale);
				int sheight = (int) (height * scale);
				tmp = Marshal.AllocHGlobal (swidth * sheight * 4);
				System.Console.WriteLine ("scaling image {0} x {1}", swidth, sheight);

				Glu.gluScaleImage (Gl.GL_BGRA,
						  width,
						  height,
						  Gl.GL_UNSIGNED_BYTE,
						  pixels,
						  swidth,
						  sheight,
						  Gl.GL_UNSIGNED_BYTE,
						  tmp);
				pixels = tmp;
				width = swidth;
				height = sheight;
			}
			
			Gl.glTexImage2D (Gl.GL_TEXTURE_RECTANGLE_ARB,
					 0,
					 Gl.GL_RGBA,
					 width,
					 height,
					 0,
					 Gl.GL_BGRA,
					 Gl.GL_UNSIGNED_BYTE,
					 pixels);
			

			if (tmp != IntPtr.Zero)
				Marshal.FreeHGlobal (tmp);

			if (Gl.glGetError () != Gl.GL_NO_ERROR)
				System.Console.WriteLine ("unable to allocate texture");
			//	throw new TextureException ("Unable to allocate texture resources");

			return texture_id;
		}

		protected void Close ()
		{
			System.Console.WriteLine ("Disposing {0} IsTexture {1}", texture_id, Gl.glIsTexture (texture_id));
			int [] ids = new int [] { texture_id };
			Gl.glDeleteTextures (1, ids);
			System.Console.WriteLine ("Done Disposing {0}", ids [0]);
		}

		public void Dispose ()
		{
			Close ();
			GC.SuppressFinalize (this);
		}
		/*
		~Texture ()
		{
			Close ();
		}
		*/

	}
}
