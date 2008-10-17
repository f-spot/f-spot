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
using FSpot.UI.Dialog;
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

		public void ShowDialog ()
		{ 			
			VBox.Spacing = 6;
			Label l = new Label ("In order to detect duplicates on pictures you imported before f-spot 0.5.0, f-spot need to analyze your image collection. This is is not done by default as it's time consuming. You can Start or Pause this update process using this dialog."); 
			l.LineWrap = true;
			VBox.PackStart (l);

			Button execute = new Button (Stock.Execute);
			execute.Clicked += HandleExecuteClicked;
			VBox.PackStart (execute);

			Button stop = new Button (Stock.Stop);
			stop.Clicked += HandleStopClicked;
			VBox.PackStart (stop);

			this.AddButton ("_Close", ResponseType.Close);
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
			SqliteDataReader reader = FSpot.Core.Database.Database.Query ("SELECT id from photos WHERE md5_sum IS NULL");
			FSpot.Core.Database.Database.BeginTransaction ();
			while (reader.Read ())
				FSpot.Jobs.CalculateHashJob.Create (FSpot.Core.Database.Jobs, Convert.ToUInt32 (reader[0]));
			reader.Close ();
			FSpot.Core.Database.Database.CommitTransaction ();
		}

		void HandleStopClicked (object o, EventArgs e)
		{
			FSpot.Core.Database.Database.ExecuteNonQuery (String.Format ("DELETE FROM jobs WHERE job_type = '{0}'", typeof(FSpot.Jobs.CalculateHashJob).ToString ()));
		}
	}

}
