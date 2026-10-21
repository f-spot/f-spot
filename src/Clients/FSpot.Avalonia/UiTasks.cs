// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia;

// Event handlers cannot await. A fault after the first await would otherwise
// never reach the caller.
static class UiTasks
{
	public static void Forget (Task? task)
	{
		if (task is null || task.IsCompletedSuccessfully)
			return;
		if (task.IsCompleted) {
			Report (task);
			return;
		}

		// The continuation only logs. Observing it again would loop.
#pragma warning disable VSTHRD110
		task.ContinueWith (
			Report,
			CancellationToken.None,
			TaskContinuationOptions.ExecuteSynchronously,
			TaskScheduler.Default);
#pragma warning restore VSTHRD110
	}

	static void Report (Task task)
	{
		if (!task.IsFaulted)
			return;
		Logger.Log.Error (task.Exception!.GetBaseException (), "Background UI work failed");
	}
}
