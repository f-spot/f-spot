// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace NDesk.Glitz
{
	public class TextureObject : IDisposable
	{
		public IntPtr Handle;

		public TextureObject (IntPtr handle)
		{
			this.Handle = handle;
			GlitzAPI.glitz_texture_object_reference (Handle);
		}

		public TextureObject (Surface surface)
		{
			Handle = GlitzAPI.glitz_texture_object_create (surface.Handle);
			GlitzAPI.glitz_texture_object_reference (Handle);
		}

		public void SetFilter (FilterType type, Filter filter)
		{
			GlitzAPI.glitz_texture_object_set_filter (Handle, type, filter);
		}

		public TextureObject Target
		{
			get {
				return new TextureObject (GlitzAPI.glitz_texture_object_get_target (Handle));
			}
		}

		~TextureObject ()
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

			Console.WriteLine ("glitz_texture_object_destroy");
			GlitzAPI.glitz_texture_object_destroy (Handle);
			Handle = IntPtr.Zero;
		}

		public void Destroy()
		{
			Dispose (true);
		}

		/*
		public void Destroy ()
		{
			GlitzAPI.glitz_texture_object_destroy (Handle);
		}
		*/
	}
}
