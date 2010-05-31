/*
 * FSpot.UI.Dialog.RepairDbDialog
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Gtk;
using Mono.Unix;
using Hyena;

namespace FSpot.UI.Dialog
{
	public class RepairDbDialog : HigMessageDialog
	{
		public RepairDbDialog (System.Exception e, string backup_path, Window parent) : 
				base (parent, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok, 
				Catalog.GetString ("Error loading database."), 
				String.Format (Catalog.GetString ("F-Spot encountered an error while loading the photo database. " +
		                		"The old database has be moved to {0} and a new database has been created."), backup_path))
		{
			Log.Exception (e);
			Run ();
			Destroy ();
		}
	}
}
