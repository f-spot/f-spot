//
// InternalProcess.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
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

using System;
using System.Runtime.InteropServices;

using GLib;

namespace FSpot.Imaging
{
	[Flags]
	enum InternalProcessFlags
	{
		LeaveDescriptorsOpen =       1 << 0,
		DoNotReapChild =             1 << 1,
		SearchPath =                 1 << 2,
		StandardOutputToDevNull =    1 << 3,
		StandardErrorToDevNull =     1 << 4,
		ChildInheritsStandardInput = 1 << 5,
		FileAndArgvZero =            1 << 6
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

		[DllImport("libglib-2.0-0.dll")]
		static extern bool g_spawn_async_with_pipes (
			string workingDir,
			string [] argv,
			string [] envp,
			InternalProcessFlags flags,
			IntPtr childSetup,
			IntPtr childData,
			IntPtr pid,
			ref int stdin,
			ref int stdout,
			IntPtr err,
			//ref int stderr,
			out IntPtr error);

		public InternalProcess (string path, string [] args)
		{
			IntPtr error;

			if (args[args.Length -1] != null) {
				var nargs = new string [args.Length + 1];
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
