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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
