using System;
using System.Runtime.InteropServices;
using FSpot.Widgets;

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
	}

	public class Context {
		private HandleRef handle;
		private Gdk.Drawable drawable;
		
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
		
		public Context (Gdk.Drawable drawable, int [] attr) : this (drawable, null, attr)
		{
		}
		
		public Context (Gdk.Drawable drawable,
				Context share_list,
				int [] attr)
		{
			this.drawable = drawable;

			if (drawable == null)
				throw new GlxException ("Invalid drawable");

			IntPtr xdisplay = GdkUtils.GetXDisplay (drawable.Display);
			IntPtr visual_info = glXChooseVisual (xdisplay,
							      drawable.Screen.Number,
							      attr);
			if (visual_info == IntPtr.Zero)
				throw new GlxException ("Unable to find matching visual");
			
			HandleRef share = share_list != null ? share_list.Handle : new HandleRef (null, IntPtr.Zero);
			IntPtr tmp = glXCreateContext (xdisplay, visual_info, share, true);
			
			if (tmp == IntPtr.Zero)
				throw new GlxException ("Unable to create context");
			
			handle = new HandleRef (this, tmp);
			
			if (visual_info != IntPtr.Zero)
				XFree (visual_info);
		}
		
		public void Destroy ()
		{
			glXDestroyContext (GdkUtils.GetXDisplay (drawable.Display),
					   Handle);
		}
		
		public bool MakeCurrent ()
		{
			return glXMakeCurrent (GdkUtils.GetXDisplay (drawable.Display),
					       GdkUtils.GetXid (drawable),
					       Handle);
		}

		public void SwapBuffers ()
		{
			glXSwapBuffers (GdkUtils.GetXDisplay (drawable.Display),
					GdkUtils.GetXid (drawable));
		}
	}
}
