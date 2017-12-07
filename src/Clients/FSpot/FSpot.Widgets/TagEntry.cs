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

using System.Text;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.Database;

namespace FSpot.Widgets
{
	public delegate void TagsAttachedHandler (object sender, string [] tags);
	public delegate void TagsRemovedHandler (object sender, Tag [] tags);

	public class TagEntry : Gtk.Entry
	{
		public event TagsAttachedHandler TagsAttached;
		public event TagsRemovedHandler TagsRemoved;

		TagStore _tagStore;

		protected TagEntry (System.IntPtr raw)
		{
			Raw = raw;
		}

		public TagEntry (TagStore tagStore, bool updateOnFocusOut = true)
		{
			_tagStore = tagStore;
			KeyPressEvent += HandleKeyPressEvent;
			if (updateOnFocusOut)
				FocusOutEvent += HandleFocusOutEvent;
		}

		List<string> selected_photos_tagnames;
		public void UpdateFromSelection (IPhoto [] selection)
		{
			Dictionary<Tag,int> taghash = new Dictionary<Tag,int> ();

			for (int i = 0; i < selection.Length; i++) {
				foreach (Tag tag in selection [i].Tags) {
					int count = 1;

					if (taghash.ContainsKey (tag))
						count = (taghash [tag]) + 1;

					if (count <= i)
						taghash.Remove (tag);
					else
						taghash [tag] = count;
				}

				if (taghash.Count == 0)
					break;
			}

			selected_photos_tagnames = new List<string> ();
			foreach (Tag tag in taghash.Keys)
				if (taghash [tag] == selection.Length)
					selected_photos_tagnames.Add (tag.Name);

			Update ();
		}

		public void UpdateFromTagNames (string [] tagnames)
		{
			selected_photos_tagnames = new List<string> ();
			foreach (string tagname in tagnames)
				selected_photos_tagnames.Add (tagname);

			Update ();
		}

		void Update ()
		{
			selected_photos_tagnames.Sort ();

			StringBuilder sb = new StringBuilder ();
			foreach (string tagname in selected_photos_tagnames) {
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

		public string [] GetTypedTagNames ()
		{
			string [] tagnames = Text.Split (new char [] {','});

			List<string> list = new List<string> ();
			for (int i = 0; i < tagnames.Length; i ++) {
				string s = tagnames [i].Trim ();

				if (s.Length > 0)
					list.Add (s);
			}
			return list.ToArray ();
		}

		int tag_completion_index = -1;
		Tag [] tag_completions;

		public void ClearTagCompletions ()
		{
			tag_completion_index = -1;
			tag_completions = null;
		}

		[GLib.ConnectBefore]
		void HandleKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			args.RetVal = false;
			if (args.Event.Key == Gdk.Key.Escape) {
				args.RetVal = false;
			} else if (args.Event.Key == Gdk.Key.comma) {
				if (tag_completion_index != -1) {
					// If we are completing a tag, then finish that
					FinishTagCompletion ();
					args.RetVal = true;
				} else
					// Otherwise do not handle this event here
					args.RetVal = false;
			} else if (args.Event.Key == Gdk.Key.Return) {
				// If we are completing a tag, then finish that
				if (tag_completion_index != -1)
					FinishTagCompletion ();
				// And pass the event to Gtk.Entry in any case,
				// which will call OnActivated
				args.RetVal = false;
			} else if (args.Event.Key == Gdk.Key.Tab) {
				DoTagCompletion (true);
				args.RetVal = true;
			} else if (args.Event.Key == Gdk.Key.ISO_Left_Tab) {
				DoTagCompletion (false);
				args.RetVal = true;
			}
		}

		bool tag_ignore_changes;

		protected override void OnChanged ()
		{
			if (tag_ignore_changes)
				return;

			ClearTagCompletions ();
		}

		string tag_completion_typed_so_far;
		int tag_completion_typed_position;

		void DoTagCompletion (bool forward)
		{
			string completion;

			if (tag_completion_index != -1) {
				if (forward)
					tag_completion_index = (tag_completion_index + 1) % tag_completions.Length;
				else
					tag_completion_index = (tag_completion_index + tag_completions.Length - 1) % tag_completions.Length;
			} else {

				tag_completion_typed_position = Position;

				string right_of_cursor = Text.Substring (tag_completion_typed_position);
				if (right_of_cursor.Length > 1)
					return;

				int last_comma = Text.LastIndexOf (',');
				if (last_comma > tag_completion_typed_position)
					return;

				tag_completion_typed_so_far = Text.Substring (last_comma + 1).TrimStart (new char [] {' '});
				if (tag_completion_typed_so_far == null || tag_completion_typed_so_far.Length == 0)
					return;

				tag_completions = _tagStore.GetTagsByNameStart (tag_completion_typed_so_far);
				if (tag_completions == null)
					return;

				if (forward)
					tag_completion_index = 0;
				else
					tag_completion_index = tag_completions.Length - 1;
			}

			tag_ignore_changes = true;
			completion = tag_completions [tag_completion_index].Name.Substring (tag_completion_typed_so_far.Length);
			Text = Text.Substring (0, tag_completion_typed_position) + completion;
			tag_ignore_changes = false;

			Position = Text.Length;
			SelectRegion (tag_completion_typed_position, Text.Length);
		}

		void FinishTagCompletion ()
		{
			if (tag_completion_index == -1)
				return;

			int sel_start, sel_end, pos;
			pos = Position;
			if (GetSelectionBounds (out sel_start, out sel_end)) {
				pos = sel_end;
				SelectRegion (-1, -1);
			}

			InsertText (", ", ref pos);
			Position = pos + 2;
			ClearTagCompletions ();

		}

		//Activated means the user pressed 'Enter'
		protected override void OnActivated ()
		{
			string [] tagnames = GetTypedTagNames ();

			if (tagnames == null)
				return;

			// Add any new tags to the selected photos
			List<string> new_tags = new List<string> ();
			for (int i = 0; i < tagnames.Length; i ++) {
				if (tagnames [i].Length == 0)
					continue;

				if (selected_photos_tagnames.Contains (tagnames [i]))
					continue;

				Tag t = _tagStore.GetTagByName (tagnames [i]);

				if (t != null) // Correct for capitalization differences
					tagnames [i] = t.Name;

				new_tags.Add (tagnames [i]);
			}

			//Send event
			if (new_tags.Count != 0 && TagsAttached != null)
				TagsAttached (this, new_tags.ToArray ());

			// Remove any removed tags from the selected photos
			List<Tag> remove_tags = new List<Tag> ();
			foreach (string tagname in selected_photos_tagnames) {
				if (! IsTagInList (tagnames, tagname)) {
					Tag tag = _tagStore.GetTagByName (tagname);
					remove_tags.Add (tag);
				}
			}

			//Send event
			if (remove_tags.Count != 0 && TagsRemoved != null)
				TagsRemoved (this, remove_tags.ToArray ());
		}

		static bool IsTagInList (string [] tags, string tag)
		{
			foreach (string t in tags)
				if (t == tag)
					return true;
			return false;
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
