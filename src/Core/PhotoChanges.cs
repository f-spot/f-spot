/*
 * FSpot.PhotoChanges.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections.Generic;

namespace FSpot
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

		public PhotoChanges ()
		{
		}
	}
}
