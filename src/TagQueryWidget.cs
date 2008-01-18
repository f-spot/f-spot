using System;
using System.Collections;
using System.Text;
using Mono.Unix;
using Gtk;
using Gdk;

using FSpot.Utils;
namespace FSpot
{
	public class LiteralPopup
	{
		//private Literal literal;

		public void Activate (Gdk.EventButton eb, Literal literal)
		{
			Activate (eb, literal, new Gtk.Menu (), true);
		}

		public void Activate (Gdk.EventButton eb, Literal literal, Gtk.Menu popup_menu, bool is_popup)
		{
			//this.literal = literal;

			/*MenuItem attach_item = new MenuItem (Catalog.GetString ("Find With"));
			TagMenu attach_menu = new TagMenu (attach_item, MainWindow.Toplevel.Database.Tags);
			attach_menu.TagSelected += literal.HandleAttachTagCommand;
			attach_item.ShowAll ();
			popup_menu.Append (attach_item);*/

			if (literal.IsNegated) {
				GtkUtil.MakeMenuItem (popup_menu,
						      String.Format (Catalog.GetString ("Include Photos Tagged \"{0}\""), literal.Tag.Name),
						      new EventHandler (literal.HandleToggleNegatedCommand),
						      true);
			} else {
				GtkUtil.MakeMenuItem (popup_menu,
						      String.Format (Catalog.GetString ("Exclude Photos Tagged \"{0}\""), literal.Tag.Name),
						      new EventHandler (literal.HandleToggleNegatedCommand),
						      true);
			}

			GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Remove From Search"),
					      "gtk-remove",
					      new EventHandler (literal.HandleRemoveCommand),
					      true);

			if (is_popup) {
				if (eb != null)
					popup_menu.Popup (null, null, null, eb.Button, eb.Time);
				else
					popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
			}
		}
	}

	public class LiteralMenu : Menu
	{
		private LiteralPopup popup;
		private Literal literal;

		public LiteralMenu (MenuItem item, Literal literal)
		{
			popup = new LiteralPopup ();

			this.literal = literal;

			item.Submenu = this;
			item.Activated += HandlePopulate;
		}

		private void HandlePopulate (object obj, EventArgs args)
		{
			foreach (Widget child in Children) {
				Remove (child);
				child.Destroy ();
			}

			popup.Activate (null, literal, this, false);
		}
	}

	public static class TermMenuItem
	{
		public static void Create (Tag [] tags, Gtk.Menu menu)
		{
			Gtk.MenuItem item = new Gtk.MenuItem (String.Format (Catalog.GetPluralString ("Find _With", "Find _With", tags.Length), tags.Length));

			Gtk.Menu submenu = GetSubmenu (tags);
			if (submenu == null)
				item.Sensitive = false;
			else
				item.Submenu = submenu;

			menu.Append (item);
			item.Show ();
		}

		public static Gtk.Menu GetSubmenu (Tag [] tags)
		{
			Tag single_tag = null;
			if (tags != null && tags.Length == 1)
				single_tag = tags[0];

			//Console.WriteLine ("creating find with menu item");
			if (LogicWidget.Root == null || LogicWidget.Root.SubTerms.Count == 0) {
				//Console.WriteLine ("root is null or has no terms");
				return null;
			} else {
				//Console.WriteLine ("root is not null and has terms");
				Gtk.Menu m = new Gtk.Menu ();

				Gtk.MenuItem all_item = GtkUtil.MakeMenuItem (m, Catalog.GetString ("All"), new EventHandler (MainWindow.Toplevel.HandleRequireTag));
				GtkUtil.MakeMenuSeparator (m);

				int sensitive_items = 0;
				foreach (Term term in LogicWidget.Root.SubTerms) {
					ArrayList term_parts = new ArrayList ();

					bool contains_tag = AppendTerm (term_parts, term, single_tag);

					string name = "_" + String.Join (", ", (string []) term_parts.ToArray (typeof(string)));

					Gtk.MenuItem item = GtkUtil.MakeMenuItem (m, name, new EventHandler (MainWindow.Toplevel.HandleAddTagToTerm));
					item.Sensitive = !contains_tag;

					if (!contains_tag)
						sensitive_items++;
				}

				if (sensitive_items == 0)
					all_item.Sensitive = false;

				return m;
			}
		}

		private static bool AppendTerm (ArrayList parts, Term term, Tag single_tag)
		{
			bool tag_matches = false;
			if (term != null) {
				Literal literal = term as Literal;
				if (literal != null) {
					if (literal.Tag == single_tag)
						tag_matches = true;

					if (literal.IsNegated)
						parts.Add (String.Format (Catalog.GetString ("Not {0}"), literal.Tag.Name));
					else
						parts.Add (literal.Tag.Name);
				} else {
					foreach (Term subterm in term.SubTerms) {
						tag_matches |= AppendTerm (parts, subterm, single_tag);
					}
				}
			}

			return tag_matches;
		}
	}

	public class LiteralBox : VBox {
		private GrabHandle handle;

		public LiteralBox () : base ()
		{
			handle = new GrabHandle (24, 8);

			PackEnd (handle, false, false, 0);

			Show ();
		}
	}

	public class GrabHandle : DrawingArea {
		public GrabHandle (int w, int h) : base ()
		{
			Size (w, h);
			Orientation = Gtk.Orientation.Horizontal;
			Show ();
		}

		private Gtk.Orientation orientation;
		public Gtk.Orientation Orientation {
			get { return orientation; }
			set { orientation = value; }
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			bool ret = base.OnExposeEvent(evnt);

			if (evnt.Window != GdkWindow) {
				return ret;
			}

			Gtk.Style.PaintHandle(Style, GdkWindow, State, ShadowType.In,
					      evnt.Area, this, "entry", 0, 0, Allocation.Width, Allocation.Height, Orientation);

			//(Style, GdkWindow, StateType.Normal, ShadowType.In,
			//evnt.Area, this, "entry", 0, y_mid - y_offset, Allocation.Width,
			//Height + (y_offset * 2));

			return ret;
		}
	}

	public class LogicWidget : HBox {
		private PhotoQuery query;
		private TagSelectionWidget tag_selection_widget;

		private static Tooltips tips = new Tooltips ();

		private static Term rootTerm;
		private EventBox rootAdd;
		private HBox rootBox;
		private Label help;
		private HBox sepBox;

		private bool preventUpdate = false;
		private bool preview = false;

		public event EventHandler Changed;

		public static Term Root
		{
			get {
				return rootTerm;
			}
		}

		private static LogicWidget logic_widget = null;
		public static LogicWidget Box {
			get { return logic_widget; }
		}

		// Drag and Drop
		private static TargetEntry [] tag_dest_target_table = new TargetEntry [] {
					new TargetEntry ("application/x-fspot-tags", 0, (uint) MainWindow.TargetType.TagList),
					new TargetEntry ("application/x-fspot-tag-query-item", 0, (uint) MainWindow.TargetType.TagQueryItem),
				};

		public LogicWidget (PhotoQuery query, TagStore tag_store, TagSelectionWidget selector) : base ()
		{
			//SetFlag (WidgetFlags.NoWindow);
			this.query = query;
			this.tag_selection_widget = selector;

			CanFocus = true;
			Sensitive = true;

			Literal.Tips = tips;

			tips.Enable ();

			Init ();

			tag_store.ItemsChanged += HandleTagChanged;
			tag_store.ItemsRemoved += HandleTagDeleted;

			Show ();

			logic_widget = this;
		}

		private void Init ()
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

		private void Preview ()
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
		private void HandleTagChanged (object sender, DbItemEventArgs args)
		{
			foreach (DbItem item in args.Items)
			foreach (Literal term in rootTerm.FindByTag (item as Tag))
			term.Update ();
		}

		// If the user deletes a tag that is in use in the query, remove it from the query too.
		private void HandleTagDeleted (object sender, DbItemEventArgs args)
		{
			foreach (DbItem item in args.Items)
			foreach (Literal term in rootTerm.FindByTag (item as Tag))
			term.RemoveSelf ();
		}

		private void HandleDragMotion (object o, DragMotionArgs args)
		{
			if (!preview && rootTerm.Count > 0 && (Literal.FocusedLiterals.Count == 0 || Children.Length > 2)) {
				Preview ();
				preview = true;
			}
		}

		private void HandleDragLeave (object o, EventArgs args)
		{
			if (preview && Children.Length > 1) {
				sepBox.Hide ();
				preview = false;
			} else if (preview && Children.Length == 1) {
				help.Show ();
			}
		}

		private void HandleLiteralsMoved (ArrayList literals, Term parent, Literal after)
		{
			preventUpdate = true;
			foreach (Literal term in literals) {
				Tag tag = term.Tag;

				// Don't listen for it to be removed since we are
				// moving it. We will update when we're done.
				term.Removed -= HandleRemoved;
				term.RemoveSelf ();

				// Add it to where it was dropped
				ArrayList groups = InsertTerm (new Tag[] {tag}, parent, after);

				if (term.IsNegated)
					foreach (Literal group in groups)
					group.IsNegated = true;
			}
			preventUpdate = false;
			UpdateQuery ();
		}

		private void HandleTermAdded (Term parent, Literal after)
		{
			InsertTerm (parent, after);
		}

		private void HandleAttachTag (Tag tag, Term parent, Literal after)
		{
			InsertTerm (new Tag [] {tag}, parent, after);
		}

		private void HandleNegated (Literal group)
		{
			UpdateQuery ();
		}

		private void HandleRemoving (Literal term)
		{
			foreach (Widget w in HangersOn (term))
			Remove (w);

			// Remove the term's widget
			Remove (term.Widget);
		}

		public ArrayList HangersOn (Literal term)
		{
			ArrayList w = new ArrayList ();

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

		private void HandleRemoved (Literal group)
		{
			UpdateQuery ();
		}

		private void HandleDragDataReceived (object o, DragDataReceivedArgs args)
		{
			InsertTerm (rootTerm, null);

			args.RetVal = true;
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
		private void InsertWidget (int index, Gtk.Widget widget) {
			widget.Visible = true;
			PackStart (widget, false, false, 0);
			ReorderChild (widget, index);
		}

		// Return the index position of a widget in this Box
		private int WidgetPosition (Gtk.Widget widget)
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
			ArrayList new_tags = new ArrayList(tags.Length);
			foreach (Tag tag in tags) {
				if (! rootTerm.TagIncluded (tag))
					new_tags.Add (tag);

			}

			if (new_tags.Count == 0)
				return;

			tags = (Tag []) new_tags.ToArray (typeof (Tag));

			InsertTerm (tags, rootTerm, null);
		}

		public void UnInclude (Tag [] tags)
		{
			ArrayList new_tags = new ArrayList(tags.Length);
			foreach (Tag tag in tags) {
				if (rootTerm.TagIncluded (tag))
					new_tags.Add (tag);
			}

			if (new_tags.Count == 0)
				return;

			tags = (Tag []) new_tags.ToArray (typeof (Tag));

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
			ArrayList new_tags = new ArrayList(tags.Length);
			foreach (Tag tag in tags) {
				if (! rootTerm.TagRequired (tag))
					new_tags.Add (tag);
			}

			if (new_tags.Count == 0)
				return;

			tags = (Tag []) new_tags.ToArray (typeof (Tag));

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
			ArrayList new_tags = new ArrayList(tags.Length);
			foreach (Tag tag in tags) {
				if (rootTerm.TagRequired (tag))
					new_tags.Add (tag);
			}

			if (new_tags.Count == 0)
				return;

			tags = (Tag []) new_tags.ToArray (typeof (Tag));

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

		private void InsertTerm (Term parent, Literal after)
		{
			if (Literal.FocusedLiterals.Count != 0) {
				HandleLiteralsMoved (Literal.FocusedLiterals, parent, after);

				// Prevent them from being removed again
				Literal.FocusedLiterals = null;
			}
			else
				InsertTerm (tag_selection_widget.TagHighlight, parent, after);
		}

		public ArrayList InsertTerm (Tag [] tags, Term parent, Literal after)
		{
			int position;
			if (after != null)
				position = WidgetPosition (after.Widget) + 1;
			else
				position = Children.Length - 1;

			ArrayList added = new ArrayList ();

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
				term.TermAdded  += HandleTermAdded;
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
				query.ExtraCondition = null;
			} else {
				help.Hide ();
				query.ExtraCondition = rootTerm.SqlCondition ();
				//Console.WriteLine ("extra_condition = {0}", query.ExtraCondition);
			}

			EventHandler handler = Changed;
			if (handler != null)
				handler (this, new EventArgs ());
		}

		public bool Clear
		{
			get {
				return rootTerm.Count == 0;
			}

			set {
				// Clear out the query, starting afresh
				foreach (Widget widget in Children) {
					Remove (widget);
					widget.Destroy ();
				}
				Init ();
			}
		}
	}
}
