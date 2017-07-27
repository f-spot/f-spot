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

using FSpot.Extensions;

using Mono.Unix;

namespace FSpot.Widgets
{
	public class EditorPage : SidebarPage
	{
		internal bool InPhotoView;
		readonly EditorPageWidget EditorPageWidget;

		public EditorPage () : base (new EditorPageWidget (),
									   Catalog.GetString ("Edit"),
									   "mode-image-edit") {
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
