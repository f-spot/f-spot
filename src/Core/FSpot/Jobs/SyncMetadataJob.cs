//
// SyncMetadataJob.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Database;
using FSpot.Models;
using FSpot.Settings;
using FSpot.Utils;

namespace FSpot.Jobs
{
	public class SyncMetadataJob : Job
	{
		readonly IDb db;

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
				Photo photo = db.Photos.Get (Guid.Parse (JobOptions));
				if (photo == null)
					return false;

				Logger.Log.Debug ($"Syncing metadata to file ({photo.DefaultVersion.Uri})...");

				WriteMetadataToImage (photo);
				return true;
			} catch (Exception e) {
				Logger.Log.Error (e, $"Error syncing metadata to file");
			}
			return false;
		}

		void WriteMetadataToImage (Photo photo)
		{
			var tags = photo.Tags;
			var names = new List<string> (tags.Count);

			foreach (var t in tags)
				names.Add (t.Name);

			using var metadata = MetadataUtils.Parse (photo.DefaultVersion.Uri);

			metadata.EnsureAvailableTags ();

			var tag = metadata.ImageTag;
			tag.DateTime = photo.UtcTime;
			tag.Comment = photo.Description ?? string.Empty;
			tag.Keywords = names.ToArray ();
			tag.Rating = (uint)photo.Rating;
			tag.Software = FSpotConfiguration.Package + " version " + FSpotConfiguration.Version;

			var always_sidecar = Preferences.Get<bool> (Preferences.MetadataAlwaysUseSidecar);
			metadata.SaveSafely (photo.DefaultVersion.Uri, always_sidecar);
		}
	}
}
