//
// ThumbnailLoads.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Imaging;

// One token per photo. Releasing the photo, or starting its next load, cancels the
// decode that has not acquired the gate yet. A running decode cannot be aborted.
// The source stays alive after the wait returns while the cell still shows the photo,
// so a later release can cancel it. It is disposed once the wait has returned and
// the cell has released it. Disposing it before that makes Token throw
// ObjectDisposedException, which is not cancellation.
sealed class ThumbnailLoads
{
	CancellationTokenSource session = new ();
	readonly Dictionary<object, CancellationTokenSource> items = new ();
	readonly HashSet<CancellationTokenSource> waiting = new ();
	readonly HashSet<CancellationTokenSource> live = new ();
	readonly List<CancellationTokenSource> retiredSessions = new ();
	int open;

	public bool IsLoading (object item)
		=> items.ContainsKey (item);

	public bool IsWaiting (object item)
		=> items.TryGetValue (item, out var source) && waiting.Contains (source);

	public CancellationTokenSource Begin (object item)
	{
		End (item);
		var linked = CancellationTokenSource.CreateLinkedTokenSource (session.Token);
		items[item] = linked;
		waiting.Add (linked);
		live.Add (linked);
		open++;
		return linked;
	}

	public void End (object item)
	{
		if (!items.Remove (item, out var source))
			return;
		source.Cancel ();
		if (!waiting.Contains (source))
			Retire (source);
	}

	public void Finish (CancellationTokenSource source)
	{
		if (!waiting.Remove (source))
			return;
		if (Holds (source))
			return;
		Retire (source);
	}

	public void EndIfCurrent (object item, CancellationToken token)
	{
		if (!items.TryGetValue (item, out var source))
			return;
		if (source.Token != token)
			return;
		End (item);
	}

	public void EndAll ()
	{
		var previous = session;
		session = new CancellationTokenSource ();
		previous.Cancel ();
		var held = items.Values.ToArray ();
		items.Clear ();
		foreach (var source in held) {
			source.Cancel ();
			if (!waiting.Contains (source))
				Retire (source);
		}
		if (open == 0)
			previous.Dispose ();
		else
			retiredSessions.Add (previous);
	}

	bool Holds (CancellationTokenSource source)
	{
		foreach (var pair in items) {
			if (ReferenceEquals (pair.Value, source))
				return true;
		}
		return false;
	}

	void Retire (CancellationTokenSource source)
	{
		if (!live.Remove (source))
			return;
		source.Dispose ();
		open--;
		if (open == 0)
			DisposeRetiredSessions ();
	}

	void DisposeRetiredSessions ()
	{
		foreach (var source in retiredSessions)
			source.Dispose ();
		retiredSessions.Clear ();
	}
}
