//
// PhotoChanges.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot.Core
{
	/// <summary>
	/// Track the changes done to a Photo between commits
	/// </summary>
	public class PhotoChanges : PhotosChanges
	{
		public override bool VersionsChanged {
			get { return VersionsAdded == null && VersionsRemoved == null && VersionsModified == null; }
		}

		public override bool TagsChanged {
			get { return TagsAdded == null && TagsRemoved == null; }
		}

		List<Tag> tagsAdded;
		public Tag[] TagsAdded {
			get {
				if (tagsAdded == null)
					return null;
				if (tagsAdded.Count == 0)
					return null;
				return tagsAdded.ToArray ();
			}
			set {
				foreach (Tag t in value)
					AddTag (t);
			}
		}

		public void AddTag (Tag t)
		{
			if (tagsAdded == null)
				tagsAdded = new List<Tag> ();
			if (tagsRemoved != null)
				tagsRemoved.Remove (t);
			tagsAdded.Add (t);

		}

		List<Tag> tagsRemoved;
		public Tag[] TagsRemoved {
			get {
				if (tagsRemoved == null)
					return null;
				if (tagsRemoved.Count == 0)
					return null;
				return tagsRemoved.ToArray ();
			}
			set {
				foreach (Tag t in value)
					RemoveTag (t);
			}
		}

		public void RemoveTag (Tag t)
		{
			if (tagsRemoved == null)
				tagsRemoved = new List<Tag> ();
			if (tagsAdded != null)
				tagsAdded.Remove (t);
			tagsRemoved.Add (t);
		}

		List<uint> versionsAdded;
		public uint[] VersionsAdded {
			get {
				if (versionsAdded == null)
					return null;
				if (versionsAdded.Count == 0)
					return null;
				return versionsAdded.ToArray ();
			}
			set {
				foreach (uint u in value)
					AddVersion (u);
			}
		}

		public void AddVersion (uint v)
		{
			if (versionsAdded == null)
				versionsAdded = new List<uint> ();
			versionsAdded.Add (v);
		}

		List<uint> versionsRemoved;
		public uint[] VersionsRemoved {
			get {
				if (versionsRemoved == null)
					return null;
				if (versionsRemoved.Count == 0)
					return null;
				return versionsRemoved.ToArray ();
			}
			set {
				foreach (uint u in value)
					RemoveVersion (u);
			}
		}

		public void RemoveVersion (uint v)
		{
			if (versionsRemoved == null)
				versionsRemoved = new List<uint> ();
			if (versionsAdded != null)
				versionsAdded.Remove (v);
			if (versionsModified != null)
				versionsModified.Remove (v);
			versionsRemoved.Add (v);
		}


		List<uint> versionsModified;
		public uint[] VersionsModified {
			get {
				if (versionsModified == null)
					return null;
				if (versionsModified.Count == 0)
					return null;
				return versionsModified.ToArray ();
			}
			set {
				foreach (uint u in value)
					ChangeVersion (u);
			}
		}

		public void ChangeVersion (uint v)
		{
			if (versionsModified == null)
				versionsModified = new List<uint> ();
			if (versionsAdded != null && versionsAdded.Contains (v))
				return;
			if (versionsRemoved != null && versionsRemoved.Contains (v))
				return;
			versionsModified.Add (v);
		}
	}
}
