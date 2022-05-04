//
// EditorPage.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2008-2010 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Extensions;
using FSpot.Resources.Lang;

namespace FSpot.Widgets
{
	public class EditorPage : SidebarPage
	{
		internal bool InPhotoView;
		readonly EditorPageWidget EditorPageWidget;

		public EditorPage () : base (new EditorPageWidget (), Strings.Edit, "mode-image-edit")
		{
			// TODO: Somebody might need to change the icon to something more suitable.
			// FIXME: The icon isn't shown in the menu, are we missing a size?
			EditorPageWidget = SidebarWidget as EditorPageWidget;
			EditorPageWidget.Page = this;
		}

		protected override void AddedToSidebar ()
		{
			(Sidebar as Sidebar).SelectionChanged += (collection) => { EditorPageWidget.ShowTools (); };
			(Sidebar as Sidebar).ContextChanged += HandleContextChanged;
		}

		void HandleContextChanged (object sender, EventArgs args)
		{
			InPhotoView = ((Sidebar as Sidebar).Context == ViewContext.Edit);
			EditorPageWidget.ChangeButtonVisibility ();
		}
	}
}
