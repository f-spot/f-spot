//
// ImportDialogController.cs
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
using FSpot.Core;
using FSpot.Database;
using FSpot.FileSystem;
using FSpot.Imaging;
using FSpot.Settings;
using Gtk;
using Hyena;
using Mono.Unix;

namespace FSpot.Import
{
	public class ImportDialogController
	{
		public PhotoList Photos { get; private set; }

		public ImportDialogController (bool persistPreferences)
		{
			// This flag determines whether or not the chosen options will be
			// saved. You don't want to overwrite user preferences when running
			// headless.
			persist_preferences = persistPreferences;

			Photos = new PhotoList ();
			FailedImports = new List<SafeUri> ();
			LoadPreferences ();
		}

#region Import Preferences

		bool persist_preferences;
		bool copy_files = true;
		bool remove_originals;
		bool recurse_subdirectories = true;
		bool duplicate_detect = true;
		bool merge_raw_and_jpeg = true;

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
				StartScan ();
			}
		}

		public bool DuplicateDetect {
			get { return duplicate_detect; }
			set { duplicate_detect = value; SavePreferences (); }
		}

		public bool MergeRawAndJpeg {
			get { return merge_raw_and_jpeg; }
			set {
				if (merge_raw_and_jpeg == value)
					return;
				merge_raw_and_jpeg = value;
				SavePreferences ();
				StartScan ();
			}
		}

		void LoadPreferences ()
		{
			if (!persist_preferences)
				return;

			copy_files = Preferences.Get<bool> (Preferences.IMPORT_COPY_FILES);
			recurse_subdirectories = Preferences.Get<bool> (Preferences.IMPORT_INCLUDE_SUBFOLDERS);
			duplicate_detect = Preferences.Get<bool> (Preferences.IMPORT_CHECK_DUPLICATES);
			remove_originals = Preferences.Get<bool> (Preferences.IMPORT_REMOVE_ORIGINALS);
			merge_raw_and_jpeg = Preferences.Get<bool> (Preferences.IMPORT_MERGE_RAW_AND_JPEG);
		}

		void SavePreferences ()
		{
			if (!persist_preferences)
				return;

			Preferences.Set(Preferences.IMPORT_COPY_FILES, copy_files);
			Preferences.Set(Preferences.IMPORT_INCLUDE_SUBFOLDERS, recurse_subdirectories);
			Preferences.Set(Preferences.IMPORT_CHECK_DUPLICATES, duplicate_detect);
			Preferences.Set(Preferences.IMPORT_REMOVE_ORIGINALS, remove_originals);
			Preferences.Set (Preferences.IMPORT_MERGE_RAW_AND_JPEG, merge_raw_and_jpeg);
		}

#endregion

#region Source Scanning

		List<ImportSource> _sources;
		public List<ImportSource> Sources {
			get {
				if (_sources == null)
					_sources = ScanSources ();
				return _sources;
			}
		}

		static List<ImportSource> ScanSources ()
		{
			var monitor = GLib.VolumeMonitor.Default;
			var sources = new List<ImportSource> ();
			foreach (var mount in monitor.Mounts) {
				var root = new SafeUri (mount.Root.Uri);

				var themed_icon = (mount.Icon as GLib.ThemedIcon);
				if (themed_icon != null && themed_icon.Names.Length > 0) {
					sources.Add (new ImportSource (root, mount.Name, themed_icon.Names [0]));
				} else {
					sources.Add (new ImportSource (root, mount.Name, null));
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
				StatusEvent?.Invoke (evnt);
			});
		}

		void ReportProgress (int current, int total)
		{
			ProgressUpdated?.Invoke (current, total);
		}

		public int PhotosImported { get; private set; }
		public Roll CreatedRoll { get; private set; }
		public List<SafeUri> FailedImports { get; private set; }

#endregion

#region Source Switching

		ImportSource active_source;
		public ImportSource ActiveSource {
			set {
				if (value == active_source)
					return;
				active_source = value;

				CancelScan ();
				StartScan ();

				FireEvent (ImportEvent.SourceChanged);
			}
			get {
				return active_source;
			}
		}

#endregion

#region Photo Scanning

		Thread scanThread;
		CancellationTokenSource scanTokenSource;

		void StartScan ()
		{
			if (scanThread != null) {
				CancelScan ();
			}
			if (active_source == null) {
				return;
			}

			var source = active_source.GetFileImportSource (
				App.Instance.Container.Resolve<IImageFileFactory> (),
				App.Instance.Container.Resolve<IFileSystem> ());
			Photos.Clear ();

			scanTokenSource = new CancellationTokenSource ();
			scanThread = ThreadAssist.Spawn (() => DoScan (source, recurse_subdirectories, merge_raw_and_jpeg, scanTokenSource.Token));
		}

		void CancelScan ()
		{
			if (scanThread == null)
				return;
			scanTokenSource.Cancel ();
			scanThread.Join ();
			scanThread = null;
			scanTokenSource = null;

			// Make sure all photos are added. This is needed to prevent
			// a race condition where a source is deactivated, yet photos
			// are still added to the collection because they are
			// queued on the mainloop.
			while (Application.EventsPending ()) {
				Application.RunIteration (false);
			}
		}

		void DoScan (IImportSource source, bool recurse, bool merge, CancellationToken token)
		{
			FireEvent (ImportEvent.PhotoScanStarted);

			foreach (var info in source.ScanPhotos (recurse, merge)) {
				ThreadAssist.ProxyToMain (() => Photos.Add (info));
				if (token.IsCancellationRequested)
					break;
			}

			FireEvent (ImportEvent.PhotoScanFinished);
		}

#endregion

#region Importing

		Thread ImportThread;
		CancellationTokenSource importTokenSource;

		public void StartImport ()
		{
			if (ImportThread != null)
				throw new Exception ("Import already running!");

			importTokenSource = new CancellationTokenSource ();
			ImportThread = ThreadAssist.Spawn (() => DoImport (importTokenSource.Token));
		}

		public void CancelImport ()
		{
			CancelScan ();

			importTokenSource?.Cancel ();
			//FIXME: there is a race condition between the null check and calling join
			ImportThread?.Join ();
		}

		void DoImport (CancellationToken token)
		{
			scanThread?.Join ();

			FireEvent (ImportEvent.ImportStarted);

			var importer = App.Instance.Container.Resolve<IImportController> ();
			importer.DoImport (App.Instance.Database, Photos, attach_tags, DuplicateDetect, CopyFiles,
				RemoveOriginals, (current, total) => ThreadAssist.ProxyToMain (() => ReportProgress (current, total)),
				token);

			PhotosImported = importer.PhotosImported;
			FailedImports.Clear ();
			FailedImports.AddRange (importer.FailedImports);

			if (!token.IsCancellationRequested) {
				ImportThread = null;
			}

			FireEvent (ImportEvent.ImportFinished);
		}

#endregion

#region Tagging

		List<Tag> attach_tags = new List<Tag> ();
		readonly TagStore tag_store = App.Instance.Database.Tags;

		// Set the tags that will be added on import.
		public void AttachTags (IEnumerable<string> tags)
		{
			App.Instance.Database.BeginTransaction ();
			var import_category = GetImportedTagsCategory ();
			foreach (var tagname in tags) {
				var tag = tag_store.GetTagByName (tagname);
				if (tag == null) {
					tag = tag_store.CreateCategory (import_category, tagname, false);
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
