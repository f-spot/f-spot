// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using Gtk;
using Cairo;
using NDesk.Glitz;

using System.Runtime.InteropServices;

public class Demo
{
	public static void Main ()
	{
		Application.Init ();

		DrawingArea da = new DrawingArea ();
		da.DoubleBuffered = false;
		da.ExposeEvent += ExposeHandler;

		Window win = new Window ("Glitz#");
		win.Add (da);
		win.ShowAll ();

		surface = SetUp (da.GdkWindow);
		cr = new Cairo.Context (surface);

		Application.Run ();
	}

	static void ExposeHandler (object o, ExposeEventArgs args)
	{
		Console.WriteLine ("exposed");

		Widget widget = o as Widget;

		//using (Cairo.Context cr = new Cairo.Context (surface)) {
		cr.Save ();
		cr.Rectangle (0, 0, widget.Allocation.Width, widget.Allocation.Height);
		//cr.Color = new Color ( 1, 1, 1, 1);
		Gdk.CairoHelper.SetSourceColor (cr, widget.Style.Background (widget.State));
		cr.Fill ();
		cr.Restore ();

		cr.Save ();

		cr.LineWidth = 1;

		cr.MoveTo (10, 10);
		cr.LineTo (20, 10);
		cr.LineTo (20, 20);
		cr.LineTo (10, 20);
		cr.ClosePath ();

		cr.Color = new Cairo.Color ( 0, 0, 0, 1);
		cr.Fill ();

		cr.Restore ();

		//NDesk.Glitz.Context ctx = new NDesk.Glitz.Context (ggd, ggd.Format);
		//Console.WriteLine (ggs.ValidTarget);
		//Console.WriteLine ("proc ptr: " + ctx.GetProcAddress ("glBindProgramARB"));
		//ctx.MakeCurrent (ggd);
		//GlHelper.Draw ();
		//GlHelper.DrawT ();

		ggs.Flush ();

		if (doublebuffer)
			ggd.SwapBuffers ();
		else
			ggd.Flush ();

		args.RetVal = true;
		//args.RetVal = false;
		//}
	}

	static Cairo.Surface surface;
	static Cairo.Context cr;

	// x11 only imports
	[DllImport("libgdk-x11-2.0.so.0")]
	internal static extern IntPtr gdk_x11_get_default_xdisplay ();

	[DllImport("libgdk-x11-2.0.so.0")]
	internal static extern int gdk_x11_get_default_screen ();

	[DllImport("libgdk-x11-2.0.so.0")]
	internal static extern IntPtr gdk_x11_drawable_get_xdisplay (IntPtr handle);

	[DllImport("libgdk-x11-2.0.so.0")]
	internal static extern IntPtr gdk_drawable_get_visual (IntPtr handle);

	[DllImport("libgdk-x11-2.0.so.0")]
	internal static extern IntPtr gdk_x11_visual_get_xvisual (IntPtr handle);

	[DllImport("libgdk-x11-2.0.so.0")]
	internal static extern uint gdk_x11_drawable_get_xid (IntPtr handle);

	[DllImport("libgdk-x11-2.0.so.0")]
	internal static extern IntPtr gdkx_visual_get (uint visualid);

	[DllImport("X11")]
	internal static extern uint XVisualIDFromVisual(IntPtr visual);

	public static bool doublebuffer = false;
	public static NDesk.Glitz.Surface ggs;
	public static NDesk.Glitz.Drawable ggd;

	public static Cairo.Surface Realize (Widget widget)
	{
		IntPtr dpy = gdk_x11_get_default_xdisplay ();
		int scr = gdk_x11_get_default_screen ();

		DrawableFormat template = new DrawableFormat ();
		template.Color = new ColorFormat ();
		FormatMask mask = FormatMask.None;
		//template.Doublebuffer = doublebuffer;
		//mask |= FormatMask.Doublebuffer;
		IntPtr dformat = GlitzAPI.glitz_glx_find_window_format (dpy, scr, mask, ref template, 0);

		if (dformat == IntPtr.Zero)
			throw new Exception ("Could not find a usable GL visual");

		IntPtr vinfo = GlitzAPI.glitz_glx_get_visual_info_from_format (dpy, scr, dformat);
		widget.DoubleBuffered = false;

		Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
				attributes.Mask = widget.Events  |
			     (Gdk.EventMask.ExposureMask |
			      Gdk.EventMask.KeyPressMask |
			      Gdk.EventMask.KeyReleaseMask |
			      Gdk.EventMask.EnterNotifyMask |
			      Gdk.EventMask.LeaveNotifyMask |
			      Gdk.EventMask.StructureMask);
				//attributes.X = widget.Allocation.X;
				//attributes.Y = widget.Allocation.Y;
				attributes.X = 0;
				attributes.Y = 0;
				attributes.Width = 100;
				attributes.Height = 100;
		attributes.Wclass = Gdk.WindowClass.InputOutput;
		attributes.Visual = new Gdk.Visual (gdkx_visual_get (XVisualIDFromVisual (vinfo)));
		attributes.Colormap = new Gdk.Colormap (attributes.Visual, true);
		attributes.WindowType = Gdk.WindowType.Child;
		Gdk.WindowAttributesType attributes_mask = Gdk.WindowAttributesType.X | Gdk.WindowAttributesType.Y | Gdk.WindowAttributesType.Visual | Gdk.WindowAttributesType.Colormap;

		widget.GdkWindow = new Gdk.Window (widget.ParentWindow, attributes, attributes_mask);
		widget.GdkWindow.UserData = widget.Handle;
		uint xid = gdk_x11_drawable_get_xid (widget.GdkWindow.Handle);

		attributes.Width = 100;
		attributes.Height = 100;

		IntPtr glitz_drawable = GlitzAPI.glitz_glx_create_drawable_for_window (dpy, scr, dformat, xid, (uint)attributes.Width, (uint)attributes.Height);
		ggd = new NDesk.Glitz.Drawable (glitz_drawable);

		IntPtr glitz_format = ggd.FindStandardFormat (FormatName.ARGB32);

		ggs = new NDesk.Glitz.Surface (ggd, glitz_format, (uint)attributes.Width, (uint)attributes.Height, 0, IntPtr.Zero);
		Console.WriteLine (ggd.Features);
		ggs.Attach (ggd, doublebuffer ? DrawableBuffer.BackColor : DrawableBuffer.FrontColor);

		//GlitzAPI.glitz_drawable_destroy (glitz_drawable);

		GlitzSurface gs = new GlitzSurface (ggs.Handle);

		return gs;
	}

	public static Cairo.Surface SetUp (Gdk.Drawable gdk_drawable)
	{
		IntPtr x_drawable = gdk_drawable.Handle;
		IntPtr dpy = gdk_x11_drawable_get_xdisplay(x_drawable);
		int scr = 0;

		IntPtr visual = gdk_drawable_get_visual(x_drawable);
		IntPtr Xvisual = gdk_x11_visual_get_xvisual(visual);
		uint XvisualID = XVisualIDFromVisual (Xvisual);

		Console.WriteLine ("XvisID: " + XvisualID);


		IntPtr fmt = GlitzAPI.glitz_glx_find_drawable_format_for_visual (dpy, scr, XvisualID);

		Console.WriteLine ("fmt: " + fmt);

		//IntPtr Xdrawable = gdk_x11_drawable_get_xid (x_drawable);
		uint win = gdk_x11_drawable_get_xid (x_drawable);
		uint w = 100, h = 100;

		IntPtr glitz_drawable = GlitzAPI.glitz_glx_create_drawable_for_window (dpy, scr, fmt, win, w, h);
		//NDesk.Glitz.Drawable ggd = new NDesk.Glitz.Drawable (glitz_drawable);
		ggd = new NDesk.Glitz.Drawable (glitz_drawable);

		IntPtr glitz_format = ggd.FindStandardFormat (FormatName.ARGB32);

		ggs = new NDesk.Glitz.Surface (ggd, glitz_format, 100, 100, 0, IntPtr.Zero);
		Console.WriteLine (ggd.Features);
		ggs.Attach (ggd, doublebuffer ? DrawableBuffer.BackColor : DrawableBuffer.FrontColor);

		//GlitzAPI.glitz_drawable_destroy (glitz_drawable);

		GlitzSurface gs = new GlitzSurface (ggs.Handle);
		//GlitzSurface gs = null;

		return gs;
	}
}
