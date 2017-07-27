//
// Sidebar.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008-2010 Ruben Vermeersch
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

using System;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.Extensions;
using FSpot.Settings;

using Gtk;

using Mono.Unix;

namespace FSpot.Widgets
{
	// Decides which sidebar page should be shown for each context. Implemented
	// using the Strategy pattern, to make it swappable easily, in case the
	// default MRUSidebarContextSwitchStrategy is not sufficiently usable.
	public interface ISidebarContextSwitchStrategy
	{
		string PageForContext (ViewContext context);

		void SwitchedToPage (ViewContext context, string name);
	}

	// Implements a Most Recently Used switching strategy. The last page you used
	// for a given context is used.
	public class MRUSidebarContextSwitchStrategy : ISidebarContextSwitchStrategy
	{
		public const string PREF_PREFIX = Preferences.APP_FSPOT + "ui/sidebar";

		string PrefKeyForContext (ViewContext context)
		{
			return string.Format ("{0}/{1}", PREF_PREFIX, context);
		}

		public string PageForContext (ViewContext context)
		{
			string name = Preferences.Get<string> (PrefKeyForContext (context));
			if (name == null)
				name = DefaultForContext (context);
			return name;
		}

		public void SwitchedToPage (ViewContext context, string name)
		{
			Preferences.Set (PrefKeyForContext (context), name);
		}

		string DefaultForContext (ViewContext context)
		{
			if (context == ViewContext.Edit)
				return Catalog.GetString ("Edit");
			// Don't care otherwise, Tags sounds reasonable
			return Catalog.GetString ("Tags");
		}
	}

	public class Sidebar : VBox  {

		HBox button_box;
		public Notebook Notebook { get; private set; }
		MenuButton choose_button;
		EventBox eventBox;
		Menu choose_menu;
		List<string> menu_list;
		List<string> image_list;

		public event EventHandler CloseRequested;

		// Selection change events, sidebar pages can subscribed to this.
		public event IBrowsableCollectionChangedHandler SelectionChanged;
		public event IBrowsableCollectionItemsChangedHandler SelectionItemsChanged;

		// The photos selected.
		public IBrowsableCollection Selection { get; private set; }

		public event EventHandler ContextChanged;
		public event EventHandler PageSwitched;
		
		ViewContext view_context = ViewContext.Unknown;
		public ViewContext Context {
			get { return view_context; }
			set {
				view_context = value;
				ContextChanged?.Invoke (this, null);
			}
		}

		readonly ISidebarContextSwitchStrategy ContextSwitchStrategy;

		public Sidebar ()
		{
			ContextSwitchStrategy = new MRUSidebarContextSwitchStrategy ();
			ContextChanged += HandleContextChanged;

			button_box = new HBox ();
			PackStart (button_box, false, false, 0);

			Notebook = new Notebook ();
			Notebook.ShowTabs = false;
			Notebook.ShowBorder = false;
			PackStart (Notebook, true, true, 0);

			var button = new Button ();
			button.Image = new Image ("gtk-close", Gtk.IconSize.Button);
			button.Relief = ReliefStyle.None;
			button.Pressed += HandleCloseButtonPressed;
			button_box.PackEnd (button, false, true, 0);

			choose_button = new MenuButton ();
			choose_button.Relief = ReliefStyle.None;

			eventBox = new EventBox ();
			eventBox.Add (choose_button);

			button_box.PackStart (eventBox, true, true, 0);

			choose_menu = new Menu ();
			choose_button.Menu = choose_menu;

			menu_list = new List<string> ();
			image_list = new List<string> ();
		}

		void HandleContextChanged (object sender, EventArgs args)
		{
			// Make sure the ViewModeCondition is set correctly.
			if (Context == ViewContext.Single)
				ViewModeCondition.Mode = FSpot.Extensions.ViewMode.Single;
			else if (Context == ViewContext.Library || Context == ViewContext.Edit)
				ViewModeCondition.Mode = FSpot.Extensions.ViewMode.Library;
			else
				ViewModeCondition.Mode = FSpot.Extensions.ViewMode.Unknown;

			string name = ContextSwitchStrategy.PageForContext (Context);
			SwitchTo (name);
		}

		void HandleCanSelectChanged (object sender, EventArgs args)
		{
			//Log.Debug ("Can select changed for {0} to {1}", sender, (sender as SidebarPage).CanSelect);
		}

		public void AppendPage (Widget widget, string label, string icon_name)
		{
			AppendPage (new SidebarPage (widget, label, icon_name));
		}

        public void AppendPage (SidebarPage page)
		{
			page.Sidebar = this;
			page.CanSelectChanged += HandleCanSelectChanged;

			string label = page.Label;
			string icon_name = page.IconName;

			Notebook.AppendPage (page.SidebarWidget, new Label (label));
			page.SidebarWidget.Show ();

			MenuItem item;
			if (icon_name == null)
				item = new MenuItem (label);
			else {
				item = new ImageMenuItem (label);
				(item as ImageMenuItem).Image = new Image ();
				((item as ImageMenuItem).Image as Image).IconName = icon_name;
			}

			item.Activated += HandleItemClicked;
			choose_menu.Append (item);
			item.Show ();

			if (Notebook.Children.Length == 1) {
				choose_button.Label = label;
				choose_button.Image.IconName = icon_name;
			}
			menu_list.Add (label);
			image_list.Add (icon_name);
		}

		public void HandleMainWindowViewModeChanged (object o, EventArgs args)
		{
			MainWindow.ModeType mode = App.Instance.Organizer.ViewMode;
			if (mode == MainWindow.ModeType.IconView)
				Context = ViewContext.Library;
			else if (mode == MainWindow.ModeType.PhotoView)
				Context = ViewContext.Edit;
		}

		public void HandleItemClicked (object o, EventArgs args)
		{
			string name = ((o as MenuItem).Child as Label).Text;
			SwitchTo (name);
			ContextSwitchStrategy.SwitchedToPage (Context, name);
		}

		public void HandleCloseButtonPressed (object sender, EventArgs args)
		{
			CloseRequested?.Invoke (this, args);
		}

		public void SwitchTo (int n)
		{
			if (n >= Notebook.Children.Length) {
				n = 0;
			}

			Notebook.CurrentPage = n;
			choose_button.Label = menu_list [n];
			choose_button.Image.IconName = image_list [n];

			PageSwitched?.Invoke (this, EventArgs.Empty);
		}

		public int CurrentPage
		{
			get { return Notebook.CurrentPage; }
		}

		public void SwitchTo (string name)
		{
			if (menu_list.Contains (name))
				SwitchTo (menu_list.IndexOf (name));
		}

		public bool IsActive (SidebarPage page)
		{
			return (Notebook.GetNthPage (Notebook.CurrentPage) == page.SidebarWidget);
		}

		public void HandleSelectionChanged (IBrowsableCollection collection) {
			Selection = collection;
			// Proxy selection change to the subscribed sidebar pages.
			if (SelectionChanged != null)
				SelectionChanged (collection);
		}

		// Proxy selection item changes to the subscribed sidebar pages.
		public void HandleSelectionItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args) {
			if (SelectionItemsChanged != null)
				SelectionItemsChanged (collection, args);
		}
	}
}
