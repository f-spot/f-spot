using System;
using System.Runtime.InteropServices;
using FSpot.Widgets;
using FSpot.Utils;

namespace GdkGlx {
	public enum GlxAttribute {
		None = 0,
		UseGL = 1,
		BufferSize = 2,
		Level = 3,
		Rgba = 4,
	        DoubleBuffer = 5,
		Stereo = 6,
		AuxBuffers = 7,
		RedSize  = 8,
		GreenSize = 9,
		BlueSize = 10,
		AlphaSize = 11,
		DepthSize = 12,
		StencilSize = 13,
		AccumRedSize = 14,
		AccumGreenSize = 15,
		AccumBlueSize = 16,
		AccumAlphaSize = 17
	}

	public class GlxException : System.Exception {
		public GlxException (string text) : base (text) 
		{
		}

		public GlxException (string text, Exception e) : base (text, e)
		{
		}
	}

	public class Context {
		private HandleRef handle;
		private Gdk.Visual visual;
		
		[DllImport("X11")]
		static extern void XFree (IntPtr handle);
		
		[DllImport("GL")]
		static extern IntPtr glXCreateContext (IntPtr display,
						       IntPtr visual_info,
						       HandleRef share_list,
						       bool direct);
		
		[DllImport("GL")]
		static extern IntPtr glXChooseVisual (IntPtr display,
						      int screen,
						      int [] attr);

		[DllImport("GL")]
		static extern void glXDestroyContext (IntPtr display, 
						      HandleRef ctx);
		
		[DllImport("GL")]
		static extern bool glXMakeCurrent (IntPtr display,
						   uint xdrawable,
						   HandleRef ctx);

		[DllImport("GL")]
		static extern void glXSwapBuffers (IntPtr display, uint drawable);
		

		public HandleRef Handle {
			get { return handle; }
		}
		
		public Context (Gdk.Screen screen, int [] attr) : this (screen, null, attr)
		{
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XVisualInfo {
			public IntPtr visual;
			public uint visualid;
			public int screen;
			public int depth;
			public int c_class;
			public uint red_mask;
			public uint blue_mask;
			public uint green_mask;
			public int colormap_size;
			public int bits_per_rgb;
		}

#if false
		public Gdk.Colormap GetColormap (Gdk.Screen screen, )
		{
			DrawableFormat template = new DrawableFormat ();
			template.Color = new ColorFormat ();
			FormatMask mask = FormatMask.None;
			int num = screen.Number;
			
			IntPtr dformat = GlitzAPI.glitz_glx_find_window_format (GdkUtils.GetXDisplay (screen.Display), 
										num, 
										mask, 
										ref template,
										0);
			
			visual_info = GlitzAPI.glitz_glx_get_visual_info_from_format (dpy, scr, dformat);
			Gdk.Visual visual = new Gdk.Visual (gdkx_visual_get (XVisualIDFromVisual (vinfo)));					
			new Gdk.Colormap (visual, true);
			*/
		}
#endif
		public Gdk.Colormap GetColormap ()
		{
			return new Gdk.Colormap (visual, false);
		}

		public Context (Gdk.Screen screen,
				Context share_list,
				int [] attr)
		{
			IntPtr xdisplay = GdkUtils.GetXDisplay (screen.Display);
			IntPtr visual_info = IntPtr.Zero;

			
			// Be careful about the first glx call and handle the exception
			// with more grace.
			try {
				visual_info = glXChooseVisual (xdisplay,
							       screen.Number,
							       attr);
			} catch (DllNotFoundException e) {
				throw new GlxException ("Unable to find OpenGL libarary", e);
			} catch (EntryPointNotFoundException enf) {
				throw new GlxException ("Unable to find Glx entry point", enf); 
			}

			if (visual_info == IntPtr.Zero)
				throw new GlxException ("Unable to find matching visual");
			
			XVisualInfo xinfo = (XVisualInfo) Marshal.PtrToStructure (visual_info, typeof (XVisualInfo));


			HandleRef share = share_list != null ? share_list.Handle : new HandleRef (null, IntPtr.Zero);
			IntPtr tmp = glXCreateContext (xdisplay, visual_info, share, true);
			
			if (tmp == IntPtr.Zero)
				throw new GlxException ("Unable to create context");
			
			handle = new HandleRef (this, tmp);
			
			visual = GdkUtils.LookupVisual (screen, xinfo.visualid);

			if (visual_info != IntPtr.Zero)
				XFree (visual_info);
		}
		
		public void Destroy ()
		{
			glXDestroyContext (GdkUtils.GetXDisplay (visual.Screen.Display),
					   Handle);
		}
		
		public bool MakeCurrent (Gdk.Drawable drawable)
		{
			return glXMakeCurrent (GdkUtils.GetXDisplay (drawable.Display),
					       GdkUtils.GetXid (drawable),
					       Handle);
		}

		public void SwapBuffers (Gdk.Drawable drawable)
		{
			glXSwapBuffers (GdkUtils.GetXDisplay (drawable.Display),
					GdkUtils.GetXid (drawable));
		}
	}
}
