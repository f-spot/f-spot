using Mono.Data.SqliteClient;
using Mono.Posix;
using Gtk;
using System;
using System.Collections;

namespace FSpot.Database {
	public static class Updater {
		private static ProgressDialog dialog;
		private static ArrayList updates = new ArrayList ();
		private static MetaItem db_version;
		private static Db db;

		public static int LatestVersion {
			get { return updates.Count; }
		}
		
		static Updater () {
			// The order these are added is important as they will be run sequentially
			
			// Update from version 0 to 1: Remove empty Other tags
			AddUpdate (delegate (Db db) {
				string other_id = SelectSingleString ("SELECT id FROM tags WHERE name = 'Other'");

				if (hidden_id == null)
					return;

				string photo_count = SelectSingleString (
					String.Format ("SELECT COUNT(*) FROM photo_tags  WHERE tag_id = {0}", other_id));

				if (photo_count == null)
					return;
				
				// Check for tags with the Other parent
				string child_count = SelectSingleString (
					String.Format ("SELECT COUNT(*) FROM tags WHERE category_id = {0}", other_id));

				if (child_count == null)
					return;

				int count = System.Int32.Parse (photo_count);
				if (count != 0)
					return;

				// Finally, we know that the Other tag exists and has no children, so remove it
				ExecuteNonQuery ("DELETE FROM tags WHERE name = 'Other'");
			});
			
			// Update from version 1 to 2
			//AddUpdate (delegate (Db db) {
			//	do update here
			//});
		}

		public static void Run (Db database)
		{
			db = database;
			db_version = db.Meta.DatabaseVersion;

			if (updates.Count == 0)
				return;

			int current_version = db_version.ValueAsInt;

			if (current_version == (updates [updates.Count - 1] as Update).Version)
				return;
			else if (current_version > (updates [updates.Count - 1] as Update).Version) {
				Console.WriteLine ("The existing database version is more recent than this version of F-Spot expects.");
				return;
			}

			Console.WriteLine ("Updating F-Spot Database");

			// Only create and show the dialog if one or more of the updates to be done is
			// marked as being slow
			bool slow = false;
			foreach (Update update in updates) {
				if (current_version < update.Version && update.IsSlow) {
					slow = true;
					break;
				}
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
				foreach (Update update in updates) {
					if (current_version >= update.Version)
						continue;

					Pulse ();
					update.Execute (db, db_version);
				}

				db.CommitTransaction ();
			} catch (Exception e) {
				Console.WriteLine ("Rolling back database changes because of Exception");
				// There was an error, roll back the database
				db.RollbackTransaction ();

				// Pass the exception on, this is fatal
				throw e;
			}
			
			if (dialog != null)
				dialog.Destroy ();
			
			if (db_version.ValueAsInt == (updates [updates.Count - 1] as Update).Version)
				Console.WriteLine ("Database updates completed successfully.");
		}
		
		private static int version_counter = 1;
		private static void AddUpdate (UpdateCode code)
		{
			AddUpdate (code, false);
		}
		
		private static void AddUpdate (UpdateCode code, bool is_slow)
		{
			updates.Add (new Update (version_counter++, code, is_slow));
		}

		public static void Pulse ()
		{
			if (dialog != null) {
				dialog.Bar.Pulse ();
				dialog.ShowAll ();
			}
		}

		private static void ExecuteNonQuery (string statement)
		{
			SqliteCommand command = new SqliteCommand ();
			command.Connection = db.Connection;

			command.CommandText = statement;

			command.ExecuteNonQuery ();
			command.Dispose ();
		}
		
		private static void ExecuteScalar (string statement)
		{
			SqliteCommand command = new SqliteCommand ();
			command.Connection = db.Connection;

			command.CommandText = statement;

			command.ExecuteScalar ();
			command.Dispose ();
		}
		
		private static string SelectSingleString (string statement)
		{
			string result = null;

			try {
				SqliteCommand command = new SqliteCommand ();
				command.Connection = db.Connection;

				command.CommandText = statement;

				SqliteDataReader reader = command.ExecuteReader ();
				
				if (reader.Read ())
					result = reader [0].ToString ();

				reader.Close ();
				command.Dispose ();
			} catch (Exception e) {}

			return result;
		}

		private static string MoveTableToTemp (string table_name)
		{
			string temp_name = table_name + "_temp";
			
			// Get the table definition for the table we are copying
			string sql = SelectSingleString (String.Format ("SELECT sql FROM sqlite_master WHERE tbl_name = '{0}' ORDER BY type DESC", table_name));

			// Change the SQL to create the temp table
			ExecuteNonQuery (sql.Replace ("CREATE TABLE " + table_name, "CREATE TEMPORARY TABLE " + temp_name));

			// Copy the data
			ExecuteScalar (String.Format ("INSERT INTO {0} SELECT * FROM {1}", temp_name, table_name));
				
			// Delete the original table
			ExecuteNonQuery ("DROP TABLE " + table_name);

			return temp_name;
		}

		private delegate void UpdateCode (Db db);

		private class Update {
			public int Version;
			private UpdateCode code;
			public bool IsSlow = false;
			
			public Update (int to_version, UpdateCode code, bool slow)
			{
				this.Version = to_version;
				this.code = code;
				IsSlow = slow;
			}

			public Update (int to_version, UpdateCode code)
			{
				this.Version = to_version;
				this.code = code;
			}

			public void Execute (Db db, MetaItem db_version)
			{
				code (db);
				
				Console.WriteLine ("Updated database from version {0} to {1}",
						db_version.ValueAsInt,
						Version);


				db_version.ValueAsInt++;
				db.Meta.Commit (db_version);
			}
		}
	}
}
