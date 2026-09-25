// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Services;

// The async equivalent of the AutoResetEvent wait in FSpot.Gtk's
// ThreadProgressDialog.PerformRetrySkip: a caller awaits Task, and whichever
// of Retry/Skip fires first resolves it (later calls are ignored).
public sealed class RetrySkipGate
{
	readonly TaskCompletionSource<bool> completion = new (TaskCreationOptions.RunContinuationsAsynchronously);

	public Task<bool> Task => completion.Task;

	public void Resolve (bool retry)
		=> completion.TrySetResult (retry);
}
