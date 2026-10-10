//
// DllMapTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Gtk.Tests
{
	// Mono resolves f-spot.exe's native imports through these dllmaps. An entry with no
	// os attribute applies everywhere, so a Linux .so target with no attribute hands
	// macOS a library that cannot exist -- which is how three entries here were left
	// behind when their siblings gained quartz targets. Written as a sweep over the
	// whole file rather than a check of the three, so the next one is caught too.
	[TestClass]
	public class DllMapTests
	{
		public TestContext TestContext { get; set; }

		[TestMethod]
		public void EveryElfTargetIsExcludedOnMacOs ()
		{
			var ungated = DllMaps ()
				.Where (map => IsElf ((string)map.Attribute ("target")))
				.Where (map => !ExcludesMacOs ((string)map.Attribute ("os")))
				.Select (map => $"{(string)map.Attribute ("dll")} -> {(string)map.Attribute ("target")}")
				.OrderBy (name => name, StringComparer.Ordinal)
				.ToList ();

			Assert.IsEmpty (ungated, "these map to an ELF library on macOS: " + string.Join (", ", ungated));
		}

		// SDK GenerateBindingRedirects rewrites $(TargetFileName).config and drops
		// Mono dllmap entries unless the hand-written file is the AppConfig input.
		[TestMethod]
		[Timeout (120_000, CooperativeCancellation = true)]
		public async Task BuiltExeConfig_KeepsGlibDllMap ()
		{
			string root = RepoRoot ();
			string output = Path.Combine (Path.GetTempPath (), "f-spot-dllmap-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (output);
			try {
				var startInfo = new ProcessStartInfo ("dotnet") {
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
				};
				startInfo.ArgumentList.Add ("build");
				startInfo.ArgumentList.Add (Path.Combine (root, "src", "Clients", "FSpot.Gtk", "FSpot.Gtk.csproj"));
				startInfo.ArgumentList.Add ("--configuration");
				startInfo.ArgumentList.Add ("Release");
				startInfo.ArgumentList.Add ("--disable-build-servers");
				startInfo.ArgumentList.Add ($"-p:OutputPath={output}{Path.DirectorySeparatorChar}");

				using var process = Process.Start (startInfo);
				var log = new StringBuilder ();
				process.OutputDataReceived += (_, e) => { if (e.Data != null) log.AppendLine (e.Data); };
				process.ErrorDataReceived += (_, e) => { if (e.Data != null) log.AppendLine (e.Data); };
				process.BeginOutputReadLine ();
				process.BeginErrorReadLine ();
				// The timed wait returns before the redirected lines are delivered.
				bool exited = process.WaitForExit (120_000);
				if (exited)
					await DrainChildOutputAsync (process);
				Assert.IsTrue (exited, "Gtk build timed out");
				Assert.AreEqual (0, process.ExitCode, log.ToString ());

				string config = await File.ReadAllTextAsync (Path.Combine (output, "f-spot.exe.config"), TestContext.CancellationToken);
				Assert.Contains ("dllmap", config);
				Assert.Contains ("libglib-2.0-0.dll", config);
			} finally {
				try {
					Directory.Delete (output, true);
				} catch (IOException) {
				}
			}
		}

		// The timed wait can return before the redirected lines are delivered.
		// The parameterless wait has no limit, and a reused MSBuild node can
		// hold the pipes, so the drain stops after 10 seconds.
		static async Task DrainChildOutputAsync (Process process)
		{
			using var drain = new CancellationTokenSource (TimeSpan.FromSeconds (10));
			bool drained = true;
			try {
				await process.WaitForExitAsync (drain.Token);
			} catch (OperationCanceledException) {
				drained = false;
			}

			Assert.IsTrue (drained, "The child exited but its output was still open after 10 seconds");
		}

		static IEnumerable<XElement> DllMaps ()
		{
			string path = Path.Combine (AppContext.BaseDirectory, "f-spot.exe.config");
			Assert.IsTrue (File.Exists (path), $"{path} is missing; the build should copy f-spot.exe.config next to the testhost.");

			return XDocument.Load (path).Descendants ("dllmap");
		}

		static string RepoRoot ()
		{
			for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
				if (File.Exists (Path.Combine (dir.FullName, "F-Spot.slnx")))
					return dir.FullName;
			}

			Assert.Fail ("F-Spot.slnx not found walking up from " + AppContext.BaseDirectory);
			return null;
		}

		static bool IsElf (string target) =>
			target != null && (target.EndsWith (".so", StringComparison.Ordinal) || target.Contains (".so."));

		// Mono reads os as a set, negated as a whole when it starts with "!".
		static bool ExcludesMacOs (string os)
		{
			if (string.IsNullOrEmpty (os))
				return false;

			bool negated = os[0] == '!';
			bool listed = os.TrimStart ('!').Split (',').Any (name => name.Trim () == "osx");

			return negated ? listed : !listed;
		}
	}
}
