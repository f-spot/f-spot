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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
				Hyena.Log.DebugFormat ("{0} not set, falling back to window", ScreenSaverEnviroment);
			}

			SetSizeRequest (640, 480);
			base.OnRealized ();
		}
	}
}
