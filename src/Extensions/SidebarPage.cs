/*
 * Widgets.SidebarPage.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *	Stephane Delcroix <stephane@delcroix.org>
 *	Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 */

using FSpot.Extensions;
using FSpot.Utils;
using Gtk;
using Mono.Addins;
using Mono.Unix;
using System;
using System.Collections.Generic;

namespace FSpot.Extensions
{
	public class SidebarPage {
		// The widget shown on the sidebar page.
		private readonly Widget widget;
		public Widget SidebarWidget {
			get { return widget; }
		}

		// Whether this page can be selected
		private bool can_select;
		public bool CanSelect {
			protected set { 
				can_select = value;
				if (CanSelectChanged != null)
					CanSelectChanged (this, null);
			}
			get { return can_select; }
		}

		public event EventHandler CanSelectChanged;

		// The label of the sidebar page.
		private readonly string label;
		public string Label {
			get { return label; }
		}

		// The icon name, used for the selector
		private readonly string icon_name;
		public string IconName {
			get { return icon_name; }
		}

		// The sidebar onto which this page is attached
		private Gtk.Widget sidebar;
		public Gtk.Widget Sidebar {
			get { return sidebar; }
			set { 
				sidebar = value; 
				AddedToSidebar ();
			}
		}

		// Can be overriden to get notified as soon as we're added to a sidebar.
		protected virtual void AddedToSidebar () { }

//		// Whether this page is currently visible
//		public bool IsActive {
//			get { return Sidebar.IsActive (this); }
//		}

		public SidebarPage (Widget widget, string label, string icon_name) {
			this.widget = widget;
			this.label = label;
			this.icon_name = icon_name;
		}
	}
}
