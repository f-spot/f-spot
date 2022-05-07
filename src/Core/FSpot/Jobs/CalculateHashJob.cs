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

using FSpot.Database;

namespace FSpot.Jobs
{
	public class CalculateHashJob : Job
	{
		readonly IDb db;

		public CalculateHashJob (IDb db, JobData jobData) : base (jobData)
		{
			this.db = db;
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

			var photo_id = Guid.Parse (JobOptions);
			Logger.Log.Debug ($"Calculating Hash {photo_id}...");

			try {
				var photo = db.Photos.Get (photo_id);
				db.Photos.CalculateMD5Sum (photo);
				return true;
			} catch (Exception e) {
				Logger.Log.Debug ($"Error Calculating Hash for photo {JobOptions}: {e.Message}");
			}

			return false;
		}
	}
}
