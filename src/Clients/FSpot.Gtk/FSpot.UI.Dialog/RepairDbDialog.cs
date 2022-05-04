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

using FSpot.Resources.Lang;

using Gtk;

using Hyena.Widgets;



namespace FSpot.UI.Dialog
{
	public class RepairDbDialog : HigMessageDialog
	{
		public RepairDbDialog (Exception e, string backup_path, Window parent) :
				base (parent, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok,
				Strings.ErrorLoadingDatabase,
				string.Format (Strings.FSpotEncounteredAnErrorWhileLoadingPhotoDatabaseMovedToX, backup_path))
		{
			Logger.Log.Error (e, "");
			Run ();
			Destroy ();
		}
	}
}
