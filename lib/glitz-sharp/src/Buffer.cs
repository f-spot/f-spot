// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace NDesk.Glitz
{
	public class Buffer : IDisposable
	{
		public IntPtr Handle;

		public Buffer (Drawable drawable, IntPtr data, uint size, BufferHint hint)
		{
			Handle = GlitzAPI.glitz_buffer_create (drawable.Handle, data, size, hint);
			GlitzAPI.glitz_buffer_reference (Handle);
		}

		public Buffer (Drawable drawable, IntPtr data)
		{
			Handle = GlitzAPI.glitz_buffer_create_for_data (data);
			GlitzAPI.glitz_buffer_reference (Handle);
		}

		public IntPtr Map (BufferAccess access)
		{
			return GlitzAPI.glitz_buffer_map (Handle, access);
		}

		public Status Unmap (BufferAccess access)
		{
			return GlitzAPI.glitz_buffer_unmap (Handle);
		}

		~Buffer ()
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

			Console.WriteLine ("glitz_buffer_destroy");
			GlitzAPI.glitz_buffer_destroy (Handle);
			Handle = IntPtr.Zero;
		}

		public void Destroy()
		{
			Dispose (true);
		}

		/*
		public void Destroy ()
		{
			GlitzAPI.glitz_buffer_destroy (Handle);
		}
		*/
	}
}
