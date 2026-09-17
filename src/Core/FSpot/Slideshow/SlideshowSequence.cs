// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot.Slideshow
{
	public sealed class SlideshowSequence
	{
		readonly IReadOnlyList<string> paths;
		int index;
		bool running = true;

		public SlideshowSequence (IReadOnlyList<string> paths, int startIndex = 0)
		{
			if (paths == null || paths.Count == 0)
				throw new ArgumentException ("Slideshow needs at least one photo", nameof (paths));
			this.paths = paths;
			index = Math.Min (Math.Max (0, startIndex), paths.Count - 1);
		}

		public string Current => paths[index];
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

		public void Stop ()
			=> running = false;
	}
}
