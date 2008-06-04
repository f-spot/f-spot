/*
 * Widgets.Sidebar.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using Gtk;
using System;
using System.Collections.Generic;

namespace FSpot.Widgets {
	public class Sidebar : VBox  {
		
		private HBox button_box;
		private Notebook notebook;
		private MenuButton choose_button;
		private EventBox eventBox;
		private Menu choose_menu;
		private List<string> menu_list;
		private List<string> image_list;

		public event EventHandler CloseRequested;

		public Sidebar () : base ()
		{
			button_box = new HBox ();
			PackStart (button_box, false, false, 0);
			
			notebook = new Notebook ();
			notebook.ShowTabs = false;
			notebook.ShowBorder = false;
			PackStart (notebook, true, true, 0);
			
			Button button = new Button ();
			button.Image = new Image ("gtk-close", IconSize.Button);
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
		
		public void AppendPage (Widget widget, string label)
		{
			AppendPage (widget, label, null);
		}

		public void AppendPage (Widget widget, string label, string icon_name)
		{	
			notebook.AppendPage (widget, new Label (label));
			
			MenuItem item; 
			if (icon_name == null)
				item = new MenuItem (label);
			else {
				item = new ImageMenuItem (label);
				(item as ImageMenuItem).Image = new Image (icon_name, IconSize.Menu);
			}

			item.Activated += HandleItemClicked;
			choose_menu.Append (item);
			item.Show ();
			
			if (notebook.Children.Length == 1)
			{
				choose_button.Label = label;
				choose_button.Image.IconName = icon_name;
			}
			menu_list.Add (label);
			image_list.Add (icon_name);
		}
		
		public void HandleItemClicked (object o, EventArgs args)
		{
			Console.WriteLine (o);
			SwitchTo (menu_list.IndexOf (((o as MenuItem).Child as Label).Text));
		}
		
		public void HandleCloseButtonPressed (object sender, EventArgs args)
		{
			if (CloseRequested != null)
				CloseRequested (this, args);
		}
		
		public void SwitchTo (int n)
		{
            if (n >= notebook.Children.Length) {
                n = 0;
            }

			if (n != notebook.CurrentPage)
			{				
				notebook.CurrentPage = n;
				choose_button.Label = menu_list [n];
				choose_button.Image.IconName = image_list [n];
			}
		}

		public int CurrentPage
		{
			get { return notebook.CurrentPage; }
		}

		public void SwitchTo (string name)
		{
			if (menu_list.Contains (name)) {
				SwitchTo (menu_list.IndexOf (name));
			}
		}
		
		public bool isActive (Widget widget)
		{
			return (notebook.GetNthPage (notebook.CurrentPage) == widget);
		}
	}
}
