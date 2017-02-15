//
// Literal.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2007-2009 Novell, Inc.
// Copyright (C) 2007 Gabriel Burt
// Copyright (C) 2007-2009 Stephane Delcroix
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

// This has to do with Finding photos based on tags
// http://mail.gnome.org/archives/f-spot-list/2005-November/msg00053.html
// http://bugzilla-attachments.gnome.org/attachment.cgi?id=54566
using System;
using System.Collections.Generic;
using System.Text;

using Mono.Unix;

using Gtk;
using Gdk;

using FSpot.Core;

namespace FSpot.Query
{
	// TODO rename to TagLiteral?
	public class Literal : AbstractLiteral
	{
		public Literal (Tag tag) : this (null, tag, null)
		{
		}

		public Literal (Term parent, Tag tag, Literal after) : base (parent, after)
		{
			Tag = tag;
		}

		static Literal ()
		{
			FocusedLiterals = new List<Literal> ();
		}

		#region Properties
		public static List<Literal> FocusedLiterals { get; set; }

		public Tag Tag { get; private set; }

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

		Pixbuf NegatedIcon {
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

				label = new Label (System.Web.HttpUtility.HtmlEncode (Tag.Name));
				label.UseMarkup = true;

				image = new Gtk.Image (NormalIcon);

				container.CanFocus = true;

				container.KeyPressEvent += KeyHandler;
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
						DragAction.Copy | DragAction.Move);

				container.TooltipText = Tag.Name;

				label.Show ();
				image.Show ();

				if (Tag.Icon == null)
					handle_box.Add (label);
				else
					handle_box.Add (image);

				handle_box.Show ();

				box.Add (handle_box);
				box.Show ();

				container.Add (box);

				widget = container;

				return widget;
			}
		}

		Pixbuf NormalIcon {
			get {
				if (normal_icon != null)
					return normal_icon;

				Pixbuf scaled = null;
				scaled = Tag.Icon;

				for (Category category = Tag.Category; category != null && scaled == null; category = category.Category) {
					scaled = category.Icon;
				}

				if (scaled == null)
					return null;

				if (scaled.Width != ICON_SIZE)
					scaled = scaled.ScaleSimple (ICON_SIZE, ICON_SIZE, InterpType.Bilinear);

				normal_icon = scaled;

				return normal_icon;
			}

			set {
				normal_icon = null;
			}
		}
		#endregion

		#region Methods
		public void Update ()
		{
			// Clear out the old icons
			normal_icon = null;
			negated_icon = null;
			if (IsNegated) {
				widget.TooltipText = string.Format (Catalog.GetString ("Not {0}"), Tag.Name);
				label.Text = "<s>" + System.Web.HttpUtility.HtmlEncode (Tag.Name) + "</s>";
				image.Pixbuf = NegatedIcon;
			} else {
				widget.TooltipText = Tag.Name;
				label.Text = System.Web.HttpUtility.HtmlEncode (Tag.Name);
				image.Pixbuf = NormalIcon;
			}

			label.UseMarkup = true;

			// Show the icon unless it's null
			if (Tag.Icon == null && container.Children [0] == image) {
				container.Remove (image);
				container.Add (label);
			} else if (Tag.Icon != null && container.Children [0] == label) {
				container.Remove (label);
				container.Add (image);
			}

			if (isHoveredOver && image.Pixbuf != null) {
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
			var ids = new StringBuilder (Tag.Id.ToString ());

			var category = Tag as Category;
			if (category != null) {
				var tags = new List<Tag> ();
				category.AddDescendentsTo (tags);

                foreach (var t in tags)
				{
				    ids.Append (", " + t.Id);
				}
			}

			return string.Format (
				"id {0}IN (SELECT photo_id FROM photo_tags WHERE tag_id IN ({1}))",
				(IsNegated ? "NOT " : string.Empty), ids);
		}

		public override Gtk.Widget SeparatorWidget ()
		{
			return new Label ("ERR");
		}

		static Pixbuf NegatedOverlay {
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
			if (focusedLiterals == null)
				return;

			foreach (var literal in focusedLiterals)
				literal.RemoveSelf ();
		}
		#endregion

		#region Handlers
		void KeyHandler (object o, KeyPressEventArgs args)
		{
			args.RetVal = false;

			switch (args.Event.Key) {
			case Gdk.Key.Delete:
				RemoveFocusedLiterals ();
				args.RetVal = true;
				return;
			}
		}

		void HandleButtonPress (object o, ButtonPressEventArgs args)
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

				} else if (args.Event.Button == 3) {
					var popup = new LiteralPopup ();
					popup.Activate (args.Event, this);
				}

				return;

			default:
				args.RetVal = false;
				return;
			}
		}

		void HandleButtonRelease (object o, ButtonReleaseEventArgs args)
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

		void HandleMouseIn (object o, EnterNotifyEventArgs args)
		{
			isHoveredOver = true;
			Update ();
		}

		void HandleMouseOut (object o, LeaveNotifyEventArgs args)
		{
			isHoveredOver = false;
			Update ();
		}

		void HandleDragDataGet (object sender, DragDataGetArgs args)
		{
			args.RetVal = true;

			if (args.Info == DragDropTargets.TagListEntry.Info || args.Info == DragDropTargets.TagQueryEntry.Info) {

				// FIXME: do really write data
				Byte [] data = Encoding.UTF8.GetBytes (string.Empty);
				Atom [] targets = args.Context.Targets;

				args.SelectionData.Set (targets [0], 8, data, data.Length);

				return;
			}

			// Drop cancelled
			args.RetVal = false;

			foreach (Widget w in hiddenWidgets) {
				w.Visible = true;
			}

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

			focusedLiterals = new List<Literal> ();
			args.RetVal = true;
		}

		void HandleDragDataReceived (object o, DragDataReceivedArgs args)
		{
			args.RetVal = true;

			if (args.Info == DragDropTargets.TagListEntry.Info) {
				TagsAdded?.Invoke (args.SelectionData.GetTagsData (), Parent, this);
				return;
			}

			if (args.Info == DragDropTargets.TagQueryEntry.Info) {

				if (!focusedLiterals.Contains (this))
					LiteralsMoved?.Invoke (focusedLiterals, Parent, this);

				// Unmark the literals as focused so they don't get nixed
				focusedLiterals = null;
			}
		}

		bool preview;
		Gtk.Widget preview_widget;

		void HandleDragMotion (object o, DragMotionArgs args)
		{
		    if (preview)
                return;

		    if (preview_widget == null) {
		        preview_widget = new Gtk.Label (" | ");
		        box.Add (preview_widget);
		    }

		    preview_widget.Show ();
		}

		void HandleDragLeave (object o, EventArgs args)
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
			AttachTag?.Invoke (t, Parent, this);
		}

		public void HandleRequireTag (object sender, EventArgs args)
		{
			RequireTag?.Invoke (new[] { Tag });
		}

		public void HandleUnRequireTag (object sender, EventArgs args)
		{
			UnRequireTag?.Invoke (new[] { Tag });
		}

		const int ICON_SIZE = 24;
		const int overlay_size = (int)(.40 * ICON_SIZE);
		static readonly TargetEntry[] tag_target_table =
			{ DragDropTargets.TagQueryEntry };
		static readonly TargetEntry[] tag_dest_target_table =
			{
				DragDropTargets.TagListEntry,
				DragDropTargets.TagQueryEntry
			};
		static List<Literal> focusedLiterals = new List<Literal> ();
		static readonly List<Widget> hiddenWidgets = new List<Widget> ();
		Gtk.Container container;
		LiteralBox handle_box;
		Gtk.Box box;
		Gtk.Image image;
		Gtk.Label label;
		Pixbuf normal_icon;
		//EventBox widget;
		Widget widget;
		Pixbuf negated_icon;
		static Pixbuf negated_overlay;
		bool isHoveredOver;

		public delegate void NegatedToggleHandler (Literal group);

		public event NegatedToggleHandler NegatedToggled;

		public delegate void RemovingHandler (Literal group);

		public event RemovingHandler Removing;

		public delegate void RemovedHandler (Literal group);

		public event RemovedHandler Removed;

		public delegate void TagsAddedHandler (Tag[] tags,Term parent,Literal after);

		public event TagsAddedHandler TagsAdded;

		public delegate void AttachTagHandler (Tag tag,Term parent,Literal after);

		public event AttachTagHandler AttachTag;

		public delegate void TagRequiredHandler (Tag[] tags);

		public event TagRequiredHandler RequireTag;

		public delegate void TagUnRequiredHandler (Tag[] tags);

		public event TagUnRequiredHandler UnRequireTag;

		public delegate void LiteralsMovedHandler (List<Literal> literals,Term parent,Literal after);

		public event LiteralsMovedHandler LiteralsMoved;
		#endregion
	}
}
