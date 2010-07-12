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
using Hyena;
using FSpot.Utils;
using Mono.Unix;

namespace FSpot.Jobs {
    public class SyncMetadataJob : Job
    {
        public SyncMetadataJob (uint id, string job_options, int run_at, JobPriority job_priority, bool persistent) : this (id, job_options, DateTimeUtil.ToDateTime (run_at), job_priority, persistent)
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

        void WriteMetadataToImage (Photo photo)
        {
            Tag [] tags = photo.Tags;
            string [] names = new string [tags.Length];

            for (int i = 0; i < tags.Length; i++)
                names [i] = tags [i].Name;

            using (var metadata = Metadata.Parse (photo.DefaultVersion.Uri)) {
                metadata.EnsureAvailableTags ();

                var tag = metadata.ImageTag;
                tag.DateTime = photo.Time;
                tag.Comment = photo.Description ?? String.Empty;
                tag.Keywords = names;
                tag.Rating = photo.Rating;
                tag.Software = FSpot.Defines.PACKAGE + " version " + FSpot.Defines.VERSION;

                var always_sidecar = Preferences.Get<bool> (Preferences.METADATA_ALWAYS_USE_SIDECAR);
                if (always_sidecar || !metadata.Writeable || metadata.PossiblyCorrupt) {
                    if (!always_sidecar && metadata.PossiblyCorrupt) {
                        Log.WarningFormat (Catalog.GetString ("Metadata of file {0} may be corrupt, refusing to write to it, falling back to XMP sidecar."), photo.DefaultVersion.Uri);
                    }

                    var sidecar_res = new GIOTagLibFileAbstraction () { Uri = photo.DefaultVersion.Uri.ReplaceExtension (".xmp") };

                    metadata.SaveXmpSidecar (sidecar_res);
                } else {
                    metadata.Save ();
                }
            }
        }
    }
}
