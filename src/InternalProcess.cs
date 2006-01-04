using System;
using System.IO;
using System.Runtime.InteropServices;
using GLib;

namespace FSpot {
	internal class InternalProcess {
		int stdin;
		int stdout;
		int stderr;
		IOChannel input;
		IOChannel output;
		IOChannel errorput;

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

		public IOChannel StandardError {
			get {
				return errorput;
			}
		}

		[DllImport("libglib-2.0-0.dll")]
		static extern bool g_spawn_async_with_pipes (string working_dir,
							     string [] argv,
							     string [] envp,
							     int flags,
							     IntPtr child_setup,
							     IntPtr child_data,
							     IntPtr pid,
							     ref int stdin,
							     ref int stdout,
							     ref int stderr,
							     out IntPtr error);
		
		public InternalProcess (string path, string [] args)
		{
			IntPtr error;

			if (args[args.Length -1] != null) {
				string [] nargs = new string [args.Length + 1];
				Array.Copy (args, nargs, args.Length);
				args = nargs;
			}
			
			g_spawn_async_with_pipes (path, args, null, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
						  ref stdin, ref stdout, ref stderr, out error);

			if (error != IntPtr.Zero)
				throw new GException (error);

			input = new IOChannel (stdin);
			output = new IOChannel (stdout);
			errorput = new IOChannel (stderr);
		}
	}
}
