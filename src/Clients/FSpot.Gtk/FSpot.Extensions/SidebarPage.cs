//
// SidebarPage.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace FSpot.Extensions
{
	public class SidebarPage
	{
		public event EventHandler CanSelectChanged;

		// The sidebar onto which this page is attached
		Widget sidebar;
		public Widget Sidebar {
			get => sidebar;
			set {
				sidebar = value;
				AddedToSidebar ();
			}
		}

		/// The label of the sidebar page.
		public string Label { get; }

		/// The icon name, used for the selector
		public string IconName { get; }

		public SidebarPage (Widget widget, string label, string iconName)
		{
			SidebarWidget = widget;
			Label = label;
			IconName = iconName;
		}

		/// The widget shown on the sidebar page.
		public Widget SidebarWidget { get; }

		// Whether this page can be selected
		bool can_select;
		public bool CanSelect {
			protected set {
				can_select = value;
				CanSelectChanged?.Invoke (this, null);
			}
			get { return can_select; }
		}

		// Can be overriden to get notified as soon as we're added to a sidebar.
		protected virtual void AddedToSidebar () { }

//		// Whether this page is currently visible
//		public bool IsActive {
//			get { return Sidebar.IsActive (this); }
//		}
	}
}
