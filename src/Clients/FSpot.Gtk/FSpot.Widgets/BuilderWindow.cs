//
// BuilderWindow.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
