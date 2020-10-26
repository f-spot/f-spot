//
// InternalProcess.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using GLib;

namespace FSpot.Imaging
{
	[Flags]
	enum InternalProcessFlags
	{
		LeaveDescriptorsOpen = 1 << 0,
		DoNotReapChild = 1 << 1,
		SearchPath = 1 << 2,
		StandardOutputToDevNull = 1 << 3,
		StandardErrorToDevNull = 1 << 4,
		ChildInheritsStandardInput = 1 << 5,
		FileAndArgvZero = 1 << 6
	}

	class InternalProcess
	{
		readonly int stdin;
		readonly int stdout;
		readonly IOChannel input;
		readonly IOChannel output;

		public IOChannel StandardInput {
			get {
				return input;
			}
		}

		public IOChannel StandardOutput {
			get {
				return output;
			}
		}

		[DllImport ("libglib-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern bool g_spawn_async_with_pipes (
			string workingDir,
			string[] argv,
			string[] envp,
			InternalProcessFlags flags,
			IntPtr childSetup,
			IntPtr childData,
			IntPtr pid,
			ref int stdin,
			ref int stdout,
			IntPtr err,
			//ref int stderr,
			out IntPtr error);

		public InternalProcess (string path, string[] args)
		{
			IntPtr error;

			if (args[args.Length - 1] != null) {
				var nargs = new string[args.Length + 1];
				Array.Copy (args, nargs, args.Length);
				args = nargs;
			}

			g_spawn_async_with_pipes (path, args, null, InternalProcessFlags.SearchPath,
				IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
				ref stdin, ref stdout, IntPtr.Zero, out error);

			if (error != IntPtr.Zero)
				throw new GException (error);

			input = new IOChannel (stdin);
			output = new IOChannel (stdout);
			//errorput = new IOChannel (stderr);
		}
	}
}
