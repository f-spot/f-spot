// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace NDesk.Glitz
{
	public class Surface : IDisposable
	{
		public IntPtr Handle;

		public Surface (Drawable drawable, IntPtr format, uint width, uint height, ulong mask, IntPtr attributes)
		{
			Handle = GlitzAPI.glitz_surface_create (drawable.Handle, format, width, height, mask, attributes);
			GlitzAPI.glitz_surface_reference (Handle);
		}

		public void Attach (Drawable drawable, DrawableBuffer buffer)
		{
			GlitzAPI.glitz_surface_attach (Handle, drawable.Handle, buffer);
		}

		public void Detach ()
		{
			GlitzAPI.glitz_surface_detach (Handle);
		}

		public void Flush ()
		{
			GlitzAPI.glitz_surface_flush (Handle);
		}

		public IntPtr Drawable {
			get {
				return GlitzAPI.glitz_surface_get_drawable (Handle);
			}
		}

		public IntPtr AttachedDrawable {
			get {
				return GlitzAPI.glitz_surface_get_attached_drawable (Handle);
			}
		}

		//TODO: consider memory management
		/*
		public void SetTransform (ref Transform transform)
		{
			GlitzAPI.glitz_surface_set_transform (Handle, ref transform);
		}
		*/
		public void SetFill (Fill fill)
		{
			GlitzAPI.glitz_surface_set_fill (Handle, fill);
		}

		public void SetComponentAlpha (bool component_alpha)
		{
			GlitzAPI.glitz_surface_set_component_alpha (Handle, component_alpha);
		}

		public void SetFilter (Filter filter, int[] @params)
		{
			GlitzAPI.glitz_surface_set_filter (Handle, filter, @params, @params.Length);
		}

		public void SetDither (bool dither)
		{
			GlitzAPI.glitz_surface_set_dither (Handle, dither);
		}

		public uint Width {
			get {
				return GlitzAPI.glitz_surface_get_width (Handle);
			}
		}

		public uint Height {
			get {
				return GlitzAPI.glitz_surface_get_height (Handle);
			}
		}

		public Status Status {
			get {
				return GlitzAPI.glitz_surface_get_status (Handle);
			}
		}

		public IntPtr Format {
			get {
				return GlitzAPI.glitz_surface_get_format (Handle);
			}
		}

		public void TranslatePoint (ref Point src, ref Point dst)
		{
			GlitzAPI.glitz_surface_translate_point (Handle, ref src, ref dst);
		}

		public void SetClipRegion (int x_origin, int y_origin, Box[] box)
		{
			GlitzAPI.glitz_surface_set_clip_region (Handle, x_origin, y_origin, box, box.Length);
		}

		public bool ValidTarget {
			get {
				return GlitzAPI.glitz_surface_valid_target (Handle);
			}
		}

		public static void Composite (Operator op, Surface src, Surface mask, Surface dst, int x_src, int y_src, int x_mask, int y_mask, int x_dst, int y_dst, int width, int height)
		{
			GlitzAPI.glitz_composite (op, src.Handle, mask.Handle, dst.Handle, x_src, y_src, x_mask, y_mask, x_dst, y_dst, width, height);
		}

		public static void CopyArea (int op, Surface src, Surface dst, int x_src, int y_src, int width, int height, int x_dst, int y_dst)
		{
			GlitzAPI.glitz_copy_area (src.Handle, dst.Handle, x_src, y_src, width, height, x_dst, y_dst);
		}

		~Surface ()
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

			Console.WriteLine ("glitz_surface_destroy");
			GlitzAPI.glitz_surface_destroy (Handle);
			Handle = IntPtr.Zero;
		}

		public void Destroy()
		{
			Dispose (true);
		}

		/*
		public void Destroy ()
		{
			GlitzAPI.glitz_surface_destroy (Handle);
		}
		*/
	}
}
