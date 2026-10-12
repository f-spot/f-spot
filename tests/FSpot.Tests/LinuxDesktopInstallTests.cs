//
// LinuxDesktopInstallTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	// The documented install is `dotnet msbuild LinuxDesktop.targets -t:InstallLinux`,
	// which does not go through build.proj's DependsOnTargets=Build. An empty bin/
	// glob is a silent no-op, then Prefix/bin/f-spot execs a missing libdir launcher.
	[TestClass]
	public class LinuxDesktopInstallTests
	{
		public TestContext TestContext { get; set; }

		readonly TemporaryDirectories temporaryDirectories = new TemporaryDirectories ();

		[TestCleanup]
		public void RemoveTemporaryDirectories ()
		{
			temporaryDirectories.DeleteAll ();
		}

		[TestMethod]
		[Timeout (60_000, CooperativeCancellation = true)]
		public async Task InstallLinux_WithoutFSpotExe_Fails ()
		{
			string output = temporaryDirectories.Create ("linux-out");
			string prefix = temporaryDirectories.Create ("linux-prefix");

			var result = await RunInstallLinuxAsync (output, prefix, TestContext.CancellationToken);

			Assert.AreNotEqual (0, result.ExitCode, result.Output);
			Assert.Contains ("f-spot.exe", result.Output);
		}

		[TestMethod]
		[Timeout (60_000, CooperativeCancellation = true)]
		public async Task InstallLinux_WithFSpotExe_CopiesItToLibdir ()
		{
			string output = temporaryDirectories.Create ("linux-out");
			string prefix = temporaryDirectories.Create ("linux-prefix");
			await File.WriteAllTextAsync (Path.Combine (output, "f-spot.exe"), "stub", TestContext.CancellationToken);

			var result = await RunInstallLinuxAsync (output, prefix, TestContext.CancellationToken);

			Assert.AreEqual (0, result.ExitCode, result.Output);
			Assert.IsTrue (File.Exists (Path.Combine (prefix, "lib", "f-spot", "f-spot.exe")));
		}

		static async Task<(int ExitCode, string Output)> RunInstallLinuxAsync (string outputDir, string prefix, CancellationToken cancellationToken)
		{
			var startInfo = new ProcessStartInfo ("dotnet") {
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};
			startInfo.ArgumentList.Add ("msbuild");
			startInfo.ArgumentList.Add (Path.Combine (RepoRoot (), "LinuxDesktop.targets"));
			startInfo.ArgumentList.Add ("-t:InstallLinux");
			startInfo.ArgumentList.Add ($"-p:FSpotOutputDir={outputDir}{Path.DirectorySeparatorChar}");
			startInfo.ArgumentList.Add ($"-p:Prefix={prefix}");
			startInfo.ArgumentList.Add ("-nologo");
			startInfo.ArgumentList.Add ("--disable-build-servers");

			using var process = Process.Start (startInfo);
			var output = new StringBuilder ();
			process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine (e.Data); };
			process.ErrorDataReceived += (_, e) => { if (e.Data != null) output.AppendLine (e.Data); };
			process.BeginOutputReadLine ();
			process.BeginErrorReadLine ();
			// The timed wait returns before the redirected lines are delivered.
			bool exited = process.WaitForExit (45_000);
			if (exited)
				await DrainChildOutputAsync (process, output, cancellationToken);
			Assert.IsTrue (exited, "InstallLinux timed out");
			return (process.ExitCode, output.ToString ());
		}

		// The timed wait can return before the redirected lines are delivered.
		// The parameterless wait has no limit, and a reused MSBuild node can
		// hold the pipes, so the drain stops after 10 seconds.
		static async Task DrainChildOutputAsync (Process process, StringBuilder log, CancellationToken cancellationToken)
		{
			using var timeout = new CancellationTokenSource (TimeSpan.FromSeconds (10));
			using var drain = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken, timeout.Token);
			bool drained = true;
			try {
				await process.WaitForExitAsync (drain.Token);
			} catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
				drained = false;
			}

			Assert.IsTrue (drained, "The child exited but its output was still open after 10 seconds" + Environment.NewLine + log);
		}

		static string RepoRoot ()
		{
			for (var dir = new DirectoryInfo (AppContext.BaseDirectory); dir != null; dir = dir.Parent) {
				if (File.Exists (Path.Combine (dir.FullName, "LinuxDesktop.targets")))
					return dir.FullName;
			}

			Assert.Fail ("LinuxDesktop.targets not found walking up from " + AppContext.BaseDirectory);
			return null;
		}
	}
}
