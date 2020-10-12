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

using Gtk;

using FSpot.Core;
using FSpot.Database;
using FSpot.Utils;

using Hyena;

namespace FSpot.Widgets
{
	public class FolderTreeModel : TreeStore
	{
		protected FolderTreeModel (IntPtr raw) : base (raw) { }

		readonly Db database;

		const string query_string =
			"SELECT base_uri, COUNT(*) AS count " +
			"FROM photos " +
			"GROUP BY base_uri " +
			"ORDER BY base_uri ASC";


		public FolderTreeModel () : base (typeof (string), typeof (int), typeof (SafeUri))
		{
			database = App.Instance.Database;
			database.Photos.ItemsChanged += HandlePhotoItemsChanged;

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

		int countAll;
		public int Count {
			get => countAll;
		}

		/*
		 * UpdateFolderTree queries for directories in database and updates
		 * a possibly existing folder-tree to the queried structure
		 */
		void UpdateFolderTree ()
		{
			Clear ();

			countAll = 0;

			/* points at start of each iteration to the leaf of the last inserted uri */
			TreeIter iter = TreeIter.Zero;

			/* stores the segments of the last inserted uri */
			var lastSegments = Array.Empty<string> ();

			int lastCount = 0;

			Hyena.Data.Sqlite.IDataReader reader = database.Database.Query (query_string);

			while (reader.Read ()) {
				var baseUri = new SafeUri (reader["base_uri"].ToString (), true);

				int count = Convert.ToInt32 (reader["count"]);

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
						countAll += (int)lastCount;
				}

				while (i < segments.Length) {
					if (IterIsValid (parentIter)) {
						iter =
							AppendValues (parentIter,
										  Uri.UnescapeDataString (segments[i]),
										  (segments.Length - 1 == i) ? count : 0,
										  (GetValue (parentIter, 2) as SafeUri).Append ($"{segments[i]}/")
										  );
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
				countAll += lastCount;
			}
		}
	}
}
