//
// ThumbnailDecorationRenderer.cs
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
	///    This is a renderer for drawing annotations to a thumbnail. The annotations
	///    are rendered directly to the thumbnail and no previous size computation is needed.
	/// </summary>
	public abstract class ThumbnailDecorationRenderer
	{
		#region Drawing Methods

		public abstract void Render (Drawable window,
									 Widget widget,
									 Rectangle cell_area,
									 Rectangle expose_area,
									 StateType cell_state,
									 IPhoto photo);

		#endregion

	}
}
