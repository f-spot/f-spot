//
// XScreenSaverSlide.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Gabriel Burt
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;
using Gdk;

using System;

namespace FSpot
{
	public class XScreenSaverSlide : Gtk.Window
	{
		public const string ScreenSaverEnviroment = "XSCREENSAVER_WINDOW";

		public XScreenSaverSlide () : base (string.Empty)
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
					GdkWindow.SetDecorations (0);
					GdkWindow.UserData = this.Handle;
					SetFlag (WidgetFlags.Realized);
					SizeRequest ();
					Gdk.Rectangle geom;
					int depth;
					GdkWindow.GetGeometry (out geom.X, out geom.Y, out geom.Width, out geom.Height, out depth);
					SizeAllocate (new Gdk.Rectangle (geom.X, geom.Y, geom.Width, geom.Height));
					Resize (geom.Width, geom.Height);
					return;
				} catch (Exception e) {
					Hyena.Log.Exception (e);
				}
			} else {
				Hyena.Log.Debug ($"{ScreenSaverEnviroment} not set, falling back to window");
			}

			SetSizeRequest (640, 480);
			base.OnRealized ();
		}
	}
}
