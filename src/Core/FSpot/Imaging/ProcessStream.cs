//
// ProcessStream.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace FSpot.Imaging
{
	// A child process's standard output, as a Stream that owns the child. Disposing the
	// stream closes the pipe and reaps the process, so callers can treat a spawned
	// decoder exactly like a file they opened.
	sealed class ProcessStream : Stream
	{
		const int ExitTimeoutMs = 2000;

		readonly string fileName;
		readonly Process process;
		readonly Stream output;

		// Read on the thread pool (Stream's default BeginRead) while Close runs on
		// another, so this needs a barrier rather than a plain bool.
		int disposed;

		bool Disposed => Volatile.Read (ref disposed) != 0;

		ProcessStream (Process process, string fileName)
		{
			this.process = process;
			this.fileName = fileName;
			output = process.StandardOutput.BaseStream;
		}

		internal static void AddArguments (ProcessStartInfo startInfo, string[] arguments)
		{
			foreach (var argument in arguments) {
				if (argument == null)
					throw new ArgumentException ("argv holds a null", nameof (arguments));
				startInfo.ArgumentList.Add (argument);
			}
		}

		public static Stream Start (string workingDirectory, string fileName, string[] arguments)
		{
			var startInfo = new ProcessStartInfo (fileName) {
				WorkingDirectory = workingDirectory,
				RedirectStandardOutput = true,
				RedirectStandardInput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				// Standard error is deliberately not redirected: a pipe nobody drains
				// would deadlock a decoder that writes more warnings than fit in it.
			};
			AddArguments (startInfo, arguments);

			Process process;
			try {
				process = Process.Start (startInfo);
			} catch (Win32Exception e) {
				// Callers open images; a missing or unrunnable decoder is an IO failure to
				// them, which is also what the GLib spawn this replaced used to raise.
				throw new IOException ($"Could not start \"{fileName}\": {e.Message}", e);
			}

			if (process == null)
				throw new IOException ($"Could not start \"{fileName}\": no process was started.");

			try {
				// Hand the child EOF immediately. It has no use for our stdin and would
				// otherwise inherit the console's, where a stray read would block it.
				process.StandardInput.Close ();
			} catch (IOException) {
				// The child exited already, so it has its EOF whether we close or not.
			} catch {
				// Anything else and we are about to throw while still owning the child;
				// let go of it here, or nothing ever will.
				process.Dispose ();
				throw;
			}

			return new ProcessStream (process, fileName);
		}

		public override bool CanRead => !Disposed;
		public override bool CanSeek => false;
		public override bool CanWrite => false;

		public override long Length => throw NoSeeking ();

		public override long Position {
			get => throw NoSeeking ();
			set => throw NoSeeking ();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			if (Disposed)
				throw new ObjectDisposedException (nameof (ProcessStream));

			int read = output.Read (buffer, offset, count);

			// EOF is not proof the decoder finished: a crash, an OOM kill or a corrupt
			// frame closes the pipe just the same, and GdkImageLoader reads a
			// zero-length read as a finished image. Ask how it ended before agreeing.
			if (read == 0 && process.WaitForExit (ExitTimeoutMs) && process.ExitCode != 0)
				throw new IOException ($"\"{fileName}\" exited with status {process.ExitCode}");

			return read;
		}

		public override void Flush ()
		{
		}

		public override void Write (byte[] buffer, int offset, int count) => throw ReadOnly ();

		public override void SetLength (long value) => throw ReadOnly ();

		public override long Seek (long offset, SeekOrigin origin) => throw NoSeeking ();

		static NotSupportedException ReadOnly () => new NotSupportedException ("ProcessStream is read-only");

		static NotSupportedException NoSeeking () => new NotSupportedException ("ProcessStream doesn't support seeking");

		protected override void Dispose (bool disposing)
		{
			if (disposing && Interlocked.Exchange (ref disposed, 1) == 0) {
				// Closing the read end is not enough on its own: dcraw -c demosaics the
				// whole frame before writing a byte, so a decoder that is still computing
				// never observes the EPIPE. Its output is already discarded, which makes
				// its exit code worthless to us, so end it now instead of blocking the
				// caller -- PhotoImageView disposes on the GTK main thread on every photo
				// navigation, where a wait is a visible freeze.
				output.Dispose ();

				try {
					// Not guarded by HasExited: that is check-then-act, and the child can
					// exit in the window. The catch is what actually closes it.
					process.Kill ();
				} catch (InvalidOperationException) {
					// Already gone; nothing left to reap.
				} catch (Win32Exception) {
					// Kill lost a race with the child exiting on its own.
				} finally {
					process.Dispose ();
				}
			}

			base.Dispose (disposing);
		}
	}
}
