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
using FSpot.Utils;
using Hyena;

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
			Log.Debug ("Syncing metadata to file...");
			try {
				Photo photo = FSpot.App.Instance.Database.Photos.Get (Convert.ToUInt32 (JobOptions)) as Photo;
				WriteMetadataToImage (photo);
				return true;
			} catch (System.Exception e) {
				Log.ErrorFormat ("Error syncing metadata to file\n{0}", e);
			}
			return false;
		}

		//FIXME: Won't work on non-file uris
		void WriteMetadataToImage (Photo photo)
		{
			string path = photo.DefaultVersion.Uri.LocalPath;
	
			using (FSpot.ImageFile img = FSpot.ImageFile.Create (photo.DefaultVersion.Uri)) {
				if (img is FSpot.JpegFile) {
					FSpot.JpegFile jimg = img as FSpot.JpegFile;
				
					jimg.SetDescription (photo.Description);
					jimg.SetDateTimeOriginal (photo.Time);
					jimg.SetXmp (UpdateXmp (photo, jimg.Header.GetXmp ()));
	
					jimg.SaveMetaData (path);
				} else if (img is FSpot.Png.PngFile) {
					FSpot.Png.PngFile png = img as FSpot.Png.PngFile;
				
					if (img.Description != photo.Description)
						png.SetDescription (photo.Description);
				
					png.SetXmp (UpdateXmp (photo, png.GetXmp ()));
	
					png.Save (path);
				}
			}
		}
		
		private static FSpot.Xmp.XmpFile UpdateXmp (FSpot.IBrowsableItem item, FSpot.Xmp.XmpFile xmp)
		{
			if (xmp == null) 
				xmp = new FSpot.Xmp.XmpFile ();
	
			Tag [] tags = item.Tags;
			string [] names = new string [tags.Length];
			
			for (int i = 0; i < tags.Length; i++)
				names [i] = tags [i].Name;
			
			xmp.Store.Update ("dc:subject", "rdf:Bag", names);
			if ((item as Photo).Rating > 0) {
				xmp.Store.Update ("xmp:Rating", (item as Photo).Rating.ToString());
				// FIXME - Should we also store/overwrite the Urgency field?
				// uint urgency_value = (item as Photo).Rating + 1; // Urgency valid values 1 - 8
				// xmp.Store.Update ("photoshop:Urgency", urgency_value.ToString());
			} else {
				xmp.Store.Delete ("xmp:Rating");
			}
			xmp.Dump ();
	
			return xmp;
		}
	}
}
