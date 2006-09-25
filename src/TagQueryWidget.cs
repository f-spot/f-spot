using System;
using System.Collections;
using System.Text;
using Mono.Posix;
using Gtk;
using Gdk;

namespace FSpot.Query
{
	public class LiteralPopup
	{
		private Literal literal;

		public void Activate (Gdk.EventButton eb, Literal literal)
        {
            Activate (eb, literal, new Gtk.Menu (), true);
        }

		public void Activate (Gdk.EventButton eb, Literal literal, Gtk.Menu popup_menu, bool is_popup)
		{
			this.literal = literal;

			/*MenuItem attach_item = new MenuItem (Catalog.GetString ("Find With"));
			TagMenu attach_menu = new TagMenu (attach_item, MainWindow.Toplevel.Database.Tags);
			attach_menu.TagSelected += literal.HandleAttachTagCommand;
			attach_item.ShowAll ();
			popup_menu.Append (attach_item);*/
			
            if (literal.IsNegated) {
                GtkUtil.MakeMenuItem (popup_menu,
                    Catalog.GetString ("Include"),
                    "gtk-cancel",
                    new EventHandler (literal.HandleToggleNegatedCommand),
                    true);
            } else {
                GtkUtil.MakeMenuItem (popup_menu,
                    Catalog.GetString ("Exclude"),
                    "gtk-delete",
                    new EventHandler (literal.HandleToggleNegatedCommand),
                    true);
            }
			
			GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Remove"),
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
                foreach (LogicTerm term in LogicWidget.Root.SubTerms) {
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

        private static bool AppendTerm (ArrayList parts, LogicTerm term, Tag single_tag)
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
                    foreach (LogicTerm subterm in term.SubTerms) {
                        tag_matches |= AppendTerm (parts, subterm, single_tag);
                    }
                }
            }

            return tag_matches;
        }
    }

	public abstract class LogicTerm {
		private ArrayList sub_terms = new ArrayList ();
		private LogicTerm parent = null;
		private string separator;

		protected Tag tag = null;

		public ArrayList SubTerms {
			get {
				return sub_terms;
			}
		}

		public LogicTerm (LogicTerm parent, Literal after)
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
		
		public LogicTerm Last {
			get {
				// Return the last Literal in this term
				if (SubTerms.Count > 0)
					return SubTerms[SubTerms.Count - 1] as LogicTerm;
				else
					return null;
			}
		}
		
		public int Count {
			get {
				return SubTerms.Count;
			}
		}

		public LogicTerm Parent {
			get {
				return parent;
			}
		}

		/** Methods **/
		
		public void Add (LogicTerm term)
		{
			SubTerms.Add (term);
		}
		
		public void Remove (LogicTerm term)
		{
			SubTerms.Remove (term);

			// Remove ourselves if we're now empty
			if (SubTerms.Count == 0)
				if (Parent != null)
					Parent.Remove (this);
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
				foreach (LogicTerm term in SubTerms)
					results.AddRange (term.FindByTag (t, true));
			else
				foreach (LogicTerm term in SubTerms) {
					foreach (LogicTerm literal in SubTerms) {
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
			foreach (LogicTerm term in SubTerms) {
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

			foreach (LogicTerm term in parents) {
				bool termHasTag = false;
				bool onlyTerm = true;
				foreach (LogicTerm literal in term.SubTerms) {
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

			foreach (LogicTerm term in parents) {
				bool termHasTag = false;

				// Don't count it as required if it's the only subterm..though it is..
				// it is more clearly identified as Included at that point.
				if (term.Count > 1) {
					foreach (LogicTerm literal in term.SubTerms) {
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
		
		// Recursively generate the SQL condition clause that this
		// term represents.
		public virtual string ConditionString ()
		{
			
			StringBuilder condition = new StringBuilder ("(");

			for (int i = 0; i < SubTerms.Count; i++) {
				LogicTerm term = SubTerms[i] as LogicTerm;
				condition.Append (term.ConditionString ());

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
			return "";
		}

        protected static Hashtable op_term_lookup = new Hashtable();
        public static LogicTerm TermFromOperator (string op, LogicTerm parent, Literal after)
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

            Console.WriteLine ("Do not have LogicTerm for operator {0}", op);
            return null;
        }
	}

	public class AndTerm : LogicTerm {
        static ArrayList operators = new ArrayList ();
        static AndTerm () {
            operators.Add (Catalog.GetString (" and "));
            //operators.Add (Catalog.GetString (" && "));
            operators.Add (Catalog.GetString (", "));
        }

        public static ArrayList Operators {
            get { return operators; }
        }

		public AndTerm (LogicTerm parent, Literal after) : base (parent, after) {}
		
		public override Widget SeparatorWidget ()
		{
			Widget sep = new Label ("");
			sep.SetSizeRequest (3, 1);
			sep.Show ();
			return sep;
			//return null;
		}
		
		public override string SQLOperator ()
		{
			return " AND ";
		}
	}
	
	public class OrTerm : LogicTerm {
        static ArrayList operators = new ArrayList ();
        static OrTerm () {
            operators.Add (Catalog.GetString (" or "));
            //operators.Add (Catalog.GetString (" || "));
        }

        public static ArrayList Operators {
            get { return operators; }
        }

		public OrTerm (LogicTerm parent, Literal after) : base (parent, after) {}
			
		private static string OR = Catalog.GetString ("or");
			
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
	
	public class Literal : LogicTerm {
		public Literal (LogicTerm parent, Tag tag, Literal after) : base (parent, after) {
            //Console.WriteLine ("new literal w/ tag {0}", tag.Name);
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

		public static Tooltips Tips {
			set {
				tips = value;
			}
		}

		public Tag Tag {
			get {
				return tag;
			}
		}

		public bool IsNegated {
			get {
				return isNegated;
			}

			set {
				if (isNegated == value)
					return;
				
				isNegated = value;

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

				label = new Label ("<u>" + tag.Name + "</u>");
				label.UseMarkup = true;

				image = new Gtk.Image (NormalIcon);

				container.CanFocus = true;

				container.KeyPressEvent		+= KeyHandler;
				container.ButtonPressEvent	+= HandleButtonPress;
				container.ButtonReleaseEvent	+= HandleButtonRelease;
				container.EnterNotifyEvent	+= HandleMouseIn;
				container.LeaveNotifyEvent	+= HandleMouseOut;
				
				//new PopupManager (new LiteralPopup (container, this));

				// Setup this widget as a drag source (so tags can be moved after being placed)
				container.DragDataGet	+= HandleDragDataGet;
				container.DragBegin	+= HandleDragBegin;
				container.DragEnd	+= HandleDragEnd;

				Gtk.Drag.SourceSet (container, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
					tag_target_table, DragAction.Copy | DragAction.Move);
				
				// Setup this widget as a drag destination (so tags can be added to our parent's LogicTerm)
				container.DragDataReceived	+= HandleDragDataReceived;

				Gtk.Drag.DestSet (container, DestDefaults.All, tag_dest_target_table, 
						  DragAction.Copy | DragAction.Move ); 

				tips.SetTip (container, tag.Name, null);

				label.Show ();
				image.Show ();

				if (tag.Icon == null) {
					container.Add (label);
				} else {
					container.Add (image);
				}

				container.Show ();

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
			if (IsNegated) {
				tips.SetTip (widget, String.Format (Catalog.GetString ("Not {0}"), tag.Name), null);
				label.Text = "<s>" + tag.Name + "</s>";
				image.Pixbuf = NegatedIcon;
			} else {
				tips.SetTip (widget, tag.Name, null);
				label.Text = "<u>" + tag.Name + "</u>";
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

		public override string ConditionString ()
		{
			StringBuilder ids = new StringBuilder (tag.Id.ToString ());

			if (tag is Category) {
				ArrayList tags = new ArrayList ();
				(tag as Category).AddDescendentsTo (tags);

				for (int i = 0; i < tags.Count; i++)
					ids.Append (", " + (tags [i] as Tag).Id.ToString ());
			}

			return String.Format (
					"id {0}IN (SELECT photo_id FROM photo_tags WHERE tag_id IN ({1}))",
					(IsNegated ? "NOT " : ""), ids.ToString ());
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
			switch (args.Info) {
			case (uint) MainWindow.TargetType.TagList:
			case (uint) MainWindow.TargetType.TagQueryItem:
				Byte [] data = Encoding.UTF8.GetBytes ("");
				Atom [] targets = args.Context.Targets;
			
				args.SelectionData.Set (targets[0], 8, data, data.Length);

				return;
			default:
				// Drop cancelled
				args.RetVal = false;

				foreach (Widget w in hiddenWidgets)
					w.Visible = true;

				focusedLiterals = null;
				break;
			}
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
		
		private void HandleDragDataReceived (object o, EventArgs args)
		{
			// If focusedLiterals is not null, this is a drag of a tag that's already been placed
			if (focusedLiterals.Count == 0)
			{
				if (TermAdded != null)
					TermAdded (Parent, this);
			}
			else
			{
				if (! focusedLiterals.Contains(this))
					if (LiteralsMoved != null)
						LiteralsMoved (focusedLiterals, Parent, this);

				// Unmark the literals as focused so they don't get nixed
				focusedLiterals = null;
			}
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
		
		private static TargetEntry [] tag_target_table = new TargetEntry [] {
			new TargetEntry ("application/x-fspot-tag-query-item", 0, (uint) MainWindow.TargetType.TagQueryItem),
		};

		private static TargetEntry [] tag_dest_target_table = new TargetEntry [] {
			new TargetEntry ("application/x-fspot-tags", 0, (uint) MainWindow.TargetType.TagList),
			new TargetEntry ("application/x-fspot-tag-query-item", 0, (uint) MainWindow.TargetType.TagQueryItem),
		};

		private static ArrayList focusedLiterals = new ArrayList();
		private static ArrayList hiddenWidgets = new ArrayList();
		private Gtk.EventBox container;
		private Gtk.Image image;
		private Gtk.Label label;
		
		private Pixbuf normal_icon;
		//private EventBox widget;
		private Widget widget;
		private Pixbuf negated_icon;
		private static Pixbuf negated_overlay;
		private static Tooltips tips;
		private bool isNegated = false;
		private bool isHoveredOver = false;
		
		public delegate void NegatedToggleHandler (Literal group); 
		public event NegatedToggleHandler NegatedToggled;

		public delegate void RemovingHandler (Literal group); 
		public event RemovingHandler Removing;
		
		public delegate void RemovedHandler (Literal group); 
		public event RemovedHandler Removed;
		
		public delegate void TermAddedHandler (LogicTerm parent, Literal after); 
		public event TermAddedHandler TermAdded;
		
		public delegate void AttachTagHandler (Tag tag, LogicTerm parent, Literal after); 
		public event AttachTagHandler AttachTag;
		
		public delegate void TagRequiredHandler (Tag [] tags); 
		public event TagRequiredHandler RequireTag;
		
		public delegate void TagUnRequiredHandler (Tag [] tags); 
		public event TagUnRequiredHandler UnRequireTag;
		
		public delegate void LiteralsMovedHandler (ArrayList literals, LogicTerm parent, Literal after); 
		public event LiteralsMovedHandler LiteralsMoved;
	}

	public class LogicWidget : HBox {
		private PhotoQuery query;
		private TagSelectionWidget tag_selection_widget;
		
		private static Tooltips tips = new Tooltips ();
		
		private static LogicTerm rootTerm;
		private EventBox rootAdd;
		private HBox rootBox;
		private Label help;
		private HBox sepBox;

		private bool preventUpdate = false;
		private bool preview = false;

		private ArrayList widgets = new ArrayList ();

        public event EventHandler Changed;
		
		public static LogicTerm Root
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
			rootAdd.CanFocus = true;
			rootAdd.DragMotion		+= HandleDragMotion;
			rootAdd.DragDataReceived	+= HandleDragDataReceived;
			rootAdd.DragLeave		+= HandleLeave;

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
		
		private void HandleLeave (object o, EventArgs args)
		{
			if (preview && Children.Length > 1) {
                sepBox.Hide ();
				preview = false;
			} else if (preview && Children.Length == 1) {
                help.Show ();
            }
		}
		
		private void HandleLiteralsMoved (ArrayList literals, LogicTerm parent, Literal after)
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
		
		private void HandleTermAdded (LogicTerm parent, Literal after)
		{
			InsertTerm (parent, after);
		}
		
		private void HandleAttachTag (Tag tag, LogicTerm parent, Literal after)
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
			foreach (LogicTerm parent in rootTerm.LiteralParents ()) {
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
			foreach (LogicTerm parent in rootTerm.LiteralParents ()) {
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
			foreach (LogicTerm parent in rootTerm.LiteralParents ()) {
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

		private void InsertTerm (LogicTerm parent, Literal after)
		{
			if (Literal.FocusedLiterals.Count != 0) {
				HandleLiteralsMoved (Literal.FocusedLiterals, parent, after);

				// Prevent them from being removed again
				Literal.FocusedLiterals = null;
			}
			else
				InsertTerm (tag_selection_widget.TagHighlight, parent, after);
		}
		
		public ArrayList InsertTerm (Tag [] tags, LogicTerm parent, Literal after)
		{
			int position;
			if (after != null)
				position = WidgetPosition (after.Widget) + 1;
			else
				position = Children.Length - 1;

			ArrayList added = new ArrayList ();

			foreach (Tag tag in tags) {
				//Console.WriteLine ("Adding tag {0}", tag.Name);
				
				// Don't put a tag into a LogicTerm twice
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

				Literal term		= new Literal (parent, tag, after);
				term.TermAdded		+= HandleTermAdded;
				term.LiteralsMoved	+= HandleLiteralsMoved;
				term.AttachTag		+= HandleAttachTag;
				term.NegatedToggled	+= HandleNegated;
				term.Removing		+= HandleRemoving;
				term.Removed		+= HandleRemoved;
				term.RequireTag		+= Require;
				term.UnRequireTag	+= UnRequire;

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
				query.ExtraCondition = rootTerm.ConditionString ();
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
