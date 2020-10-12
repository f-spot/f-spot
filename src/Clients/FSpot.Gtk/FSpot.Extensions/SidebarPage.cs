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

		Widget sidebar;
		/// <summary>
		/// The sidebar this page is attached
		/// </summary>
		public Widget Sidebar {
			get => sidebar;
			set {
				sidebar = value;
				AddedToSidebar ();
			}
		}

		/// <summary>
		/// Label of the sidebar page.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Icone name used for the selector
		/// </summary>
		public string IconName { get; }

		public SidebarPage (Widget widget, string label, string iconName)
		{
			SidebarWidget = widget;
			Label = label;
			IconName = iconName;
		}

		/// <summary>
		/// The widget shown on the sidebar page.
		/// </summary>
		public Widget SidebarWidget { get; }

		// Whether this page can be selected
		bool canSelect;
		public bool CanSelect {
			protected set {
				canSelect = value;
				CanSelectChanged?.Invoke (this, null);
			}
			get { return canSelect; }
		}

		/// <summary>
		/// Override to get notified once added to the sidebar.
		/// </summary>
		protected virtual void AddedToSidebar () { }

//		// Whether this page is currently visible
//		public bool IsActive {
//			get { return Sidebar.IsActive (this); }
//		}
	}
}
