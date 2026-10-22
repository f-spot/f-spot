// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

using FSpot.Platform;

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
				["-compose", ComposeFields (subject, attachments)], null);
		case "seamonkey -mail -compose %s":
			return new MailLaunch ("seamonkey",
				["-mail", "-compose", ComposeFields (subject, attachments)], null);
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

	// A quote or a comma is syntax in Thunderbird's compose string. A photo named
	// a.jpg',body=pwned would add a body field. The attachment is renamed before this.
	static string ComposeFields (string subject, IReadOnlyList<string> attachments)
	{
		foreach (string file in attachments) {
			if (file.IndexOfAny (['\'', ',', '\r', '\n']) >= 0)
				throw new ArgumentException ("An attachment path cannot contain a comma or a quote.", nameof (attachments));
		}
		return $"subject={SanitizeComposeField (subject)},attachment='{string.Join (",", attachments)}'";
	}

	// A comma or a quote in the subject is the same syntax as in an attachment name.
	// The English subject has neither. A translation might. Replacing them keeps the
	// message one subject field instead of failing the send.
	static string SanitizeComposeField (string value)
	{
		var buffer = new StringBuilder (value.Length);
		foreach (char c in value) {
			if (c is '\'' or ',' or '\r' or '\n')
				buffer.Append ('_');
			else
				buffer.Append (c);
		}
		return buffer.ToString ();
	}

	static string AttachQuery (IReadOnlyList<string> attachments)
	{
		var query = new StringBuilder ();
		foreach (string file in attachments)
			query.Append ("&attach=").Append (FileUri.FromPath (file));
		return query.ToString ();
	}

	internal static Action<ProcessStartInfo> StartProcess = DefaultStart;

	public static void Start (MailLaunch launch)
	{
		if (launch.FileName is not null) {
			string file = ExternalTool.Resolve (launch.FileName);
			var info = new ProcessStartInfo (file) { UseShellExecute = false };
			foreach (string arg in launch.Arguments)
				info.ArgumentList.Add (arg);
			StartProcess (info);
		} else if (launch.Uri is not null) {
			using var _ = Process.Start (new ProcessStartInfo (launch.Uri) { UseShellExecute = true });
		}
		if (launch.RevealFolder is not null)
			using (Process.Start (new ProcessStartInfo (launch.RevealFolder) { UseShellExecute = true })) { }
	}

	static void DefaultStart (ProcessStartInfo info)
	{
		using var process = Process.Start (info) ?? throw new InvalidOperationException ($"{info.FileName} did not start");
	}
}
