//
// SidebarPage.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Stephane Delcroix
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

using Gtk;

namespace FSpot.Extensions
{
	public class SidebarPage
	{
		// The widget shown on the sidebar page.
		readonly Widget widget;
		public Widget SidebarWidget {
			get { return widget; }
		}

		// Whether this page can be selected
		bool can_select;
		public bool CanSelect {
			protected set {
				can_select = value;
				CanSelectChanged?.Invoke (this, null);
			}
			get { return can_select; }
		}

		public event EventHandler CanSelectChanged;

		// The label of the sidebar page.
		readonly string label;
		public string Label {
			get { return label; }
		}

		// The icon name, used for the selector
		readonly string icon_name;
		public string IconName {
			get { return icon_name; }
		}

		// The sidebar onto which this page is attached
		Gtk.Widget sidebar;
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

		public SidebarPage (Widget widget, string label, string icon_name)
		{
			this.widget = widget;
			this.label = label;
			this.icon_name = icon_name;
		}
	}
}
