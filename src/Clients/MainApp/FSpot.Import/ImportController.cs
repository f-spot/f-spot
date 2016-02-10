//
// ImportController.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2014 Daniel Köb
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
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
using System.Collections.Generic;
using System.Threading;

using Hyena;

using FSpot.Core;
using FSpot.Utils;

using Mono.Unix;

namespace FSpot.Import
{
    public enum ImportEvent {
        SourceChanged,
        PhotoScanStarted,
        PhotoScanFinished,
        ImportStarted,
        ImportFinished,
        ImportError
    }

    public class ImportController
    {
        public BrowsableCollectionProxy Photos { get; private set; }

        public ImportController (bool persist_preferences)
        {
            // This flag determines whether or not the chosen options will be
            // saved. You don't want to overwrite user preferences when running
            // headless.
            this.persist_preferences = persist_preferences;

            Photos = new BrowsableCollectionProxy ();
            FailedImports = new List<SafeUri> ();
            LoadPreferences ();
        }

        ~ImportController ()
        {
            DeactivateSource (ActiveSource);
        }

#region Import Preferences

        private bool persist_preferences = false;
        private bool copy_files = true;
        private bool remove_originals = false;
        private bool recurse_subdirectories = true;
        private bool duplicate_detect = true;

        public bool CopyFiles {
            get { return copy_files; }
            set { copy_files = value; SavePreferences (); }
        }

        public bool RemoveOriginals {
            get { return remove_originals; }
            set { remove_originals = value; SavePreferences (); }
        }

        public bool RecurseSubdirectories {
            get { return recurse_subdirectories; }
            set {
                if (recurse_subdirectories == value)
                    return;
                recurse_subdirectories = value;
                SavePreferences ();
                RescanPhotos ();
            }
        }

        public bool DuplicateDetect {
            get { return duplicate_detect; }
            set { duplicate_detect = value; SavePreferences (); }
        }

        void LoadPreferences ()
        {
            if (!persist_preferences)
                return;

            copy_files = Preferences.Get<bool> (Preferences.IMPORT_COPY_FILES);
            recurse_subdirectories = Preferences.Get<bool> (Preferences.IMPORT_INCLUDE_SUBFOLDERS);
            duplicate_detect = Preferences.Get<bool> (Preferences.IMPORT_CHECK_DUPLICATES);
            remove_originals = Preferences.Get<bool> (Preferences.IMPORT_REMOVE_ORIGINALS);
        }

        void SavePreferences ()
        {
            if (!persist_preferences)
                return;

            Preferences.Set(Preferences.IMPORT_COPY_FILES, copy_files);
            Preferences.Set(Preferences.IMPORT_INCLUDE_SUBFOLDERS, recurse_subdirectories);
            Preferences.Set(Preferences.IMPORT_CHECK_DUPLICATES, duplicate_detect);
            Preferences.Set(Preferences.IMPORT_REMOVE_ORIGINALS, remove_originals);
        }

#endregion

#region Source Scanning

        private List<IImportSource> _sources;
        public List<IImportSource> Sources {
            get {
                if (_sources == null)
                    _sources = ScanSources ();
                return _sources;
            }
        }

        List<IImportSource> ScanSources ()
        {
            var monitor = GLib.VolumeMonitor.Default;
            var sources = new List<IImportSource> ();
            foreach (var mount in monitor.Mounts) {
                var root = new SafeUri (mount.Root.Uri.ToString(), true);

                var themed_icon = (mount.Icon as GLib.ThemedIcon);
                if (themed_icon != null && themed_icon.Names.Length > 0) {
                    sources.Add (new FileImportSource (root, mount.Name, themed_icon.Names [0]));
                } else {
                    sources.Add (new FileImportSource (root, mount.Name, null));
                }
            }
            return sources;
        }

#endregion

#region Status Reporting

        public delegate void ImportProgressHandler (int current, int total);
        public event ImportProgressHandler ProgressUpdated;

        public delegate void ImportEventHandler (ImportEvent evnt);
        public event ImportEventHandler StatusEvent;

        void FireEvent (ImportEvent evnt)
        {
            ThreadAssist.ProxyToMain (() => {
                var h = StatusEvent;
                if (h != null)
                    h (evnt);
            });
        }

        void ReportProgress (int current, int total)
        {
            var h = ProgressUpdated;
            if (h != null)
                h (current, total);
        }

        public int PhotosImported { get; private set; }
        public Roll CreatedRoll { get; private set; }
        public List<SafeUri> FailedImports { get; private set; }

#endregion

#region Source Switching

        private IImportSource active_source;
        public IImportSource ActiveSource {
            set {
                if (value == active_source)
                    return;
                var old_source = active_source;
                active_source = value;
                FireEvent (ImportEvent.SourceChanged);
                RescanPhotos ();
                DeactivateSource (old_source);
            }
            get {
                return active_source;
            }
        }

        void DeactivateSource (IImportSource source)
        {
            if (source == null)
                return;
            source.Deactivate ();
        }

        void RescanPhotos ()
        {
            if (ActiveSource == null)
                return;

            photo_scan_running = true;
            PhotoList pl = new PhotoList ();
            Photos.Collection = pl;
            ActiveSource.StartPhotoScan (this, pl);
            FireEvent (ImportEvent.PhotoScanStarted);
        }

#endregion

#region Source Progress Signalling

        // These are callbacks that should be called by the sources.

        public void PhotoScanFinished ()
        {
            photo_scan_running = false;
            FireEvent (ImportEvent.PhotoScanFinished);
        }

#endregion

#region Importing

        Thread ImportThread;

        public void StartImport ()
        {
            if (ImportThread != null)
                throw new Exception ("Import already running!");

            ImportThread = ThreadAssist.Spawn (() => DoImport ());
        }

        public void CancelImport ()
        {
            if (ActiveSource != null) {
                ActiveSource.Deactivate ();
            }

            import_cancelled = true;
            if (ImportThread != null)
                ImportThread.Join ();
            Cleanup ();
        }

        Stack<SafeUri> created_directories;
        List<uint> imported_photos;
        List<SafeUri> copied_files;
        List<SafeUri> original_files;
        PhotoStore store = App.Instance.Database.Photos;
        RollStore rolls = App.Instance.Database.Rolls;
        volatile bool photo_scan_running;
        MetadataImporter metadata_importer;
        volatile bool import_cancelled = false;

        void DoImport ()
        {
            while (photo_scan_running) {
                Thread.Sleep (1000); // FIXME: we can do this with a better primitive!
            }

            FireEvent (ImportEvent.ImportStarted);
            App.Instance.Database.Sync = false;
            created_directories = new Stack<SafeUri> ();
            imported_photos = new List<uint> ();
            copied_files = new List<SafeUri> ();
            original_files = new List<SafeUri> ();
            metadata_importer = new MetadataImporter ();
            CreatedRoll = rolls.Create ();

            EnsureDirectory (Global.PhotoUri);

            try {
                int i = 0;
                int total = Photos.Count;
                foreach (var info in Photos.Items) {
                    if (import_cancelled) {
                        RollbackImport ();
                        return;
                    }

                    ThreadAssist.ProxyToMain (() => ReportProgress (i++, total));
                    try {
                        ImportPhoto (info, CreatedRoll);
                    } catch (Exception e) {
                        Log.DebugFormat ("Failed to import {0}", info.DefaultVersion.Uri);
                        Log.DebugException (e);
                        FailedImports.Add (info.DefaultVersion.Uri);
                    }
                }

                PhotosImported = imported_photos.Count;
                FinishImport ();
            } catch (Exception e) {
                RollbackImport ();
                throw e;
            } finally {
                Cleanup ();
            }
        }

        void Cleanup ()
        {
            if (imported_photos != null && imported_photos.Count == 0)
                rolls.Remove (CreatedRoll);
            imported_photos = null;
            created_directories = null;
            Photo.ResetMD5Cache ();
            DeactivateSource (ActiveSource);
            System.GC.Collect ();
            App.Instance.Database.Sync = true;
        }

        void FinishImport ()
        {
            if (RemoveOriginals) {
                foreach (var uri in original_files) {
                    try {
                        var file = GLib.FileFactory.NewForUri (uri);
                        file.Delete (null);
                    } catch (Exception) {
                        Log.WarningFormat ("Failed to remove original file: {0}", uri);
                    }
                }
            }

            ImportThread = null;
            FireEvent (ImportEvent.ImportFinished);
        }

        void RollbackImport ()
        {
            // Remove photos
            foreach (var id in imported_photos) {
                store.Remove (store.Get (id));
            }

            foreach (var uri in copied_files) {
                var file = GLib.FileFactory.NewForUri (uri);
                file.Delete (null);
            }

            // Clean up directories
            while (created_directories.Count > 0) {
                var uri = created_directories.Pop ();
                var dir = GLib.FileFactory.NewForUri (uri);
                var enumerator = dir.EnumerateChildren ("standard::name", GLib.FileQueryInfoFlags.None, null);
                if (!enumerator.HasPending) {
                    dir.Delete (null);
                }
            }

            // Clean created tags
            metadata_importer.Cancel();

            // Remove created roll
            rolls.Remove (CreatedRoll);
        }

        void ImportPhoto (IPhoto item, Roll roll)
        {
            if (item is IInvalidPhotoCheck && (item as IInvalidPhotoCheck).IsInvalid) {
                throw new Exception ("Failed to parse metadata, probably not a photo");
            }

            var destination = FindImportDestination (item);

            // Do duplicate detection
            if (DuplicateDetect && store.HasDuplicate (item)) {
                return;
            }

            // Copy into photo folder.
            CopyIfNeeded (item, destination);

            // Import photo
            var photo = store.CreateFrom (item, roll.Id);

            bool needs_commit = false;

            // Add tags
            if (attach_tags.Count > 0) {
                photo.AddTag (attach_tags);
                needs_commit = true;
            }

            // Import XMP metadata
            needs_commit |= metadata_importer.Import (photo, item);

            if (needs_commit) {
                store.Commit (photo);
            }

            // Prepare thumbnail (Import is I/O bound anyway)
            ThumbnailLoader.Default.Request (destination, ThumbnailSize.Large, 10);

            imported_photos.Add (photo.Id);
        }

        void CopyIfNeeded (IPhoto item, SafeUri destination)
        {
            var source = item.DefaultVersion.Uri;

            if (source.Equals (destination))
                return;

            // Copy image
            var file = GLib.FileFactory.NewForUri (source);
            var new_file = GLib.FileFactory.NewForUri (destination);
			try {
				file.Copy (new_file, GLib.FileCopyFlags.AllMetadata, null, null);
			} catch (GLib.GException e){
				// TODO Gdk#2.12 doesn't allow using anything other than e.Message. Gdk#3 allows using the exception Code.
				if (!e.Message.Equals ("Error setting permissions: Operation not permitted") && !e.Message.Equals("Error setting owner: Operation not permitted")) {
					throw e;
				}
			}

            copied_files.Add (destination);
            original_files.Add (source);
            item.DefaultVersion.Uri = destination;

            // Copy XMP sidecar
            var xmp_original = source.ReplaceExtension(".xmp");
            var xmp_file = GLib.FileFactory.NewForUri (xmp_original);
            if (xmp_file.Exists) {
                var xmp_destination = destination.ReplaceExtension (".xmp");
                var new_xmp_file = GLib.FileFactory.NewForUri (xmp_destination);
                try {
					xmp_file.Copy (new_xmp_file, GLib.FileCopyFlags.AllMetadata | GLib.FileCopyFlags.Overwrite, null, null);
                } catch (GLib.GException e){
					// TODO Gdk#2.12 doesn't allow using anything other than e.Message. Gdk#3 allows using the exception Code.
					if (!e.Message.Equals ("Error setting permissions: Operation not permitted") && !e.Message.Equals("Error setting owner: Operation not permitted")) {
                        throw e;
                    }
                }
                copied_files.Add (xmp_destination);
                original_files.Add (xmp_original);
            }
        }

        SafeUri FindImportDestination (IPhoto item)
        {
            var uri = item.DefaultVersion.Uri;

            if (!CopyFiles)
                return uri; // Keep it at the same place

            // Find a new unique location inside the photo folder
            string name = uri.GetFilename ();
            DateTime time = item.Time;

            var dest_uri = Global.PhotoUri.Append (time.Year.ToString ())
                                          .Append (String.Format ("{0:D2}", time.Month))
                                          .Append (String.Format ("{0:D2}", time.Day));
            EnsureDirectory (dest_uri);

            // If the destination we'd like to use is the file itself return that
            if (dest_uri.Append (name) == uri)
                return uri;

            // Find an unused name
            int i = 1;
            var dest = dest_uri.Append (name);
            var file = GLib.FileFactory.NewForUri (dest);
            while (file.Exists) {
                var filename = uri.GetFilenameWithoutExtension ();
                var extension = uri.GetExtension ();
                dest = dest_uri.Append (String.Format ("{0}-{1}{2}", filename, i++, extension));
                file = GLib.FileFactory.NewForUri (dest);
            }

            return dest;
        }

        void EnsureDirectory (SafeUri uri)
        {
            var parts = uri.AbsolutePath.Split('/');
            SafeUri current = new SafeUri (uri.Scheme + ":///", true);
            for (int i = 0; i < parts.Length; i++) {
                current = current.Append (parts [i]);
                var file = GLib.FileFactory.NewForUri (current);
                if (!file.Exists) {
                    file.MakeDirectory (null);
                }
            }
        }

#endregion

#region Tagging

        List<Tag> attach_tags = new List<Tag> ();
        TagStore tag_store = App.Instance.Database.Tags;

        // Set the tags that will be added on import.
        public void AttachTags (IEnumerable<string> tags)
        {
            App.Instance.Database.BeginTransaction ();
            var import_category = GetImportedTagsCategory ();
            foreach (var tagname in tags) {
                var tag = tag_store.GetTagByName (tagname);
                if (tag == null) {
                    tag = tag_store.CreateCategory (import_category, tagname, false) as Tag;
                    tag_store.Commit (tag);
                }
                attach_tags.Add (tag);
            }
            App.Instance.Database.CommitTransaction ();
        }

        Category GetImportedTagsCategory ()
        {
            var default_category = tag_store.GetTagByName (Catalog.GetString ("Imported Tags")) as Category;
            if (default_category == null) {
                default_category = tag_store.CreateCategory (null, Catalog.GetString ("Imported Tags"), false);
                default_category.ThemeIconName = "gtk-new";
            }
            return default_category;
        }

#endregion

    }
}
