//
// ExportStore.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@src.gnome.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
// Copyright (C) 2009-2010 Ruben Vermeersch
// Copyright (C) 2007 Larry Ewing
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


using System;
using System.Collections.Generic;

using FSpot.Core;

using Hyena.Data.Sqlite;

namespace FSpot.Database
{
	public class ExportItem : DbItem
	{

		public uint ImageId { get; set; }

		public uint ImageVersionId { get; set; }

		public string ExportType { get; set; }

		public string ExportToken { get; set; }

		public ExportItem (uint id, uint image_id, uint image_version_id, string export_type, string export_token) : base (id)
		{
			ImageId = image_id;
			ImageVersionId = image_version_id;
			ExportType = export_type;
			ExportToken = export_token;
		}
	}

	public class ExportStore : DbStore<ExportItem>
	{

		public const string FlickrExportType = "fspot:Flickr";
		// TODO: This is obsolete and meant to be remove once db reach rev4
		public const string OldFolderExportType = "fspot:Folder";
		public const string FolderExportType = "fspot:FolderUri";
		public const string PicasaExportType = "fspot:Picasa";
		public const string SmugMugExportType = "fspot:SmugMug";
		public const string Gallery2ExportType = "fspot:Gallery2";

		private void CreateTable ()
		{
			Database.Execute (
			"CREATE TABLE exports (\n" +
			"	id			INTEGER PRIMARY KEY NOT NULL, \n" +
			"	image_id		INTEGER NOT NULL, \n" +
			"	image_version_id	INTEGER NOT NULL, \n" +
			"	export_type		TEXT NOT NULL, \n" +
			"	export_token		TEXT NOT NULL\n" +
			")");
		}

		private ExportItem LoadItem (Hyena.Data.Sqlite.IDataReader reader)
		{
			return new ExportItem (Convert.ToUInt32 (reader ["id"]),
				       Convert.ToUInt32 (reader ["image_id"]),
				       Convert.ToUInt32 (reader ["image_version_id"]),
				       reader ["export_type"].ToString (),
				       reader ["export_token"].ToString ());
		}

		private void LoadAllItems ()
		{
			Hyena.Data.Sqlite.IDataReader reader = Database.Query ("SELECT id, image_id, image_version_id, export_type, export_token FROM exports");

			while (reader.Read ()) {
				AddToCache (LoadItem (reader));
			}

			reader.Dispose ();
		}

		public ExportItem Create (uint image_id, uint image_version_id, string export_type, string export_token)
		{
			long id = Database.Execute (new HyenaSqliteCommand ("INSERT INTO exports (image_id, image_version_id, export_type, export_token) VALUES (?, ?, ?, ?)",
		image_id, image_version_id, export_type, export_token));

			// The table in the database is setup to be an INTEGER.
			ExportItem item = new ExportItem ((uint)id, image_id, image_version_id, export_type, export_token);

			AddToCache (item);
			EmitAdded (item);

			return item;
		}

		public override void Commit (ExportItem item)
		{
			Database.Execute (new HyenaSqliteCommand ("UPDATE exports SET image_id = ?, image_version_id = ?, export_type = ? SET export_token = ? WHERE id = ?",
                    item.ImageId, item.ImageVersionId, item.ExportType, item.ExportToken, item.Id));

			EmitChanged (item);
		}

		public override ExportItem Get (uint id)
		{
			// we never use this
			return null;
		}

		public List<ExportItem> GetByImageId (uint image_id, uint image_version_id)
		{

			Hyena.Data.Sqlite.IDataReader reader = Database.Query (new HyenaSqliteCommand ("SELECT id, image_id, image_version_id, export_type, export_token FROM exports WHERE image_id = ? AND image_version_id = ?",
                    image_id, image_version_id));

			List<ExportItem> export_items = new List<ExportItem> ();
			while (reader.Read ()) {
				export_items.Add (LoadItem (reader));
			}
			reader.Dispose ();

			return export_items;
		}

		public override void Remove (ExportItem item)
		{
			RemoveFromCache (item);

			Database.Execute (new HyenaSqliteCommand ("DELETE FROM exports WHERE id = ?", item.Id));

			EmitRemoved (item);
		}

		#region Constructor
		public ExportStore (IDb db, bool is_new) : base (db, true)
		{
			if (is_new || !Database.TableExists ("exports"))
				CreateTable ();
			else
				LoadAllItems ();
		}
		#endregion
	}
}
