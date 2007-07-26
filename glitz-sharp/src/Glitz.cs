// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Runtime.InteropServices;
//using Cairo;
//using Gtk;

namespace NDesk.Glitz
{
	public
	enum Status
	{
		Success,
		NoMemory,
		BadCoordinate,
		NotSupported,
		ContentDestroyed,
	}

	public
	enum FormatName
	{
		ARGB32,
		RGB24,
		A8,
		A1,
	}

	public
	enum DrawableBuffer
	{
		FrontColor,
		BackColor,
	}

	[Flags]
	public
	enum FormatMask
	{
		None = 0,

		ID = 1 << 0,
		RedSize = 1 << 1,
		GreenSize = 1 << 2,
		BlueSize = 1 << 3,
		AlphaSize = 1 << 4,

		Fourcc = 1 << 5,

		DepthSize = 1 << 6,
		StencilSize = 1 << 7,
		Doublebuffer = 1 << 8,
		Samples = 1 << 9,
	}

	public
	enum Fill
	{
		Transparent,
		Nearest,
		Repeat,
		Reflect,
	}

	public
	struct Rectangle
	{
		public short X, Y;
		public ushort Width, Height;
	}

	public
	struct Box
	{
		public short X1, Y1, X2, Y2;
	}

	public
	struct Point
	{
		public int X, Y;
	}

	public
	struct Line
	{
		public Point P1, P2;
	}

	public
	struct Trapezoid
	{
		public int Top, Bottom;
		public Line Left, Right;
	}

	public
	struct Span
	{
		public int Left, Right, Y;
	}

	public
	struct Trap
	{
		public Span Top, Bottom;
	}

	public unsafe
	struct Transform
	{
		//TODO: should be 3x3
		public fixed int Matrix[9];
	}

	public
	struct Color
	{
		public uint Fourcc;
		public ushort Red;
		public ushort Green;
		public ushort Blue;
		public ushort Alpha;
	}

	public
	enum FilterType
	{
		Mag,
		Min,
	}

	public
	enum Filter
	{
		Nearest,
		Bilinear,
		Convolution,
		Gaussian,
		LinearGradient,
		RadialGradient,
	}

	public
	enum Operator
	{
		Clear,
		Src,
		Dst,
		Over,
		OverReverse,
		In,
		InReverse,
		Out,
		OutReverse,
		Atop,
		AtopReverse,
		Xor,
		Add,
	}

	public
	struct ColorFormat
	{
		public uint Fourcc;
		public ushort RedSize;
		public ushort GreenSize;
		public ushort BlueSize;
		public ushort AlphaSize;
	}

	public
	struct DrawableFormat
	{
		public ulong ID;
		public ColorFormat Color;
		public ushort DepthSize;
		public ushort StencilSize;
		public ushort Samples;
		public bool Doublebuffer;
	}

	[Flags]
	public
	enum FeatureMask
	{
		None = 0,

		TextureRectangle = 1 << 0,
		TextureNonPowerOfTwo = 1 << 1,
		TextureMirroredRepeat = 1 << 2,
		TextureBorderClamp = 1 << 3,
		Multisample = 1 << 4,
		MultisampleFilterHint = 1 << 5,
		Multitexture = 1 << 6,
		TextureEnvCombine = 1 << 7,
		TextureEnvDot3 = 1 << 8,
		FragmentProgram = 1 << 9,
		VertexBufferObject = 1 << 10,
		PixelBufferObject = 1 << 11,
		PerComponentRendering = 1 << 12,
		BlendColor = 1 << 13,
		PackedPixels = 1 << 14,
		MultiDrawArrays = 1 << 15,
		FramebufferObject = 1 << 16,
		CopySubBuffer = 1 << 17,
	}

	public
	enum BufferHint
	{
		StreamDraw,
		StreamRead,
		StreamCopy,
		StaticDraw,
		StaticRead,
		StaticCopy,
		DynamicDraw,
		DynamicRead,
		DynamicCopy,
	}

	public
	enum BufferAccess
	{
		ReadOnly,
		WriteOnly,
		ReadWrite,
	}

	public class GlitzAPI
	{
		[DllImport("glitz-glx")]
		public
		static extern IntPtr glitz_glx_create_drawable_for_window (IntPtr display, int screen, IntPtr glitz_drawable_format, uint window, uint width, uint height);

		//this one needs work
		[DllImport("glitz-glx")]
		public
		//static extern IntPtr glitz_glx_find_window_format (IntPtr display, int screen, FormatMask mask, IntPtr glitz_drawable_format, int count);
		static extern IntPtr glitz_glx_find_window_format (IntPtr display, int screen, FormatMask mask, ref DrawableFormat templ, int count);

		[DllImport("glitz-glx")]
		public
		static extern IntPtr glitz_glx_find_drawable_format_for_visual (IntPtr display, int screen, uint visualid);

		[DllImport("glitz-glx")]
		public
		static extern IntPtr glitz_glx_get_visual_info_from_format (IntPtr display, int screen, IntPtr format);


		[DllImport("glitz")]
		public
		static extern IntPtr glitz_create_drawable (IntPtr other_drawable, ref DrawableFormat format, uint width, uint height);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_create_pbuffer_drawable (IntPtr other_drawable, ref DrawableFormat format, uint width, uint height);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_find_standard_format (IntPtr drawable, FormatName format_name);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_surface_create (IntPtr drawable, IntPtr format, uint width, uint height, ulong mask, IntPtr attributes);

		[DllImport("glitz")]
		public
		static extern void glitz_surface_destroy (IntPtr surface);

		[DllImport("glitz")]
		public
		static extern void glitz_surface_reference (IntPtr surface);

		[DllImport("glitz")]
		public
		static extern void glitz_surface_attach (IntPtr surface, IntPtr drawable, DrawableBuffer buffer);

		[DllImport("glitz")]
		public
		static extern void glitz_surface_detach (IntPtr surface);

		[DllImport("glitz")]
		public
		static extern void glitz_surface_flush (IntPtr surface);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_surface_get_drawable (IntPtr surface);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_surface_get_attached_drawable (IntPtr surface);

		[DllImport("glitz")]
		public
		static extern void glitz_surface_set_transform (IntPtr surface, IntPtr transform);

		[DllImport("glitz")]
		public
		static extern void glitz_surface_set_fill (IntPtr surface, Fill fill);

		[DllImport("glitz")]
		public
		static extern void glitz_surface_set_component_alpha (IntPtr surface, bool component_alpha);

		//TODO: review this
		[DllImport("glitz")]
		public
		static extern void glitz_surface_set_filter (IntPtr surface, Filter filter, int[] @params, int n_params);

		[DllImport("glitz")]
		public
		static extern void glitz_surface_set_dither (IntPtr surface, bool dither);

		[DllImport("glitz")]
		public
		static extern uint glitz_surface_get_width (IntPtr surface);

		[DllImport("glitz")]
		public
		static extern uint glitz_surface_get_height (IntPtr surface);

		[DllImport("glitz")]
		public
		static extern Status glitz_surface_get_status (IntPtr surface);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_surface_get_format (IntPtr surface);

		//TODO: complete point_fixed_t
		[DllImport("glitz")]
		public
		static extern void glitz_surface_translate_point (IntPtr surface, ref Point src, ref Point dst);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_surface_set_clip_region (IntPtr surface, int x_origin, int y_origin, Box[] box, int n_box);

		[DllImport("glitz")]
		public
		static extern bool glitz_surface_valid_target (IntPtr surface);

		//TextureObject

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_texture_object_create (IntPtr surface);

		[DllImport("glitz")]
		public
		static extern void glitz_texture_object_destroy (IntPtr texture);

		[DllImport("glitz")]
		public
		static extern void glitz_texture_object_reference (IntPtr texture);

		[DllImport("glitz")]
		public
		static extern void glitz_texture_object_set_filter (IntPtr texture, FilterType type, Filter filter);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_texture_object_get_target (IntPtr texture);

		//Drawable

		[DllImport("glitz")]
		public
		static extern void glitz_drawable_destroy (IntPtr drawable);

		[DllImport("glitz")]
		public
		static extern void glitz_drawable_reference (IntPtr drawable);

		[DllImport("glitz")]
		public
		static extern void glitz_drawable_update_size (IntPtr drawable, uint width, uint height);

		[DllImport("glitz")]
		public
		static extern uint glitz_drawable_get_width (IntPtr drawable);

		[DllImport("glitz")]
		public
		static extern uint glitz_drawable_get_height (IntPtr drawable);

		[DllImport("glitz")]
		public
		static extern void glitz_drawable_swap_buffer_region (IntPtr drawable, int x_origin, int y_origin, Box[] box, int n_box);

		[DllImport("glitz")]
		public
		static extern void glitz_drawable_swap_buffers (IntPtr drawable);

		[DllImport("glitz")]
		public
		static extern void glitz_drawable_flush (IntPtr drawable);

		[DllImport("glitz")]
		public
		static extern void glitz_drawable_finish (IntPtr drawable);

		[DllImport("glitz")]
		public
		static extern FeatureMask glitz_drawable_get_features (IntPtr drawable);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_drawable_get_format (IntPtr drawable);

		//Context

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_context_create (IntPtr drawable, IntPtr drawable_format);

		[DllImport("glitz")]
		public
		static extern void glitz_context_destroy (IntPtr context);

		[DllImport("glitz")]
		public
		static extern void glitz_context_reference (IntPtr context);

		[DllImport("glitz")]
		public
		static extern void glitz_context_copy (IntPtr context, IntPtr dst_context, ulong mask);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_context_get_proc_address (IntPtr context, string name);

		[DllImport("glitz")]
		public
		static extern void glitz_context_make_current (IntPtr context, IntPtr drawable);

		[DllImport("glitz")]
		public
		static extern void glitz_context_bind_texture (IntPtr context, IntPtr texture);

		[DllImport("glitz")]
		public
		static extern void glitz_context_draw_buffers (IntPtr context, DrawableBuffer[] buffers, int n);

		[DllImport("glitz")]
		public
		static extern void glitz_context_read_buffer (IntPtr context, DrawableBuffer buffer);

		//Buffer

		[DllImport("glitz")]
		public
		//static extern IntPtr glitz_buffer_create (IntPtr drawable, IntPtr data, uint size, BufferHint hint);
		static extern IntPtr glitz_buffer_create (IntPtr drawable, IntPtr data, uint size, BufferHint hint);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_buffer_create_for_data (IntPtr data);

		[DllImport("glitz")]
		public
		static extern void glitz_buffer_destroy (IntPtr buffer);

		[DllImport("glitz")]
		public
		static extern void glitz_buffer_reference (IntPtr buffer);

		[DllImport("glitz")]
		public
		static extern IntPtr glitz_buffer_map (IntPtr buffer, BufferAccess access);

		[DllImport("glitz")]
		public
		static extern Status glitz_buffer_unmap (IntPtr buffer);

		//Glitz

		[DllImport("glitz")]
		public
		static extern void glitz_composite (Operator op, IntPtr src, IntPtr mask, IntPtr dst, int x_src, int y_src, int x_mask, int y_mask, int x_dst, int y_dst, int width, int height);

		[DllImport("glitz")]
		public
		static extern void glitz_copy_area (IntPtr src, IntPtr dst, int x_src, int y_src, int width, int height, int x_dst, int y_dst);


		//[DllImport("cairo")]
		//static extern IntPtr cairo_glitz_surface_create (IntPtr surface);
	}
}

