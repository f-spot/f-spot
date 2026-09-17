// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Utils
{
	// IconTheme.LoadIcon throws when gtk-missing-image is absent; GetThumbnail
	// returns null or throws on files GDK cannot decode. Draw paths need an image
	// either way, never null.
	public static class LoadOrPlaceholder
	{
		public static T Resolve<T> (int size, Func<int, T> placeholder, params Func<T>[] attempts)
			where T : class
		{
			if (attempts != null) {
				foreach (var attempt in attempts) {
					if (attempt == null)
						continue;
					try {
						var value = attempt ();
						if (value != null)
							return value;
					} catch (Exception) {
					}
				}
			}

			return placeholder (Math.Max (1, size));
		}
	}
}
