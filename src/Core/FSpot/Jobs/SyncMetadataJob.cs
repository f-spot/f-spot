// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2007-2008 Stephane Delcroix
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Database;
using FSpot.Models;
using FSpot.Settings;
using FSpot.Utils;

using Hyena;

namespace FSpot.Jobs
{
	public class SyncMetadataJob : Job
	{
		IDb db;

		public SyncMetadataJob (IDb db, JobData jobData) : base (jobData)
		{
			this.db = db;
		}

		//Use THIS static method to create a job...
		public static SyncMetadataJob Create (JobStore jobStore, Photo photo)
		{
			return (SyncMetadataJob)jobStore.CreatePersistent (JobName, photo.Id.ToString ());
		}

		public static string JobName => "SyncMetadata";

		protected override bool Execute ()
		{
			// FIXME, Task.Delay?
			//this will add some more reactivity to the system
			System.Threading.Thread.Sleep (500);

			try {
				var photo = db.Photos.Get (Guid.Parse(JobOptions));
				if (photo == null)
					return false;

				Log.Debug ($"Syncing metadata to file ({photo.DefaultVersion.Uri})...");

				WriteMetadataToImage (photo);
				return true;
			} catch (Exception e) {
				Log.Error ($"Error syncing metadata to file\n{e}");
			}
			return false;
		}

		void WriteMetadataToImage (Photo photo)
		{
			var tags = photo.Tags;
			string[] names = new string[tags.Count];

			for (int i = 0; i < tags.Count; i++)
				names[i] = tags[i].Name;

			using var metadata = MetadataUtils.Parse (photo.DefaultVersion.Uri);

			metadata.EnsureAvailableTags ();

			var tag = metadata.ImageTag;
			tag.DateTime = photo.UtcTime;
			tag.Comment = photo.Description ?? string.Empty;
			tag.Keywords = names;
			tag.Rating = (uint)photo.Rating;
			tag.Software = $"{FSpotConfiguration.Package} version {FSpotConfiguration.Version}";

			var alwaysSidecar = Preferences.Get<bool> (Preferences.MetadataAlwaysUseSidecar);
			metadata.SaveSafely (photo.DefaultVersion.Uri, alwaysSidecar);
		}
	}
}
