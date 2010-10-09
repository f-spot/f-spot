//
// Accelerometer.cs
//
// Author:
//   Nat Friedman <nat@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006 Nat Friedman
// Copyright (C) 2007, 2010 Ruben Vermeersch
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

//
// Support for the ThinkPad accelerometer.
//

using System;
using System.IO;

using FSpot.Utils;
using Hyena;
using TagLib.Image;

namespace FSpot {

	public class Accelerometer {
		public const string SYSFS_FILE = "/sys/devices/platform/hdaps/position";

		public static event EventHandler OrientationChanged;

		public enum Orient {
			Normal,
			TiltClockwise,
			TiltCounterclockwise,
		}

		private static Orient current_orientation;

		public static Orient CurrentOrientation {
			get {
				return current_orientation;
			}
		}

		public Accelerometer ()
		{
		}

		public static ImageOrientation GetViewOrientation (ImageOrientation po)
		{
			if (timer == 0 && available)
				SetupAccelerometer ();

			if (current_orientation == Orient.TiltCounterclockwise)
				return FSpot.Utils.PixbufUtils.Rotate90 (po);

			if (current_orientation == Orient.TiltClockwise)
				return FSpot.Utils.PixbufUtils.Rotate270 (po);

			return po;
		}

		static uint timer = 0;
		static bool available = true;

		public static void SetupAccelerometer ()
		{
			if (!System.IO.File.Exists (SYSFS_FILE)) {
				available = false;
				return;
			}

			int x, y;

			// Call once to set a baseline value.
			// Hopefully the laptop is flat when this is
			// called.
			GetHDAPSCoords (out x, out y);

			timer = GLib.Timeout.Add (500, new GLib.TimeoutHandler (CheckOrientation));
		}

		private static bool CheckOrientation ()
		{
			Orient new_orient = GetScreenOrientation ();

			if (new_orient != current_orientation) {
				current_orientation = new_orient;

				EventHandler eh = OrientationChanged;
				if (eh != null)
					eh (null, EventArgs.Empty);

				Log.Debug ("Laptop orientation changed...");
			}

			return true;
		}

		public static Orient GetScreenOrientation ()
		{
			int x, y;

			GetHDAPSCoords (out x, out y);

			if (x > 40)
				return Orient.TiltClockwise;

			if (x < -40)
				return Orient.TiltCounterclockwise;

			return Orient.Normal;
		}

		static int base_x = -1000; // initial nonsense values
		static int base_y = -1000;

		private static void GetHDAPSCoords (out int x, out int y)
		{
			try {
				using (Stream file = System.IO.File.OpenRead (SYSFS_FILE)) {
					StreamReader sr = new StreamReader (file);

					string s = sr.ReadLine ();
					string [] ss = s.Substring (1, s.Length - 2).Split (',');
					x = int.Parse (ss [0]);
					y = int.Parse (ss [1]);

					if (base_x == -1000)
						base_x = x;

					if (base_y == -1000)
						base_y = y;

					x -= base_x;
					y -= base_y;

					return;
				}
			} catch (IOException) {
				x = 0;
				y = 0;
			}
		}
	}
}
