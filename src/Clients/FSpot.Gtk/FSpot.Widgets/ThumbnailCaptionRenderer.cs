//
// ThumbnailCaptionRenderer.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

using Gdk;

using Gtk;

namespace FSpot.Widgets
{
	/// <summary>
	///    Renders a caption below a thumbnail. It must compute the height needed for
	///    the annotation.
	/// </summary>
	public abstract class ThumbnailCaptionRenderer
	{
		public abstract int GetHeight (Widget widget, int width);

		public abstract void Render (Drawable window,
									 Widget widget,
									 Rectangle cell_area,
									 Rectangle expose_area,
									 StateType cell_state,
									 IPhoto photo);

	}
}
