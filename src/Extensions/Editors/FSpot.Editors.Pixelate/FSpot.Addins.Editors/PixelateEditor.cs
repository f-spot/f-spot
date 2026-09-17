//
// PixelateEditor.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2008-2009 Novell, Inc.
// Copyright (C) 2008 Lorenzo Milesi
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Editors;
using FSpot.Resources.Lang;

using Gdk;

using Gtk;

namespace FSpot.Addins.Editors
{
	class PixelateEditor : Editor
	{
		public PixelateEditor () : base (PhotoEditorCatalog.Pixelate)
		{
		}

		public override Widget ConfigurationWidget ()
		{
			var vbox = new VBox ();

			var info = new Label (Strings.SelectTheAreaThatYouWantPixelated);

			vbox.Add (info);

			return vbox;
		}


	}
}
