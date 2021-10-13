// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Thomas Van Machelen
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using FSpot.Database;

//using Banshee.Kernel;

using Hyena;

namespace FSpot.Jobs
{
	public class CalculateHashJob : Job
	{
		IDb db;

		public CalculateHashJob (IDb db, JobData jobData) : base (jobData)
		{
			this.db = db;
		}

		public static CalculateHashJob Create (JobStore jobStore, Guid photoId)
		{
			return (CalculateHashJob)jobStore.CreatePersistent (JobName, photoId.ToString ());
		}

		public static string JobName => "CalculateHash";

		protected override bool Execute ()
		{
			//this will add some more reactivity to the system
			System.Threading.Thread.Sleep (200);

			var photoId = Guid.Parse (JobOptions);
			Log.Debug ($"Calculating Hash {photoId}...");

			try {
				var photo = db.Photos.Get (photoId);
				db.Photos.CalculateMD5Sum (photo);
				return true;
			} catch (Exception e) {
				Log.Debug ($"Error Calculating Hash for photo {JobOptions}: {e.Message}");
			}

			return false;
		}
	}
}
