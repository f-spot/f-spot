//
// Updater.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2009-2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2006, 2009 Gabriel Burt
// Copyright (C) 2007-2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Utils;

using Hyena;
using Hyena.Data.Sqlite;

namespace FSpot.Database
{
	public static class Updater
	{
		static IUpdaterUI _dialog;
		static readonly Dictionary<Version, Update> Updates = new Dictionary<Version, Update> ();
		static Version _dbVersion;
		static FSpotDatabaseConnection _db;
		public static bool Silent { get; set; }

		public static Version LatestVersion {
			get {
				if (Updates == null || Updates.Count == 0)
					return new Version (0, 0);

				var keys = new List<Version> (Updates.Keys);
				keys.Sort ();

				return keys[keys.Count - 1];
			}
		}

		static Updater ()
		{
			// Update from version 0 to 1: Remove empty Other tags
			AddUpdate (new Version ("1"), delegate {
				string otherId = SelectSingleString ("SELECT id FROM tags WHERE name = 'Other'");

				if (otherId == null)
					return;

				// Don't do anything if there are subtags
				string tagCount = SelectSingleString (
					$"SELECT COUNT(*) FROM tags WHERE category_id = {otherId}");

				if (tagCount == null || int.Parse (tagCount) != 0)
					return;

				// Don't do anything if there are photos tagged with this
				string photoCount = SelectSingleString (
					$"SELECT COUNT(*) FROM photo_tags WHERE tag_id = {otherId}");

				if (photoCount == null || int.Parse (photoCount) != 0)
					return;

				// Finally, we know that the Other tag exists and has no children, so remove it
				Execute ("DELETE FROM tags WHERE name = 'Other'");
			});

			// Update from version 1 to 2: Restore Other tags that were removed leaving dangling child tags
			AddUpdate (new Version ("2"), delegate {
				string tagCount = SelectSingleString ("SELECT COUNT(*) FROM tags WHERE category_id != 0 AND category_id NOT IN (SELECT id FROM tags)");

				// If there are no dangling tags, then don't do anything
				if (tagCount == null || int.Parse (tagCount) == 0)
					return;

				int id = ExecuteScalar ("INSERT INTO tags (name, category_id, is_category, icon) VALUES ('Other', 0, 1, 'stock_icon:f-spot-other.png')");

				Execute (
					$"UPDATE tags SET category_id = {id} WHERE id IN " +
					"(SELECT id FROM tags WHERE category_id != 0 AND category_id " +
					"NOT IN (SELECT id FROM tags))");

				Log.Debug ("Other tag restored. Sorry about that!");
			});

			// Update from version 2 to 3: ensure that Hidden is the only tag left which is a real tag (not category)
			AddUpdate (new Version ("3"), delegate {
				Execute ("UPDATE tags SET is_category = 1 WHERE name != 'Hidden'");
			});

			//Version 3.1, clean old (and unused) items in Export
			AddUpdate (new Version (3, 1), delegate {
				if (TableExists ("exports"))
					ExecuteScalar ("DELETE FROM exports WHERE export_type='fspot:Folder'");
			});

			//Version 4.0, bump the version number to a integer, for backward compatibility
			AddUpdate (new Version (4, 0), delegate { });


			//Version 5.0, add a roll_id field to photos, rename table 'imports' to 'rolls'
			//and fix bgo 324425.
			AddUpdate (new Version (5, 0), delegate {
				Log.Debug ("Will add a roll_id field to photos!");
				string tmpPhotos = MoveTableToTemp ("photos");
				Execute (
					"CREATE TABLE photos (                                     " +
					"	id                 INTEGER PRIMARY KEY NOT NULL,   " +
					"       time               INTEGER NOT NULL,	   	   " +
					"       directory_path     STRING NOT NULL,		   " +
					"       name               STRING NOT NULL,		   " +
					"       description        TEXT NOT NULL,	           " +
					"       roll_id            INTEGER NOT NULL,		   " +
					"       default_version_id INTEGER NOT NULL		   " +
					")");
				ExecuteScalar ($"INSERT INTO photos SELECT id, time, directory_path, name, description, 0, default_version_id FROM {tmpPhotos}");

				Log.Debug ("Will rename imports to rolls!");
				string tmpRolls = MoveTableToTemp ("imports");
				Execute (
					"CREATE TABLE rolls (                                     " +
					"	id                 INTEGER PRIMARY KEY NOT NULL,   " +
					"       time               INTEGER NOT NULL	   	   " +
					")");
				ExecuteScalar ($"INSERT INTO rolls SELECT id, time FROM {tmpRolls}");

				Log.Debug ("Cleaning weird descriptions, fixes bug #324425.");
				Execute ("UPDATE photos SET description = \"\" WHERE description LIKE \"Invalid size of entry%\"");
			});


			//Version 6.0, change tag icon f-spot-tag-other to emblem-generic
			AddUpdate (new Version (6, 0), delegate {
				ExecuteScalar ("UPDATE tags SET icon = \"stock_icon:emblem-generic\" " +
						" WHERE icon LIKE \"stock_icon:f-spot-other.png\"");
			});

			//Update to version 7.0, keep photo uri instead of path
			AddUpdate (new Version (7, 0), delegate {
				string tmpPhotos = MoveTableToTemp ("photos");
				Execute (
					"CREATE TABLE photos (" +
					"	id                 INTEGER PRIMARY KEY NOT NULL," +
					"       time               INTEGER NOT NULL," +
					"       uri                STRING NOT NULL," +
					"       description        TEXT NOT NULL," +
					"       roll_id            INTEGER NOT NULL," +
					"       default_version_id INTEGER NOT NULL" +
					")");
				Execute ("INSERT INTO photos (id, time, uri, description, roll_id, default_version_id)	" +
				         "SELECT id, time, 'file://' || directory_path || '/' || name, 		" +
				         $"description, roll_id, default_version_id FROM {tmpPhotos}");
			}, true);

			// Update to version 8.0, store full version uri
			AddUpdate (new Version (8, 0), delegate {
				string tmpVersions = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (          " +
					"       photo_id        INTEGER,        " +
					"       version_id      INTEGER,        " +
					"       name            STRING,         " +
					"       uri             STRING NOT NULL " +
					")");

				Hyena.Data.Sqlite.IDataReader reader = ExecuteReader ("SELECT photo_id, version_id, name, uri " +
				                                                      $"FROM {tmpVersions}, photos " +
				                                                      "WHERE photo_id = id ");

				while (reader.Read ()) {
					System.Uri photoUri = new System.Uri (reader[3] as string);
					string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension (photoUri.AbsolutePath);
					string extension = System.IO.Path.GetExtension (photoUri.AbsolutePath);

					string uri = photoUri.Scheme + "://" +
						photoUri.Host +
						System.IO.Path.GetDirectoryName (photoUri.AbsolutePath) + "/" +
						nameWithoutExtension + " (" + (reader[2]).ToString () + ")" + extension;

					Execute (new HyenaSqliteCommand (
						"INSERT INTO photo_versions (photo_id, version_id, name, uri) " +
						"VALUES (?, ?, ?, ?)",
						Convert.ToUInt32 (reader[0]),
						Convert.ToUInt32 (reader[1]),
						(reader[2]).ToString (),
						uri));
				}

			}, true);

			// Update to version 9.0
			AddUpdate (new Version (9, 0), delegate {
				string tmpVersions = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (          " +
					"       photo_id        INTEGER,        " +
					"       version_id      INTEGER,        " +
					"       name            STRING,         " +
					"       uri             STRING NOT NULL," +
					"	protected	BOOLEAN		" +
					")");
				Execute ("INSERT INTO photo_versions (photo_id, version_id, name, uri, protected) " +
				         "SELECT photo_id, version_id, name, uri, 0 " + $"FROM {tmpVersions} ");
			});

			// Update to version 10.0, make id autoincrement
			AddUpdate (new Version (10, 0), delegate {
				string tmpPhotos = MoveTableToTemp ("photos");
				Execute (
					"CREATE TABLE photos (                                     " +
					"	id                 INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
					"	time               INTEGER NOT NULL,	   	   " +
					"	uri		   STRING NOT NULL,		   " +
					"	description        TEXT NOT NULL,	           " +
					"	roll_id            INTEGER NOT NULL,		   " +
					"	default_version_id INTEGER NOT NULL		   " +
					")");

				Execute ("INSERT INTO photos (id, time, uri, description, roll_id, default_version_id) " +
				         "SELECT id, time, uri, description, roll_id, default_version_id  " + $"FROM  {tmpPhotos} ");
			}, false);

			// Update to version 11.0, rating
			AddUpdate (new Version (11, 0), delegate {
				string tmpPhotos = MoveTableToTemp ("photos");
				Execute (
					"CREATE TABLE photos (                                     " +
					"	id                 INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
					"	time               INTEGER NOT NULL,	   	   " +
					"	uri		   STRING NOT NULL,		   " +
					"	description        TEXT NOT NULL,	           " +
					"	roll_id            INTEGER NOT NULL,		   " +
					"	default_version_id INTEGER NOT NULL,		   " +
					"       rating             INTEGER NULL			   " +
					")");

				Execute ("INSERT INTO photos (id, time, uri, description, roll_id, default_version_id, rating) " +
				         "SELECT id, time, uri, description, roll_id, default_version_id, null  " +
				         $"FROM  {tmpPhotos} ");
			});

			//Update to version 12.0, remove dead associations, bgo #507950, #488545
			AddUpdate (new Version (12, 0), delegate {
				Execute ("DELETE FROM photo_tags WHERE tag_id NOT IN (SELECT id FROM tags)");
			});

			// Update to version 13.0
			AddUpdate (new Version (13, 0), delegate {
				Execute ("UPDATE photos SET rating = 0 WHERE rating IS NULL");
			});

			// Update to version 14.0
			AddUpdate (new Version (14, 0), delegate {
				Execute ("UPDATE photos SET rating = 0 WHERE rating IS NULL");
			});

			// Update to version 15.0
			AddUpdate (new Version (15, 0), delegate {
				string tmpPhotoTags = MoveTableToTemp ("photo_tags");
				Execute (
					"CREATE TABLE photo_tags (        " +
					"	photo_id      INTEGER,    " +
					"       tag_id        INTEGER,    " +
					"       UNIQUE (photo_id, tag_id) " +
					")");
				Execute ("INSERT OR IGNORE INTO photo_tags (photo_id, tag_id) " +
				         $"SELECT photo_id, tag_id FROM {tmpPhotoTags}");
				string tmpPhotoVersions = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (		" +
					"	photo_id	INTEGER,	" +
					"	version_id	INTEGER,	" +
					"	name		STRING,		" +
					"	uri		STRING NOT NULL," +
					"	protected	BOOLEAN, 	" +
					"	UNIQUE (photo_id, version_id)	" +
					")");
				Execute ("INSERT OR IGNORE INTO photo_versions 		" + "(photo_id, version_id, name, uri, protected)	" +
				         $"SELECT photo_id, version_id, name, uri, protected FROM {tmpPhotoVersions}");
			});

			// Update to version 16.0
			AddUpdate (new Version (16, 0), delegate {
				string tempTable = MoveTableToTemp ("photos");

				Execute ("CREATE TABLE photos ( " +
					  "	id                 INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,   " +
					  "	time               INTEGER NOT NULL,	   	   " +
					  "	uri		   STRING NOT NULL,		   " +
					  "	description        TEXT NOT NULL,	           " +
					  "	roll_id            INTEGER NOT NULL,		   " +
					  "	default_version_id INTEGER NOT NULL,		   " +
					  "	rating		   INTEGER NULL,		   " +
					  "	md5_sum		   TEXT NULL  			   " +
					  ")"
				 );

				Execute ("INSERT INTO photos (id, time, uri, description, roll_id, " +
				         "default_version_id, rating, md5_sum) " + "SELECT id, time, uri, description, roll_id, " +
				         "       default_version_id, rating, '' " + $"FROM   {tempTable} "
				);


				string tempVersionsTable = MoveTableToTemp ("photo_versions");

				Execute ("CREATE TABLE photo_versions (    	" +
					  "      photo_id        INTEGER,  	" +
					  "      version_id      INTEGER,  	" +
					  "      name            STRING,    	" +
					  "	uri		STRING NOT NULL," +
					  "	md5_sum		STRING NOT NULL," +
					  "	protected	BOOLEAN		" +
					  ")");

				Execute ("INSERT INTO photo_versions (photo_id, version_id, name, uri, md5_sum, protected) " +
				         "SELECT photo_id, version_id, name, uri, '', protected " + $"FROM   {tempVersionsTable} "
				);

				JobStore.CreateTable (_db);

				// This is kind of hacky but should be a lot faster on
				// large photo databases
				Execute (string.Format ("INSERT INTO jobs (job_type, job_options, run_at, job_priority) " +
							 "SELECT '{0}', id, {1}, {2} " +
							 "FROM   photos ",
							 typeof (Jobs.CalculateHashJob).ToString (),
							 DateTimeUtil.FromDateTime (DateTime.Now),
							 0
							)
				 );
			}, true);

			// Update to version 16.1
			AddUpdate (new Version (16, 1), delegate {
				Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");
			}, false);

			// Update to version 16.2
			AddUpdate (new Version (16, 2), delegate {
				Execute ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");
			}, false);

			// Update to version 16.3
			AddUpdate (new Version (16, 3), delegate {
				Execute ($"DELETE FROM jobs WHERE job_type = '{typeof(Jobs.CalculateHashJob).ToString ()}'");
			}, false);

			// Update to version 16.4
			AddUpdate (new Version (16, 4), delegate { //fix the tables schema EOL
				string tempTable = MoveTableToTemp ("exports");
				Execute (
					"CREATE TABLE exports (\n" +
					"	id			INTEGER PRIMARY KEY NOT NULL, \n" +
					"	image_id		INTEGER NOT NULL, \n" +
					"	image_version_id	INTEGER NOT NULL, \n" +
					"	export_type		TEXT NOT NULL, \n" +
					"	export_token		TEXT NOT NULL\n" +
					")");
				Execute ("INSERT INTO exports (id, image_id, image_version_id, export_type, export_token) " +
				         "SELECT id, image_id, image_version_id, export_type, export_token " + $"FROM {tempTable}");

				tempTable = MoveTableToTemp ("jobs");
				Execute (
					"CREATE TABLE jobs (\n" +
					"	id		INTEGER PRIMARY KEY NOT NULL, \n" +
					"	job_type	TEXT NOT NULL, \n" +
					"	job_options	TEXT NOT NULL, \n" +
					"	run_at		INTEGER, \n" +
					"	job_priority	INTEGER NOT NULL\n" +
					")");
				Execute ("INSERT INTO jobs (id, job_type, job_options, run_at, job_priority) " +
				         "SELECT id, job_type, job_options, run_at, job_priority " + $"FROM {tempTable}");

				tempTable = MoveTableToTemp ("meta");
				Execute (
					"CREATE TABLE meta (\n" +
					"	id	INTEGER PRIMARY KEY NOT NULL, \n" +
					"	name	TEXT UNIQUE NOT NULL, \n" +
					"	data	TEXT\n" +
					")");
				Execute ("INSERT INTO meta (id, name, data) " + "SELECT id, name, data " + $"FROM {tempTable}");

				tempTable = MoveTableToTemp ("photos");
				Execute (
					"CREATE TABLE photos (\n" +
					"	id			INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, \n" +
					"	time			INTEGER NOT NULL, \n" +
					"	uri			STRING NOT NULL, \n" +
					"	description		TEXT NOT NULL, \n" +
					"	roll_id			INTEGER NOT NULL, \n" +
					"	default_version_id	INTEGER NOT NULL, \n" +
					"	rating			INTEGER NULL, \n" +
					"	md5_sum			TEXT NULL\n" +
					")");
				Execute (
					"INSERT INTO photos (id, time, uri, description, roll_id, default_version_id, rating, md5_sum) " +
					"SELECT id, time, uri, description, roll_id, default_version_id, rating, md5_sum " +
					$"FROM {tempTable}");

				tempTable = MoveTableToTemp ("photo_tags");
				Execute (
					"CREATE TABLE photo_tags (\n" +
					"	photo_id	INTEGER, \n" +
					"       tag_id		INTEGER, \n" +
					"       UNIQUE (photo_id, tag_id)\n" +
					")");
				Execute ("INSERT OR IGNORE INTO photo_tags (photo_id, tag_id) " + "SELECT photo_id, tag_id " +
				         $"FROM {tempTable}");

				tempTable = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (\n" +
					"	photo_id	INTEGER, \n" +
					"	version_id	INTEGER, \n" +
					"	name		STRING, \n" +
					"	uri		STRING NOT NULL, \n" +
					"	md5_sum		STRING NOT NULL, \n" +
					"	protected	BOOLEAN, \n" +
					"	UNIQUE (photo_id, version_id)\n" +
					")");
				Execute ("INSERT OR IGNORE INTO photo_versions (photo_id, version_id, name, uri, md5_sum, protected) " +
				         "SELECT photo_id, version_id, name, uri, md5_sum, protected " + $"FROM {tempTable}");

				Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");
				Execute ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");

				tempTable = MoveTableToTemp ("rolls");
				Execute (
					"CREATE TABLE rolls (\n" +
					"	id	INTEGER PRIMARY KEY NOT NULL, \n" +
					"       time	INTEGER NOT NULL\n" +
					")");
				Execute ("INSERT INTO rolls (id, time) " + "SELECT id, time " + $"FROM {tempTable}");

				tempTable = MoveTableToTemp ("tags");
				Execute (
					"CREATE TABLE tags (\n" +
					"	id		INTEGER PRIMARY KEY NOT NULL, \n" +
					"	name		TEXT UNIQUE, \n" +
					"	category_id	INTEGER, \n" +
					"	is_category	BOOLEAN, \n" +
					"	sort_priority	INTEGER, \n" +
					"	icon		TEXT\n" +
					")");
				Execute ("INSERT INTO tags (id, name, category_id, is_category, sort_priority, icon) " +
				         "SELECT id, name, category_id, is_category, sort_priority, icon " + $"FROM {tempTable}");
			});

			// Update to version 16.5
			AddUpdate (new Version (16, 5), delegate { //fix md5 null in photos and photo_versions table
				string tempTable = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (\n" +
					"	photo_id	INTEGER, \n" +
					"	version_id	INTEGER, \n" +
					"	name		STRING, \n" +
					"	uri		STRING NOT NULL, \n" +
					"	md5_sum		TEXT NULL, \n" +
					"	protected	BOOLEAN, \n" +
					"	UNIQUE (photo_id, version_id)\n" +
					")");
				Execute ("INSERT OR IGNORE INTO photo_versions (photo_id, version_id, name, uri, md5_sum, protected) " +
				         "SELECT photo_id, version_id, name, uri, md5_sum, protected " + $"FROM {tempTable}");

				Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");

				Execute ("UPDATE photos SET md5_sum = NULL WHERE md5_sum = ''");
				Execute ("UPDATE photo_versions SET md5_sum = NULL WHERE md5_sum = ''");
			});

			// Update to version 17.0, split uri and filename
			AddUpdate (new Version (17, 0), delegate {
				string tmpPhotos = MoveTableToTemp ("photos");
				string tmpVersions = MoveTableToTemp ("photo_versions");

				Execute (
					"CREATE TABLE photos (\n" +
					"	id			INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, \n" +
					"	time			INTEGER NOT NULL, \n" +
					"	base_uri		STRING NOT NULL, \n" +
					"	filename		STRING NOT NULL, \n" +
					"	description		TEXT NOT NULL, \n" +
					"	roll_id			INTEGER NOT NULL, \n" +
					"	default_version_id	INTEGER NOT NULL, \n" +
					"	rating			INTEGER NULL, \n" +
					"	md5_sum			TEXT NULL\n" +
					")");

				Execute (
					"CREATE TABLE photo_versions (\n" +
					"	photo_id	INTEGER, \n" +
					"	version_id	INTEGER, \n" +
					"	name		STRING, \n" +
					"	base_uri		STRING NOT NULL, \n" +
					"	filename		STRING NOT NULL, \n" +
					"	md5_sum		TEXT NULL, \n" +
					"	protected	BOOLEAN, \n" +
					"	UNIQUE (photo_id, version_id)\n" +
					")");

				Hyena.Data.Sqlite.IDataReader reader = ExecuteReader (
					"SELECT id, time, uri, description, roll_id, default_version_id, rating, md5_sum " +
					$"FROM {tmpPhotos} ");

				while (reader.Read ()) {
					System.Uri photoUri = new System.Uri (reader["uri"] as string);

					string filename = photoUri.GetFilename ();
					Uri baseUri = photoUri.GetDirectoryUri ();

					string md5 = reader["md5_sum"] != null ? reader["md5_sum"].ToString () : null;

					Execute (new HyenaSqliteCommand (
						"INSERT INTO photos (id, time, base_uri, filename, description, roll_id, default_version_id, rating, md5_sum) " +
						"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
						Convert.ToUInt32 (reader["id"]),
						reader["time"],
						baseUri.ToString (),
						filename,
						reader["description"].ToString (),
						Convert.ToUInt32 (reader["roll_id"]),
						Convert.ToUInt32 (reader["default_version_id"]),
						Convert.ToUInt32 (reader["rating"]),
						string.IsNullOrEmpty (md5) ? null : md5));
				}

				reader.Dispose ();

				reader = ExecuteReader ("SELECT photo_id, version_id, name, uri, md5_sum, protected " +
				                        $"FROM {tmpVersions} ");

				while (reader.Read ()) {
					System.Uri photoUri = new System.Uri (reader["uri"] as string);

					string filename = photoUri.GetFilename ();
					Uri baseUri = photoUri.GetDirectoryUri ();

					string md5 = reader["md5_sum"] != null ? reader["md5_sum"].ToString () : null;

					Execute (new HyenaSqliteCommand (
						"INSERT INTO photo_versions (photo_id, version_id, name, base_uri, filename, protected, md5_sum) " +
						"VALUES (?, ?, ?, ?, ?, ?, ?)",
						Convert.ToUInt32 (reader["photo_id"]),
						Convert.ToUInt32 (reader["version_id"]),
						reader["name"].ToString (),
						baseUri.ToString (),
						filename,
						Convert.ToBoolean (reader["protected"]),
						string.IsNullOrEmpty (md5) ? null : md5));
				}

				Execute ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");
				Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");


			}, true);

			// Update to version 17.1, Rename 'Import Tags' to 'Imported Tags'
			AddUpdate (new Version (17, 1), delegate {
				Execute ("UPDATE tags SET name = 'Imported Tags' WHERE name = 'Import Tags'");
			});

			// Update to version 17.2, Make sure every photo has an Original version in photo_versions
			AddUpdate (new Version (17, 2), delegate {
				// Find photos that have no original version;
				var haveOriginalQuery = "SELECT id FROM photos LEFT JOIN photo_versions AS pv ON pv.photo_id = id WHERE pv.version_id = 1";
				var noOriginalQuery =
					$"SELECT id, base_uri, filename FROM photos WHERE id NOT IN ({haveOriginalQuery})";

				var reader = ExecuteReader (noOriginalQuery);

				while (reader.Read ()) {
					Execute (new HyenaSqliteCommand (
						"INSERT INTO photo_versions (photo_id, version_id, name, base_uri, filename, protected, md5_sum) " +
						"VALUES (?, ?, ?, ?, ?, ?, ?)",
						Convert.ToUInt32 (reader["id"]),
						1,
						"Original",
						reader["base_uri"].ToString (),
						reader["filename"].ToString (),
						1,
						""));
				}
			}, true);

			// Update to version 18.0, Import MD5 hashes
			AddUpdate (new Version (18, 0), delegate {
				string tmpPhotos = MoveTableToTemp ("photos");
				string tmpVersions = MoveTableToTemp ("photo_versions");

				Execute (
					"CREATE TABLE photos (\n" +
					"	id			INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, \n" +
					"	time			INTEGER NOT NULL, \n" +
					"	base_uri		STRING NOT NULL, \n" +
					"	filename		STRING NOT NULL, \n" +
					"	description		TEXT NOT NULL, \n" +
					"	roll_id			INTEGER NOT NULL, \n" +
					"	default_version_id	INTEGER NOT NULL, \n" +
					"	rating			INTEGER NULL \n" +
					")");

				Execute (
					"CREATE TABLE photo_versions (\n" +
					"	photo_id	INTEGER, \n" +
					"	version_id	INTEGER, \n" +
					"	name		STRING, \n" +
					"	base_uri		STRING NOT NULL, \n" +
					"	filename		STRING NOT NULL, \n" +
					"	import_md5		TEXT NULL, \n" +
					"	protected	BOOLEAN, \n" +
					"	UNIQUE (photo_id, version_id)\n" +
					")");

				var reader = ExecuteReader (
					"SELECT id, time, base_uri, filename, description, roll_id, default_version_id, rating " +
					$"FROM {tmpPhotos} ");

				while (reader.Read ()) {
					Execute (new HyenaSqliteCommand (
						"INSERT INTO photos (id, time, base_uri, filename, description, roll_id, default_version_id, rating) " +
						"VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
						Convert.ToUInt32 (reader["id"]),
						reader["time"],
						reader["base_uri"].ToString (),
						reader["filename"].ToString (),
						reader["description"].ToString (),
						Convert.ToUInt32 (reader["roll_id"]),
						Convert.ToUInt32 (reader["default_version_id"]),
						Convert.ToUInt32 (reader["rating"])));
				}

				reader.Dispose ();

				reader = ExecuteReader ("SELECT photo_id, version_id, name, base_uri, filename, protected " +
				                        $"FROM {tmpVersions} ");

				while (reader.Read ()) {
					Execute (new HyenaSqliteCommand (
						"INSERT INTO photo_versions (photo_id, version_id, name, base_uri, filename, protected, import_md5) " +
						"VALUES (?, ?, ?, ?, ?, ?, ?)",
						Convert.ToUInt32 (reader["photo_id"]),
						Convert.ToUInt32 (reader["version_id"]),
						reader["name"].ToString (),
						reader["base_uri"].ToString (),
						reader["filename"].ToString (),
						Convert.ToBoolean (reader["protected"]),
						""));
				}

				Execute ("CREATE INDEX idx_photo_versions_import_md5 ON photo_versions(import_md5)");

			}, true);
		}

		const string MetaDbVersionString = "F-Spot Database Version";

		static Version GetDatabaseVersion ()
		{
			if (!TableExists ("meta"))
				throw new Exception ("No meta table found!");

			var query = $"SELECT data FROM meta WHERE name = '{MetaDbVersionString}'";
			var versionId = SelectSingleString (query);
			return new Version (versionId);
		}

		public static void Run (FSpotDatabaseConnection database, IUpdaterUI updaterDialog)
		{
			_db = database;
			_dialog = updaterDialog;

			_dbVersion = GetDatabaseVersion ();

			if (Updates.Count == 0)
				return;

			if (_dbVersion == LatestVersion)
				return;

			if (_dbVersion > LatestVersion) {
				if (!Silent)
					Log.Information ("The existing database version is more recent than this version of F-Spot expects.");
				return;
			}

			uint timer = 0;
			if (!Silent)
				timer = Log.InformationTimerStart ("Updating F-Spot Database");

			// Only create and show the dialog if one or more of the updates to be done is
			// marked as being slow
			bool slow = false;
			foreach (Version version in Updates.Keys) {
				if (version > _dbVersion && Updates[version].IsSlow)
					slow = true;
				break;
			}

			if (slow && !Silent) {
				_dialog.Show ();
			}

			_db.BeginTransaction ();
			try {
				var keys = new List<Version> (Updates.Keys);
				keys.Sort ();

				foreach (Version version in keys) {
					if (version <= _dbVersion)
						continue;
					_dialog.Pulse ();
					Updates[version].Execute (_db, _dbVersion);
				}

				_db.CommitTransaction ();
			} catch (Exception e) {
				if (!Silent) {
					Log.DebugException (e);
					Log.Warning ("Rolling back database changes because of Exception");
				}
				// There was an error, roll back the database
				_db.RollbackTransaction ();

				// Pass the exception on, this is fatal
				throw;
			}

			_dialog.Destroy ();

			if (_dbVersion == LatestVersion && !Silent)
				Log.InformationTimerPrint (timer, "Database updates completed successfully (in {0}).");
		}

		static void AddUpdate (Version version, UpdateCode code, bool isSlow = false)
		{
			Updates[version] = new Update (version, code, isSlow);
		}

		static int Execute (string statement)
		{
			int result;
			try {
				result = Convert.ToInt32 (_db.Execute (statement));
			} catch (OverflowException e) {
				Log.Exception ($"Updater.Execute failed. ({statement})", e);
				throw;
			}
			return result;
		}

		static int Execute (HyenaSqliteCommand command)
		{
			int result = -1;
			try {
				result = Convert.ToInt32 (_db.Execute (command));
			} catch (OverflowException e) {
				Log.Exception ($"Updater.Execute failed. ({command})", e);
				throw;
			}
			return result;
		}

		static int ExecuteScalar (string statement)
		{
			return Execute (statement);
		}

		static IDataReader ExecuteReader (string statement)
		{
			return _db.Query (statement);
		}

		static bool TableExists (string table)
		{
			return _db.TableExists (table);
		}

		static string SelectSingleString (string statement)
		{
			string result = null;

			try {
				result = _db.Query<string> (statement);
			} catch (Exception) {
			}
			return result;
		}

		static string MoveTableToTemp (string tableName)
		{
			string tempName = tableName + "_temp";

			// Get the table definition for the table we are copying
			string sql = SelectSingleString (
				$"SELECT sql FROM sqlite_master WHERE tbl_name = '{tableName}' AND type = 'table' ORDER BY type DESC");

			// Drop temp table if already exists
			Execute ("DROP TABLE IF EXISTS " + tempName);

			// Change the SQL to create the temp table
			Execute (sql.Replace ("CREATE TABLE " + tableName, "CREATE TEMPORARY TABLE " + tempName));

			// Copy the data
			ExecuteScalar ($"INSERT INTO {tempName} SELECT * FROM {tableName}");

			// Delete the original table
			Execute ("DROP TABLE " + tableName);

			return tempName;
		}

		delegate void UpdateCode ();

		class Update
		{
			Version Version { get; }

			readonly UpdateCode code;
			public bool IsSlow { get; }

			public Update (Version toVersion, UpdateCode code, bool slow)
			{
				Version = toVersion;
				this.code = code;
				IsSlow = slow;
			}

			public Update (Version toVersion, UpdateCode code)
			{
				Version = toVersion;
				this.code = code;
			}

			public void Execute (HyenaSqliteConnection db, Version dbVersion)
			{
				code ();

				if (!Silent) {
					Log.Debug ($"Updated database from version {dbVersion} to {Version}");
				}

				dbVersion = Version;
				db.Execute (new HyenaSqliteCommand ("UPDATE meta SET data = ? WHERE name = ?", dbVersion.ToString (), MetaDbVersionString));
			}
		}

		// TODO: Look into System.Version
		public class Version : IComparable<Version>
		{
			readonly int major;
			readonly int minor;

			public Version (int major, int minor)
			{
				this.major = major;
				this.minor = minor;
			}

			public Version (string version)
			{
				if (string.IsNullOrEmpty (version)) {
					major = minor = 0;
					return;
				}

				var parts = version.Split (new[] { '.' }, 2);

				if (!int.TryParse (parts[0], out major))
					major = 0;

				if (parts.Length <= 1 || !int.TryParse (parts[1], out minor))
					minor = 0;
			}

			public int CompareTo (Version version)
			{
				return Compare (this, version);
			}

			public static int Compare (Version v1, Version v2)
			{
				if (v1.major == v2.major)
					return v1.minor.CompareTo (v2.minor);
				return v1.major.CompareTo (v2.major);
			}

			public override string ToString ()
			{
				if (minor == 0)
					return major.ToString ();
				return $"{major}.{minor}";
			}

			public override int GetHashCode ()
			{
				return major ^ minor;
			}

			public static bool operator == (Version v1, Version v2)
			{
				return v1.major == v2.major && v1.minor == v2.minor;
			}

			public static bool operator != (Version v1, Version v2)
			{
				return !(v1 == v2);
			}

			public static bool operator < (Version v1, Version v2)
			{
				return Compare (v1, v2) < 0;
			}

			public static bool operator > (Version v1, Version v2)
			{
				return Compare (v1, v2) > 0;
			}

			public static bool operator <= (Version v1, Version v2)
			{
				return !(v1 > v2);
			}

			public static bool operator >= (Version v1, Version v2)
			{
				return !(v1 < v2);
			}
		}
	}
}
