//
// TagEntry.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Joachim Breitner <mail@joachim-breitner.de>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006-2008 Stephane Delcroix
// Copyright (C) 2009 Joachim Breitner
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.Database;

namespace FSpot.Widgets
{
	public delegate void TagsAttachedHandler (object sender, string[] tags);
	public delegate void TagsRemovedHandler (object sender, Tag[] tags);

	public class TagEntry : Gtk.Entry
	{
		public event TagsAttachedHandler TagsAttached;
		public event TagsRemovedHandler TagsRemoved;

		readonly TagStore tagStore;

		int tagCompletionIndex = -1;
		Tag[] tagCompletions;
		List<string> selectedPhotosTagnames;
		bool tagIgnoreChanges;
		string tagCompletionTypedSoFar;
		int tagCompletionTypedPosition;

		protected TagEntry (IntPtr raw) : base (raw) { }

		public TagEntry (TagStore tagStore, bool updateOnFocusOut = true)
		{
			this.tagStore = tagStore;
			KeyPressEvent += HandleKeyPressEvent;
			if (updateOnFocusOut)
				FocusOutEvent += HandleFocusOutEvent;
		}

		public void UpdateFromSelection (List<Photo> selection)
		{
			var taghash = new Dictionary<Tag, int> ();

			for (int i = 0; i < selection.Count; i++) {
				foreach (Tag tag in selection[i].Tags) {
					int count = 1;

					if (taghash.ContainsKey (tag))
						count = (taghash[tag]) + 1;

					if (count <= i)
						taghash.Remove (tag);
					else
						taghash[tag] = count;
				}

				if (taghash.Count == 0)
					break;
			}

			selectedPhotosTagnames = new List<string> ();
			foreach (Tag tag in taghash.Keys)
				if (taghash[tag] == selection.Count)
					selectedPhotosTagnames.Add (tag.Name);

			Update ();
		}

		public void UpdateFromTagNames (string[] tagnames)
		{
			selectedPhotosTagnames = new List<string> ();
			foreach (string tagname in tagnames)
				selectedPhotosTagnames.Add (tagname);

			Update ();
		}

		void Update ()
		{
			selectedPhotosTagnames.Sort ();

			var sb = new StringBuilder ();
			foreach (string tagname in selectedPhotosTagnames) {
				if (sb.Length > 0)
					sb.Append (", ");

				sb.Append (tagname);
			}

			Text = sb.ToString ();
			ClearTagCompletions ();
		}

		void AppendComma ()
		{
			if (Text.Length != 0 && !Text.Trim ().EndsWith (",")) {
				int pos = Text.Length;
				InsertText (", ", ref pos);
				Position = Text.Length;
			}
		}

		public List<string> GetTypedTagNames ()
		{
			var tagnames = Text.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			var tags = new List<string> ();

			foreach (var tag in tagnames)
				tags.Add (tag.Trim ());

			return tags;
		}

		public void ClearTagCompletions ()
		{
			tagCompletionIndex = -1;
			tagCompletions = null;
		}

		[GLib.ConnectBefore]
		void HandleKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			args.RetVal = false;
			switch (args.Event.Key) {
			case Gdk.Key.Escape:
				args.RetVal = false;
				break;
			case Gdk.Key.comma:
				if (tagCompletionIndex != -1) {
					// If we are completing a tag, then finish that
					FinishTagCompletion ();
					args.RetVal = true;
				} else
					// Otherwise do not handle this event here
					args.RetVal = false;
				break;
			case Gdk.Key.Return:
				// If we are completing a tag, then finish that
				if (tagCompletionIndex != -1)
					FinishTagCompletion ();
				// And pass the event to Gtk.Entry in any case,
				// which will call OnActivated
				args.RetVal = false;
				break;
			case Gdk.Key.Tab:
				DoTagCompletion (true);
				args.RetVal = true;
				break;
			case Gdk.Key.ISO_Left_Tab:
				DoTagCompletion (false);
				args.RetVal = true;
				break;
			}
		}

		protected override void OnChanged ()
		{
			if (tagIgnoreChanges)
				return;

			ClearTagCompletions ();
		}

		void DoTagCompletion (bool forward)
		{
			string completion;

			if (tagCompletionIndex != -1) {
				if (forward)
					tagCompletionIndex = (tagCompletionIndex + 1) % tagCompletions.Length;
				else
					tagCompletionIndex = (tagCompletionIndex + tagCompletions.Length - 1) % tagCompletions.Length;
			} else {

				tagCompletionTypedPosition = Position;

				string right_of_cursor = Text.Substring (tagCompletionTypedPosition);
				if (right_of_cursor.Length > 1)
					return;

				int last_comma = Text.LastIndexOf (',');
				if (last_comma > tagCompletionTypedPosition)
					return;

				tagCompletionTypedSoFar = Text.Substring (last_comma + 1).TrimStart (new char[] { ' ' });
				if (tagCompletionTypedSoFar == null || tagCompletionTypedSoFar.Length == 0)
					return;

				tagCompletions = tagStore.GetTagsByNameStart (tagCompletionTypedSoFar);
				if (tagCompletions == null)
					return;

				if (forward)
					tagCompletionIndex = 0;
				else
					tagCompletionIndex = tagCompletions.Length - 1;
			}

			tagIgnoreChanges = true;
			completion = tagCompletions[tagCompletionIndex].Name.Substring (tagCompletionTypedSoFar.Length);
			Text = Text.Substring (0, tagCompletionTypedPosition) + completion;
			tagIgnoreChanges = false;

			Position = Text.Length;
			SelectRegion (tagCompletionTypedPosition, Text.Length);
		}

		void FinishTagCompletion ()
		{
			if (tagCompletionIndex == -1)
				return;

			int pos = Position;
			if (GetSelectionBounds (out var selectionStart, out var selectionEnd)) {
				pos = selectionEnd;
				SelectRegion (-1, -1);
			}

			InsertText (", ", ref pos);
			Position = pos + 2;
			ClearTagCompletions ();
		}

		//Activated means the user pressed 'Enter'
		protected override void OnActivated ()
		{
			var tagnames = GetTypedTagNames ();

			if (tagnames == null)
				return;

			// Add any new tags to the selected photos
			var newTags = new List<string> ();
			for (int i = 0; i < tagnames.Count; i++) {
				if (tagnames[i].Length == 0)
					continue;

				if (selectedPhotosTagnames.Contains (tagnames[i]))
					continue;

				Tag t = tagStore.GetTagByName (tagnames[i]);

				if (t != null) // Correct for capitalization differences
					tagnames[i] = t.Name;

				newTags.Add (tagnames[i]);
			}

			//Send event
			if (newTags.Count != 0 && TagsAttached != null)
				TagsAttached (this, newTags.ToArray ());

			// Remove any removed tags from the selected photos
			var removeTags = new List<Tag> ();
			foreach (string tagname in selectedPhotosTagnames) {
				if (!tagnames.Contains (tagname)) {
					Tag tag = tagStore.GetTagByName (tagname);
					removeTags.Add (tag);
				}
			}

			//Send event
			if (removeTags.Count != 0 && TagsRemoved != null)
				TagsRemoved (this, removeTags.ToArray ());
		}

		void HandleFocusOutEvent (object o, Gtk.FocusOutEventArgs args)
		{
			Update ();
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			AppendComma ();
			return base.OnFocusInEvent (evnt);
		}
	}
}
