//
// ConsoleCrayon.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena
{
	static class ConsoleCrayon
	{

		#region Public API

		static ConsoleColor foreground_color;
		public static ConsoleColor ForegroundColor {
			get { return foreground_color; }
			set {
				foreground_color = value;
				SetColor (foreground_color, true);
			}
		}

		static ConsoleColor background_color;
		public static ConsoleColor BackgroundColor {
			get { return background_color; }
			set {
				background_color = value;
				SetColor (background_color, false);
			}
		}

		public static void ResetColor ()
		{
			if (XtermColors) {
				Console.Write (GetAnsiResetControlCode ());
			} else if (Environment.OSVersion.Platform != PlatformID.Unix && !RuntimeIsMono) {
				Console.ResetColor ();
			}
		}

		static void SetColor (ConsoleColor color, bool isForeground)
		{
			if (color < ConsoleColor.Black || color > ConsoleColor.White) {
				throw new ArgumentOutOfRangeException (nameof (color), "Not a ConsoleColor value.");
			}

			if (XtermColors) {
				Console.Write (GetAnsiColorControlCode (color, isForeground));
			} else if (Environment.OSVersion.Platform != PlatformID.Unix && !RuntimeIsMono) {
				if (isForeground) {
					Console.ForegroundColor = color;
				} else {
					Console.BackgroundColor = color;
				}
			}
		}

		#endregion

		#region Ansi/VT Code Calculation

		// Modified from Mono's System.TermInfoDriver
		// License: MIT/X11
		// Authors: Gonzalo Paniagua Javier <gonzalo@ximian.com>
		// (C) 2005-2006 Novell, Inc <http://www.novell.com>

		static int TranslateColor (ConsoleColor desired, out bool light)
		{
			light = false;
			switch (desired) {
			// Dark colors
			case ConsoleColor.Black: return 0;
			case ConsoleColor.DarkRed: return 1;
			case ConsoleColor.DarkGreen: return 2;
			case ConsoleColor.DarkYellow: return 3;
			case ConsoleColor.DarkBlue: return 4;
			case ConsoleColor.DarkMagenta: return 5;
			case ConsoleColor.DarkCyan: return 6;
			case ConsoleColor.Gray: return 7;

			// Light colors
			case ConsoleColor.DarkGray: light = true; return 0;
			case ConsoleColor.Red: light = true; return 1;
			case ConsoleColor.Green: light = true; return 2;
			case ConsoleColor.Yellow: light = true; return 3;
			case ConsoleColor.Blue: light = true; return 4;
			case ConsoleColor.Magenta: light = true; return 5;
			case ConsoleColor.Cyan: light = true; return 6;
			case ConsoleColor.White: default: light = true; return 7;
			}
		}

		static string GetAnsiColorControlCode (ConsoleColor color, bool isForeground)
		{
			// lighter fg colours are 90 -> 97 rather than 30 -> 37
			// lighter bg colours are 100 -> 107 rather than 40 -> 47
			int code = TranslateColor (color, out var light) + (isForeground ? 30 : 40) + (light ? 60 : 0);
			return string.Format ("\x001b[{0}m", code);
		}

		static string GetAnsiResetControlCode ()
		{
			return "\x001b[0m";
		}

		#endregion

		#region xterm Detection

		static bool? xterm_colors = null;
		public static bool XtermColors {
			get {
				if (xterm_colors == null) {
					DetectXtermColors ();
				}

				return xterm_colors.Value;
			}
		}

		[System.Runtime.InteropServices.DllImport ("libc", EntryPoint = "isatty")]
		static extern int _isatty (int fd);

		static bool isatty (int fd)
		{
			try {
				return _isatty (fd) == 1;
			} catch {
				return false;
			}
		}

		static void DetectXtermColors ()
		{
			bool _xterm_colors = false;

			switch (Environment.GetEnvironmentVariable ("TERM")) {
			case "xterm":
			case "rxvt":
			case "rxvt-unicode":
				if (Environment.GetEnvironmentVariable ("COLORTERM") != null) {
					_xterm_colors = true;
				}
				break;
			case "xterm-color":
				_xterm_colors = true;
				break;
			}

			xterm_colors = _xterm_colors && isatty (1) && isatty (2);
		}

		#endregion

		#region Runtime Detection

		static bool? runtime_is_mono;
		public static bool RuntimeIsMono {
			get {
				if (runtime_is_mono == null) {
					runtime_is_mono = Type.GetType ("System.MonoType") != null;
				}

				return runtime_is_mono.Value;
			}
		}

		#endregion

		#region Tests

		public static void Test ()
		{
			TestSelf ();
			Console.WriteLine ();
			TestAnsi ();
			Console.WriteLine ();
			TestRuntime ();
		}

		static void TestSelf ()
		{
			Console.WriteLine ("==SELF TEST==");
			foreach (ConsoleColor color in Enum.GetValues (typeof (ConsoleColor))) {
				ForegroundColor = color;
				Console.Write (color);
				ResetColor ();
				Console.Write (" :: ");
				BackgroundColor = color;
				Console.Write (color);
				ResetColor ();
				Console.WriteLine ();
			}
		}

		static void TestAnsi ()
		{
			Console.WriteLine ("==ANSI TEST==");
			foreach (ConsoleColor color in Enum.GetValues (typeof (ConsoleColor))) {
				string color_code_fg = GetAnsiColorControlCode (color, true);
				string color_code_bg = GetAnsiColorControlCode (color, false);
				Console.Write ("{0}{1}: {2}{3} :: {4}{1}: {5}{3}", color_code_fg, color, color_code_fg.Substring (2),
					GetAnsiResetControlCode (), color_code_bg, color_code_bg.Substring (2));
				Console.WriteLine ();
			}
		}

		static void TestRuntime ()
		{
			Console.WriteLine ("==RUNTIME TEST==");
			foreach (ConsoleColor color in Enum.GetValues (typeof (ConsoleColor))) {
				Console.ForegroundColor = color;
				Console.Write (color);
				Console.ResetColor ();
				Console.Write (" :: ");
				Console.BackgroundColor = color;
				Console.Write (color);
				Console.ResetColor ();
				Console.WriteLine ();
			}
		}

		#endregion

	}
}
