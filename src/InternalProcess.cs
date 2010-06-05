using System;
using System.IO;
using System.Runtime.InteropServices;
using GLib;

namespace FSpot {
	[Flags]
	internal enum InternalProcessFlags {
		LeaveDescriptorsOpen =       1 << 0,
		DoNotReapChild =             1 << 1,
		SearchPath =                 1 << 2,
		StandardOutputToDevNull =    1 << 3,
		StandardErrorToDevNull =     1 << 4,
		ChildInheritsStandardInput = 1 << 5,
		FileAndArgvZero =            1 << 6
	}

	internal class InternalProcess {
		int stdin;
		int stdout;
		IOChannel input;
		IOChannel output;

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
		static extern bool g_spawn_async_with_pipes (string working_dir,
							     string [] argv,
							     string [] envp,
							     InternalProcessFlags flags,
							     IntPtr child_setup,
							     IntPtr child_data,
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
				string [] nargs = new string [args.Length + 1];
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
