//
// ThumbnailLoads.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Imaging;

// One token per photo. Releasing the photo, or starting its next load, cancels the
// decode that has not acquired the gate yet. A running decode cannot be aborted.
sealed class ThumbnailLoads
{
	CancellationTokenSource session = new ();
	readonly Dictionary<object, CancellationTokenSource> items = new ();

	public bool IsLoading (object item)
		=> items.ContainsKey (item);

	public CancellationToken Begin (object item)
	{
		End (item);
		var linked = CancellationTokenSource.CreateLinkedTokenSource (session.Token);
		items[item] = linked;
		return linked.Token;
	}

	public void End (object item)
	{
		if (!items.Remove (item, out var source))
			return;
		source.Cancel ();
		source.Dispose ();
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
		session.Cancel ();
		session.Dispose ();
		session = new CancellationTokenSource ();
		foreach (var source in items.Values) {
			source.Cancel ();
			source.Dispose ();
		}
		items.Clear ();
	}
}
