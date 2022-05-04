//  EditorState.cs
//
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
//
//  Copyright (c) 2017 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;
using FSpot.Widgets;

using Gdk;

namespace FSpot.Editors
{
	// TODO: Move EditorNode to FSpot.Extionsions?

	public class EditorState
	{
		// The area selected by the user.
		public Rectangle Selection { get; set; }

		// The images selected by the user.
		public IPhoto[] Items { get; set; }

		// The view, into which images are shown (null if we are in the browse view).
		public PhotoImageView PhotoImageView { get; set; }

		// Has a portion of the image been selected?
		public bool HasSelection {
			get { return Selection != Rectangle.Zero; }
		}

		// Is the user in browse mode?
		public bool InBrowseMode {
			get { return PhotoImageView == null; }
		}
	}

	// This is the base class from which all editors inherit.
}
