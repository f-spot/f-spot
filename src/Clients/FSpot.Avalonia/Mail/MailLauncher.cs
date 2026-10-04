// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

namespace FSpot.Avalonia.Mail;

// How SendEmail.HandleResponse starts the mail client: a known composer command
// line, or a mailto: URI with attach= parameters.
public sealed record MailLaunch (string? FileName, IReadOnlyList<string> Arguments, string? Uri, string? RevealFolder = null);

public static class MailLauncher
{
	// Builds the launch for Preferences.MailToCommand. An empty command means the
	// platform's own mail client: Apple Mail takes the files directly on macOS;
	// Windows has no mailto attachment support, so the message is opened with the
	// prepared folder shown next to it; elsewhere mailto: carries attach= (GTK's default).
	public static MailLaunch Build (string? command, string subject, IReadOnlyList<string> attachments,
		string? attachmentFolder = null, bool? isMac = null, bool? isWindows = null)
	{
		command = command?.Trim () ?? string.Empty;
		bool mac = isMac ?? OperatingSystem.IsMacOS ();
		bool windows = isWindows ?? OperatingSystem.IsWindows ();

		switch (command) {
		case "thunderbird %s":
		case "icedove %s":
		case "mozilla-thunderbird %s":
			return new MailLaunch (command[..command.IndexOf (' ')],
				["-compose", $"subject={subject},attachment='{string.Join (",", attachments)}'"], null);
		case "seamonkey -mail -compose %s":
			return new MailLaunch ("seamonkey",
				["-mail", "-compose", $"subject={subject},attachment='{string.Join (",", attachments)}'"], null);
		case "kmail %s": {
				var args = new List<string> { "--composer", "--subject", subject };
				foreach (string file in attachments) {
					args.Add ("--attach");
					args.Add (file);
				}
				return new MailLaunch ("kmail", args, null);
			}
		case "evolution %s":
			// Evolution does not url-decode the subject.
			return new MailLaunch (null, [], "mailto:?subject=" + subject + AttachQuery (attachments));
		}

		if (command.Length == 0 && mac)
			return new MailLaunch ("open", ["-a", "Mail", .. attachments], null);
		if (command.Length == 0 && windows)
			return new MailLaunch (null, [], "mailto:?subject=" + Uri.EscapeDataString (subject), attachmentFolder);
		return new MailLaunch (null, [], "mailto:?subject=" + Uri.EscapeDataString (subject) + AttachQuery (attachments));
	}

	static string AttachQuery (IReadOnlyList<string> attachments)
	{
		var query = new StringBuilder ();
		foreach (string file in attachments)
			query.Append ("&attach=").Append (FileUri.FromPath (file));
		return query.ToString ();
	}

	public static void Start (MailLaunch launch)
	{
		if (launch.FileName is not null) {
			var info = new ProcessStartInfo (launch.FileName) { UseShellExecute = false };
			foreach (string arg in launch.Arguments)
				info.ArgumentList.Add (arg);
			using var _ = Process.Start (info) ?? throw new InvalidOperationException ($"{launch.FileName} did not start");
		} else if (launch.Uri is not null) {
			using var _ = Process.Start (new ProcessStartInfo (launch.Uri) { UseShellExecute = true });
		}
		if (launch.RevealFolder is not null)
			using (Process.Start (new ProcessStartInfo (launch.RevealFolder) { UseShellExecute = true })) { }
	}
}
