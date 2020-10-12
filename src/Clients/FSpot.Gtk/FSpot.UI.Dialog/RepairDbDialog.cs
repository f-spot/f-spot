//
// RepairDbDialog.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

using Mono.Unix;

using Hyena;
using Hyena.Widgets;

namespace FSpot.UI.Dialog
{
	public class RepairDbDialog : HigMessageDialog
	{
		public RepairDbDialog (Exception e, string backup_path, Window parent) :
				base (parent, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok,
				Catalog.GetString ("Error loading database."),
				string.Format (Catalog.GetString ("F-Spot encountered an error while loading the photo database. " +
						"The old database has been moved to {0} and a new database has been created."), backup_path))
		{
			Log.Exception (e);
			Run ();
			Destroy ();
		}
	}
}
