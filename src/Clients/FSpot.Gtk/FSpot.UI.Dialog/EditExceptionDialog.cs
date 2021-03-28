//
// EditExceptionDialog.cs
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

using Mono.Unix;

using Gtk;

using Hyena;
using Hyena.Widgets;

using FSpot.Core;

namespace FSpot.UI.Dialog
{
	public class EditException : Exception
	{
		public IPhoto Item { get; private set; }

		public EditException (IPhoto item, Exception e) : base (
						string.Format (Catalog.GetString ("Received exception \"{0}\". Unable to save photo {1}"),
					   e.Message, item.Name), e)
		{
			Item = item;
		}
	}

	public class EditExceptionDialog : HigMessageDialog
	{
		const int MaxErrors = 10;

		public EditExceptionDialog (Gtk.Window parent, Exception[] errors) : base (parent, DialogFlags.DestroyWithParent,
												Gtk.MessageType.Error, ButtonsType.Ok,
												Catalog.GetString ("Error editing photo"),
												GenerateMessage (errors))
		{
			foreach (Exception e in errors)
				Log.Exception (e);
		}

		public EditExceptionDialog (Gtk.Window parent, Exception e, IPhoto item) : this (parent, new EditException (item, e))
		{
		}

		public EditExceptionDialog (Gtk.Window parent, Exception e) : this (parent, new Exception[] { e })
		{
		}

		static string GenerateMessage (Exception[] errors)
		{
			string desc = string.Empty;
			for (int i = 0; i < errors.Length && i < MaxErrors; i++) {
				Exception e = errors[i];
				desc += e.Message + Environment.NewLine;
			}
			return desc;
		}
	}
}
