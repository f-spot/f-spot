//
// ProcessStreamTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.UnitTest
{
	[TestClass]
	public class ProcessStreamTests
	{
		[TestMethod]
		public void Start_ReadsTheChildsStandardOutput ()
		{
			using var stream = ProcessStream.Start (Path.GetTempPath (), "dotnet", new[] { "--version" });
			using var reader = new StreamReader (stream);

			var output = reader.ReadToEnd ();

			Assert.IsFalse (string.IsNullOrWhiteSpace (output), "expected a version on stdout");
		}

		[TestMethod]
		[OSCondition (OperatingSystems.Linux | OperatingSystems.OSX)]
		public void Start_PassesArgumentsContainingSpacesThrough ()
		{
			var directory = Path.Combine (Path.GetTempPath (), "f-spot tests " + Guid.NewGuid ());
			Directory.CreateDirectory (directory);
			try {
				var file = Path.Combine (directory, "a raw file.txt");
				File.WriteAllText (file, "dcraw output", Encoding.ASCII);

				using var stream = ProcessStream.Start (directory, "/bin/cat", new[] { file });
				using var reader = new StreamReader (stream);

				Assert.AreEqual ("dcraw output", reader.ReadToEnd ());
			} finally {
				Directory.Delete (directory, true);
			}
		}

		[TestMethod]
		[OSCondition (OperatingSystems.Linux | OperatingSystems.OSX)]
		public void Dispose_DoesNotBlockOnAChildStillWriting ()
		{
			var stream = ProcessStream.Start (Path.GetTempPath (), "/bin/cat", new[] { "/dev/zero" });
			var buffer = new byte[64];
			Assert.IsGreaterThan (0, stream.Read (buffer, 0, buffer.Length), "the child should be producing output");

			var stopwatch = Stopwatch.StartNew ();
			stream.Dispose ();
			stopwatch.Stop ();

			Assert.IsLessThan (TimeSpan.FromSeconds (1), stopwatch.Elapsed,
				$"Dispose took {stopwatch.Elapsed}, so it is waiting on a child that never ends");
		}

		[TestMethod]
		[OSCondition (OperatingSystems.Linux | OperatingSystems.OSX)]
		public void Read_ThrowsWhenTheDecoderDiesMidStream ()
		{
			// EOF alone does not mean the decoder finished: a crash, an OOM kill or a
			// corrupt frame closes the pipe too, and GdkImageLoader reads a zero-length
			// read as a finished image and fires Completed. A partial frame must not be
			// presented as a complete load.
			using var stream = ProcessStream.Start (Path.GetTempPath (), "/bin/sh",
				new[] { "-c", "printf partial; exit 3" });
			using var reader = new StreamReader (stream);

			var exception = Assert.ThrowsExactly<IOException> (() => reader.ReadToEnd ());
			Assert.Contains ("3", exception.Message);
		}

		[TestMethod]
		[OSCondition (OperatingSystems.Linux | OperatingSystems.OSX)]
		public void Read_DoesNotThrowWhenTheChildExitsCleanly ()
		{
			using var stream = ProcessStream.Start (Path.GetTempPath (), "/bin/sh",
				new[] { "-c", "printf complete; exit 0" });
			using var reader = new StreamReader (stream);

			Assert.AreEqual ("complete", reader.ReadToEnd ());
		}

		[TestMethod]
		[OSCondition (OperatingSystems.Linux | OperatingSystems.OSX)]
		public void Dispose_DoesNotBlockOnAChildThatIsStillComputing ()
		{
			// dcraw -c demosaics the whole frame before writing a byte, so closing the read
			// end does not reach it: there is no EPIPE to notice while it is computing.
			// PhotoImageView disposes the loader on the GTK main thread on every photo
			// navigation, so any wait here is a visible freeze.
			var stream = ProcessStream.Start (Path.GetTempPath (), "/bin/sh",
				new[] { "-c", "sleep 10; echo done" });

			var stopwatch = Stopwatch.StartNew ();
			stream.Dispose ();
			stopwatch.Stop ();

			Assert.IsLessThan (TimeSpan.FromSeconds (1), stopwatch.Elapsed,
				$"Dispose blocked for {stopwatch.ElapsedMilliseconds}ms waiting on a child that never writes");
		}

		[TestMethod]
		public void Dispose_IsIdempotent ()
		{
			var stream = ProcessStream.Start (Path.GetTempPath (), "dotnet", new[] { "--version" });

			stream.Dispose ();
			stream.Dispose ();

			Assert.ThrowsExactly<ObjectDisposedException> (() => stream.ReadByte ());
		}

		[TestMethod]
		public void Start_ThrowsIOExceptionWhenTheProgramIsMissing ()
		{
			// The GLib spawn this replaced surfaced a failed exec as an IOException;
			// callers of OpenImageStream still expect an IO failure, not a Win32 one.
			Assert.ThrowsExactly<IOException> (
				() => ProcessStream.Start (Path.GetTempPath (), "f-spot-no-such-decoder", new[] { "-c" }));
		}

		[TestMethod]
		public void Start_IOExceptionNamesTheMissingProgram ()
		{
			var exception = Assert.ThrowsExactly<IOException> (
				() => ProcessStream.Start (Path.GetTempPath (), "f-spot-no-such-decoder", new[] { "-c" }));

			Assert.Contains ("f-spot-no-such-decoder", exception.Message);
			Assert.IsNotNull (exception.InnerException, "the original Win32Exception should be kept");
		}

		[TestMethod]
		public void Dispose_IsSafeWhenCalledConcurrently ()
		{
			var stream = ProcessStream.Start (Path.GetTempPath (), "dotnet", new[] { "--version" });

			Parallel.Invoke (() => stream.Dispose (), () => stream.Dispose (), () => stream.Dispose ());

			Assert.ThrowsExactly<ObjectDisposedException> (() => stream.ReadByte ());
		}

		[TestMethod]
		public void Stream_IsReadOnly ()
		{
			using var stream = ProcessStream.Start (Path.GetTempPath (), "dotnet", new[] { "--version" });

			Assert.IsTrue (stream.CanRead);
			Assert.IsFalse (stream.CanWrite);
			Assert.IsFalse (stream.CanSeek);
		}
	}
}
