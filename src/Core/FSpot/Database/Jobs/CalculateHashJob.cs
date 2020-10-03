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

using Hyena;

namespace FSpot.Database.Jobs
{
	public class CalculateHashJob : Job
	{
		public CalculateHashJob (IDb db, JobData jobData)
			: base (db, jobData)
		{
		}

		public static CalculateHashJob Create (JobStore jobStore, uint photoId)
		{
			return (CalculateHashJob)jobStore.CreatePersistent (JobName, photoId.ToString ());
		}

		public static string JobName => "CalculateHash";

		protected override bool Execute ()
		{
			//this will add some more reactivity to the system
			System.Threading.Thread.Sleep (200);

			uint photoId = Convert.ToUInt32 (JobOptions);
			Log.Debug ($"Calculating Hash {photoId}...");

			try {
				Photo photo = Db.Photos.Get (Convert.ToUInt32 (photoId));
				Db.Photos.CalculateMD5Sum (photo);
				return true;
			} catch (Exception e) {
				Log.Debug ($"Error Calculating Hash for photo {JobOptions}: {e.Message}");
			}
			return false;
		}
	}
}
