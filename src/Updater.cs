using Mono.Data.SqliteClient;
using Mono.Unix;
using Gtk;
using System;
using System.Collections;
using Banshee.Database;
using FSpot.Utils;
using FSpot.UI.Dialog;

namespace FSpot.Database {
	public static class Updater {
		private static ProgressDialog dialog;
		private static Hashtable updates = new Hashtable ();
		private static MetaItem db_version;
		private static Db db;


		public static Version LatestVersion {
			get {
				if (updates == null || updates.Count == 0)
					return new Version (0, 0);
				ArrayList keys = new ArrayList (updates.Keys);
				keys.Sort ();
				return keys[keys.Count - 1] as Version;
			}
		}
		
		static Updater () {
			// Update from version 0 to 1: Remove empty Other tags
			AddUpdate (new Version ("1"), delegate () {
				string other_id = SelectSingleString ("SELECT id FROM tags WHERE name = 'Other'");

				if (other_id == null)
					return;

				// Don't do anything if there are subtags
				string tag_count = SelectSingleString (
					String.Format ("SELECT COUNT(*) FROM tags WHERE category_id = {0}", other_id));
				
				if (tag_count == null || System.Int32.Parse (tag_count) != 0)
					return;
				
				// Don't do anything if there are photos tagged with this
				string photo_count = SelectSingleString (
					String.Format ("SELECT COUNT(*) FROM photo_tags WHERE tag_id = {0}", other_id));

				if (photo_count == null || System.Int32.Parse (photo_count) != 0)
					return;

				// Finally, we know that the Other tag exists and has no children, so remove it
				ExecuteNonQuery ("DELETE FROM tags WHERE name = 'Other'");
			});

			// Update from version 1 to 2: Restore Other tags that were removed leaving dangling child tags
			AddUpdate (new Version ("2"), delegate () {
				string tag_count = SelectSingleString ("SELECT COUNT(*) FROM tags WHERE category_id != 0 AND category_id NOT IN (SELECT id FROM tags)");

				// If there are no dangling tags, then don't do anything
				if (tag_count == null || System.Int32.Parse (tag_count) == 0)
					return;

 				int id = ExecuteScalar ("INSERT INTO tags (name, category_id, is_category, icon) VALUES ('Other', 0, 1, 'stock_icon:f-spot-other.png')");

				ExecuteNonQuery (String.Format (
					"UPDATE tags SET category_id = {0} WHERE id IN "		+
					"(SELECT id FROM tags WHERE category_id != 0 AND category_id "	+
					"NOT IN (SELECT id FROM tags))",
					id));

				Log.Debug ("Other tag restored.  Sorry about that!");
			});
			
			// Update from version 2 to 3: ensure that Hidden is the only tag left which is a real tag (not category)
			AddUpdate (new Version ("3"), delegate () {
				ExecuteNonQuery ("UPDATE tags SET is_category = 1 WHERE name != 'Hidden'");
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
				ExecuteNonQuery (
					"CREATE TABLE photos (                                     " +
					"	id                 INTEGER PRIMARY KEY NOT NULL,   " +
					"       time               INTEGER NOT NULL,	   	   " +
					"       directory_path     STRING NOT NULL,		   " +
					"       name               STRING NOT NULL,		   " +
					"       description        TEXT NOT NULL,	           " +
					"       roll_id            INTEGER NOT NULL,		   " +
					"       default_version_id INTEGER NOT NULL		   " +
					")");
				ExecuteScalar (String.Format("INSERT INTO photos SELECT id, time, directory_path, name, description, 0, default_version_id FROM {0}", tmp_photos));

				Log.Debug ("Will rename imports to rolls!");
				string tmp_rolls = MoveTableToTemp ("imports");
				ExecuteNonQuery (
					"CREATE TABLE rolls (                                     " +
					"	id                 INTEGER PRIMARY KEY NOT NULL,   " +
					"       time               INTEGER NOT NULL	   	   " +
					")");
				ExecuteScalar (String.Format("INSERT INTO rolls SELECT id, time FROM {0}", tmp_rolls));

				Log.Debug ("Cleaning weird descriptions, fixes bug #324425.");
				ExecuteNonQuery ("UPDATE photos SET description = \"\" WHERE description LIKE \"Invalid size of entry%\"");
			});				


			//Version 6.0, change tag icon f-spot-tag-other to emblem-generic
			AddUpdate (new Version (6,0), delegate () {
				ExecuteScalar ("UPDATE tags SET icon = \"stock_icon:emblem-generic\" " +
						" WHERE icon LIKE \"stock_icon:f-spot-other.png\"");
			});

			//Update to version 7.0, keep photo uri instead of path
			AddUpdate (new Version (7,0), delegate () {
				string tmp_photos = MoveTableToTemp ("photos");
				ExecuteNonQuery ( 
					"CREATE TABLE photos (" +
					"	id                 INTEGER PRIMARY KEY NOT NULL," +
					"       time               INTEGER NOT NULL," +
					"       uri                STRING NOT NULL," +
					"       description        TEXT NOT NULL," +
					"       roll_id            INTEGER NOT NULL," +
					"       default_version_id INTEGER NOT NULL" +
					")");
				ExecuteNonQuery (String.Format (
					"INSERT INTO photos (id, time, uri, description, roll_id, default_version_id)	" +
					"SELECT id, time, 'file://' || directory_path || '/' || name, 		" +
					"description, roll_id, default_version_id FROM {0}", tmp_photos));
			}, true);
			
			// Update to version 8.0, store full version uri
			AddUpdate (new Version (8,0),delegate () {
				string tmp_versions = MoveTableToTemp ("photo_versions");
				ExecuteNonQuery (
					"CREATE TABLE photo_versions (          " +
					"       photo_id        INTEGER,        " +
					"       version_id      INTEGER,        " +
					"       name            STRING,         " +
					"       uri             STRING NOT NULL " +
					")");

				SqliteDataReader reader = ExecuteReader (String.Format (
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

					ExecuteNonQuery (new DbCommand (
						"INSERT INTO photo_versions (photo_id, version_id, name, uri) " +
						"VALUES (:photo_id, :version_id, :name, :uri)",
						"photo_id", Convert.ToUInt32 (reader [0]),
						"version_id", Convert.ToUInt32 (reader [1]),
						"name", (reader [2]).ToString (),
						"uri", uri));
				}

			}, true);
			
			// Update to version 9.0
			AddUpdate (new Version (9,0),delegate () {
				string tmp_versions = MoveTableToTemp ("photo_versions");
				ExecuteNonQuery (
					"CREATE TABLE photo_versions (          " +
					"       photo_id        INTEGER,        " +
					"       version_id      INTEGER,        " +
					"       name            STRING,         " +
					"       uri             STRING NOT NULL," +
					"	protected	BOOLEAN		" +
					")");
				ExecuteNonQuery (String.Format (
					"INSERT INTO photo_versions (photo_id, version_id, name, uri, protected) " +
					"SELECT photo_id, version_id, name, uri, 0 " +
					"FROM {0} ", tmp_versions));
			});

 			// Update to version 10.0, make id autoincrement
 			AddUpdate (new Version (10,0),delegate () {
 				string tmp_photos = MoveTableToTemp ("photos");
 				ExecuteNonQuery (
 					"CREATE TABLE photos (                                     " +
 					"	id                 INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
 					"	time               INTEGER NOT NULL,	   	   " +
 					"	uri		   STRING NOT NULL,		   " +
 					"	description        TEXT NOT NULL,	           " +
 					"	roll_id            INTEGER NOT NULL,		   " +
 					"	default_version_id INTEGER NOT NULL		   " +
 					")");
 
 				ExecuteNonQuery (String.Format (
 					"INSERT INTO photos (id, time, uri, description, roll_id, default_version_id) " +
 					"SELECT id, time, uri, description, roll_id, default_version_id  " + 
 					"FROM  {0} ", tmp_photos));
 			}, false);
 
			// Update to version 11.0, rating
			AddUpdate (new Version (11,0),delegate () {
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
 
 				Execute (String.Format (
 					"INSERT INTO photos (id, time, uri, description, roll_id, default_version_id, rating) " +
 					"SELECT id, time, uri, description, roll_id, default_version_id, null  " + 
 					"FROM  {0} ", tmp_photos));
			});

			//Update to version 12.0, remove dead associations, bgo #507950, #488545
			AddUpdate (new Version (12, 0), delegate () {
				Execute ("DELETE FROM photo_tags WHERE tag_id NOT IN (SELECT id FROM tags)");
			});
			
			// Update to version 13.0
			AddUpdate (new Version (13,0), delegate () {
				Execute ("UPDATE photos SET rating = 0 WHERE rating IS NULL");
			});

			// Update to version 14.0
			AddUpdate (new Version (14,0), delegate () {
				Execute ("UPDATE photos SET rating = 0 WHERE rating IS NULL");
			});

			// Update to version 15.0
			AddUpdate (new Version (15,0), delegate () {
				string tmp_photo_tags = MoveTableToTemp ("photo_tags");
				Execute (
					"CREATE TABLE photo_tags (        " +
					"	photo_id      INTEGER,    " +
					"       tag_id        INTEGER,    " +
					"       UNIQUE (photo_id, tag_id) " +
					")");
				Execute (String.Format (
					"INSERT OR IGNORE INTO photo_tags (photo_id, tag_id) " +
					"SELECT photo_id, tag_id FROM {0}", tmp_photo_tags));
				string tmp_photo_versions = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (		"+
					"	photo_id	INTEGER,	" +
					"	version_id	INTEGER,	" +
					"	name		STRING,		" +
					"	uri		STRING NOT NULL," +
					"	protected	BOOLEAN, 	" +
					"	UNIQUE (photo_id, version_id)	" +
					")");
				Execute (String.Format (
					"INSERT OR IGNORE INTO photo_versions 		" +
					"(photo_id, version_id, name, uri, protected)	" +
					"SELECT photo_id, version_id, name, uri, protected FROM {0}", tmp_photo_versions));
			});

			// Update to version 16.0
			 AddUpdate (new Version (16,0), delegate () {
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

				 Execute (String.Format ("INSERT INTO photo_versions (photo_id, version_id, name, uri, md5_sum, protected) " + 
							 "SELECT photo_id, version_id, name, uri, '', protected " +
							 "FROM   {0} ", 
							 temp_versions_table
							)
				 );

				 JobStore.CreateTable (db.Database);

				 // This is kind of hacky but should be a lot faster on
				 // large photo databases
				 Execute (string.Format ("INSERT INTO jobs (job_type, job_options, run_at, job_priority) " +
							 "SELECT '{0}', id, {1}, {2} " +
							 "FROM   photos ",
							 typeof(Jobs.CalculateHashJob).ToString (),
							 FSpot.Utils.DbUtils.UnixTimeFromDateTime (DateTime.Now),
							 0
							)
				 );
			 }, true);

			// Update to version 16.1
			 AddUpdate (new Version (16,1), delegate () {
				 Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");
			 }, false);

			// Update to version 16.2
			 AddUpdate (new Version (16,2), delegate () {
				 Execute ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");
			 }, false);

			// Update to version 16.3
			AddUpdate (new Version (16,3), delegate () {
				Execute (String.Format ("DELETE FROM jobs WHERE job_type = '{0}'", typeof(Jobs.CalculateHashJob).ToString ()));
			}, false);

			// Update to version 16.4
			AddUpdate (new Version (16,4), delegate () { //fix the tables schema EOL
				string temp_table = MoveTableToTemp ("exports");
			 	Execute (
					"CREATE TABLE exports (\n" +
					"	id			INTEGER PRIMARY KEY NOT NULL, \n" +
					"	image_id		INTEGER NOT NULL, \n" +
					"	image_version_id	INTEGER NOT NULL, \n" +
					"	export_type		TEXT NOT NULL, \n" +
					"	export_token		TEXT NOT NULL\n" +
					")");
				Execute (String.Format (
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
				Execute (String.Format (
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
				Execute (String.Format (
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
				Execute (String.Format (
					"INSERT INTO photos (id, time, uri, description, roll_id, default_version_id, rating, md5_sum) " +
					"SELECT id, time, uri, description, roll_id, default_version_id, rating, md5_sum " +
					"FROM {0}", temp_table));

				temp_table = MoveTableToTemp ("photo_tags");
				Execute(
					"CREATE TABLE photo_tags (\n" +
					"	photo_id	INTEGER, \n" +
					"       tag_id		INTEGER, \n" +
					"       UNIQUE (photo_id, tag_id)\n" +
					")");
				Execute (String.Format (
					"INSERT OR IGNORE INTO photo_tags (photo_id, tag_id) " +
					"SELECT photo_id, tag_id " +
					"FROM {0}", temp_table));

				temp_table = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (\n"+
					"	photo_id	INTEGER, \n" +
					"	version_id	INTEGER, \n" +
					"	name		STRING, \n" +
					"	uri		STRING NOT NULL, \n" +
					"	md5_sum		STRING NOT NULL, \n" +
					"	protected	BOOLEAN, \n" +
					"	UNIQUE (photo_id, version_id)\n" +
					")");
				Execute (String.Format (
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
				Execute (String.Format (
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
				Execute (String.Format (
					"INSERT INTO tags (id, name, category_id, is_category, sort_priority, icon) " +
					"SELECT id, name, category_id, is_category, sort_priority, icon " +
					"FROM {0}", temp_table));
			});

			// Update to version 16.5
			AddUpdate (new Version (16,5), delegate () { //fix md5 null in photos and photo_versions table
				string temp_table = MoveTableToTemp ("photo_versions");
				Execute (
					"CREATE TABLE photo_versions (\n"+
					"	photo_id	INTEGER, \n" +
					"	version_id	INTEGER, \n" +
					"	name		STRING, \n" +
					"	uri		STRING NOT NULL, \n" +
					"	md5_sum		TEXT NULL, \n" +
					"	protected	BOOLEAN, \n" +
					"	UNIQUE (photo_id, version_id)\n" +
					")");
				Execute (String.Format (
					"INSERT OR IGNORE INTO photo_versions (photo_id, version_id, name, uri, md5_sum, protected) " +
					"SELECT photo_id, version_id, name, uri, md5_sum, protected " +
					"FROM {0}", temp_table));

				Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");

				Execute ("UPDATE photos SET md5_sum = NULL WHERE md5_sum = ''");
				Execute ("UPDATE photo_versions SET md5_sum = NULL WHERE md5_sum = ''");
			});

			// Update to version 17.0
			//AddUpdate (new Version (14,0), delegate () {
			//	do update here
			//});
			
			// Update to version 17.0, split uri and filename
			AddUpdate (new Version (17,0),delegate () {
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
					"CREATE TABLE photo_versions (\n"+
					"	photo_id	INTEGER, \n" +
					"	version_id	INTEGER, \n" +
					"	name		STRING, \n" +
					"	base_uri		STRING NOT NULL, \n" +
					"	filename		STRING NOT NULL, \n" +
					"	md5_sum		TEXT NULL, \n" +
					"	protected	BOOLEAN, \n" +
					"	UNIQUE (photo_id, version_id)\n" +
					")");
				
				SqliteDataReader reader = ExecuteReader (String.Format (
					"SELECT id, time, uri, description, roll_id, default_version_id, rating, md5_sum " +
					"FROM {0} ", tmp_photos));
		
				while (reader.Read ()) {
					System.Uri photo_uri = new System.Uri (reader ["uri"] as string);
					
					string filename = photo_uri.GetFilename ();
					Uri base_uri = photo_uri.GetDirectoryUri ();

					string md5 = reader["md5_sum"] != null ? reader ["md5_sum"].ToString () : null;

					Execute (new DbCommand (
						"INSERT INTO photos (id, time, base_uri, filename, description, roll_id, default_version_id, rating, md5_sum) "	+
						"VALUES (:id, :time, :base_uri, :filename, :description, :roll_id, :default_version_id, :rating, :md5_sum)",
						"id", Convert.ToUInt32 (reader ["id"]),
						"time", reader ["time"],
						"base_uri", base_uri.ToString (),
						"filename", filename,
						"description", reader["description"].ToString (),
						"roll_id", Convert.ToUInt32 (reader ["roll_id"]),
						"default_version_id", Convert.ToUInt32 (reader ["default_version_id"]),
						"rating", Convert.ToUInt32 (reader ["rating"]),
						"md5_sum", String.IsNullOrEmpty (md5) ? null : md5));
				}
				
				reader.Close ();
				
				reader = ExecuteReader (String.Format (
						"SELECT photo_id, version_id, name, uri, md5_sum, protected " +
						"FROM {0} ", tmp_versions));
				
				while (reader.Read ()) {
					System.Uri photo_uri = new System.Uri (reader ["uri"] as string);
					
					string filename = photo_uri.GetFilename ();
					Uri base_uri = photo_uri.GetDirectoryUri ();

					string md5 = reader["md5_sum"] != null ? reader ["md5_sum"].ToString () : null;
					
					Execute (new DbCommand (				
						"INSERT INTO photo_versions (photo_id, version_id, name, base_uri, filename, protected, md5_sum) " +
						"VALUES (:photo_id, :version_id, :name, :base_uri, :filename, :is_protected, :md5_sum)",
						"photo_id", Convert.ToUInt32 (reader ["photo_id"]),
						"version_id", Convert.ToUInt32 (reader ["version_id"]),
						"name", reader["name"].ToString (),
						"base_uri", base_uri.ToString (),
						"filename", filename,
						"is_protected", Convert.ToBoolean (reader["protected"]),
						"md5_sum", String.IsNullOrEmpty (md5) ? null : md5));
				}
				
				Execute ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");
				Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");
				

			}, true);

			// Update to version 17.1, Rename 'Import Tags' to 'Imported Tags'
			AddUpdate (new Version (17,1),delegate () {	
				Execute ("UPDATE tags SET name = 'Imported Tags' WHERE name = 'Import Tags'");
			});
			
		}

		public static void Run (Db database)
		{
			db = database;
			db_version = db.Meta.DatabaseVersion;

			if (updates.Count == 0)
				return;

			Version current_version = new Version (db_version.Value);

			if (current_version == LatestVersion)
				return;
			else if (current_version > LatestVersion) {
				Log.Information ("The existing database version is more recent than this version of F-Spot expects.");
				return;
			}

			uint timer = Log.InformationTimerStart ("Updating F-Spot Database");

			// Only create and show the dialog if one or more of the updates to be done is
			// marked as being slow
			bool slow = false;
			foreach (Version version in updates.Keys) {
				if (version > current_version && (updates[version] as Update).IsSlow)
					slow = true;
					break;
			}

			if (slow) {
				dialog = new ProgressDialog (Catalog.GetString ("Updating F-Spot Database"), ProgressDialog.CancelButtonType.None, 0, null);
				dialog.Message.Text = Catalog.GetString ("Please wait while your F-Spot gallery's database is updated. This may take some time.");
				dialog.Bar.Fraction = 0.0;
				dialog.Modal = false;
				dialog.SkipTaskbarHint = true;
				dialog.WindowPosition = WindowPosition.Center;
				dialog.ShowAll ();
				dialog.Present ();
				dialog.QueueDraw ();
			}

			db.BeginTransaction ();
			try {
				ArrayList keys = new ArrayList (updates.Keys);
				keys.Sort ();
				foreach (Version version in keys) {
					if (version <= current_version)
						continue;

					Pulse ();
					(updates[version] as Update).Execute (db, db_version);
				}

				db.CommitTransaction ();
			} catch (Exception e) {Log.DebugException (e);
				Log.Warning ("Rolling back database changes because of Exception");
				// There was an error, roll back the database
				db.RollbackTransaction ();

				// Pass the exception on, this is fatal
				throw e;
			}
			
			if (dialog != null)
				dialog.Destroy ();
			
			if (new Version(db_version.Value) == LatestVersion)
				Log.InformationTimerPrint (timer, "Database updates completed successfully (in {0}).");
		}
		
		private static void AddUpdate (Version version, UpdateCode code)
		{
			AddUpdate (version, code, false);
		}
		
		private static void AddUpdate (Version version, UpdateCode code, bool is_slow)
		{
			updates[version] = new Update (version, code, is_slow);
		}

		public static void Pulse ()
		{
			if (dialog != null) {
				dialog.Bar.Pulse ();
				dialog.ShowAll ();
			}
		}

		private static int Execute (string statement)
		{
			return db.Database.Execute (statement);
		}

		private static int Execute (DbCommand command)
		{
			return db.Database.Execute (command);
		}

		private static void ExecuteNonQuery (string statement)
		{
			db.Database.ExecuteNonQuery(statement);
		}

		private static void ExecuteNonQuery (DbCommand command)
		{
			db.Database.ExecuteNonQuery(command);
		}
		
		private static int ExecuteScalar (string statement)
		{
			return db.Database.Execute(statement);
		}

		private static SqliteDataReader ExecuteReader (string statement)
		{
			return db.Database.Query (statement);
		}
		
		private static bool TableExists (string table)
		{
			return db.Database.TableExists (table);
		}

		private static string SelectSingleString (string statement)
		{
			string result = null;

			try {
				result = (string)db.Database.QuerySingle(statement);
			} catch (Exception) {}

			return result;
		}

		private static string MoveTableToTemp (string table_name)
		{
			string temp_name = table_name + "_temp";
			
			// Get the table definition for the table we are copying
			string sql = SelectSingleString (String.Format ("SELECT sql FROM sqlite_master WHERE tbl_name = '{0}' AND type = 'table' ORDER BY type DESC", table_name));
			
			// Drop temp table if already exists
			ExecuteNonQuery ("DROP TABLE IF EXISTS " + temp_name);

			// Change the SQL to create the temp table
			ExecuteNonQuery (sql.Replace ("CREATE TABLE " + table_name, "CREATE TEMPORARY TABLE " + temp_name));

			// Copy the data
			ExecuteScalar (String.Format ("INSERT INTO {0} SELECT * FROM {1}", temp_name, table_name));
				
			// Delete the original table
			ExecuteNonQuery ("DROP TABLE " + table_name);

			return temp_name;
		}

		private delegate void UpdateCode ();

		private class Update {
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

			public void Execute (Db db, MetaItem db_version)
			{
				code ();
				
				Log.Debug ("Updated database from version {0} to {1}",
						db_version.Value,
						Version.ToString ());


				db_version.Value = Version.ToString ();
				db.Meta.Commit (db_version);
			}
		}

		public class Version : IComparable {
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
				}
				catch (Exception) {
					this.maj = 0;
				}
				try {
					this.min = Convert.ToInt32 (parts [1]);
				}
				catch (Exception) {
					this.min = 0;
				}
			}

			//IComparable
			public int CompareTo (object obj) {
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
				return Compare (v1,v2) < 0;
			}

			public static bool operator > (Version v1, Version v2)
			{
				return Compare (v1,v2) > 0;
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
