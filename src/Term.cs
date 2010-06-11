/*
 * Term.cs
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mono.Unix;
using Gtk;
using Gdk;
using Hyena;

namespace FSpot {
	public abstract class Term {
		private ArrayList sub_terms = new ArrayList ();
		private Term parent = null;

		protected bool is_negated = false;
		protected Tag tag = null;

		public Term (Term parent, Literal after)
		{
			this.parent = parent;

			if (parent != null) {
				if (after == null)
					parent.Add (this);
				else
					parent.SubTerms.Insert (parent.SubTerms.IndexOf (after) + 1, this);
			}
		}

		/** Properties **/
		public bool HasMultiple {
			get {
				return (SubTerms.Count > 1);
			}
		}

		public ArrayList SubTerms {
			get {
				return sub_terms;
			}
		}

		public Term Last {
			get {
				// Return the last Literal in this term
				if (SubTerms.Count > 0)
					return SubTerms[SubTerms.Count - 1] as Term;
				else
					return null;
			}
		}

		public int Count {
			get {
				return SubTerms.Count;
			}
		}

		public Term Parent {
			get { return parent; }
			set {
				if (parent == value)
					return;

				// If our parent was already set, remove ourself from it
				if (parent != null)
					parent.Remove(this);

				// Add ourself to our new parent
				parent = value;
				parent.Add(this);
			}
		}

		public virtual bool IsNegated {
			get { return is_negated; }
			set {
				if (is_negated != value)
					Invert(false);

				is_negated = value;
			}
		}


		/** Methods **/

		public void Add (Term term)
		{
			SubTerms.Add (term);
		}

		public void Remove (Term term)
		{
			SubTerms.Remove (term);

			// Remove ourselves if we're now empty
			if (SubTerms.Count == 0)
				if (Parent != null)
					Parent.Remove (this);
		}

		public void CopyAndInvertSubTermsFrom (Term term, bool recurse)
		{
			is_negated = true;
			ArrayList termsToMove = new ArrayList(term.SubTerms);
			foreach (Term subterm in termsToMove) {
				if (recurse)
					subterm.Invert(true).Parent = this;
				else
					subterm.Parent = this;
			}
		}

		public ArrayList FindByTag (Tag t)
		{
			return FindByTag (t, true);
		}

		public ArrayList FindByTag (Tag t, bool recursive)
		{
			ArrayList results = new ArrayList ();

			if (tag != null && tag == t)
				results.Add (this);

			if (recursive)
				foreach (Term term in SubTerms)
				results.AddRange (term.FindByTag (t, true));
			else
				foreach (Term term in SubTerms) {
				foreach (Term literal in SubTerms) {
					if (literal.tag != null && literal.tag == t) {
						results.Add (literal);
					}
				}

				if (term.tag != null && term.tag == t) {
					results.Add (term);
				}
			}

			return results;
		}

		public ArrayList LiteralParents ()
		{
			ArrayList results = new ArrayList ();

			bool meme = false;
			foreach (Term term in SubTerms) {
				if (term is Literal)
					meme = true;

				results.AddRange (term.LiteralParents ());
			}

			if (meme)
				results.Add (this);

			return results;
		}

		public bool TagIncluded(Tag t)
		{
			ArrayList parents = LiteralParents ();

			if (parents.Count == 0)
				return false;

			foreach (Term term in parents) {
				bool termHasTag = false;
				bool onlyTerm = true;
				foreach (Term literal in term.SubTerms) {
					if (literal.tag != null) {
						if (literal.tag == t) {
							termHasTag = true;
						} else {
							onlyTerm = false;
						}
					}
				}

				if (termHasTag && onlyTerm)
					return true;
			}

			return false;
		}

		public bool TagRequired(Tag t)
		{
			int count, grouped_with;
			return TagRequired(t, out count, out grouped_with);
		}

		public bool TagRequired(Tag t, out int num_terms, out int grouped_with)
		{
			ArrayList parents = LiteralParents ();

			num_terms = 0;
			grouped_with = 100;
			int min_grouped_with = 100;

			if (parents.Count == 0)
				return false;

			foreach (Term term in parents) {
				bool termHasTag = false;

				// Don't count it as required if it's the only subterm..though it is..
				// it is more clearly identified as Included at that point.
				if (term.Count > 1) {
					foreach (Term literal in term.SubTerms) {
						if (literal.tag != null) {
							if (literal.tag == t) {
								num_terms++;
								termHasTag = true;
								grouped_with = term.SubTerms.Count;
								break;
							}
						}
					}
				}

				if (grouped_with < min_grouped_with)
					min_grouped_with = grouped_with;

				if (!termHasTag)
					return false;
			}

			grouped_with = min_grouped_with;

			return true;
		}

		public abstract Term Invert(bool recurse);

		// Recursively generate the SQL condition clause that this
		// term represents.
		public virtual string SqlCondition ()
		{
			StringBuilder condition = new StringBuilder ("(");

			for (int i = 0; i < SubTerms.Count; i++) {
				Term term = SubTerms[i] as Term;
				condition.Append (term.SqlCondition ());

				if (i != SubTerms.Count - 1)
					condition.Append (SQLOperator ());
			}

			condition.Append(")");

			return condition.ToString ();
		}

		public virtual Gtk.Widget SeparatorWidget ()
		{
			return null;
		}

		public virtual string SQLOperator ()
		{
			return String.Empty;
		}

		protected static Hashtable op_term_lookup = new Hashtable();
		public static Term TermFromOperator (string op, Term parent, Literal after)
		{
			//Console.WriteLine ("finding type for operator {0}", op);
			//op = op.Trim ();
			op = op.ToLower ();

			if (AndTerm.Operators.Contains (op)) {
				//Console.WriteLine ("AND!");
				return new AndTerm (parent, after);
			} else if (OrTerm.Operators.Contains (op)) {
				//Console.WriteLine ("OR!");
				return new OrTerm (parent, after);
			}

			Log.DebugFormat ("Do not have Term for operator {0}", op);
			return null;
		}
	}

	public class AndTerm : Term {
		static ArrayList operators = new ArrayList ();
		static AndTerm () {
			operators.Add (Catalog.GetString (" and "));
			//operators.Add (Catalog.GetString (" && "));
			operators.Add (Catalog.GetString (", "));
		}

		public static ArrayList Operators {
			get { return operators; }
		}

		public AndTerm (Term parent, Literal after) : base (parent, after) {}

		public override Term Invert (bool recurse)
		{
			OrTerm newme = new OrTerm(Parent, null);
			newme.CopyAndInvertSubTermsFrom(this, recurse);
			if (Parent != null)
				Parent.Remove(this);
			return newme;
		}

		public override Widget SeparatorWidget ()
		{
			Widget sep = new Label (String.Empty);
			sep.SetSizeRequest (3, 1);
			sep.Show ();
			return sep;
			//return null;
		}

		public override string SqlCondition ()
		{
			StringBuilder condition = new StringBuilder ("(");

			condition.Append (base.SqlCondition());

			Tag hidden = App.Instance.Database.Tags.Hidden;
			if (hidden != null) {
				if (FindByTag (hidden, true).Count == 0) {
					condition.Append (String.Format (
								  " AND id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {0})", hidden.Id
							  ));
				}
			}

			condition.Append (")");

			return condition.ToString ();
		}

		public override string SQLOperator ()
		{
			return " AND ";
		}
	}

	public class OrTerm : Term {
		static ArrayList operators = new ArrayList ();
		static OrTerm () {
			operators.Add (Catalog.GetString (" or "));
			//operators.Add (Catalog.GetString (" || "));
		}

		public static OrTerm FromTags(Tag [] from_tags)
		{
			if (from_tags == null || from_tags.Length == 0)
				return null;

			OrTerm or = new OrTerm(null, null);
			foreach (Tag t in from_tags) {
				Literal l = new Literal(t);
				l.Parent = or;
			}
			return or;
		}


		public static ArrayList Operators {
			get { return operators; }
		}

		public OrTerm (Term parent, Literal after) : base (parent, after) {}

		private static string OR = Catalog.GetString ("or");

		public override Term Invert (bool recurse)
		{
			AndTerm newme = new AndTerm(Parent, null);
			newme.CopyAndInvertSubTermsFrom(this, recurse);
			if (Parent != null)
				Parent.Remove(this);
			return newme;
		}

		public override Gtk.Widget SeparatorWidget ()
		{
			Widget label = new Label (" " + OR + " ");
			label.Show ();
			return label;
		}

		public override string SQLOperator ()
		{
			return " OR ";
		}
	}

	public abstract class AbstractLiteral : Term {
		public AbstractLiteral(Term parent, Literal after) : base (parent, after) {}

		public override Term Invert (bool recurse)
		{
			is_negated = !is_negated;
			return this;
		}
	}

	// TODO rename to TagLiteral?
	public class Literal : AbstractLiteral {
		public Literal (Tag tag) : this (null, tag, null)
		{
		}

		public Literal (Term parent, Tag tag, Literal after) : base (parent, after) {
			this.tag = tag;
		}

		/** Properties **/

		public static ArrayList FocusedLiterals
		{
			get {
				return focusedLiterals;
			}
			set {
				focusedLiterals = value;
			}
		}

		public Tag Tag {
			get {
				return tag;
			}
		}

		public override bool IsNegated {
			get {
				return is_negated;
			}

			set {
				if (is_negated == value)
					return;

				is_negated = value;

				NormalIcon = null;
				NegatedIcon = null;
				Update ();

				if (NegatedToggled != null)
					NegatedToggled (this);
			}
		}

		private Pixbuf NegatedIcon
		{
			get {
				if (negated_icon != null)
					return negated_icon;

				if (NormalIcon == null)
					return null;

				negated_icon = NormalIcon.Copy ();

				int offset = ICON_SIZE - overlay_size;
				NegatedOverlay.Composite (negated_icon, offset, 0, overlay_size, overlay_size, offset, 0, 1.0, 1.0, InterpType.Bilinear, 200);

				return negated_icon;
			}

			set {
				negated_icon = null;
			}
		}

		public Widget Widget {
			get {
				if (widget != null)
					return widget;

				container = new EventBox ();
				box = new HBox ();

				handle_box = new LiteralBox ();
				handle_box.BorderWidth = 1;

				label = new Label (System.Web.HttpUtility.HtmlEncode (tag.Name));
				label.UseMarkup = true;

				image = new Gtk.Image (NormalIcon);

				container.CanFocus = true;

				container.KeyPressEvent  += KeyHandler;
				container.ButtonPressEvent += HandleButtonPress;
				container.ButtonReleaseEvent += HandleButtonRelease;
				container.EnterNotifyEvent += HandleMouseIn;
				container.LeaveNotifyEvent += HandleMouseOut;

				//new PopupManager (new LiteralPopup (container, this));

				// Setup this widget as a drag source (so tags can be moved after being placed)
				container.DragDataGet += HandleDragDataGet;
				container.DragBegin += HandleDragBegin;
				container.DragEnd += HandleDragEnd;

				Gtk.Drag.SourceSet (container, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
						    tag_target_table, DragAction.Copy | DragAction.Move);

				// Setup this widget as a drag destination (so tags can be added to our parent's Term)
				container.DragDataReceived += HandleDragDataReceived;
				container.DragMotion += HandleDragMotion;
				container.DragLeave += HandleDragLeave;

				Gtk.Drag.DestSet (container, DestDefaults.All, tag_dest_target_table,
						  DragAction.Copy | DragAction.Move );

				container.TooltipText = tag.Name;

				label.Show ();
				image.Show ();

				if (tag.Icon == null) {
					handle_box.Add (label);
				} else {
					handle_box.Add (image);
				}

				handle_box.Show ();

				box.Add (handle_box);
				box.Show ();

				container.Add (box);

				widget = container;

				return widget;
			}
		}

		private Pixbuf NormalIcon
		{
			get {
				if (normal_icon != null)
					return normal_icon;

				Pixbuf scaled = null;
				scaled = tag.Icon;

				for (Category category = tag.Category; category != null && scaled == null; category = category.Category)
					scaled = category.Icon;

				if (scaled == null)
					return null;

				if (scaled.Width != ICON_SIZE) {
					scaled = scaled.ScaleSimple (ICON_SIZE, ICON_SIZE, InterpType.Bilinear);
				}

				normal_icon = scaled;

				return normal_icon;
			}

			set {
				normal_icon = null;
			}
		}

		/** Methods **/
		public void Update ()
		{
			// Clear out the old icons
			normal_icon = null;
			negated_icon = null;
			if (IsNegated) {
				widget.TooltipText = String.Format (Catalog.GetString ("Not {0}"), tag.Name);
				label.Text = "<s>" + System.Web.HttpUtility.HtmlEncode (tag.Name) + "</s>";
				image.Pixbuf = NegatedIcon;
			} else {
				widget.TooltipText = tag.Name;
				label.Text = System.Web.HttpUtility.HtmlEncode (tag.Name);
				image.Pixbuf = NormalIcon;
			}

			label.UseMarkup = true;

			// Show the icon unless it's null
			if (tag.Icon == null && container.Children [0] == image) {
				container.Remove (image);
				container.Add (label);
			} else if (tag.Icon != null && container.Children [0] == label) {
				container.Remove (label);
				container.Add (image);
			}


			if (isHoveredOver && image.Pixbuf != null ) {
				// Brighten the image slightly
				Pixbuf brightened = image.Pixbuf.Copy ();
				image.Pixbuf.SaturateAndPixelate (brightened, 1.85f, false);
				//Pixbuf brightened = PixbufUtils.Glow (image.Pixbuf, .6f);

				image.Pixbuf = brightened;
			}
		}

		public void RemoveSelf ()
		{
			if (Removing != null)
				Removing (this);

			if (Parent != null)
				Parent.Remove (this);

			if (Removed != null)
				Removed (this);
		}

		public override string SqlCondition ()
		{
			StringBuilder ids = new StringBuilder (tag.Id.ToString ());

			if (tag is Category) {
				List<Tag> tags = new List<Tag> ();
				(tag as Category).AddDescendentsTo (tags);

				for (int i = 0; i < tags.Count; i++)
					ids.Append (", " + (tags [i] as Tag).Id.ToString ());
			}

			return String.Format (
				       "id {0}IN (SELECT photo_id FROM photo_tags WHERE tag_id IN ({1}))",
				       (IsNegated ? "NOT " : String.Empty), ids.ToString ());
		}

		public override Gtk.Widget SeparatorWidget ()
		{
			return new Label ("ERR");
		}

		private static Pixbuf NegatedOverlay
		{
			get {
				if (negated_overlay == null) {
					System.Reflection.Assembly assembly = System.Reflection.Assembly.GetCallingAssembly ();
					negated_overlay = new Pixbuf (assembly.GetManifestResourceStream ("f-spot-not.png"));
					negated_overlay = negated_overlay.ScaleSimple (overlay_size, overlay_size, InterpType.Bilinear);
				}

				return negated_overlay;
			}
		}

		public static void RemoveFocusedLiterals ()
		{
			if (focusedLiterals != null)
				foreach (Literal literal in focusedLiterals)
				literal.RemoveSelf ();
		}

		/** Handlers **/

		private void KeyHandler (object o, KeyPressEventArgs args)
		{
			args.RetVal = false;

			switch (args.Event.Key) {
			case Gdk.Key.Delete:
				RemoveFocusedLiterals ();
				args.RetVal = true;
				return;
			}
		}

		private void HandleButtonPress (object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;

			switch (args.Event.Type) {
			case EventType.TwoButtonPress:
				if (args.Event.Button == 1)
					IsNegated = !IsNegated;
				else
					args.RetVal = false;
				return;

			case EventType.ButtonPress:
				Widget.GrabFocus ();

				if (args.Event.Button == 1) {
					// TODO allow multiple selection of literals so they can be deleted, modified all at once
					//if ((args.Event.State & ModifierType.ControlMask) != 0) {
					//}

				}
				else if (args.Event.Button == 3)
				{
					LiteralPopup popup = new LiteralPopup ();
					popup.Activate (args.Event, this);
				}

				return;

			default:
				args.RetVal = false;
				return;
			}
		}

		private void HandleButtonRelease (object o, ButtonReleaseEventArgs args)
		{
			args.RetVal = true;

			switch (args.Event.Type) {
			case EventType.TwoButtonPress:
				args.RetVal = false;
				return;

			case EventType.ButtonPress:
				if (args.Event.Button == 1) {
				}
				return;

			default:
				args.RetVal = false;
				return;
			}
		}

		private void HandleMouseIn (object o, EnterNotifyEventArgs args)
		{
			isHoveredOver = true;
			Update ();
		}

		private void HandleMouseOut (object o, LeaveNotifyEventArgs args)
		{
			isHoveredOver = false;
			Update ();
		}

		void HandleDragDataGet (object sender, DragDataGetArgs args)
		{
			args.RetVal = true;
			
			if (args.Info == DragDropTargets.TagListEntry.Info || args.Info == DragDropTargets.TagQueryEntry.Info) {
				
				// FIXME: do really write data
				Byte [] data = Encoding.UTF8.GetBytes (String.Empty);
				Atom [] targets = args.Context.Targets;

				args.SelectionData.Set (targets[0], 8, data, data.Length);

				return;
			}
			
			// Drop cancelled
			args.RetVal = false;

			foreach (Widget w in hiddenWidgets)
				w.Visible = true;

			focusedLiterals = null;
		}

		void HandleDragBegin (object sender, DragBeginArgs args)
		{
			Gtk.Drag.SetIconPixbuf (args.Context, image.Pixbuf, 0, 0);

			focusedLiterals.Add (this);

			// Hide the tag and any separators that only exist because of it
			container.Visible = false;
			hiddenWidgets.Add (container);
			foreach (Widget w in LogicWidget.Box.HangersOn (this)) {
				hiddenWidgets.Add (w);
				w.Visible = false;
			}
		}

		void HandleDragEnd (object sender, DragEndArgs args)
		{
			// Remove any literals still marked as focused, because
			// the user is throwing them away.
			RemoveFocusedLiterals ();

			focusedLiterals = new ArrayList();
			args.RetVal = true;
		}

		private void HandleDragDataReceived (object o, DragDataReceivedArgs args)
		{
			args.RetVal = true;
			
			if (args.Info == DragDropTargets.TagListEntry.Info) {

				if (TagsAdded != null)
					TagsAdded (args.SelectionData.GetTagsData (), Parent, this);
				
				return;
			}
			
			if (args.Info == DragDropTargets.TagQueryEntry.Info) {

				if (! focusedLiterals.Contains(this))
					if (LiteralsMoved != null)
						LiteralsMoved (focusedLiterals, Parent, this);

				// Unmark the literals as focused so they don't get nixed
				focusedLiterals = null;
			}
		}

		private bool preview = false;
		private Gtk.Widget preview_widget;
		private void HandleDragMotion (object o, DragMotionArgs args)
		{
			if (!preview) {
				if (preview_widget == null) {
					preview_widget = new Gtk.Label (" | ");
					box.Add (preview_widget);
				}

				preview_widget.Show ();
			}
		}

		private void HandleDragLeave (object o, EventArgs args)
		{
			preview = false;
			preview_widget.Hide ();
		}

		public void HandleToggleNegatedCommand (object o, EventArgs args)
		{
			IsNegated = !IsNegated;
		}

		public void HandleRemoveCommand (object o, EventArgs args)
		{
			RemoveSelf ();
		}

		public void HandleAttachTagCommand (Tag t)
		{
			if (AttachTag != null)
				AttachTag (t, Parent, this);
		}

		public void HandleRequireTag (object sender, EventArgs args)
		{
			if (RequireTag != null)
				RequireTag (new Tag [] {this.Tag});
		}

		public void HandleUnRequireTag (object sender, EventArgs args)
		{
			if (UnRequireTag != null)
				UnRequireTag (new Tag [] {this.Tag});
		}

		private const int ICON_SIZE = 24;

		private const int overlay_size = (int) (.40 * ICON_SIZE);

		private static TargetEntry [] tag_target_table =
			new TargetEntry [] { DragDropTargets.TagQueryEntry };

		private static TargetEntry [] tag_dest_target_table =
			new TargetEntry [] {
				DragDropTargets.TagListEntry,
				DragDropTargets.TagQueryEntry
			};

		private static ArrayList focusedLiterals = new ArrayList();
		private static ArrayList hiddenWidgets = new ArrayList();
		private Gtk.Container container;
		private LiteralBox handle_box;
		private Gtk.Box box;
		private Gtk.Image image;
		private Gtk.Label label;

		private Pixbuf normal_icon;
		//private EventBox widget;
		private Widget widget;
		private Pixbuf negated_icon;
		private static Pixbuf negated_overlay;
		private bool isHoveredOver = false;

		public delegate void NegatedToggleHandler (Literal group);
		public event NegatedToggleHandler NegatedToggled;

		public delegate void RemovingHandler (Literal group);
		public event RemovingHandler Removing;

		public delegate void RemovedHandler (Literal group);
		public event RemovedHandler Removed;

		public delegate void TagsAddedHandler (Tag[] tags, Term parent, Literal after);
		public event TagsAddedHandler TagsAdded;

		public delegate void AttachTagHandler (Tag tag, Term parent, Literal after);
		public event AttachTagHandler AttachTag;

		public delegate void TagRequiredHandler (Tag [] tags);
		public event TagRequiredHandler RequireTag;

		public delegate void TagUnRequiredHandler (Tag [] tags);
		public event TagUnRequiredHandler UnRequireTag;

		public delegate void LiteralsMovedHandler (ArrayList literals, Term parent, Literal after);
		public event LiteralsMovedHandler LiteralsMoved;
	}

	public class TextLiteral : AbstractLiteral {
		private string text;

		public TextLiteral (Term parent, string text) : base (parent, null)
		{
			this.text = text;
		}

		public override string SqlCondition ()
		{
			return String.Format (
				       "id {0}IN (SELECT id FROM photos WHERE base_uri LIKE '%{1}%' OR filename LIKE '%{1}%' OR description LIKE '%{1}%')",
				       (IsNegated ? "NOT " : ""), EscapeQuotes(text)
			       );
		}

		protected static string EscapeQuotes (string v)
		{
			return v == null ? String.Empty : v.Replace("'", "''");
		}
	}
}
