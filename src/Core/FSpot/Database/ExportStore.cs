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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

		public ExportItem (uint id, uint imageId, uint imageVersionId, string exportType, string exportToken) : base (id)
		{
			ImageId = imageId;
			ImageVersionId = imageVersionId;
			ExportType = exportType;
			ExportToken = exportToken;
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

		void CreateTable ()
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

		ExportItem LoadItem (IDataReader reader)
		{
			return new ExportItem (Convert.ToUInt32 (reader["id"]),
					   Convert.ToUInt32 (reader["image_id"]),
					   Convert.ToUInt32 (reader["image_version_id"]),
					   reader["export_type"].ToString (),
					   reader["export_token"].ToString ());
		}

		void LoadAllItems ()
		{
			IDataReader reader = Database.Query ("SELECT id, image_id, image_version_id, export_type, export_token FROM exports");

			while (reader.Read ()) {
				AddToCache (LoadItem (reader));
			}

			reader.Dispose ();
		}

		public ExportItem Create (uint imageId, uint imageVersionId, string exportType, string exportToken)
		{
			long id = Database.Execute (new HyenaSqliteCommand ("INSERT INTO exports (image_id, image_version_id, export_type, export_token) VALUES (?, ?, ?, ?)",
		imageId, imageVersionId, exportType, exportToken));

			// The table in the database is setup to be an INTEGER.
			var item = new ExportItem ((uint)id, imageId, imageVersionId, exportType, exportToken);

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

		public List<ExportItem> GetByImageId (uint imageId, uint imageVersionId)
		{

			IDataReader reader = Database.Query (new HyenaSqliteCommand ("SELECT id, image_id, image_version_id, export_type, export_token FROM exports WHERE image_id = ? AND image_version_id = ?",
					imageId, imageVersionId));

			var export_items = new List<ExportItem> ();
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

		public ExportStore (IDb db, bool isNew) : base (db, true)
		{
			if (isNew || !Database.TableExists ("exports"))
				CreateTable ();
			else
				LoadAllItems ();
		}
	}
}
