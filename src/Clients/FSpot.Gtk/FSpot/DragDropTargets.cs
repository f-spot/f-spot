//
// DragDropTargets.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Mike Gemuende <mike@gemuende.de>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Gtk;

namespace FSpot
{
	public static class DragDropTargets
	{
		public enum TargetType : uint {
			PlainText = 0,
			UriList,
			TagList,
			TagQueryItem,
			UriQueryItem,
			PhotoList,
			RootWindow,
			CopyFiles,
		};

		public static readonly TargetEntry PhotoListEntry =
			new TargetEntry ("application/x-fspot-photos", 0, (uint)TargetType.PhotoList);

		public static readonly TargetEntry TagListEntry =
			new TargetEntry ("application/x-fspot-tags", 0, (uint)TargetType.TagList);

		/* FIXME: maybe we need just one fspot-query-item */
		public static readonly TargetEntry UriQueryEntry =
			new TargetEntry ("application/x-fspot-uri-query-item", 0, (uint)TargetType.UriQueryItem);

		public static readonly TargetEntry TagQueryEntry =
			new TargetEntry ("application/x-fspot-tag-query-item", 0, (uint)TargetType.TagQueryItem);

		public static readonly TargetEntry RootWindowEntry =
			new TargetEntry ("application/x-root-window-drop", 0, (uint)TargetType.RootWindow);

		public static readonly TargetEntry CopyFilesEntry =
			new TargetEntry ("x-special/gnome-copied-files", 0, (uint)TargetType.CopyFiles);
	}
}
