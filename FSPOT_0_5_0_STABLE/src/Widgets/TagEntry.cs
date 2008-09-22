/*
 * Widgets/TagEntry.cs
 *
 * Author(s)
 *   Original work by Nat Friedman in MainWindow.cs
 *   Refactored as a self-contained widget by Stephane Delcroix
 *
 * This is free software. See COPYING for details.
 *
 */

using System.Text;
using System.Collections;

namespace FSpot.Widgets {

	public delegate void TagsAttachedHandler (object sender, string [] tags);
	public delegate void TagsRemovedHandler (object sender, Tag [] tags);

	public class TagEntry : Gtk.Entry {

		public event TagsAttachedHandler TagsAttached;
		public event TagsRemovedHandler TagsRemoved;

		TagStore tag_store;

		public TagEntry (TagStore tag_store) : this (tag_store, true)
		{
		}

		protected TagEntry (System.IntPtr raw)
		{
			Raw = raw;
		}

		public TagEntry (TagStore tag_store, bool update_on_focus_out) : base ()
		{
			this.tag_store = tag_store;
			this.KeyPressEvent += HandleKeyPressEvent;
			if (update_on_focus_out)
				this.FocusOutEvent += HandleFocusOutEvent;
		}

		ArrayList selected_photos_tagnames;
		public void UpdateFromSelection (Photo [] sel)
		{
			Hashtable taghash = new Hashtable ();
	
			for (int i = 0; i < sel.Length; i++) {
				foreach (Tag tag in sel [i].Tags) {
					int count = 1;
	
					if (taghash.Contains (tag))
						count = ((int) taghash [tag]) + 1;
	
					if (count <= i)
						taghash.Remove (tag);
					else 
						taghash [tag] = count;
				}
				
				if (taghash.Count == 0)
					break;
			}
	
			selected_photos_tagnames = new ArrayList ();
			foreach (Tag tag in taghash.Keys)
				if ((int) (taghash [tag]) == sel.Length)
					selected_photos_tagnames.Add (tag.Name);
	
			Update ();
		}

		public void UpdateFromTagNames (string [] tagnames)
		{
			selected_photos_tagnames = new ArrayList ();
			foreach (string tagname in tagnames)
				selected_photos_tagnames.Add (tagname);

			Update ();
		}

		private void Update ()
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

		private void AppendComma ()
		{
			if (Text.Length != 0 && !Text.Trim ().EndsWith (","))
				AppendText (", ");	
		}

		public string [] GetTypedTagNames ()
		{
			string [] tagnames = Text.Split (new char [] {','});
	
			ArrayList list = new ArrayList ();
			for (int i = 0; i < tagnames.Length; i ++) {
				string s = tagnames [i].Trim ();
	
				if (s.Length > 0)
					list.Add (s);
			}
	
			return (string []) (list.ToArray (typeof (string)));
		}

		int tag_completion_index = -1;
		Tag [] tag_completions;

		public void ClearTagCompletions ()
		{
			tag_completion_index = -1;
			tag_completions = null;
		}

		[GLib.ConnectBefore]
		private void HandleKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			args.RetVal = false;
			if (args.Event.Key == Gdk.Key.Escape) { 
				args.RetVal = false;
			} else if (args.Event.Key == Gdk.Key.Return) { 
				if (tag_completion_index != -1) {
					OnActivated ();
					args.RetVal = true;
				} else
					args.RetVal = false;
			} else if (args.Event.Key == Gdk.Key.Tab) {
				DoTagCompletion ();
				args.RetVal = true;
			} else 
				ClearTagCompletions ();
		}

		bool tag_ignore_changes = false;

		protected override void OnChanged ()
		{
			if (tag_ignore_changes)
				return;

			ClearTagCompletions ();
		}

		string tag_completion_typed_so_far;
		int tag_completion_typed_position;

		private void DoTagCompletion ()
		{
			string completion;
			
			if (tag_completion_index != -1) {
				tag_completion_index = (tag_completion_index + 1) % tag_completions.Length;
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
	
				tag_completions = tag_store.GetTagsByNameStart (tag_completion_typed_so_far);
				if (tag_completions == null)
					return;
	
				tag_completion_index = 0;
			}
	
			tag_ignore_changes = true;
			completion = tag_completions [tag_completion_index].Name.Substring (tag_completion_typed_so_far.Length);
			Text = Text.Substring (0, tag_completion_typed_position) + completion;
			tag_ignore_changes = false;
	
			Position = Text.Length;
			SelectRegion (tag_completion_typed_position, Text.Length);
		}

		//Activated means the user pressed 'Enter'
		protected override void OnActivated ()
		{
			string [] tagnames = GetTypedTagNames ();
	
			if (tagnames == null)
				return;

			int sel_start, sel_end;
			if (GetSelectionBounds (out sel_start, out sel_end) && tag_completion_index != -1) {
				InsertText (", ", ref sel_end);
				SelectRegion (-1, -1);
				Position = sel_end + 2;
				ClearTagCompletions ();
				return;
			}

			// Add any new tags to the selected photos
			ArrayList new_tags = new ArrayList ();
			for (int i = 0; i < tagnames.Length; i ++) {
				if (tagnames [i].Length == 0)
					continue;

				if (selected_photos_tagnames.Contains (tagnames [i]))
					continue;

				Tag t = tag_store.GetTagByName (tagnames [i]);

				if (t != null) // Correct for capitalization differences
					tagnames [i] = t.Name;

				new_tags.Add (tagnames [i]);
			}

			//Send event
			if (new_tags.Count != 0 && TagsAttached != null)
				TagsAttached (this, (string []) new_tags.ToArray (typeof (string)));

			// Remove any removed tags from the selected photos
			ArrayList remove_tags = new ArrayList ();
			foreach (string tagname in selected_photos_tagnames) {
				if (! IsTagInList (tagnames, tagname)) {
					Tag tag = tag_store.GetTagByName (tagname);
					remove_tags.Add (tag);
				}
			}

			//Send event
			if (remove_tags.Count != 0 && TagsRemoved != null)
				TagsRemoved (this, (Tag []) remove_tags.ToArray (typeof (Tag)));
		}

		private static bool IsTagInList (string [] tags, string tag)
		{
			foreach (string t in tags)
				if (t == tag)
					return true;
			return false;
		}

		private void HandleFocusOutEvent (object o, Gtk.FocusOutEventArgs args)
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
