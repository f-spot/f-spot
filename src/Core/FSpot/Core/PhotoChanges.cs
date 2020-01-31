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
	/// <summary>
	/// Track the changes done to a Photo between Commit's
	/// </summary>
	public class PhotoChanges : PhotosChanges
	{
		List<Tag> tagsAdded;
		List<Tag> tagsRemoved;
		List<uint> versionsAdded;
		List<uint> versionsRemoved;
		List<uint> versionsModified;

		public PhotoChanges ()
		{
			tagsAdded = new List<Tag> ();
			tagsRemoved = new List<Tag> ();
			versionsAdded = new List<uint> ();
			versionsRemoved = new List<uint> ();
			versionsModified = new List<uint> ();
		}

		public override bool VersionsChanged {
			get { return VersionsAdded == null && VersionsRemoved == null && VersionsModified == null; }
		}

		public override bool TagsChanged {
			get { return TagsAdded == null && TagsRemoved == null; }
		}

		public Tag [] TagsAdded {
			get {
				if (tagsAdded.Count == 0)
					return null;

				return tagsAdded.ToArray ();
			}
			set {
				if (value == null)
					return;

				foreach (var t in value)
					AddTag (t);
			}
		}

		public void AddTag (Tag t)
		{
			tagsRemoved.Remove (t);

			tagsAdded.Add (t);
		}

		public Tag [] TagsRemoved {
			get {
				if (tagsRemoved.Count == 0)
					return null;

				return tagsRemoved.ToArray ();
			}
			set {
				if (value == null)
					return;

				foreach (Tag t in value)
					RemoveTag (t);
			}
		}

		public void RemoveTag (Tag t)
		{
			tagsAdded.Remove (t);

			tagsRemoved.Add (t);
		}


		public uint [] VersionsAdded {
			get {
				if (versionsAdded.Count == 0)
					return null;

				return versionsAdded.ToArray ();
			}
			set {
				if (value == null)
					return;

				foreach (uint u in value)
					AddVersion (u);
			}
		}

		public void AddVersion (uint v)
		{
			versionsAdded.Add (v);
		}

		public uint [] VersionsRemoved {
			get {
				if (versionsRemoved.Count == 0)
					return null;

				return versionsRemoved.ToArray ();
			}
			set {
				if (value == null)
					return;

				foreach (uint u in value)
					RemoveVersion (u);
			}
		}

		public void RemoveVersion (uint v)
		{
			versionsAdded.Remove (v);

			versionsModified.Remove (v);

			versionsRemoved.Add (v);
		}


		public uint [] VersionsModified {
			get {
				if (versionsModified.Count == 0)
					return null;

				return versionsModified.ToArray ();
			}
			set {
				if (value == null)
					return;

				foreach (uint u in value)
					ChangeVersion (u);
			}
		}

		public void ChangeVersion (uint v)
		{
			if (versionsAdded.Contains (v))
				return;

			if (versionsRemoved.Contains (v))
				return;

			versionsModified.Add (v);
		}
	}
}
