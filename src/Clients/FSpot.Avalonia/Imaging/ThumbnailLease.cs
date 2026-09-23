//
// ThumbnailLease.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Imaging;

/// <summary>
/// Reference count of realized containers. A photo stays held while any grid,
/// filmstrip, or import row still shows it, and is released when the last one clears.
/// </summary>
public sealed class ThumbnailLease
{
	readonly Dictionary<object, object> byContainer = new ();
	readonly Dictionary<object, int> holds = new ();

	public int HeldCount => holds.Count;

	public IReadOnlyCollection<object> Held => holds.Keys;

	public bool IsHeld (object item)
		=> holds.TryGetValue (item, out int count) && count > 0;

	public void Realize (object container, object item)
	{
		ArgumentNullException.ThrowIfNull (container);
		ArgumentNullException.ThrowIfNull (item);
		if (byContainer.TryGetValue (container, out var previous)) {
			if (ReferenceEquals (previous, item))
				return;
			Release (container);
		}

		byContainer[container] = item;
		holds.TryGetValue (item, out int count);
		holds[item] = count + 1;
	}

	public object? Release (object container)
	{
		if (!byContainer.Remove (container, out var item))
			return null;
		if (!holds.TryGetValue (item, out int count))
			return null;
		if (count <= 1)
			holds.Remove (item);
		else
			holds[item] = count - 1;
		return count <= 1 ? item : null;
	}

	public void Clear ()
	{
		byContainer.Clear ();
		holds.Clear ();
	}
}
