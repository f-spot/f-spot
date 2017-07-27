//  LogicWidget.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Larry Ewing <lewing@novell.com>
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2006-2007 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
// Copyright (C) 2006-2007 Gabriel Burt
//
//  Permission is hereby granted, free of charge, to any person obtaining
//  a copy of this software and associated documentation files (the
//  "Software"), to deal in the Software without restriction, including
//  without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to
//  permit persons to whom the Software is furnished to do so, subject to
//  the following conditions:
//
//  The above copyright notice and this permission notice shall be
//  included in all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
//  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//  WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;

using Mono.Unix;

using Gtk;
using Gdk;

using FSpot.Core;
using FSpot.Database;

namespace FSpot.Query
{
	public class LogicWidget : HBox
	{
		readonly PhotoQuery query;

		static Term rootTerm;
		EventBox rootAdd;
		HBox rootBox;
		Label help;
		HBox sepBox;

		bool preventUpdate;
		bool preview;

		public event EventHandler Changed;

		public static Term Root
		{
			get {
				return rootTerm;
			}
		}

		static LogicWidget logic_widget;
		public static LogicWidget Box {
			get { return logic_widget; }
		}

		// Drag and Drop
		static readonly TargetEntry [] tag_dest_target_table =
			{
				DragDropTargets.TagListEntry,
				DragDropTargets.TagQueryEntry
			};

		public LogicWidget (PhotoQuery query, TagStore tagStore)
		{
			//SetFlag (WidgetFlags.NoWindow);
			this.query = query;

			CanFocus = true;
			Sensitive = true;

			Init ();

			tagStore.ItemsChanged += HandleTagChanged;
			tagStore.ItemsRemoved += HandleTagDeleted;

			Show ();

			logic_widget = this;
		}

		void Init ()
		{
			sepBox = null;
			preview = false;

			rootAdd = new Gtk.EventBox ();
			rootAdd.VisibleWindow = false;
			rootAdd.CanFocus = true;
			rootAdd.DragMotion  += HandleDragMotion;
			rootAdd.DragDataReceived += HandleDragDataReceived;
			rootAdd.DragLeave  += HandleDragLeave;

			help = new Gtk.Label ("<i>" + Catalog.GetString ("Drag tags here to search for them") + "</i>");
			help.UseMarkup = true;
			help.Visible = true;

			rootBox = new HBox();
			rootBox.Add (help);
			rootBox.Show ();

			rootAdd.Child = rootBox;
			rootAdd.Show ();

			Gtk.Drag.DestSet (rootAdd, DestDefaults.All, tag_dest_target_table,
					  DragAction.Copy | DragAction.Move );

			PackEnd (rootAdd, true, true, 0);

			rootTerm = new OrTerm (null, null);
		}

		void Preview ()
		{
			if (sepBox == null) {
				sepBox = new HBox ();
				Widget sep = rootTerm.SeparatorWidget ();
				if (sep != null) {
					sep.Show ();
					sepBox.PackStart (sep, false, false, 0);
				}
				rootBox.Add (sepBox);
			}

			help.Hide ();
			sepBox.Show ();
		}

		/** Handlers **/

		// When the user edits a tag (it's icon, name, etc) we get called
		// and update the images/text in the query as needed to reflect the changes.
		void HandleTagChanged (object sender, DbItemEventArgs<Tag> args)
		{
			foreach (Tag t in args.Items)
		                foreach (Literal term in rootTerm.FindByTag (t))
		                    term.Update ();
		}

		// If the user deletes a tag that is in use in the query, remove it from the query too.
		void HandleTagDeleted (object sender, DbItemEventArgs<Tag> args)
		{
			foreach (Tag t in args.Items)
		                foreach (Literal term in rootTerm.FindByTag (t))
		                    term.RemoveSelf ();
		}

		void HandleDragMotion (object o, DragMotionArgs args)
		{
			if (!preview && rootTerm.Count > 0 && (Literal.FocusedLiterals.Count == 0 || Children.Length > 2)) {
				Preview ();
				preview = true;
			}
		}

		void HandleDragLeave (object o, EventArgs args)
		{
			if (preview && Children.Length > 1) {
				sepBox.Hide ();
				preview = false;
			} else if (preview && Children.Length == 1) {
				help.Show ();
			}
		}

		void HandleLiteralsMoved (List<Literal> literals, Term parent, Literal after)
		{
			preventUpdate = true;
			foreach (Literal term in literals) {
				Tag tag = term.Tag;

				// Don't listen for it to be removed since we are
				// moving it. We will update when we're done.
				term.Removed -= HandleRemoved;
				term.RemoveSelf ();

				// Add it to where it was dropped
				List<Literal> groups = InsertTerm (new [] {tag}, parent, after);

				if (term.IsNegated)
					foreach (Literal group in groups)
						group.IsNegated = true;
			}
			preventUpdate = false;
			UpdateQuery ();
		}

		void HandleTagsAdded (Tag[] tags, Term parent, Literal after)
		{
			InsertTerm (tags, parent, after);
		}

		void HandleAttachTag (Tag tag, Term parent, Literal after)
		{
			InsertTerm (new Tag [] {tag}, parent, after);
		}

		void HandleNegated (Literal group)
		{
			UpdateQuery ();
		}

		void HandleRemoving (Literal term)
		{
			foreach (Widget w in HangersOn (term))
				Remove (w);

			// Remove the term's widget
			Remove (term.Widget);
		}

		public List<Gtk.Widget> HangersOn (Literal term)
		{
			var w = new List<Gtk.Widget> ();

			// Find separators that only exist because of this term
			if (term.Parent != null) {
				if (term.Parent.Count > 1)
				{
					if (term == term.Parent.Last)
						w.Add (Children[WidgetPosition (term.Widget) - 1]);
					else
						w.Add (Children[WidgetPosition (term.Widget) + 1]);
				}
				else if (term.Parent.Count == 1)
				{
					if (term.Parent.Parent != null) {
						if (term.Parent.Parent.Count > 1) {
							if (term.Parent == term.Parent.Parent.Last)
								w.Add (Children[WidgetPosition (term.Widget) - 1]);
							else
								w.Add (Children[WidgetPosition (term.Widget) + 1]);
						}
					}
				}
			}
			return w;
		}

		void HandleRemoved (Literal group)
		{
			UpdateQuery ();
		}

		void HandleDragDataReceived (object o, DragDataReceivedArgs args)
		{
			args.RetVal = true;

			if (args.Info == DragDropTargets.TagListEntry.Info) {

				InsertTerm (args.SelectionData.GetTagsData (), rootTerm, null);
				return;
			}

			if (args.Info == DragDropTargets.TagQueryEntry.Info) {

				// FIXME: use drag data
				HandleLiteralsMoved (Literal.FocusedLiterals, rootTerm, null);

				// Prevent them from being removed again
				Literal.FocusedLiterals = new List<Literal> ();
			}
		}

		/** Helper Functions **/

		public void PhotoTagsChanged (Tag [] tags)
		{
			bool refresh_required = false;

			foreach (Tag tag in tags) {
				if ((rootTerm.FindByTag (tag)).Count > 0) {
					refresh_required = true;
					break;
				}
			}

			if (refresh_required)
				UpdateQuery ();
		}

		// Inserts a widget into a Box at a certain index
		void InsertWidget (int index, Gtk.Widget widget) {
			widget.Visible = true;
			PackStart (widget, false, false, 0);
			ReorderChild (widget, index);
		}

		// Return the index position of a widget in this Box
		int WidgetPosition (Gtk.Widget widget)
		{
			for (int i = 0; i < Children.Length; i++)
				if (Children[i] == widget)
					return i;

			return Children.Length - 1;
		}

		public bool TagIncluded (Tag tag)
		{
			return rootTerm.TagIncluded (tag);
		}

		public bool TagRequired (Tag tag)
		{
			return rootTerm.TagRequired (tag);
		}

		// Add a tag or group of tags to the rootTerm, at the end of the Box
		public void Include (Tag [] tags)
		{
			// Filter out any tags that are already included
			// FIXME: Does this really need to be set to a length?
			List<Tag> new_tags = new List<Tag> (tags.Length);
			foreach (Tag tag in tags) {
				if (! rootTerm.TagIncluded (tag))
					new_tags.Add (tag);

			}

			if (new_tags.Count == 0)
				return;

			tags = new_tags.ToArray ();

			InsertTerm (tags, rootTerm, null);
		}

		public void UnInclude (Tag [] tags)
		{
			var new_tags = new List<Tag> (tags.Length);
			foreach (Tag tag in tags) {
				if (rootTerm.TagIncluded (tag))
					new_tags.Add (tag);
			}

			if (new_tags.Count == 0)
				return;

			tags = new_tags.ToArray ();

			bool needsUpdate = false;
			preventUpdate = true;
			foreach (Term parent in rootTerm.LiteralParents ()) {
				if (parent.Count == 1) {
					foreach (Tag tag in tags) {
						if ((parent.Last as Literal).Tag == tag) {
							(parent.Last as Literal).RemoveSelf ();
							needsUpdate = true;
							break;
						}
					}
				}
			}
			preventUpdate = false;

			if (needsUpdate)
				UpdateQuery ();
		}

		// AND this tag with all terms
		public void Require (Tag [] tags)
		{
			// TODO it would be awesome if this was done by putting parentheses around
			// OR terms and ANDing the result with this term (eg factored out)

			// Trim out tags that are already required
			var new_tags = new List<Tag> (tags.Length);
			foreach (Tag tag in tags) {
				if (! rootTerm.TagRequired (tag))
					new_tags.Add (tag);
			}

			if (new_tags.Count == 0)
				return;

			tags = new_tags.ToArray ();

			bool added = false;
			preventUpdate = true;
			foreach (Term parent in rootTerm.LiteralParents ()) {
				// TODO logic could be broken if a term's SubTerms are a mixture
				// of Literals and non-Literals
				InsertTerm (tags, parent, parent.Last as Literal);
				added = true;
			}

			// If there were no LiteralParents to add this tag to, then add it to the rootTerm
			// TODO should add the first tag in the array,
			// then add the others to the first's parent (so they will be ANDed together)
			if (!added)
				InsertTerm (tags, rootTerm, null);

			preventUpdate = false;

			UpdateQuery ();
		}

		public void UnRequire (Tag [] tags)
		{
			// Trim out tags that are not required
			var new_tags = new List<Tag> (tags.Length);
			foreach (Tag tag in tags) {
				if (rootTerm.TagRequired (tag))
					new_tags.Add (tag);
			}

			if (new_tags.Count == 0)
				return;

			tags = new_tags.ToArray ();

			preventUpdate = true;
			foreach (Term parent in rootTerm.LiteralParents ()) {
				// Don't remove if this tag is the only child of a term
				if (parent.Count > 1) {
					foreach (Tag tag in tags) {
						((parent.FindByTag (tag))[0] as Literal).RemoveSelf ();
					}
				}
			}

			preventUpdate = false;

			UpdateQuery ();
		}

		public List<Literal> InsertTerm (Tag [] tags, Term parent, Literal after)
		{
			int position;
			if (after != null)
				position = WidgetPosition (after.Widget) + 1;
			else
				position = Children.Length - 1;

			var added = new List<Literal>();

			foreach (Tag tag in tags) {
				//Console.WriteLine ("Adding tag {0}", tag.Name);

				// Don't put a tag into a Term twice
				if (parent != Root && (parent.FindByTag (tag, true)).Count > 0)
					continue;

				if (parent.Count > 0) {
					Widget sep = parent.SeparatorWidget ();

					InsertWidget (position, sep);
					position++;
				}

				// Encapsulate new OR terms within a new AND term of which they are the
				// only member, so later other terms can be AND'd with them
				//
				// TODO should really see what type of term the parent is, and
				// encapsulate this term in a term of the opposite type. This will
				// allow the query system to be expanded to work for multiple levels much easier.
				if (parent == rootTerm) {
					parent = new AndTerm (rootTerm, after);
					after = null;
				}

				Literal term  = new Literal (parent, tag, after);
				term.TagsAdded  += HandleTagsAdded;
				term.LiteralsMoved += HandleLiteralsMoved;
				term.AttachTag  += HandleAttachTag;
				term.NegatedToggled += HandleNegated;
				term.Removing  += HandleRemoving;
				term.Removed  += HandleRemoved;
				term.RequireTag  += Require;
				term.UnRequireTag += UnRequire;

				added.Add (term);

				// Insert this widget into the appropriate place in the hbox
				InsertWidget (position, term.Widget);
			}

			UpdateQuery ();

			return added;
		}

		// Update the query, which updates the icon_view
		public void UpdateQuery ()
		{
			if (preventUpdate)
				return;

			if (sepBox != null)
				sepBox.Hide ();

			if (rootTerm.Count == 0) {
				help.Show ();
				query.TagTerm = null;
			} else {
				help.Hide ();
				query.TagTerm = new ConditionWrapper (rootTerm.SqlCondition ());
			}

			Changed?.Invoke (this, new EventArgs ());
		}

		public bool IsClear => rootTerm.Count == 0;

		public void Clear ()
		{
			// Clear out the query, starting afresh
			foreach (Widget widget in Children) {
				Remove (widget);
				widget.Destroy ();
			}
			Init ();
		}
	}
}
