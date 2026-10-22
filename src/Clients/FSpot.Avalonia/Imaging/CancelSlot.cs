// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Imaging;

// A newer request cancels the previous source and leaves it for the task that
// is still awaiting that token. That task disposes it. Disposing here makes the
// waiter throw ObjectDisposedException, which is not cancellation.
static class CancelSlot
{
	public static CancellationTokenSource Renew (ref CancellationTokenSource? slot)
	{
		var previous = slot;
		var next = new CancellationTokenSource ();
		slot = next;
		previous?.Cancel ();
		return next;
	}

	public static CancellationTokenSource RenewLinked (ref CancellationTokenSource? slot, CancellationToken parent)
	{
		var previous = slot;
		var next = CancellationTokenSource.CreateLinkedTokenSource (parent);
		slot = next;
		previous?.Cancel ();
		return next;
	}

	public static void Finish (ref CancellationTokenSource? slot, CancellationTokenSource source)
	{
		if (ReferenceEquals (slot, source))
			slot = null;
		source.Dispose ();
	}
}

// One source shared by every load in a window. Release cancels it. The last
// load to leave disposes it, so a load still inside its wait does not see a
// disposed token.
sealed class SharedCancel
{
	readonly object gate = new ();
	CancellationTokenSource source = new ();
	int users;
	bool released;

	public bool IsDisposed { get; private set; }

	public CancellationToken Enter ()
	{
		lock (gate) {
			if (released)
				return new CancellationToken (canceled: true);
			users++;
			return source.Token;
		}
	}

	public void Leave ()
	{
		lock (gate) {
			if (users == 0)
				return;
			users--;
			DisposeIfIdle ();
		}
	}

	public void Release ()
	{
		lock (gate) {
			if (released)
				return;
			released = true;
			source.Cancel ();
			DisposeIfIdle ();
		}
	}

	void DisposeIfIdle ()
	{
		if (!released || users != 0 || IsDisposed)
			return;
		source.Dispose ();
		IsDisposed = true;
	}
}
