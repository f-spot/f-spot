//
// FSpot.Widgets.BuilderWindow.cs
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

namespace FSpot.Widgets
{
	public abstract class BuilderWindow : Gtk.Window
	{
		protected BuilderWindow (string resourceName, string widgetName) : this (null, resourceName, widgetName)
		{
		}

		protected BuilderWindow (System.Reflection.Assembly assembly, string resourceName, string widgetName) : this (new Builder (assembly, resourceName, null), widgetName)
		{
		}

		protected BuilderWindow (Builder builder, string widgetName) : base (builder.GetRawObject (widgetName))
		{
			builder.Autoconnect (this);
		}
	}
}
