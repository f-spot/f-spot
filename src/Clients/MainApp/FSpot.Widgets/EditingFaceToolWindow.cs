//
// EditingFaceToolWindow.cs
//
// TODO: Add authors and license.
//

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Gdk;

using Gtk;

using FSpot;

namespace FSpot.Widgets
{
	public class EditingFaceToolWindow : Gtk.Window
	{
		private const int FRAME_BORDER = 6;
		private const int MIN_LENGTH_TO_SEARCH = 3;

		public event KeyPressEventHandler KeyPressed;
		public event EventHandler NameSelected;

		public Entry Entry { get; set; }

		private Frame layout_frame;
		private HSeparator separator = new Gtk.HSeparator();
		private VBox layout = null;
		private HBox suggestions_layout = null;
		private WeakReference faces_tool_ref;
		private ICollection<string> added_face_names;
		
		public EditingFaceToolWindow (FacesTool faces_tool, Gtk.Window container)
			: base (Gtk.WindowType.Toplevel)
		{
			TypeHint = WindowTypeHint.Utility;

			Decorated = false;
			TransientFor = container;

			Frame outer_frame = new Frame (null);
			outer_frame.BorderWidth = 0;
			outer_frame.ShadowType = ShadowType.Out;

			layout_frame = new Frame (null);
			layout_frame.BorderWidth = FRAME_BORDER;
			layout_frame.ShadowType = ShadowType.None;

			outer_frame.Add (layout_frame);
			base.Add (outer_frame);

			AddEvents ((int) (EventMask.ButtonPressMask | EventMask.KeyPressMask));
			FocusOnMap = true;
			AcceptFocus = true;
			CanFocus = true;
			Resizable = false;

			faces_tool_ref = new WeakReference (faces_tool);

			Entry = new Entry ();
			Entry.Changed += EntryChangedHandler;
			
			layout = new VBox (false, FacesTool.CONTROL_SPACING);
			suggestions_layout = new HBox (false, FacesTool.CONTROL_SPACING);
			
			layout.PackStart (Entry, false, false, 0);
			layout.PackStart (separator, false, false, 0);
			layout.PackStart (suggestions_layout, false, false, 0);
			
			Add (layout);
			
			ShowAll ();
			Hide ();

			added_face_names = App.Instance.Organizer.Database.Faces.GetAllNames ();

			KeyPressEvent += OnKeyPressEvent;
		}
		
		~EditingFaceToolWindow ()
		{
			Entry.Changed -= EntryChangedHandler;

			ClearSuggestions ();
		}

		public new void Add (Widget widget)
		{
			layout_frame.Add (widget);
		}

		[GLib.ConnectBefore]
		public void OnKeyPressEvent (object sender, KeyPressEventArgs e)
		{
			string keyval_name = Keyval.Name (e.Event.KeyValue);
			
			if (keyval_name == "Return" || keyval_name == "KP_Enter") {
				foreach (Widget widget in suggestions_layout.Children) {
					if (widget.HasFocus) {
						Entry.Text = ((Button) widget).Label;

						break;
					}
				}
			}

			KeyPressEventHandler handler = KeyPressed;
			if (handler != null)
				handler (sender, e);
		}
		
		private void ShowSuggestions ()
		{
			FacesTool faces_tool = faces_tool_ref.Target as FacesTool;
			if (faces_tool == null)
				return;

			string input = Entry.Text.Trim ();
			if (input.Length < MIN_LENGTH_TO_SEARCH) {
				ClearSuggestions ();
				
				return;
			}
			
			// "\s+" stands for "one or more spacing chars", the extra
			// '\' is for escaping
			string [] words = Regex.Split (input, "\\s+");
			IList<string> matches = new List<string> ();
			ICollection<string> already_added = faces_tool.GetAddedFaceNames ();

			foreach (string name in added_face_names) {
				if (already_added.Contains (name))
					continue;
				
				int count = 0;
				foreach (string word in words)
					if (name.IndexOf (word, StringComparison.CurrentCultureIgnoreCase) >= 0)
						count++;
				
				if (count == words.Length)
					matches.Add (name);
			}
			
			ClearSuggestions ();
			
			if (matches.Count > 0) {
				foreach (string name in matches) {
					Button button = Button.NewWithLabel (name);
					button.Clicked += ButtonClickedHandler;

					suggestions_layout.PackStart (button, true, true, 0);
				}
				
				ShowAll ();
			}
		}
		
		private void ClearSuggestions ()
		{
			separator.Hide ();
			suggestions_layout.Hide ();
			
			foreach (Widget widget in suggestions_layout.Children) {
				Button button = (Button) widget;
				button.Clicked -= ButtonClickedHandler;
				button.Destroy ();
			}
		}
		
		private void ButtonClickedHandler (object sender, EventArgs e)
		{
			Entry.Text = ((Button) sender).Label;

			EventHandler handler = NameSelected;
			if (handler != null)
				handler (this, EventArgs.Empty);
		}

		private void EntryChangedHandler (object sender, EventArgs e)
		{
			ShowSuggestions ();
		}
	}
}