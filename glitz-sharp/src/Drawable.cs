// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace NDesk.Glitz
{
	public class Drawable : IDisposable
	{
		public IntPtr Handle;

		public Drawable (IntPtr handle)
		{
			this.Handle = handle;
			GlitzAPI.glitz_drawable_reference (Handle);
		}

		public Drawable (Drawable other, ref DrawableFormat format, uint width, uint height)
		{
			this.Handle = GlitzAPI.glitz_create_drawable (other.Handle, ref format, width, height);
			GlitzAPI.glitz_drawable_reference (Handle);
		}

		public static Drawable CreatePbuffer (Drawable other, ref DrawableFormat format, uint width, uint height)
		{
			IntPtr Handle = GlitzAPI.glitz_create_pbuffer_drawable (other.Handle, ref format, width, height);
			return new Drawable (Handle);
		}

		public void UpdateSize (uint width, uint height)
		{
			GlitzAPI.glitz_drawable_update_size (Handle, width, height);
		}

		public void SwapBufferRegion (int x_origin, int y_origin, Box[] box)
		{
			GlitzAPI.glitz_drawable_swap_buffer_region (Handle, x_origin, y_origin, box, box.Length);
		}

		public void SwapBuffers ()
		{
			GlitzAPI.glitz_drawable_swap_buffers (Handle);
		}

		public void Flush ()
		{
			GlitzAPI.glitz_drawable_flush (Handle);
		}

		public void Finish ()
		{
			GlitzAPI.glitz_drawable_flush (Handle);
		}

		public FeatureMask Features {
			get {
				return GlitzAPI.glitz_drawable_get_features (Handle);
			}
		}

		public IntPtr Format {
			get {
				return GlitzAPI.glitz_drawable_get_format (Handle);
			}
		}

		public uint Width {
			get {
				return GlitzAPI.glitz_drawable_get_width (Handle);
			}
		}

		public uint Height {
			get {
				return GlitzAPI.glitz_drawable_get_height (Handle);
			}
		}

		public IntPtr FindStandardFormat (FormatName format_name)
		{
			return GlitzAPI.glitz_find_standard_format (Handle, format_name);
		}

		~Drawable ()
		{
			Dispose (false);
		}

		void IDisposable.Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (Handle == IntPtr.Zero)
				return;

			Console.WriteLine ("glitz_drawable_destroy");
			GlitzAPI.glitz_drawable_destroy (Handle);
			Handle = IntPtr.Zero;
		}

		public void Destroy()
		{
			Dispose (true);
		}

		/*
		public void Destroy ()
		{
			GlitzAPI.glitz_drawable_destroy (Handle);
		}
		*/
	}
}
