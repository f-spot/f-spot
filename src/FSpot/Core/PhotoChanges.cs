//
// PhotoChanges.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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

using System.Collections.Generic;

namespace FSpot.Core
{
	//Track the changes done to a Photo between Commit's
	public class PhotoChanges : PhotosChanges
	{

		public override bool VersionsChanged {
			get { return VersionsAdded == null && VersionsRemoved == null && VersionsModified == null; }
		}

		public override bool TagsChanged {
			get { return TagsAdded == null && TagsRemoved == null; }
		}

		List<Tag> tags_added = null;
		public Tag [] TagsAdded {
			get {
				if (tags_added == null)
					return null;
				if (tags_added.Count == 0)
					return null;
				return tags_added.ToArray ();
			}
			set {
				foreach (Tag t in value)
					AddTag (t);
			}
		}

		public void AddTag (Tag t)
		{
			if (tags_added == null)
				tags_added = new List<Tag> ();
			if (tags_removed != null)
				tags_removed.Remove (t);
			tags_added.Add (t);

		}

		List<Tag> tags_removed = null;
		public Tag [] TagsRemoved {
			get {
				if (tags_removed == null)
					return null;
				if (tags_removed.Count == 0)
					return null;
				return tags_removed.ToArray ();
			}
			set {
				foreach (Tag t in value)
					RemoveTag (t);
			}
		}

		public void RemoveTag (Tag t)
		{
			if (tags_removed == null)
				tags_removed = new List<Tag> ();
			if (tags_added != null)
				tags_added.Remove (t);
			tags_removed.Add (t);
		}


		List<uint> versions_added = null;
		public uint [] VersionsAdded {
			get {
				if (versions_added == null)
					return null;
				if (versions_added.Count == 0)
					return null;
				return versions_added.ToArray ();
			}
			set {
				foreach (uint u in value)
					AddVersion (u);
			}
		}

		public void AddVersion (uint v)
		{
			if (versions_added == null)
				versions_added = new List<uint> ();
			versions_added.Add (v);
		}

		List<uint> versions_removed = null;
		public uint [] VersionsRemoved {
			get {
				if (versions_removed == null)
					return null;
				if (versions_removed.Count == 0)
					return null;
				return versions_removed.ToArray ();
			}
			set {
				foreach (uint u in value)
					RemoveVersion (u);
			}
		}

		public void RemoveVersion (uint v)
		{
			if (versions_removed == null)
				versions_removed= new List<uint> ();
			if (versions_added != null)
				versions_added.Remove (v);
			if (versions_modified != null)
				versions_modified.Remove (v);
			versions_removed.Add (v);
		}


		List<uint> versions_modified = null;
		public uint [] VersionsModified {
			get {
				if (versions_modified == null)
					return null;
				if (versions_modified.Count == 0)
					return null;
				return versions_modified.ToArray ();
			}
			set {
				foreach (uint u in value)
					ChangeVersion (u);
			}
		}

		public void ChangeVersion (uint v)
		{
			if (versions_modified == null)
				versions_modified = new List<uint> ();
			if (versions_added != null && versions_added.Contains (v))
				return;
			if (versions_removed != null && versions_removed.Contains (v))
				return;
			versions_modified.Add (v);
		}
	}
}
