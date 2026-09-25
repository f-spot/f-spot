//
// AvaloniaUpdaterUI.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Threading;

using FSpot.Database;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

// Avalonia counterpart of FSpot.Gtk's UpdaterUI: shows a small, ownerless,
// indeterminate ProgressWindow while Updater.Run migrates the catalog, which
// (per Updater.Run) only happens when a pending update is marked slow.
//
// Show/Pulse/Destroy all run synchronously on the UI thread, before
// OnFrameworkInitializationCompleted hands control back to the dispatcher's
// own loop, so each pumps pending jobs with RunJobs to keep the window
// painting - the same role FSpot.Gtk's Application.RunIteration plays in
// ProgressDialog.Update.
sealed class AvaloniaUpdaterUI : IUpdaterUI
{
	ProgressWindow? window;

	public void Show ()
	{
		window = ProgressWindow.Create (Strings.UpdatingFSpotDatabase, total: 0, cancelCaption: null);
		window.Update (Strings.PleaseWaitFSpotGalleryDBUpdating);
		window.Show ();
		Dispatcher.UIThread.RunJobs ();
	}

	public void Pulse ()
		=> Dispatcher.UIThread.RunJobs ();

	public void Destroy ()
	{
		window?.Close ();
		window = null;
		Dispatcher.UIThread.RunJobs ();
	}
}
