//
// Category.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot.Core
{
	public class Category : Tag
	{
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
				throw new ArgumentNullException (nameof (list));

			foreach (Tag tag in children) {
				if (!list.Contains (tag))
					list.Add (tag);

				var cat = tag as Category;
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

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				// free managed resources
				foreach (Tag tag in children) {
					tag.Dispose ();
				}
			}
			// free unmanaged resources

			base.Dispose (disposing);
		}
	}
}
