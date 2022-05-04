//
// CalculateHashJob.cs
//
// Author:
//   Thomas Van Machelen <thomasvm@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Thomas Van Machelen
// Copyright (C) 2008, 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Database.Jobs
{
	public class CalculateHashJob : Job
	{
		public CalculateHashJob (IDb db, JobData jobData) : base (db, jobData)
		{
		}

		public static CalculateHashJob Create (JobStore job_store, uint photo_id)
		{
			return (CalculateHashJob)job_store.CreatePersistent (JobName, photo_id.ToString ());
		}

		public static string JobName => "CalculateHash";

		protected override bool Execute ()
		{
			//this will add some more reactivity to the system
			System.Threading.Thread.Sleep (200);

			uint photo_id = Convert.ToUInt32 (JobOptions);
			Logger.Log.Debug ($"Calculating Hash {photo_id}...");

			try {
				var photo = Db.Photos.Get (Convert.ToUInt32 (photo_id));
				Db.Photos.CalculateMD5Sum (photo);
				return true;
			} catch (Exception e) {
				Logger.Log.Debug ($"Error Calculating Hash for photo {JobOptions}: {e.Message}");
			}

			return false;
		}
	}
}
