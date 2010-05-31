using Gtk;
using Gdk;
using System;
using GLib;
using System.Runtime.InteropServices;
using FSpot;
using FSpot.Utils;
using Hyena;

namespace FSpot {
	public class XScreenSaverSlide : Gtk.Window {
		public const string ScreenSaverEnviroment = "XSCREENSAVER_WINDOW";

		public XScreenSaverSlide () : base (String.Empty)
		{
		}
	       
		protected override void OnRealized ()
		{
			string env = Environment.GetEnvironmentVariable (ScreenSaverEnviroment);
			
			if (env != null) {
				try {
					env = env.ToLower ();
					
					if (env.StartsWith ("0x"))
						env = env.Substring (2);

					uint xid = UInt32.Parse (env, System.Globalization.NumberStyles.HexNumber);
					
					GdkWindow = Gdk.Window.ForeignNew (xid);
					Style.Attach (GdkWindow);
					GdkWindow.Events = EventMask.ExposureMask 
						| EventMask.StructureMask 
						| EventMask.EnterNotifyMask 
						| EventMask.LeaveNotifyMask 
						| EventMask.FocusChangeMask;
					
					Style.SetBackground (GdkWindow, Gtk.StateType.Normal);
					GdkWindow.SetDecorations ((Gdk.WMDecoration) 0);
					GdkWindow.UserData = this.Handle;
					SetFlag (WidgetFlags.Realized);
					SizeRequest ();
					Gdk.Rectangle geom;
					int depth;
					GdkWindow.GetGeometry (out geom.X, out geom.Y, out geom.Width, out geom.Height, out depth);
					SizeAllocate (new Gdk.Rectangle (geom.X, geom.Y, geom.Width, geom.Height));
					Resize (geom.Width, geom.Height);
					return;
				} catch (System.Exception e) {
					Hyena.Log.Exception (e);
				}
			} else {
				Hyena.Log.DebugFormat ("{0} not set, falling back to window", ScreenSaverEnviroment);
			}

			SetSizeRequest (640, 480);
			base.OnRealized ();
		}
	}
}		
