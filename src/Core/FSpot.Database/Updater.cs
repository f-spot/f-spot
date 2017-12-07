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
using System.Collections.Generic;

using FSpot.Utils;

using Hyena;
using Hyena.Data.Sqlite;

namespace FSpot.Database
{
	public static class Updater
	{
		private static IUpdaterUI dialog;
		private static Dictionary<Version, Update> updates = new Dictionary<Version, Update> ();
		private static Version db_version;
		private static FSpotDatabaseConnection db;
		public static bool silent = false;

		public static Version LatestVersion {
			get {
				if (updates == null || updates.Count == 0)
					return new Version (0, 0);

				List<Version> keys = new List<Version> ();
				foreach (Version k in updates.Keys) {
					keys.Add(k);
				}
				keys.Sort ();

				return keys [keys.Count - 1];
			}
		}

		static Updater ()
		{
			// Update from version 0 to 1: Remove empty Other tags
			AddUpdate (new Version ("1"), delegate () {
				string other_id = SelectSingleString ("SELECT id FROM tags WHERE name = 'Other'");

				if (other_id == null)
					return;

				// Don't do anything if there are subtags
				string tag_count = SelectSingleString (
					string.Format ("SELECT COUNT(*) FROM tags WHERE category_id = {0}", other_id));

				if (tag_count == null || System.Int32.Parse (tag_count) != 0)
					return;

				// Don't do anything if there are photos tagged with this
				string photo_count = SelectSingleString (
					string.Format ("SELECT COUNT(*) FROM photo_tags WHERE tag_id = {0}", other_id));

				if (photo_count == null || System.Int32.Parse (photo_count) != 0)
					return;

				// Finally, we know that the Other tag exists and has no children, so remove it
				Execute ("DELETE FROM tags WHERE name = 'Other'");
			});

			// Update from version 1 to 2: Restore Other tags that were removed leaving dangling child tags
			AddUpdate (new Version ("2"), delegate () {
				string tag_count = SelectSingleString ("SELECT COUNT(*) FROM tags WHERE category_id != 0 AND category_id NOT IN (SELECT id FROM tags)");

				// If there are no dangling tags, then don't do anything
				if (tag_count == null || System.Int32.Parse (tag_count) == 0)
					return;

				int id = ExecuteScalar ("INSERT INTO tags (name, category_id, is_category, icon) VALUES ('Other', 0, 1, 'stock_icon:f-spot-other.png')");

				Execute (string.Format (
					"UPDATE tags SET category_id = {0} WHERE id IN " +
					"(SELECT id FROM tags WHERE category_id != 0 AND category_id " +
					"NOT IN (SELECT id FROM tags))",
					id));

				Log.Debug ("Other tag restored.  Sorry about that!");
			});

			// Update from version 2 to 3: ensure that Hidden is the only tag left which is a real tag (not category)
			AddUpdate (new Version ("3"), delegate () {
				Execute ("UPDATE tags SET is_category = 1 WHERE name != 'Hidden'");
			});

			//Version 3.1, clean old (and unused) items in Export
			AddUpdate (new Version (3, 1), delegate () {
				if (TableExists ("exports"))
					ExecuteScalar ("DELETE FROM exports WHERE export_type='fspot:Folder'");
			});

			//Version 4.0, bump the version number to a integer, for backward compatibility
			AddUpdate (new Version (4, 0), delegate () {});


			//Version 5.0, add a roll_id field to photos, rename table 'imports' to 'rolls'
			//and fix bgo 324425.
			AddUpdate (new Version (5, 0), delegate () {
				Log.Debug ("Will add a roll_id field to photos!");
				string tmp_photos = MoveTableToTemp ("photos");
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
				ExecuteScalar (string.Format ("INSERT INTO photos SELECT id, time, directory_path, name, description, 0, default_version_id FROM {0}", tmp_photos));

				Log.Debug ("Will rename imports to rolls!");
				string tmp_rolls = MoveTableToTemp ("imports");
				Execute (
					"CREATE TABLE rolls (                                     " +
					"	id                 INTEGER PRIMARY KEY NOT NULL,   " +
					"       time               INTEGER NOT NULL	   	   " +
					")");
				ExecuteScalar (string.Format ("INSERT INTO rolls SELECT id, time FROM {0}", tmp_rolls));

				Log.Debug ("Cleaning weird descriptions, fixes bug #324425.");
				Execute ("UPDATE photos SET description = \"\" WHERE description LIKE \"Invalid size of entry%\"");
			});


			//Version 6.0, change tag icon f-spot-tag-other to emblem-generic
			AddUpdate (new Version (6, 0), delegate () {
				ExecuteScalar ("UPDATE tags SET icon = \"stock_icon:emblem-generic\" " +
						" WHERE icon LIKE \"stock_icon:f-spot-other.png\"");
			});

			//Update to version 7.0, keep photo uri instead of path
			AddUpdate (new Version (7, 0), delegate () {
				string tmp_photos = MoveTableToTemp ("photos");
				Execute (
					"CREATE TABLE photos (" +
					"	id                 INTEGER PRIMARY KEY NOT NULL," +
					"       time               INTEGER NOT NULL," +
					"       uri                STRING NOT NULL," +
					"       description        TEXT NOT NULL," +
					"       roll_id            INTEGER NOT NULL," +
					"       default_version_id INTEGER NOT NULL" +
					")");
				Execute (string.Format (
					"INSERT INTO photos (id, time, uri, description, roll_id, default_version_id)	" +
					"SELECT id, time, 'file://' || directory_path || '/' || name, 		" +
					"description, roll_id, default_version_id FROM {0}", tmp_photos));
			}, true);

			// Update to version 8.0, store full version uri
			AddUpdate (new Version (8, 0), delegate () {
				string tmp_versions = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (          " +
					"       photo_id        INTEGER,        " +
					"       version_id      INTEGER,        " +
					"       name            STRING,         " +
					"       uri             STRING NOT NULL " +
					")");

				Hyena.Data.Sqlite.IDataReader reader = ExecuteReader (string.Format (
						"SELECT photo_id, version_id, name, uri " +
						"FROM {0}, photos " +
						"WHERE photo_id = id ", tmp_versions));

				while (reader.Read ()) {
					System.Uri photo_uri = new System.Uri (reader [3] as string);
					string name_without_extension = System.IO.Path.GetFileNameWithoutExtension (photo_uri.AbsolutePath);
					string extension = System.IO.Path.GetExtension (photo_uri.AbsolutePath);

					string uri = photo_uri.Scheme + "://" +
						photo_uri.Host +
						System.IO.Path.GetDirectoryName (photo_uri.AbsolutePath) + "/" +
						name_without_extension + " (" + (reader [2]).ToString () + ")" + extension;

					Execute (new HyenaSqliteCommand (
						"INSERT INTO photo_versions (photo_id, version_id, name, uri) " +
						"VALUES (?, ?, ?, ?)",
						Convert.ToUInt32 (reader [0]),
						Convert.ToUInt32 (reader [1]),
						(reader [2]).ToString (),
						uri));
				}

			}, true);

			// Update to version 9.0
			AddUpdate (new Version (9, 0), delegate () {
				string tmp_versions = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (          " +
					"       photo_id        INTEGER,        " +
					"       version_id      INTEGER,        " +
					"       name            STRING,         " +
					"       uri             STRING NOT NULL," +
					"	protected	BOOLEAN		" +
					")");
				Execute (string.Format (
					"INSERT INTO photo_versions (photo_id, version_id, name, uri, protected) " +
					"SELECT photo_id, version_id, name, uri, 0 " +
					"FROM {0} ", tmp_versions));
			});

			// Update to version 10.0, make id autoincrement
			AddUpdate (new Version (10, 0), delegate () {
				string tmp_photos = MoveTableToTemp ("photos");
				Execute (
					"CREATE TABLE photos (                                     " +
					"	id                 INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
					"	time               INTEGER NOT NULL,	   	   " +
					"	uri		   STRING NOT NULL,		   " +
					"	description        TEXT NOT NULL,	           " +
					"	roll_id            INTEGER NOT NULL,		   " +
					"	default_version_id INTEGER NOT NULL		   " +
					")");

				Execute (string.Format (
					"INSERT INTO photos (id, time, uri, description, roll_id, default_version_id) " +
					"SELECT id, time, uri, description, roll_id, default_version_id  " +
					"FROM  {0} ", tmp_photos));
			}, false);

			// Update to version 11.0, rating
			AddUpdate (new Version (11, 0), delegate () {
				string tmp_photos = MoveTableToTemp ("photos");
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

				Execute (string.Format (
					"INSERT INTO photos (id, time, uri, description, roll_id, default_version_id, rating) " +
					"SELECT id, time, uri, description, roll_id, default_version_id, null  " +
					"FROM  {0} ", tmp_photos));
			});

			//Update to version 12.0, remove dead associations, bgo #507950, #488545
			AddUpdate (new Version (12, 0), delegate () {
				Execute ("DELETE FROM photo_tags WHERE tag_id NOT IN (SELECT id FROM tags)");
			});

			// Update to version 13.0
			AddUpdate (new Version (13, 0), delegate () {
				Execute ("UPDATE photos SET rating = 0 WHERE rating IS NULL");
			});

			// Update to version 14.0
			AddUpdate (new Version (14, 0), delegate () {
				Execute ("UPDATE photos SET rating = 0 WHERE rating IS NULL");
			});

			// Update to version 15.0
			AddUpdate (new Version (15, 0), delegate () {
				string tmp_photo_tags = MoveTableToTemp ("photo_tags");
				Execute (
					"CREATE TABLE photo_tags (        " +
					"	photo_id      INTEGER,    " +
					"       tag_id        INTEGER,    " +
					"       UNIQUE (photo_id, tag_id) " +
					")");
				Execute (string.Format (
					"INSERT OR IGNORE INTO photo_tags (photo_id, tag_id) " +
					"SELECT photo_id, tag_id FROM {0}", tmp_photo_tags));
				string tmp_photo_versions = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (		" +
					"	photo_id	INTEGER,	" +
					"	version_id	INTEGER,	" +
					"	name		STRING,		" +
					"	uri		STRING NOT NULL," +
					"	protected	BOOLEAN, 	" +
					"	UNIQUE (photo_id, version_id)	" +
					")");
				Execute (string.Format (
					"INSERT OR IGNORE INTO photo_versions 		" +
					"(photo_id, version_id, name, uri, protected)	" +
					"SELECT photo_id, version_id, name, uri, protected FROM {0}", tmp_photo_versions));
			});

			// Update to version 16.0
			AddUpdate (new Version (16, 0), delegate () {
				string temp_table = MoveTableToTemp ("photos");

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

				Execute (string.Format ("INSERT INTO photos (id, time, uri, description, roll_id, " +
							 "default_version_id, rating, md5_sum) " +
							 "SELECT id, time, uri, description, roll_id, " +
							 "       default_version_id, rating, '' " +
							 "FROM   {0} ",
							 temp_table
							)
				 );


				string temp_versions_table = MoveTableToTemp ("photo_versions");

				Execute ("CREATE TABLE photo_versions (    	" +
					  "      photo_id        INTEGER,  	" +
					  "      version_id      INTEGER,  	" +
					  "      name            STRING,    	" +
					  "	uri		STRING NOT NULL," +
					  "	md5_sum		STRING NOT NULL," +
					  "	protected	BOOLEAN		" +
					  ")");

				Execute (string.Format ("INSERT INTO photo_versions (photo_id, version_id, name, uri, md5_sum, protected) " +
							 "SELECT photo_id, version_id, name, uri, '', protected " +
							 "FROM   {0} ",
							 temp_versions_table
							)
				 );

				JobStore.CreateTable (db);

				// This is kind of hacky but should be a lot faster on
				// large photo databases
				Execute (string.Format ("INSERT INTO jobs (job_type, job_options, run_at, job_priority) " +
							 "SELECT '{0}', id, {1}, {2} " +
							 "FROM   photos ",
							 typeof(Jobs.CalculateHashJob).ToString (),
							 DateTimeUtil.FromDateTime (DateTime.Now),
							 0
							)
				 );
			}, true);

			// Update to version 16.1
			AddUpdate (new Version (16, 1), delegate () {
				Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");
			}, false);

			// Update to version 16.2
			AddUpdate (new Version (16, 2), delegate () {
				Execute ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");
			}, false);

			// Update to version 16.3
			AddUpdate (new Version (16, 3), delegate () {
				Execute (string.Format ("DELETE FROM jobs WHERE job_type = '{0}'", typeof(Jobs.CalculateHashJob).ToString ()));
			}, false);

			// Update to version 16.4
			AddUpdate (new Version (16, 4), delegate () { //fix the tables schema EOL
				string temp_table = MoveTableToTemp ("exports");
				Execute (
					"CREATE TABLE exports (\n" +
					"	id			INTEGER PRIMARY KEY NOT NULL, \n" +
					"	image_id		INTEGER NOT NULL, \n" +
					"	image_version_id	INTEGER NOT NULL, \n" +
					"	export_type		TEXT NOT NULL, \n" +
					"	export_token		TEXT NOT NULL\n" +
					")");
				Execute (string.Format (
					"INSERT INTO exports (id, image_id, image_version_id, export_type, export_token) " +
					"SELECT id, image_id, image_version_id, export_type, export_token " +
					"FROM {0}", temp_table));

				temp_table = MoveTableToTemp ("jobs");
				Execute (
					"CREATE TABLE jobs (\n" +
					"	id		INTEGER PRIMARY KEY NOT NULL, \n" +
					"	job_type	TEXT NOT NULL, \n" +
					"	job_options	TEXT NOT NULL, \n" +
					"	run_at		INTEGER, \n" +
					"	job_priority	INTEGER NOT NULL\n" +
					")");
				Execute (string.Format (
					"INSERT INTO jobs (id, job_type, job_options, run_at, job_priority) " +
					"SELECT id, job_type, job_options, run_at, job_priority " +
					"FROM {0}", temp_table));

				temp_table = MoveTableToTemp ("meta");
				Execute (
					"CREATE TABLE meta (\n" +
					"	id	INTEGER PRIMARY KEY NOT NULL, \n" +
					"	name	TEXT UNIQUE NOT NULL, \n" +
					"	data	TEXT\n" +
					")");
				Execute (string.Format (
					"INSERT INTO meta (id, name, data) " +
					"SELECT id, name, data " +
					"FROM {0}", temp_table));

				temp_table = MoveTableToTemp ("photos");
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
				Execute (string.Format (
					"INSERT INTO photos (id, time, uri, description, roll_id, default_version_id, rating, md5_sum) " +
					"SELECT id, time, uri, description, roll_id, default_version_id, rating, md5_sum " +
					"FROM {0}", temp_table));

				temp_table = MoveTableToTemp ("photo_tags");
				Execute (
					"CREATE TABLE photo_tags (\n" +
					"	photo_id	INTEGER, \n" +
					"       tag_id		INTEGER, \n" +
					"       UNIQUE (photo_id, tag_id)\n" +
					")");
				Execute (string.Format (
					"INSERT OR IGNORE INTO photo_tags (photo_id, tag_id) " +
					"SELECT photo_id, tag_id " +
					"FROM {0}", temp_table));

				temp_table = MoveTableToTemp ("photo_versions");
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
				Execute (string.Format (
					"INSERT OR IGNORE INTO photo_versions (photo_id, version_id, name, uri, md5_sum, protected) " +
					"SELECT photo_id, version_id, name, uri, md5_sum, protected " +
					"FROM {0}", temp_table));

				Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");
				Execute ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");

				temp_table = MoveTableToTemp ("rolls");
				Execute (
					"CREATE TABLE rolls (\n" +
					"	id	INTEGER PRIMARY KEY NOT NULL, \n" +
					"       time	INTEGER NOT NULL\n" +
					")");
				Execute (string.Format (
					"INSERT INTO rolls (id, time) " +
					"SELECT id, time " +
					"FROM {0}", temp_table));

				temp_table = MoveTableToTemp ("tags");
				Execute (
					"CREATE TABLE tags (\n" +
					"	id		INTEGER PRIMARY KEY NOT NULL, \n" +
					"	name		TEXT UNIQUE, \n" +
					"	category_id	INTEGER, \n" +
					"	is_category	BOOLEAN, \n" +
					"	sort_priority	INTEGER, \n" +
					"	icon		TEXT\n" +
					")");
				Execute (string.Format (
					"INSERT INTO tags (id, name, category_id, is_category, sort_priority, icon) " +
					"SELECT id, name, category_id, is_category, sort_priority, icon " +
					"FROM {0}", temp_table));
			});

			// Update to version 16.5
			AddUpdate (new Version (16, 5), delegate () { //fix md5 null in photos and photo_versions table
				string temp_table = MoveTableToTemp ("photo_versions");
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
				Execute (string.Format (
					"INSERT OR IGNORE INTO photo_versions (photo_id, version_id, name, uri, md5_sum, protected) " +
					"SELECT photo_id, version_id, name, uri, md5_sum, protected " +
					"FROM {0}", temp_table));

				Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");

				Execute ("UPDATE photos SET md5_sum = NULL WHERE md5_sum = ''");
				Execute ("UPDATE photo_versions SET md5_sum = NULL WHERE md5_sum = ''");
			});

			// Update to version 17.0, split uri and filename
			AddUpdate (new Version (17, 0), delegate () {
				string tmp_photos = MoveTableToTemp ("photos");
				string tmp_versions = MoveTableToTemp ("photo_versions");

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

				Hyena.Data.Sqlite.IDataReader reader = ExecuteReader (string.Format (
					"SELECT id, time, uri, description, roll_id, default_version_id, rating, md5_sum " +
					"FROM {0} ", tmp_photos));

				while (reader.Read ()) {
					System.Uri photo_uri = new System.Uri (reader ["uri"] as string);

					string filename = photo_uri.GetFilename ();
					Uri base_uri = photo_uri.GetDirectoryUri ();

					string md5 = reader ["md5_sum"] != null ? reader ["md5_sum"].ToString () : null;

					Execute (new HyenaSqliteCommand (
						"INSERT INTO photos (id, time, base_uri, filename, description, roll_id, default_version_id, rating, md5_sum) " +
						"VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
						Convert.ToUInt32 (reader ["id"]),
						reader ["time"],
						base_uri.ToString (),
						filename,
						reader ["description"].ToString (),
						Convert.ToUInt32 (reader ["roll_id"]),
						Convert.ToUInt32 (reader ["default_version_id"]),
						Convert.ToUInt32 (reader ["rating"]),
						string.IsNullOrEmpty (md5) ? null : md5));
				}

				reader.Dispose ();

				reader = ExecuteReader (string.Format (
						"SELECT photo_id, version_id, name, uri, md5_sum, protected " +
						"FROM {0} ", tmp_versions));

				while (reader.Read ()) {
					System.Uri photo_uri = new System.Uri (reader ["uri"] as string);

					string filename = photo_uri.GetFilename ();
					Uri base_uri = photo_uri.GetDirectoryUri ();

					string md5 = reader ["md5_sum"] != null ? reader ["md5_sum"].ToString () : null;

					Execute (new HyenaSqliteCommand (
						"INSERT INTO photo_versions (photo_id, version_id, name, base_uri, filename, protected, md5_sum) " +
						"VALUES (?, ?, ?, ?, ?, ?, ?)",
						Convert.ToUInt32 (reader ["photo_id"]),
						Convert.ToUInt32 (reader ["version_id"]),
						reader ["name"].ToString (),
						base_uri.ToString (),
						filename,
						Convert.ToBoolean (reader ["protected"]),
						string.IsNullOrEmpty (md5) ? null : md5));
				}

				Execute ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");
				Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");


			}, true);

			// Update to version 17.1, Rename 'Import Tags' to 'Imported Tags'
			AddUpdate (new Version (17, 1), delegate () {
				Execute ("UPDATE tags SET name = 'Imported Tags' WHERE name = 'Import Tags'");
			});

			// Update to version 17.2, Make sure every photo has an Original version in photo_versions
			AddUpdate (new Version (17, 2), delegate () {
				// Find photos that have no original version;
				var have_original_query = "SELECT id FROM photos LEFT JOIN photo_versions AS pv ON pv.photo_id = id WHERE pv.version_id = 1";
				var no_original_query = string.Format ("SELECT id, base_uri, filename FROM photos WHERE id NOT IN ({0})", have_original_query);

				var reader = ExecuteReader (no_original_query);

				while (reader.Read ()) {
					Execute (new HyenaSqliteCommand (
						"INSERT INTO photo_versions (photo_id, version_id, name, base_uri, filename, protected, md5_sum) " +
						"VALUES (?, ?, ?, ?, ?, ?, ?)",
						Convert.ToUInt32 (reader ["id"]),
						1,
						"Original",
						reader ["base_uri"].ToString (),
						reader ["filename"].ToString (),
						1,
						""));
				}
			}, true);

			// Update to version 18.0, Import MD5 hashes
			AddUpdate (new Version (18, 0), delegate () {
				string tmp_photos = MoveTableToTemp ("photos");
				string tmp_versions = MoveTableToTemp ("photo_versions");

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

				var reader = ExecuteReader (string.Format (
					"SELECT id, time, base_uri, filename, description, roll_id, default_version_id, rating " +
					"FROM {0} ", tmp_photos));

				while (reader.Read ()) {
					Execute (new HyenaSqliteCommand (
						"INSERT INTO photos (id, time, base_uri, filename, description, roll_id, default_version_id, rating) " +
						"VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
						Convert.ToUInt32 (reader ["id"]),
						reader ["time"],
						reader ["base_uri"].ToString (),
						reader ["filename"].ToString (),
						reader ["description"].ToString (),
						Convert.ToUInt32 (reader ["roll_id"]),
						Convert.ToUInt32 (reader ["default_version_id"]),
						Convert.ToUInt32 (reader ["rating"])));
				}

				reader.Dispose ();

				reader = ExecuteReader (string.Format (
						"SELECT photo_id, version_id, name, base_uri, filename, protected " +
						"FROM {0} ", tmp_versions));

				while (reader.Read ()) {
					Execute (new HyenaSqliteCommand (
						"INSERT INTO photo_versions (photo_id, version_id, name, base_uri, filename, protected, import_md5) " +
						"VALUES (?, ?, ?, ?, ?, ?, ?)",
						Convert.ToUInt32 (reader ["photo_id"]),
						Convert.ToUInt32 (reader ["version_id"]),
						reader ["name"].ToString (),
						reader ["base_uri"].ToString (),
						reader ["filename"].ToString (),
						Convert.ToBoolean (reader ["protected"]),
						""));
				}

				Execute ("CREATE INDEX idx_photo_versions_import_md5 ON photo_versions(import_md5)");

			}, true);
		}

		private const string meta_db_version_string = "F-Spot Database Version";

		private static Version GetDatabaseVersion ()
		{
			if (!TableExists ("meta"))
				throw new Exception ("No meta table found!");

			var query = string.Format ("SELECT data FROM meta WHERE name = '{0}'", meta_db_version_string);
			var version_id = SelectSingleString (query);
			return new Version (version_id);
		}

		public static void Run (FSpotDatabaseConnection database, IUpdaterUI updaterDialog)
		{
			db = database;
			dialog = updaterDialog;

			db_version = GetDatabaseVersion ();

			if (updates.Count == 0)
				return;

			if (db_version == LatestVersion)
				return;
			else if (db_version > LatestVersion) {
				if (!silent)
					Log.Information ("The existing database version is more recent than this version of F-Spot expects.");
				return;
			}

			uint timer = 0;
			if (!silent)
				timer = Log.InformationTimerStart ("Updating F-Spot Database");

			// Only create and show the dialog if one or more of the updates to be done is
			// marked as being slow
			bool slow = false;
			foreach (Version version in updates.Keys) {
				if (version > db_version && (updates [version] as Update).IsSlow)
					slow = true;
				break;
			}

			if (slow && !silent) {
				dialog.Show ();
			}

			db.BeginTransaction ();
			try {
				List<Version> keys = new List<Version> ();
				foreach (Version k in updates.Keys) {
					keys.Add(k);
				}
				keys.Sort ();
				foreach (Version version in keys) {
					if (version <= db_version)
						continue;
					dialog.Pulse ();
					(updates [version] as Update).Execute (db, db_version);
				}

				db.CommitTransaction ();
			} catch (Exception e) {
				if (!silent) {
					Log.DebugException (e);
					Log.Warning ("Rolling back database changes because of Exception");
				}
				// There was an error, roll back the database
				db.RollbackTransaction ();

				// Pass the exception on, this is fatal
				throw e;
			}

			dialog.Destroy ();

			if (db_version == LatestVersion && !silent)
				Log.InformationTimerPrint (timer, "Database updates completed successfully (in {0}).");
		}

		private static void AddUpdate (Version version, UpdateCode code)
		{
			AddUpdate (version, code, false);
		}

		private static void AddUpdate (Version version, UpdateCode code, bool is_slow)
		{
			updates [version] = new Update (version, code, is_slow);
		}

		private static int Execute (string statement)
		{
			int result = -1;
			try {
				result = Convert.ToInt32 (db.Execute (statement));
			}
			catch (OverflowException e)
			{
				Log.Exception (string.Format ("Updater.Execute failed. ({0})", statement), e);
				throw;
			}
			return result;
		}

		private static int Execute (HyenaSqliteCommand command)
		{
			int result = -1;
			try {
				result = Convert.ToInt32 (db.Execute (command));
			}
			catch (OverflowException e)
			{
				Log.Exception (string.Format ("Updater.Execute failed. ({0})", command), e);
				throw;
			}
			return result;
		}

		private static int ExecuteScalar (string statement)
		{
			return Execute (statement);
		}

		private static Hyena.Data.Sqlite.IDataReader ExecuteReader (string statement)
		{
			return db.Query (statement);
		}

		private static bool TableExists (string table)
		{
			return db.TableExists (table);
		}

		private static string SelectSingleString (string statement)
		{
			string result = null;

			try {
				result = db.Query<string> (statement);
			} catch (Exception) {
			}
				return result;
		}

		private static string MoveTableToTemp (string table_name)
		{
			string temp_name = table_name + "_temp";

			// Get the table definition for the table we are copying
			string sql = SelectSingleString (string.Format ("SELECT sql FROM sqlite_master WHERE tbl_name = '{0}' AND type = 'table' ORDER BY type DESC", table_name));

			// Drop temp table if already exists
			Execute ("DROP TABLE IF EXISTS " + temp_name);

			// Change the SQL to create the temp table
			Execute (sql.Replace ("CREATE TABLE " + table_name, "CREATE TEMPORARY TABLE " + temp_name));

			// Copy the data
			ExecuteScalar (string.Format ("INSERT INTO {0} SELECT * FROM {1}", temp_name, table_name));

			// Delete the original table
			Execute ("DROP TABLE " + table_name);

			return temp_name;
		}

		private delegate void UpdateCode ();

		private class Update
		{
			public Version Version;
			private UpdateCode code;
			public bool IsSlow = false;

			public Update (Version to_version, UpdateCode code, bool slow)
			{
				this.Version = to_version;
				this.code = code;
				IsSlow = slow;
			}

			public Update (Version to_version, UpdateCode code)
			{
				this.Version = to_version;
				this.code = code;
			}

			public void Execute (HyenaSqliteConnection db, Version db_version)
			{
				code ();

				if (!silent) {
					Log.DebugFormat ("Updated database from version {0} to {1}",
							db_version.ToString (),
							Version.ToString ());
				}

				db_version = Version;
				db.Execute (new HyenaSqliteCommand ("UPDATE meta SET data = ? WHERE name = ?", db_version.ToString (), meta_db_version_string));
			}
		}

		// TODO: Look into System.Version
		public class Version : IComparable
		{
			int maj = 0;
			int min = 0;

			public Version (int maj, int min)
			{
				this.maj = maj;
				this.min = min;
			}

			public Version (string version)
			{
				string [] parts = version.Split (new char [] {'.'}, 2);
				try {
					this.maj = Convert.ToInt32 (parts [0]);
				} catch (Exception) {
					this.maj = 0;
				}
				try {
					this.min = Convert.ToInt32 (parts [1]);
				} catch (Exception) {
					this.min = 0;
				}
			}

			//IComparable
			public int CompareTo (object obj)
			{
				if (this.GetType () == obj.GetType ())
					return Compare (this, (Version)obj);
				else
					throw new Exception ("Object must be of type Version");
			}

			public int CompareTo (Version version)
			{
				return Compare (this, version);
			}

			public static int Compare (Version v1, Version v2)
			{
				if (v1.maj == v2.maj)
					return v1.min.CompareTo (v2.min);
				return v1.maj.CompareTo (v2.maj);
			}

			public override string ToString ()
			{
				if (min == 0)
					return maj.ToString ();
				return maj + "." + min;
			}

			public override bool Equals (object obj)
			{
				return obj is Version && this == (Version)obj;
			}

			public override int GetHashCode ()
			{
				return maj ^ min;
			}

			public static bool operator == (Version v1, Version v2)
			{
				return v1.maj == v2.maj && v1.min == v2.min;
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
