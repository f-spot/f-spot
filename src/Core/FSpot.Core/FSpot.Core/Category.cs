/*
 * FSpot.Category.cs
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections.Generic;

namespace FSpot.Core
{
	public class Category : Tag {
		List<Tag> children;
		bool children_need_sort;
		public IList<Tag> Children {
			get {
				if (children_need_sort)
					children.Sort ();
				return children.ToArray ();
			}
			set {
				children = new List<Tag> (value);
				children_need_sort = true;
			}
		}

		// Appends all of this categories descendents to the list
		public void AddDescendentsTo (IList<Tag> list)
		{
			if (list == null)
				throw new ArgumentNullException ("list");

			foreach (Tag tag in children) {
				if (! list.Contains (tag))
					list.Add (tag);

				Category cat = tag as Category;
				if (cat == null)
					continue;

				cat.AddDescendentsTo (list);
			}
		}

		public void AddChild (Tag child)
		{
			children.Add (child);
			children_need_sort = true;
		}

		public void RemoveChild (Tag child)
		{
			children.Remove (child);
			children_need_sort = true;
		}

		public Category (Category category, uint id, string name)
			: base (category, id, name)
		{
			children = new List<Tag> ();
		}
	}
}
