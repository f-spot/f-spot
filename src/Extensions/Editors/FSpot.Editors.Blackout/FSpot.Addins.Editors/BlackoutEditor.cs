//
// BlackoutEditor.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Lorenzo Milesi
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Editors;
using FSpot.Resources.Lang;

using Gdk;

using Gtk;

namespace FSpot.Addins.Editors
{
	class BlackoutEditor : Editor
	{
		public BlackoutEditor () : base (PhotoEditorCatalog.Blackout)
		{
		}

		public override Widget ConfigurationWidget ()
		{
			var vbox = new VBox ();

			var info = new Label (Strings.SelectTheAreaThatYouWantBlackedOut);

			vbox.Add (info);

			return vbox;
		}
	}
}
