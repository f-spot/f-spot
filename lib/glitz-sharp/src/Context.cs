// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace NDesk.Glitz
{
	public class Context : IDisposable
	{
		public IntPtr Handle;

		public Context (Drawable drawable, IntPtr format)
		{
			Handle = GlitzAPI.glitz_context_create (drawable.Handle, format);
			GlitzAPI.glitz_context_reference (Handle);
		}

		public IntPtr GetProcAddress (string name)
		{
			return GlitzAPI.glitz_context_get_proc_address (Handle, name);
		}

		public void MakeCurrent (Drawable drawable)
		{
			GlitzAPI.glitz_context_make_current (Handle, drawable.Handle);
		}

		public void BindTexture (TextureObject texture)
		{
			GlitzAPI.glitz_context_bind_texture (Handle, texture.Handle);
		}

		public void DrawBuffers (DrawableBuffer[] buffers)
		{
			GlitzAPI.glitz_context_draw_buffers (Handle, buffers, buffers.Length);
		}

		public void ReadBuffer (DrawableBuffer buffer)
		{
			GlitzAPI.glitz_context_read_buffer (Handle, buffer);
		}

		~Context ()
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

			Console.WriteLine ("glitz_context_destroy");
			GlitzAPI.glitz_context_destroy (Handle);
			Handle = IntPtr.Zero;
		}

		public void Destroy()
		{
			Dispose (true);
		}

		/*
		public void Destroy ()
		{
			GlitzAPI.glitz_context_destroy (Handle);
		}
		*/
	}
}
