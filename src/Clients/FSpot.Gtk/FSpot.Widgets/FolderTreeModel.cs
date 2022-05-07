//
// FolderTreeModel.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using FSpot.Core;
using FSpot.Database;
using FSpot.Models;
using FSpot.Utils;

using Gtk;

using Hyena;

namespace FSpot.Widgets
{
	public class FolderTreeModel : TreeStore
	{
		protected FolderTreeModel (IntPtr raw) : base (raw) { }

		public FolderTreeModel ()
			: base (typeof (string), typeof (int), typeof (SafeUri))
		{
			// FIXME, DBConversion: subscribe to photo changes
			//database.Photos.ItemsChanged += HandlePhotoItemsChanged;

			UpdateFolderTree ();
		}

		void HandlePhotoItemsChanged (object sender, DbItemEventArgs<Photo> e)
		{
			UpdateFolderTree ();
		}

		public string GetFolderNameByIter (TreeIter iter)
		{
			if (!IterIsValid (iter))
				return null;

			return (string)GetValue (iter, 0);
		}

		public int GetPhotoCountByIter (TreeIter iter)
		{
			if (!IterIsValid (iter))
				return -1;

			return (int)GetValue (iter, 1);
		}

		public SafeUri GetUriByIter (TreeIter iter)
		{
			if (!IterIsValid (iter))
				return null;

			return (SafeUri)GetValue (iter, 2);
		}

		public SafeUri GetUriByPath (TreePath row)
		{

			GetIter (out var iter, row);

			return GetUriByIter (iter);
		}

		public int Count { get; private set; }

		/*
		 * UpdateFolderTree queries for directories in database and updates
		 * a possibly existing folder-tree to the queried structure
		 */
		void UpdateFolderTree ()
		{
			Clear ();

			Count = 0;

			/* points at start of each iteration to the leaf of the last inserted uri */
			TreeIter iter = TreeIter.Zero;

			/* stores the segments of the last inserted uri */
			string[] last_segments = Array.Empty<string> ();

			int last_count = 0;

			var results = new FSpotContext ().Photos
				.GroupBy (p => p.BaseUri)
				.Select (x => new { BaseUri = x.Key, Count = x.Count () })
				.OrderBy (x => x.BaseUri);

			foreach (var result in results) {
				var baseUri = new SafeUri (result.BaseUri, true);
				int count = result.Count;

				// FIXME: this is a workaround hack to stop things from crashing - https://bugzilla.gnome.org/show_bug.cgi?id=622318
				int index = baseUri.ToString ().IndexOf ("://");
				var hack = baseUri.ToString ().Substring (index + 3);
				hack = hack.IndexOf ('/') == 0 ? hack : "/" + hack;
				string[] segments = hack.TrimEnd ('/').Split ('/');

				/* First segment contains nothing (since we split by /), so we
				 * can overwrite the first segment for our needs and put the
				 * scheme here.
				 */
				segments[0] = baseUri.Scheme;

				int i = 0;

				/* find first difference of last inserted an current uri */
				while (i < last_segments.Length && i < segments.Length) {
					if (segments[i] != last_segments[i])
						break;

					i++;
				}

				/* points to the parent node of the current iter */
				TreeIter parent_iter = iter;

				/* step back to the level, where the difference occur */
				for (int j = 0; j + i < last_segments.Length; j++) {

					iter = parent_iter;

					if (IterParent (out parent_iter, iter)) {
						last_count += (int)GetValue (parent_iter, 1);
						SetValue (parent_iter, 1, last_count);
					} else
						Count += (int)last_count;
				}

				while (i < segments.Length) {
					if (IterIsValid (parent_iter)) {
						iter =
							AppendValues (parent_iter,
										  Uri.UnescapeDataString (segments[i]),
										  (segments.Length - 1 == i) ? count : 0,
										  (GetValue (parent_iter, 2) as SafeUri).Append ($"{segments[i]}/")
										  );
					} else {
						iter =
							AppendValues (Uri.UnescapeDataString (segments[i]),
										  (segments.Length - 1 == i) ? count : 0,
										  new SafeUri ($"{baseUri.Scheme}:///", true));
					}

					parent_iter = iter;

					i++;
				}

				last_count = count;
				last_segments = segments;

			}

			if (IterIsValid (iter)) {
				/* and at least, step back and update photo count */
				while (IterParent (out iter, iter)) {
					last_count += (int)GetValue (iter, 1);
					SetValue (iter, 1, last_count);
				}
				Count += last_count;
			}
		}
	}
}
