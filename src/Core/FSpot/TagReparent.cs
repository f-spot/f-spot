// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;

namespace FSpot
{
	public static class TagReparent
	{
		public static bool Move (Tag tag, Category parent)
		{
			if (tag == null)
				throw new ArgumentNullException (nameof (tag));
			if (parent != null && (ReferenceEquals (tag, parent) || tag.IsAncestorOf (parent)))
				return false;
			tag.Category = parent;
			return true;
		}
	}
}
