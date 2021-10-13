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

using System;
using System.Linq;

using FSpot.Database;
using FSpot.Utils;

using Gtk;

using Hyena;

namespace FSpot.Widgets
{
	public class FolderTreeModel : TreeStore
	{
		protected FolderTreeModel (IntPtr raw) : base (raw) { }

		public FolderTreeModel () : base (typeof (string), typeof (int), typeof (SafeUri))
		{
			// FIXME, subscribe to photo changes
			//database.Photos.ItemsChanged += HandlePhotoItemsChanged;

			UpdateFolderTree ();
		}

		void HandlePhotoItemsChanged (object sender, DbItemEventArgs<Models.Photo> e)
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
			string[] lastSegments = Array.Empty <string> ();

			int lastCount = 0;

			//var reader = database.Database.Query (query_string);
			var results = new FSpotContext ().Photos
				.GroupBy (p => p.BaseUri)
				.Select (x => new {BaseUri = x.Key, Count = x.Count ()})
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
				while (i < lastSegments.Length && i < segments.Length) {
					if (segments[i] != lastSegments[i])
						break;

					i++;
				}

				/* points to the parent node of the current iter */
				TreeIter parentIter = iter;

				/* step back to the level, where the difference occur */
				for (int j = 0; j + i < lastSegments.Length; j++) {

					iter = parentIter;

					if (IterParent (out parentIter, iter)) {
						lastCount += (int)GetValue (parentIter, 1);
						SetValue (parentIter, 1, lastCount);
					} else
						Count += (int)lastCount;
				}

				while (i < segments.Length) {
					if (IterIsValid (parentIter)) {
						iter = AppendValues (parentIter, Uri.UnescapeDataString (segments[i]),
										  (segments.Length - 1 == i) ? count : 0,
										  (GetValue (parentIter, 2) as SafeUri).Append ($"{segments[i]}/"));
					} else {
						iter =
							AppendValues (Uri.UnescapeDataString (segments[i]),
										  (segments.Length - 1 == i) ? count : 0,
										  new SafeUri ($"{baseUri.Scheme}:///", true));
					}

					parentIter = iter;

					i++;
				}

				lastCount = count;
				lastSegments = segments;

			}

			if (IterIsValid (iter)) {
				/* and at least, step back and update photo count */
				while (IterParent (out iter, iter)) {
					lastCount += (int)GetValue (iter, 1);
					SetValue (iter, 1, lastCount);
				}
				Count += lastCount;
			}
		}
	}
}
