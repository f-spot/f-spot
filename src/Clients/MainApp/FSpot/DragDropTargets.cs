/*
 * FSpot.DragDropTargets.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Gtk;

namespace FSpot
{
	public static class DragDropTargets
	{
		enum TargetType : uint {
			PlainText = 0,
			UriList,
			TagList,
			TagQueryItem,
			UriQueryItem,
			PhotoList,
			RootWindow,
			CopyFiles,
		};

		[Obsolete ("better use gtk_target_list_add_text_targets")]
		public static readonly TargetEntry PlainTextEntry =
			new TargetEntry ("text/plain", 0, (uint)TargetType.PhotoList);

		public static readonly TargetEntry PhotoListEntry =
			new TargetEntry ("application/x-fspot-photos", 0, (uint)TargetType.PhotoList);

		[Obsolete ("Use gtk_target_list_add_uri_target")]
		public static readonly TargetEntry UriListEntry =
			new TargetEntry ("text/uri-list", 0, (uint)TargetType.UriList);

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
