// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;

using FSpot.Resources.Lang;
using FSpot.Settings;

namespace FSpot.Avalonia.Mail;

// SendEmail end to end: the Create Mail dialog, the "Preparing email" progress
// while attachments are written, then the mail client.
public static class MailSender
{
	public static async Task SendAsync (Window owner, IReadOnlyList<string> paths)
	{
		if (paths.Count == 0)
			return;
		var dialog = new MailWindow (paths);
		await dialog.ShowDialog (owner);
		if (dialog.Result is not int index)
			return;

		int size = MailSizing.Sizes[index];
		string directory = MailAttachments.CreateTempDirectory ();
		var attachments = new List<string> ();
		var progress = new ProgressWindow (Strings.PreparingEmail, paths.Count);
		progress.Show (owner);
		(string Name, string Message)? failure = null;
		try {
			foreach (string path in paths) {
				string name = Path.GetFileName (path);
				if (progress.Update (string.Format (Strings.ExportingPhotoX, name)))
					return;
				try {
					attachments.Add (await Task.Run (() => MailAttachments.Prepare (path, size, directory)));
				} catch (Exception e) {
					Logger.Log.Error ($"Error preparing {name}: {e.Message}");
					failure = (name, e.Message);
					break;
				}
				if (progress.StopRequested.IsCancellationRequested)
					return;
			}
		} finally {
			progress.Close ();
		}

		if (failure is { } failed) {
			await MessageDialog.ShowAsync (owner, Strings.ErrorProcessingImage,
				string.Format (Strings.AnErrorOccurredWhileProcessingXMessage, failed.Name, failed.Message), MessageDialogKind.Error);
			return;
		}

		try {
			var launch = MailLauncher.Build (Preferences.Get<string> (Preferences.MailToCommand), Strings.MyPhotos, attachments, directory);
			MailLauncher.Start (launch);
		} catch (Exception e) {
			Logger.Log.Error (e, "Unable to open the mail client");
			await MessageDialog.ShowAsync (owner, GtkMnemonic.Display (Strings.SendMail),
				string.Format (Strings.MailClientFailedXMessage, e.Message), MessageDialogKind.Error);
		}
	}
}
