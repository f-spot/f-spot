//
// FSpot.UI.Dialog.BuilderDialog.cs
//
// Author(s):
//	Stephane Delcroix  <stephane@delcroix.org>
//
// Copyright (c)2009 Novell, Inc.
//
// This is free software. See COPYING fro details.
//

using System;
using GtkBeans;

namespace FSpot.UI.Dialog
{
	public abstract class BuilderDialog : Gtk.Dialog
	{
		protected BuilderDialog (string resourceName, string dialogName) : this (null, resourceName, dialogName)
		{
		}

		protected BuilderDialog (System.Reflection.Assembly assembly, string resourceName, string dialogName) : this (new Builder (assembly, resourceName, null), dialogName)
		{
		}

		protected BuilderDialog (Builder builder, string dialogName) : base (builder.GetRawObject (dialogName))
		{
			builder.Autoconnect (this);
		}
	}
}
