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
            string path = photo.DefaultVersion.Uri.LocalPath;

            Tag [] tags = photo.Tags;
            string [] names = new string [tags.Length];

            for (int i = 0; i < tags.Length; i++)
                names [i] = tags [i].Name;

            var res = new GIOTagLibFileAbstraction () { Uri = photo.DefaultVersion.Uri };
            using (var metadata = TagLib.File.Create (res) as TagLib.Image.File) {
                metadata.GetTag (TagLib.TagTypes.XMP, true);

                var tag = metadata.ImageTag;
                tag.DateTime = photo.Time;
                tag.Comment = photo.Description ?? String.Empty;
                tag.Keywords = names;
                tag.Rating = photo.Rating;
                tag.Software = FSpot.Defines.PACKAGE + " version " + FSpot.Defines.VERSION;

                Hyena.Log.Information (photo.DefaultVersion.Uri);
                if (Preferences.Get<bool> (Preferences.METADATA_ALWAYS_USE_SIDECAR) || !metadata.Writeable) {
                    var sidecar_res = new GIOTagLibFileAbstraction () { Uri = photo.DefaultVersion.Uri.ReplaceExtension (".xmp") };

                    metadata.SaveXmpSidecar (sidecar_res);
                } else {
                    metadata.Save ();
                }
            }
        }
    }
}
