//
// GenericToolItem.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;

namespace Hyena.Widgets
{
	public class GenericToolItem<T> : ToolItem where T : Widget
	{
		T widget;

		public GenericToolItem (T widget)
		{
			this.widget = widget;
			Add (widget);
		}

		public T Widget {
			get { return widget; }
		}
	}
}
