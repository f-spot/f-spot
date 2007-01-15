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
		
		public MemorySurface CreateMatchingSurface ()
		{
			return new MemorySurface (Format.Argb32, width, height);
		}
		
		public Texture (Gdk.Pixbuf pixbuf) : this (pixbuf.Width, pixbuf.Height)
		{
			MemorySurface image = CairoUtils.CreateSurface (pixbuf);
			CopyFromSurface (image);
		}

		public Texture (int width, int height)
		{
			this.width = width;
			this.height = height;
		
			Gl.glClear (Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
			Gl.glGenTextures (1, out texture_id);
		}

		public void Bind ()
		{
			Gl.glBindTexture (Gl.GL_TEXTURE_RECTANGLE_ARB, texture_id);
		}

		public int CopyFromSurface (MemorySurface surface)
		{
			Bind ();
			if (surface.Width != width || surface.Height != height)
				throw new ApplicationException ("Bad Surface Match");

			if (surface.Pixels == IntPtr.Zero)
				throw new ApplicationException ("Surface has no data");

			Gl.glTexImage2D (Gl.GL_TEXTURE_RECTANGLE_ARB,
					 0,
					 Gl.GL_RGBA,
					 width,
					 height,
					 0,
					 Gl.GL_BGRA,
					 Gl.GL_UNSIGNED_BYTE,
					 surface.Pixels);
			
			return texture_id;
		}

		public void Dispose ()
		{
			Gl.glDeleteTextures (1, new int [] { texture_id });
			GC.SuppressFinalize (this);
		}

		~Texture ()
		{
			Dispose ();
		}

	}
}
