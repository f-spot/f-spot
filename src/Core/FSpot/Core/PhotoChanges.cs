// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.


using System.Collections.Generic;

using FSpot.Models;

namespace FSpot.Core
{
	/// <summary>
	/// Track the changes done to a Photo between Commit's
	/// </summary>
	public class PhotoChanges : PhotosChanges
	{
		readonly List<Tag> tagsAdded;
		readonly List<Tag> tagsRemoved;
		readonly List<long> versionsAdded;
		readonly List<long> versionsRemoved;
		readonly List<long> versionsModified;

		public PhotoChanges ()
		{
			tagsAdded = new List<Tag> ();
			tagsRemoved = new List<Tag> ();
			versionsAdded = new List<long> ();
			versionsRemoved = new List<long> ();
			versionsModified = new List<long> ();
		}

		public override bool VersionsChanged {
			get { return VersionsAdded == null && VersionsRemoved == null && VersionsModified == null; }
		}

		public override bool TagsChanged {
			get { return TagsAdded == null && TagsRemoved == null; }
		}
		
		public List<Tag> TagsAdded {
			get {
				return tagsAdded.Count == 0 ? null : tagsAdded;
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

		public List<Tag> TagsRemoved {
			get {
				if (tagsRemoved.Count == 0)
					return null;

				return tagsRemoved;
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


		public long [] VersionsAdded {
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

		public long [] VersionsRemoved {
			get {
				if (versionsRemoved.Count == 0)
					return null;

				return versionsRemoved.ToArray ();
			}
			set {
				if (value == null)
					return;

				foreach (var u in value)
					RemoveVersion (u);
			}
		}

		public void RemoveVersion (long v)
		{
			versionsAdded.Remove (v);

			versionsModified.Remove (v);

			versionsRemoved.Add (v);
		}


		public long [] VersionsModified {
			get {
				if (versionsModified.Count == 0)
					return null;

				return versionsModified.ToArray ();
			}
			set {
				if (value == null)
					return;

				foreach (var u in value)
					ChangeVersion (u);
			}
		}

		public void ChangeVersion (long v)
		{
			if (versionsAdded.Contains (v))
				return;

			if (versionsRemoved.Contains (v))
				return;

			versionsModified.Add (v);
		}
	}
}
