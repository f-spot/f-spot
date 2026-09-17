// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Core;

namespace FSpot.Query
{
	public sealed class LibraryFind
	{
		public string Text { get; set; }
		public IReadOnlyList<Tag> Tags { get; set; } = Array.Empty<Tag> ();
		public bool RequireAllTags { get; set; }
		public bool Untagged { get; set; }
		public uint? MinRating { get; set; }
		public uint? MaxRating { get; set; }
		public DateTime? Start { get; set; }
		public DateTime? End { get; set; }
		public IReadOnlyList<uint> RollIds { get; set; } = Array.Empty<uint> ();
	}
}
