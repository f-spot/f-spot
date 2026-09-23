// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot
{
	public static class TagTyping
	{
		public static IReadOnlyList<string> Names (string text)
		{
			if (string.IsNullOrWhiteSpace (text))
				return Array.Empty<string> ();
			var names = new List<string> ();
			foreach (string part in text.Split (new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)) {
				string name = part.Trim ();
				if (name.Length > 0)
					names.Add (name);
			}
			return names;
		}
	}
}
