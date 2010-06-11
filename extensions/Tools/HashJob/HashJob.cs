/*
 * HashJob.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.Collections.Generic;
using Mono.Unix;
using Mono.Data.SqliteClient;

using Gtk;

using FSpot;
using FSpot.Extensions;
using FSpot.Jobs;

namespace HashJobExtension {

	public class HashJob : ICommand {
		public void Run (object o, EventArgs e) {
			HashJobDialog dialog = new HashJobDialog ();  
			dialog.ShowDialog ();

		}
	}

	public class HashJobDialog : Dialog 
	{
		private Gtk.Label status_label;

		public void ShowDialog ()
		{ 			
			// This query is not very fast, but it's a 'one-time' so don't care much...
			SqliteDataReader reader = FSpot.App.Instance.Database.Database.Query (
				"SELECT COUNT(*) FROM photos p WHERE EXISTS " +
					"(SELECT * FROM photo_versions pv WHERE p.id=pv.photo_id AND " +
					"(pv.import_md5 IS NULL OR pv.import_md5 = ''))");
			reader.Read ();
			uint missing_md5 = Convert.ToUInt32 (reader[0]);
			reader.Close ();

			reader = FSpot.App.Instance.Database.Database.Query (String.Format (
				"SELECT COUNT(*) FROM jobs WHERE job_type = '{0}' ", typeof(FSpot.Jobs.CalculateHashJob).ToString ()));
			reader.Read ();
			uint active_jobs = Convert.ToUInt32 (reader[0]);
			reader.Close ();

			VBox.Spacing = 6;
			Label l = new Label (Catalog.GetString ("In order to detect duplicates on pictures you imported before 0.5.0, " +
					"F-Spot needs to analyze your image collection. This is not done by default as it's time consuming. " +
					"You can Start or Pause this update process using this dialog."));
			l.LineWrap = true;
			VBox.PackStart (l);

			Label l2 = new Label (String.Format (Catalog.GetString ("You currently have {0} photos needing md5 calculation, and {1} pending jobs"),
				missing_md5, active_jobs));
			l2.LineWrap = true;
			VBox.PackStart (l2);

			Button execute = new Button (Stock.Execute);
			execute.Clicked += HandleExecuteClicked;
			VBox.PackStart (execute);

			Button stop = new Button (Stock.Stop);
			stop.Clicked += HandleStopClicked;
			VBox.PackStart (stop);

			status_label = new Label ();
			VBox.PackStart (status_label);

			this.AddButton (Catalog.GetString ("_Close"), ResponseType.Close);
			this.Response += HandleResponse;

			ShowAll ();
		}

		void HandleResponse (object obj, ResponseArgs args)
	        {
			switch(args.ResponseId)
			{
				case ResponseType.Close:
					this.Destroy ();
					break;
			}
	        }

		void HandleExecuteClicked (object o, EventArgs e)
		{
			SqliteDataReader reader = FSpot.App.Instance.Database.Database.Query (
				"SELECT id FROM photos p WHERE EXISTS " +
					"(SELECT * FROM photo_versions pv WHERE p.id=pv.photo_id AND " +
					"(pv.import_md5 IS NULL OR pv.import_md5 = '') )");
			FSpot.App.Instance.Database.Database.BeginTransaction ();
			while (reader.Read ())
				FSpot.Jobs.CalculateHashJob.Create (FSpot.App.Instance.Database.Jobs, Convert.ToUInt32 (reader[0]));
			reader.Close ();
			FSpot.App.Instance.Database.Database.CommitTransaction ();
			status_label.Text = Catalog.GetString ("Processing images...");
		}

		void HandleStopClicked (object o, EventArgs e)
		{
			FSpot.App.Instance.Database.Database.ExecuteNonQuery (String.Format ("DELETE FROM jobs WHERE job_type = '{0}'", typeof(FSpot.Jobs.CalculateHashJob).ToString ()));
			status_label.Text = Catalog.GetString ("Stopped");
		}

	}

}
