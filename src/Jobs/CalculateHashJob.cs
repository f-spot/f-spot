/*
 * Jobs/CalculateHashJob.cs
 *
 * Author(s)
 *   Thomas Van Machelen <thomas.vanmachelen@gmail.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Banshee.Kernel;
using FSpot.Utils;
using Hyena;

namespace FSpot.Jobs {
	public class CalculateHashJob : Job
	{
		public CalculateHashJob (uint id, string job_options, int run_at, JobPriority job_priority, bool persistent) 
			: this (id, job_options, DbUtils.DateTimeFromUnixTime (run_at), job_priority, persistent)
		{
		}

		public CalculateHashJob (uint id, string job_options, DateTime run_at, JobPriority job_priority, bool persistent) 
			: base (id, job_options, job_priority, run_at, persistent)
		{
		}

		public static CalculateHashJob Create (JobStore job_store, uint photo_id)
		{
			return (CalculateHashJob) job_store.CreatePersistent (typeof(FSpot.Jobs.CalculateHashJob), photo_id.ToString ()); 
		}

		protected override bool Execute ()
		{
			//this will add some more reactivity to the system
			System.Threading.Thread.Sleep (200);

			uint photo_id = Convert.ToUInt32 (JobOptions);
			Log.DebugFormat ("Calculating Hash {0}...", photo_id);

			try {
				Photo photo = FSpot.App.Instance.Database.Photos.Get (Convert.ToUInt32 (photo_id)) as Photo;
				FSpot.App.Instance.Database.Photos.CalculateMD5Sum (photo);
				return true;
			} catch (System.Exception e) {
				Log.DebugFormat ("Error Calculating Hash for photo {0}: {1}", JobOptions, e.Message);
			}
			return false;
		}
	} 
}

