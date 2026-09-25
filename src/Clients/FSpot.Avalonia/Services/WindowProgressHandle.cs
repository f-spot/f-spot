// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Services;

// Adapts a shown ProgressWindow to IAvaloniaShell.ShowProgress's handle.
sealed class WindowProgressHandle (ProgressWindow window) : IProgressHandle
{
	public bool Update (string message)
		=> window.Update (message);

	public Task<bool> PerformRetrySkipAsync ()
		=> window.PerformRetrySkipAsync ();

	public void Dispose ()
		=> window.Close ();
}
