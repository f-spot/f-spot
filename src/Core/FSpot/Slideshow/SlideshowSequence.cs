// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot.Slideshow
{
	public sealed class SlideshowSequence
	{
		readonly List<string> paths;
		int index;
		bool running = true;

		public SlideshowSequence (IReadOnlyList<string> paths, int startIndex = 0)
		{
			if (paths == null || paths.Count == 0)
				throw new ArgumentException ("Slideshow needs at least one photo", nameof (paths));
			this.paths = new List<string> (paths);
			index = Math.Min (Math.Max (0, startIndex), paths.Count - 1);
		}

		public string Current => paths.Count == 0 ? string.Empty : paths[index];
		public int Index => index;
		public int Count => paths.Count;
		public bool IsRunning => running;

		public bool Advance ()
		{
			if (!running)
				return false;
			index = (index + 1) % paths.Count;
			return true;
		}

		public bool Retreat ()
		{
			if (!running)
				return false;
			index = (index - 1 + paths.Count) % paths.Count;
			return true;
		}

		/// <summary>Jumps to <paramref name="target"/>, clamped to the sequence.</summary>
		public bool MoveTo (int target)
		{
			if (!running || paths.Count == 0)
				return false;
			int clamped = Math.Min (Math.Max (0, target), paths.Count - 1);
			if (clamped == index)
				return false;
			index = clamped;
			return true;
		}

		/// <summary>
		/// Drops <paramref name="path"/> even when the show has moved on. When the photo on screen is that
		/// path, that copy is the one removed; otherwise the first match goes. Returns false when the path
		/// is absent or nothing is left to show. Removing the current photo leaves the one that followed it.
		/// </summary>
		public bool Remove (string path)
		{
			if (paths.Count == 0 || path is null)
				return false;
			int at = string.Equals (paths[index], path, StringComparison.Ordinal)
				? index
				: paths.FindIndex (item => string.Equals (item, path, StringComparison.Ordinal));
			if (at < 0)
				return false;
			paths.RemoveAt (at);
			if (paths.Count == 0) {
				index = 0;
				running = false;
				return false;
			}
			if (at < index)
				index--;
			if (index >= paths.Count)
				index = paths.Count - 1;
			return true;
		}

		/// <summary>
		/// Drops the current photo (removed from the catalog while shown). Returns false when nothing is
		/// left to show; otherwise the photo that followed it becomes current.
		/// </summary>
		public bool RemoveCurrent ()
			=> paths.Count == 0 ? false : Remove (Current);

		/// <summary>
		/// Points <paramref name="path"/> at <paramref name="updated"/> without leaving the slide.
		/// The edited file can disappear while the original stays.
		/// </summary>
		public bool Replace (string path, string updated)
		{
			if (paths.Count == 0 || path is null || string.IsNullOrEmpty (updated))
				return false;
			int at = string.Equals (paths[index], path, StringComparison.Ordinal)
				? index
				: paths.FindIndex (item => string.Equals (item, path, StringComparison.Ordinal));
			if (at < 0)
				return false;
			paths[at] = updated;
			return true;
		}

		public void Stop ()
			=> running = false;
	}
}
