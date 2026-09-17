//
// ResizeEditor.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Editors;
using FSpot.Imaging;
using FSpot.Resources.Lang;
using FSpot.Utils;

using Gdk;

using Gtk;

namespace FSpot.Addins.Editors
{
	class ResizeEditor : Editor
	{
		SpinButton size;

		public ResizeEditor () : base (PhotoEditorCatalog.Resize)
		{
		}

		protected override PhotoEditorContext CreateContext ()
		{
			var context = base.CreateContext ();
			context.ResizeMaxEdge = (int)size.Value;
			return context;
		}

		public override Widget ConfigurationWidget ()
		{
			int max;
			using (var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (State.Items[0].DefaultVersion.Uri))
			using (Pixbuf p = img.Load ().ToPixbuf ())
				max = Math.Max (p.Width, p.Height);

			size = new SpinButton (128, max, 10);
			size.Value = max;
			return size;
		}
	}
}
