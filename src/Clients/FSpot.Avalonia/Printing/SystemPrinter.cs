// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using FSpot.Platform;

namespace FSpot.Avalonia.Printing;

// Hands a rendered PDF to the operating system's print spooler. macOS and Linux go
// through CUPS (lpstat / lp); Windows prints with the shell's "print" verb, which
// sends the file to the default printer through the registered PDF handler.
public static class SystemPrinter
{
	public static bool UsesCups => OperatingSystem.IsMacOS () || OperatingSystem.IsLinux () || OperatingSystem.IsFreeBSD ();

	// The CUPS destinations and the default one; empty when CUPS is unavailable.
	public static (IReadOnlyList<string> Printers, string? Default) Printers ()
	{
		if (!UsesCups)
			return ([], null);
		var printers = ParsePrinters (Run ("lpstat", ["-e"]) ?? string.Empty);
		string? fallback = ParseDefault (Run ("lpstat", ["-d"]) ?? string.Empty);
		return (printers, fallback);
	}

	// `lpstat -e` prints one destination per line.
	public static IReadOnlyList<string> ParsePrinters (string output)
		=> output.Split ('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Where (line => line.Length > 0 && !line.Contains (' '))
			.Distinct (StringComparer.Ordinal)
			.ToArray ();

	// `lpstat -d` prints "system default destination: NAME" (or "no system default destination").
	public static string? ParseDefault (string output)
	{
		int colon = output.IndexOf (':');
		if (colon < 0)
			return null;
		string name = output[(colon + 1)..].Trim ();
		return name.Length == 0 || name.Contains (' ') ? null : name;
	}

	public static IReadOnlyList<string> LpArguments (string pdfPath, string? printer, int copies, string? media, string title)
	{
		var args = new List<string> ();
		if (!string.IsNullOrEmpty (printer)) {
			args.Add ("-d");
			args.Add (printer);
		}
		args.Add ("-n");
		args.Add (Math.Max (1, copies).ToString (System.Globalization.CultureInfo.InvariantCulture));
		args.Add ("-t");
		args.Add (title);
		if (!string.IsNullOrEmpty (media)) {
			args.Add ("-o");
			args.Add ("media=" + media);
		}
		args.Add (pdfPath);
		return args;
	}

	// Submits the job; throws with the spooler's message when it refuses.
	public static async Task PrintAsync (string pdfPath, string? printer, int copies, string? media, string title)
	{
		if (UsesCups) {
			var info = new ProcessStartInfo (ExternalTool.Resolve ("lp")) {
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
			};
			foreach (string arg in LpArguments (pdfPath, printer, copies, media, title))
				info.ArgumentList.Add (arg);
			using var process = Process.Start (info) ?? throw new InvalidOperationException ("lp did not start");
			string error = await process.StandardError.ReadToEndAsync ();
			await process.WaitForExitAsync ();
			if (process.ExitCode != 0)
				throw new InvalidOperationException (string.IsNullOrWhiteSpace (error) ? $"lp exited with {process.ExitCode}" : error.Trim ());
			return;
		}

		if (OperatingSystem.IsWindows ()) {
			for (int i = 0; i < Math.Max (1, copies); i++) {
				using var process = Process.Start (new ProcessStartInfo (pdfPath) { UseShellExecute = true, Verb = "print" });
			}
			return;
		}

		throw new PlatformNotSupportedException ();
	}

	static string? Run (string file, IReadOnlyList<string> args)
	{
		try {
			file = ExternalTool.Resolve (file);
			var info = new ProcessStartInfo (file) {
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};
			foreach (string arg in args)
				info.ArgumentList.Add (arg);
			using var process = Process.Start (info);
			if (process is null)
				return null;
			string output = process.StandardOutput.ReadToEnd ();
			process.WaitForExit (5000);
			return output;
		} catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or FileNotFoundException) {
			return null;
		}
	}
}
