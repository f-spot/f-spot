// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

using FSpot.Core;
using FSpot.Settings;

namespace FSpot.Models
{
	public partial class Tag : BaseDbSet, IComparable<Tag>
	{
		[NotMapped]
		public long OldId { get; set; }
		[NotMapped]
		public long OldCategoryId { get; set; }
		public string Name { get; set; }
		public Guid CategoryId { get; set; }
		public bool IsCategory { get; set; }
		public long SortPriority { get; set; }
		public string Icon { get; set; }


		[NotMapped]
		List<Tag> children = new List<Tag> ();

		[NotMapped]
		bool childrenNeedSort;

		[NotMapped]
		public int Popularity { get; set; }

		Tag category;
		[NotMapped]
		public Tag Category {
			get { return category; }
			set {
				Category?.RemoveChild (this);

				category = value;
				category?.AddChild (this);
			}
		}

		[NotMapped]
		public List<Tag> Children {
			get {
				if (childrenNeedSort)
					children.Sort ();

				return children;
			}
			set {
				children = new List<Tag> (value);
				childrenNeedSort = true;
			}
		}

		[NotMapped]
		public bool IconWasCleared { get; set; }

		// Icon.  If ThemeIconName is not null, then we save the name of the icon instead
		// of the actual icon data.
		[NotMapped]
		public string ThemeIconName { get; set; }

		[NotMapped]
		public static IconSize TagIconSize { get; set; } = IconSize.Large;

		public Tag ()
		{
			Popularity = 0;
			IconWasCleared = false;
			TagIcon = new TagIcon (this);
			TagIcon.SetIconFromString ();
		}

		public Tag (Tag category) : this ()
		{
			Category = category;
		}

		public Tag (Tag category, Guid id, string name) : this ()
		{
			Category = category;
			Id = id;
			Name = name;
			Children = new List<Tag> ();
		}

		[NotMapped]
		public TagIcon TagIcon { get; }

		public int CompareTo (Tag otherTag)
		{
			if (otherTag == null)
				throw new ArgumentNullException (nameof (otherTag));

			if (Category == otherTag.Category) {
				if (SortPriority == otherTag.SortPriority)
					return string.Compare (Name, otherTag.Name, StringComparison.OrdinalIgnoreCase);

				return (int)(SortPriority - otherTag.SortPriority);
			}

			return Category.CompareTo (otherTag.Category);
		}

		public bool IsAncestorOf (Tag tag)
		{
			if (tag == null)
				throw new ArgumentNullException (nameof (tag));

			for (Tag parent = tag.Category; parent != null; parent = parent.Category) {
				if (parent == this)
					return true;
			}

			return false;
		}

		// Appends all of this categories descendents to the list
		public void AddDescendentsTo (IList<Tag> list)
		{
			if (list == null)
				throw new ArgumentNullException (nameof (list));

			foreach (Tag tag in children) {
				if (!list.Contains (tag))
					list.Add (tag);

				if (!tag.IsCategory)
					continue;

				tag.AddDescendentsTo (list);
			}
		}

		public void AddChild (Tag child)
		{
			children.Add (child);
			childrenNeedSort = true;
		}

		public void RemoveChild (Tag child)
		{
			children.Remove (child);
			childrenNeedSort = true;
		}
	}
}
