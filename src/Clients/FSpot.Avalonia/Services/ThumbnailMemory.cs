// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace FSpot.Avalonia.Services;

// Owns the decoded grid bitmaps. Callers get a copy they can dispose.
// The master stays until it is evicted or the catalog invalidates it.
sealed class ThumbnailMemory
{
	const int MaxEntries = 512;

	readonly object gate = new ();
	readonly Dictionary<Key, Bitmap> bitmaps = new ();
	readonly List<Key> order = new ();

	readonly record struct Key (string Path, int Size, bool Color);

	public Bitmap? Copy (string path, int size, bool color)
	{
		if (!IsGridSize (size) || string.IsNullOrEmpty (path))
			return null;

		lock (gate) {
			var key = new Key (path, size, color);
			if (!bitmaps.TryGetValue (key, out Bitmap? bitmap))
				return null;
			order.Remove (key);
			order.Add (key);
			try {
				return Clone (bitmap);
			} catch (Exception) {
				return null;
			}
		}
	}

	public void Remember (string path, int size, bool color, Bitmap bitmap)
	{
		if (!IsGridSize (size) || string.IsNullOrEmpty (path) || bitmap is null)
			return;

		Bitmap master;
		try {
			master = Clone (bitmap);
		} catch (Exception) {
			return;
		}

		if (ReferenceEquals (master, bitmap))
			return;

		lock (gate) {
			var key = new Key (path, size, color);
			if (bitmaps.Remove (key, out Bitmap? previous)) {
				order.Remove (key);
				previous.Dispose ();
			}

			while (bitmaps.Count >= MaxEntries && order.Count > 0) {
				Key oldest = order[0];
				order.RemoveAt (0);
				if (bitmaps.Remove (oldest, out Bitmap? evicted))
					evicted.Dispose ();
			}

			bitmaps[key] = master;
			order.Add (key);
		}
	}

	public void Invalidate (string? path)
	{
		lock (gate) {
			if (path is null) {
				foreach (Bitmap bitmap in bitmaps.Values)
					bitmap.Dispose ();
				bitmaps.Clear ();
				order.Clear ();
				return;
			}

			if (path.Length == 0)
				return;

			var drop = new List<Key> ();
			foreach (Key key in bitmaps.Keys) {
				if (key.Path == path)
					drop.Add (key);
			}

			foreach (Key key in drop) {
				order.Remove (key);
				if (bitmaps.Remove (key, out Bitmap? bitmap))
					bitmap.Dispose ();
			}
		}
	}

	static bool IsGridSize (int size)
		=> size >= 1 && size <= 256;

	static Bitmap Clone (Bitmap source)
	{
		int width = source.PixelSize.Width;
		int height = source.PixelSize.Height;
		if (width < 1 || height < 1)
			return source;

		var clone = new WriteableBitmap (
			source.PixelSize,
			source.Dpi,
			PixelFormat.Bgra8888,
			source.AlphaFormat ?? AlphaFormat.Premul);
		// CopyPixels into the locked buffer transcodes. A raw copy of an Rgba8888
		// decode into this Bgra8888 bitmap swaps red and blue.
		using (ILockedFramebuffer locked = clone.Lock ())
			source.CopyPixels (locked);
		return clone;
	}
}
