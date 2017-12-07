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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Banshee.Kernel;

using Hyena;

using FSpot.Core;
using FSpot.Database;
using FSpot.Settings;
using FSpot.Utils;

namespace FSpot.Database.Jobs {
    public class SyncMetadataJob : Job
    {
        public SyncMetadataJob (IDb db, uint id, string job_options, int run_at, JobPriority job_priority, bool persistent) : this (db, id, job_options, DateTimeUtil.ToDateTime (run_at), job_priority, persistent)
        {
        }

        public SyncMetadataJob (IDb db, uint id, string job_options, DateTime run_at, JobPriority job_priority, bool persistent) : base (db, id, job_options, job_priority, run_at, persistent)
        {
        }

        //Use THIS static method to create a job...
        public static SyncMetadataJob Create (JobStore job_store, Photo photo)
        {
            return (SyncMetadataJob) job_store.CreatePersistent (typeof (SyncMetadataJob), photo.Id.ToString ());
        }

        protected override bool Execute ()
        {
            //this will add some more reactivity to the system
            System.Threading.Thread.Sleep (500);

            try {
                Photo photo = Db.Photos.Get (Convert.ToUInt32 (JobOptions));
                if (photo == null)
                    return false;

                Log.DebugFormat ("Syncing metadata to file ({0})...", photo.DefaultVersion.Uri);

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
                tag.Comment = photo.Description ?? string.Empty;
                tag.Keywords = names;
                tag.Rating = photo.Rating;
                tag.Software = Defines.PACKAGE + " version " + Defines.VERSION;

                var always_sidecar = Preferences.Get<bool> (Preferences.METADATA_ALWAYS_USE_SIDECAR);
                metadata.SaveSafely (photo.DefaultVersion.Uri, always_sidecar);
            }
        }
    }
}
