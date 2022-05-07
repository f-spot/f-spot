// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Models
{
	public class OldTag : IConvert
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public long CategoryId { get; set; }
		public bool IsCategory { get; set; }
		public long SortPriority { get; set; }
		public string Icon { get; set; }

		public object Convert ()
		{
			return new Tag {
				OldId = Id,
				Name = Name,
				OldCategoryId = CategoryId,
				IsCategory = IsCategory,
				SortPriority = SortPriority,
				Icon = Icon
			};
		}
	}
}
