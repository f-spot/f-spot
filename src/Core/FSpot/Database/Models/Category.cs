// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot.Models
{
	//public class Category : Tag
	//{
	//	List<Tag> children;
	//	bool childrenNeedSort;

	//	public Category (Category category, Guid id, string name) : base (category)
	//	{
	//		Id = id;
	//		Name = name;
	//		children = new List<Tag> ();
	//	}

	//	public List<Tag> Children {
	//		get {
	//			if (childrenNeedSort)
	//				children.Sort ();

	//			return children;
	//		}
	//		set {
	//			children = new List<Tag> (value);
	//			childrenNeedSort = true;
	//		}
	//	}

	//	// Appends all of this categories descendents to the list
	//	public void AddDescendentsTo (IList<Tag> list)
	//	{
	//		if (list == null)
	//			throw new ArgumentNullException (nameof (list));

	//		foreach (Tag tag in children) {
	//			if (!list.Contains (tag))
	//				list.Add (tag);

	//			if (!(tag is Category cat))
	//				continue;

	//			cat.AddDescendentsTo (list);
	//		}
	//	}

	//	public void AddChild (Tag child)
	//	{
	//		children.Add (child);
	//		childrenNeedSort = true;
	//	}

	//	public void RemoveChild (Tag child)
	//	{
	//		children.Remove (child);
	//		childrenNeedSort = true;
	//	}
	//}
}
