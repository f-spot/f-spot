//
// ApplicationContext.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

using Hyena.CommandLine;

namespace Hyena
{
	public delegate void InvokeHandler ();

	public static class ApplicationContext
	{
		public static readonly DateTime StartedAt = DateTime.Now;

		static ApplicationContext ()
		{
			Log.Debugging = Debugging;
		}

		static CommandLineParser command_line = new CommandLineParser ();
		public static CommandLineParser CommandLine {
			set { command_line = value; }
			get { return command_line; }
		}

		public static string ApplicationName { get; set; }

		static Layout command_line_layout;
		public static Layout CommandLineLayout {
			get { return command_line_layout; }
			set { command_line_layout = value; }
		}

		static bool? debugging = null;
		public static bool Debugging {
			get {
				if (debugging == null) {
					debugging = CommandLine.Contains ("debug");
					debugging |= CommandLine.Contains ("debug-sql");
					debugging |= EnvironmentIsSet ("BANSHEE_DEBUG");
				}

				return debugging.Value;
			}
			set {
				debugging = value;
				Log.Debugging = Debugging;
			}
		}

		public static bool EnvironmentIsSet (string env)
		{
			return !string.IsNullOrEmpty (Environment.GetEnvironmentVariable (env));
		}

		static CultureInfo current_culture = CultureInfo.CurrentCulture;
		public static CultureInfo CurrentCulture {
			get { return current_culture; }
		}

		public static CultureInfo InternalCultureInfo {
			get { return CultureInfo.InvariantCulture; }
		}

		[DllImport ("libc")] // Linux
		static extern int prctl (int option, byte[] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);

		[DllImport ("libc")] // BSD
		static extern void setproctitle (byte[] fmt, byte[] str_arg);

		static void SetProcessName (string name)
		{
			if (Environment.OSVersion.Platform != PlatformID.Unix) {
				return;
			}

			try {
				if (prctl (15 /* PR_SET_NAME */, Encoding.ASCII.GetBytes (name + "\0"),
					IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0) {
					throw new ApplicationException ($"Error setting process name {name}");
				}
			} catch (EntryPointNotFoundException) {
				setproctitle (Encoding.ASCII.GetBytes ("%s\0"),
					Encoding.ASCII.GetBytes (name + "\0"));
			}
		}

		public static void TrySetProcessName (string name)
		{
			try {
				SetProcessName (name);
			} catch {
			}
		}
	}
}
