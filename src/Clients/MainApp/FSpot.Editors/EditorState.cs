//  EditorState.cs
//
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
//
//  Copyright (c) 2017 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
//  Permission is hereby granted, free of charge, to any person obtaining
//  a copy of this software and associated documentation files (the
//  "Software"), to deal in the Software without restriction, including
//  without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to
//  permit persons to whom the Software is furnished to do so, subject to
//  the following conditions:
//
//  The above copyright notice and this permission notice shall be
//  included in all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
//  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//  WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//

using System;

using Hyena;

using FSpot.Core;
using FSpot.Widgets;
using FSpot.Imaging;

using Gdk;
using Gtk;

using Mono.Addins;

namespace FSpot.Editors {

	// TODO: Move EditorNode to FSpot.Extionsions?

	public class EditorState
	{
		// The area selected by the user.
		public Rectangle Selection;

		// The images selected by the user.
		public IPhoto [] Items;

		// The view, into which images are shown (null if we are in the browse view).
		public PhotoImageView PhotoImageView;

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
