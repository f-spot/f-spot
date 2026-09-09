//
// LinuxDesktopInstallTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	// The documented install is `dotnet msbuild LinuxDesktop.targets -t:InstallLinux`,
	// which does not go through build.proj's DependsOnTargets=Build. An empty bin/
	// glob is a silent no-op, then Prefix/bin/f-spot execs a missing libdir launcher.
	[TestClass]
	public class LinuxDesktopInstallTests
	{
		readonly TemporaryDirectories temporaryDirectories = new TemporaryDirectories ();

		[TestCleanup]
		public void RemoveTemporaryDirectories ()
		{
			temporaryDirectories.DeleteAll ();
		}

		[TestMethod]
		[Timeout (60_000, CooperativeCancellation = true)]
		public void InstallLinux_WithoutFSpotExe_Fails ()
		{
			string output = temporaryDirectories.Create ("linux-out");
			string prefix = temporaryDirectories.Create ("linux-prefix");

			var result = RunInstallLinux (output, prefix);

			Assert.AreNotEqual (0, result.ExitCode, result.Output);
			Assert.Contains ("f-spot.exe", result.Output);
		}

		[TestMethod]
		[Timeout (60_000, CooperativeCancellation = true)]
		public void InstallLinux_WithFSpotExe_CopiesItToLibdir ()
		{
			string output = temporaryDirectories.Create ("linux-out");
			string prefix = temporaryDirectories.Create ("linux-prefix");
			File.WriteAllText (Path.Combine (output, "f-spot.exe"), "stub");

			var result = RunInstallLinux (output, prefix);

			Assert.AreEqual (0, result.ExitCode, result.Output);
			Assert.IsTrue (File.Exists (Path.Combine (prefix, "lib", "f-spot", "f-spot.exe")));
		}

		static (int ExitCode, string Output) RunInstallLinux (string outputDir, string prefix)
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

			using var process = Process.Start (startInfo);
			var output = new StringBuilder ();
			process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine (e.Data); };
			process.ErrorDataReceived += (_, e) => { if (e.Data != null) output.AppendLine (e.Data); };
			process.BeginOutputReadLine ();
			process.BeginErrorReadLine ();
			Assert.IsTrue (process.WaitForExit (60_000), "InstallLinux timed out");
			return (process.ExitCode, output.ToString ());
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
