/*
 * FSpot.Widgets.FolderTreeModel.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections.Generic;

using Gtk;
using GLib;

using FSpot;
using FSpot.Database;
using FSpot.Core;
using Hyena;
using Hyena.Data.Sqlite;

using Mono.Unix;

namespace FSpot.Widgets
{
	public class FolderTreeModel : TreeStore
	{
		protected FolderTreeModel (IntPtr raw) : base (raw) { }

		Db database;

		const string query_string =
			"SELECT base_uri, COUNT(*) AS count " +
			"FROM photos " +
			"GROUP BY base_uri " +
			"ORDER BY base_uri ASC";


		public FolderTreeModel ()
			: base (typeof (string), typeof (int), typeof (SafeUri))
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
			if ( ! IterIsValid (iter))
				return null;

			return (string) GetValue (iter, 0);
		}

		public int GetPhotoCountByIter (TreeIter iter)
		{
			if ( ! IterIsValid (iter))
				return -1;

			return (int) GetValue (iter, 1);
		}

		public SafeUri GetUriByIter (TreeIter iter)
		{
			if ( ! IterIsValid (iter))
				return null;

			return (SafeUri) GetValue (iter, 2);
		}

		public SafeUri GetUriByPath (TreePath row)
		{
			TreeIter iter;

			GetIter (out iter, row);

			return GetUriByIter (iter);
		}

		int count_all;
		public int Count {
			get { return count_all; }
		}

		/*
		 * UpdateFolderTree queries for directories in database and updates
		 * a possibly existing folder-tree to the queried structure
		 */
		private void UpdateFolderTree ()
		{
			Clear ();

			count_all = 0;

			/* points at start of each iteration to the leaf of the last inserted uri */
			TreeIter iter = TreeIter.Zero;

			/* stores the segments of the last inserted uri */
			string[] last_segments = new string[] {};

			int last_count = 0;

			IDataReader reader = database.Database.Query (query_string);

			while (reader.Read ()) {
				var base_uri = new SafeUri (reader["base_uri"].ToString (), true);

				int count = Convert.ToInt32 (reader["count"]);

				// FIXME: this is a workaround hack to stop things from crashing - https://bugzilla.gnome.org/show_bug.cgi?id=622318
				int index = base_uri.ToString ().IndexOf ("://");
				var hack = base_uri.ToString ().Substring (index + 3);
				hack = hack.IndexOf ('/') == 0 ? hack : "/" + hack;
				string[] segments = hack.TrimEnd ('/').Split ('/');

				/* First segment contains nothing (since we split by /), so we
				 * can overwrite the first segment for our needs and put the
				 * scheme here.
				 */
				segments[0] = base_uri.Scheme;

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
						count_all += (int)last_count;
				}

				while (i < segments.Length) {
					if (IterIsValid (parent_iter)) {
						iter =
							AppendValues (parent_iter,
							              Uri.UnescapeDataString (segments[i]),
							              (segments.Length - 1 == i)? count : 0,
							              (GetValue (parent_iter, 2) as SafeUri).Append (String.Format ("{0}/", segments[i]))
							              );
					} else {
						iter =
							AppendValues (Uri.UnescapeDataString (segments[i]),
							              (segments.Length - 1 == i)? count : 0,
							              new SafeUri (String.Format ("{0}:///", base_uri.Scheme), true));
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
				count_all += (int)last_count;
			}
		}
	}
}
