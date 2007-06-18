/*
 * Jobs/SyncMetadataJob.cs
 *
 * Author(s)
 *   Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Banshee.Kernel;

namespace FSpot.Jobs {
	public class SyncMetadataJob : Job
	{
		public SyncMetadataJob (uint id, string job_options, int run_at, JobPriority job_priority, bool persistent) : this (id, job_options, DbUtils.DateTimeFromUnixTime (run_at), job_priority, persistent)
		{
		}

		public SyncMetadataJob (uint id, string job_options, DateTime run_at, JobPriority job_priority, bool persistent) : base (id, job_options, job_priority, run_at, persistent)
		{
		}

		//Use THIS static method to create a job...
		public static SyncMetadataJob Create (JobStore job_store, Photo photo)
		{
			return (SyncMetadataJob) job_store.CreatePersistent (typeof (FSpot.Jobs.SyncMetadataJob), photo.Id.ToString ());
		}

		protected override bool Execute ()
		{
			//this will add some more reactivity to the system
			System.Threading.Thread.Sleep (500);
			Console.WriteLine ("Syncing metadata to file...");
			try {
				Photo photo = FSpot.Core.Database.Photos.Get (Convert.ToUInt32 (JobOptions)) as Photo;
				photo.WriteMetadataToImage ();
				return true;
			} catch (System.Exception e) {
				Console.WriteLine ("Error syncing metadata to file\n{0}", e);
			}
			return false;
		}
	}
}
