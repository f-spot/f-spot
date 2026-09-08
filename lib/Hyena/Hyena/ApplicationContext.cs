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
using System.Runtime.InteropServices;
using System.Text;


namespace Hyena
{
	public delegate void InvokeHandler ();

	public static class ApplicationContext
	{
		public static string ApplicationName { get; set; }

		// Set by the client from its parsed command line rather than read from a parser
		// here, so nothing in this assembly depends on how arguments are parsed. The
		// BANSHEE_DEBUG fallback that used to live in this getter moved with it: the
		// client assigns before anything reads, so a fallback here could never evaluate.
		public static bool Debugging { get; set; }

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
