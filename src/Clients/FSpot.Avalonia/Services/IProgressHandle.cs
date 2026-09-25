// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Services;

// Whether IAvaloniaShell.ShowProgress offers a Cancel/Stop button, matching
// FSpot.Gtk's ProgressDialog.CancelButtonType (its "Stop" caption is folded
// into Cancelable; callers that need the exact wording pass their own).
public enum ProgressCancelKind
{
	None,
	Cancelable,
}

// A running progress dialog, matching what FSpot.Gtk's ProgressDialog and
// ThreadProgressDialog offer a caller: advance/inspect via Update, an
// optional Retry/Skip wait per step, and Dispose to close it.
public interface IProgressHandle : IDisposable
{
	// Advances the count, updates the message, and returns whether the user
	// has asked to cancel since the previous call.
	bool Update (string message);

	// Shows Retry/Skip and waits for the user to pick one; true means Retry.
	Task<bool> PerformRetrySkipAsync ();
}

// The no-op IProgressHandle a shell falls back to when it doesn't show real
// UI (e.g. the NullShell test double), so callers can always Update/Dispose
// without null-checking.
public sealed class NullProgressHandle : IProgressHandle
{
	public static readonly NullProgressHandle Instance = new ();

	NullProgressHandle () { }

	public bool Update (string message) => false;

	public Task<bool> PerformRetrySkipAsync () => Task.FromResult (false);

	public void Dispose () { }
}
