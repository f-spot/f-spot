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
		public PhotoList Photos { get; }

		public ImportDialogController (bool persistPreferences)
		{
			// This flag determines whether or not the chosen options will be
			// saved. You don't want to overwrite user preferences when running
			// headless.
			this.persistPreferences = persistPreferences;

			Photos = new PhotoList ();
			FailedImports = new List<SafeUri> ();
			LoadPreferences ();
		}

#region Import Preferences

		readonly bool persistPreferences;
		bool copyFiles = true;
		bool removeOriginals;
		bool recurseSubdirectories = true;
		bool duplicateDetect = true;
		bool mergeRawAndJpeg = true;

		public bool CopyFiles {
			get { return copyFiles; }
			set { copyFiles = value; SavePreferences (); }
		}

		public bool RemoveOriginals {
			get { return removeOriginals; }
			set { removeOriginals = value; SavePreferences (); }
		}

		public bool RecurseSubdirectories {
			get { return recurseSubdirectories; }
			set {
				if (recurseSubdirectories == value)
					return;
				recurseSubdirectories = value;
				SavePreferences ();
				StartScan ();
			}
		}

		public bool DuplicateDetect {
			get { return duplicateDetect; }
			set { duplicateDetect = value; SavePreferences (); }
		}

		public bool MergeRawAndJpeg {
			get { return mergeRawAndJpeg; }
			set {
				if (mergeRawAndJpeg == value)
					return;
				mergeRawAndJpeg = value;
				SavePreferences ();
				StartScan ();
			}
		}

		void LoadPreferences ()
		{
			if (!persistPreferences)
				return;

			copyFiles = Preferences.Get<bool> (Preferences.IMPORT_COPY_FILES);
			recurseSubdirectories = Preferences.Get<bool> (Preferences.IMPORT_INCLUDE_SUBFOLDERS);
			duplicateDetect = Preferences.Get<bool> (Preferences.IMPORT_CHECK_DUPLICATES);
			removeOriginals = Preferences.Get<bool> (Preferences.IMPORT_REMOVE_ORIGINALS);
			mergeRawAndJpeg = Preferences.Get<bool> (Preferences.IMPORT_MERGE_RAW_AND_JPEG);
		}

		void SavePreferences ()
		{
			if (!persistPreferences)
				return;

			Preferences.Set(Preferences.IMPORT_COPY_FILES, copyFiles);
			Preferences.Set(Preferences.IMPORT_INCLUDE_SUBFOLDERS, recurseSubdirectories);
			Preferences.Set(Preferences.IMPORT_CHECK_DUPLICATES, duplicateDetect);
			Preferences.Set(Preferences.IMPORT_REMOVE_ORIGINALS, removeOriginals);
			Preferences.Set (Preferences.IMPORT_MERGE_RAW_AND_JPEG, mergeRawAndJpeg);
		}

#endregion

#region Source Scanning

		List<ImportSource> sources;
		public List<ImportSource> Sources => sources ?? (sources = ScanSources ());

		static List<ImportSource> ScanSources ()
		{
			var monitor = GLib.VolumeMonitor.Default;
			var sources = new List<ImportSource> ();
			foreach (var mount in monitor.Mounts) {
				var root = new SafeUri (mount.Root.Path);

				var themedIcon = mount.Icon as GLib.ThemedIcon;
				if (themedIcon != null && themedIcon.Names.Length > 0) {
					sources.Add (new ImportSource (root, mount.Name, themedIcon.Names [0]));
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
		public List<SafeUri> FailedImports { get; }

#endregion

#region Source Switching

		ImportSource activeSource;
		public ImportSource ActiveSource {
			set {
				if (value == activeSource)
					return;
				activeSource = value;

				CancelScan ();
				StartScan ();

				FireEvent (ImportEvent.SourceChanged);
			}
			get {
				return activeSource;
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
			if (activeSource == null) {
				return;
			}

			var source = activeSource.GetFileImportSource (
				App.Instance.Container.Resolve<IImageFileFactory> (),
				App.Instance.Container.Resolve<IFileSystem> ());
			Photos.Clear ();

			scanTokenSource = new CancellationTokenSource ();
			scanThread = ThreadAssist.Spawn (() => DoScan (source, recurseSubdirectories, mergeRawAndJpeg, scanTokenSource.Token));
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

		Thread importThread;
		CancellationTokenSource importTokenSource;

		public void StartImport ()
		{
			if (importThread != null)
				throw new Exception ("Import already running!");

			importTokenSource = new CancellationTokenSource ();
			importThread = ThreadAssist.Spawn (() => DoImport (importTokenSource.Token));
		}

		public void CancelImport ()
		{
			CancelScan ();

			importTokenSource?.Cancel ();
			//FIXME: there is a race condition between the null check and calling join
			importThread?.Join ();
		}

		void DoImport (CancellationToken token)
		{
			scanThread?.Join ();

			FireEvent (ImportEvent.ImportStarted);

			var importer = App.Instance.Container.Resolve<IImportController> ();
			importer.DoImport (App.Instance.Database, Photos, attachTags, DuplicateDetect, CopyFiles,
				RemoveOriginals, (current, total) => ThreadAssist.ProxyToMain (() => ReportProgress (current, total)),
				token);

			PhotosImported = importer.PhotosImported;
			FailedImports.Clear ();
			FailedImports.AddRange (importer.FailedImports);

			if (!token.IsCancellationRequested) {
				importThread = null;
			}

			FireEvent (ImportEvent.ImportFinished);
		}

#endregion

#region Tagging

		readonly List<Tag> attachTags = new List<Tag> ();
		readonly TagStore tagStore = App.Instance.Database.Tags;

		// Set the tags that will be added on import.
		public void AttachTags (IEnumerable<string> tags)
		{
			App.Instance.Database.BeginTransaction ();
			var importCategory = GetImportedTagsCategory ();
			foreach (var tagname in tags) {
				var tag = tagStore.GetTagByName (tagname);
				if (tag == null) {
					tag = tagStore.CreateCategory (importCategory, tagname, false);
					tagStore.Commit (tag);
				}
				attachTags.Add (tag);
			}
			App.Instance.Database.CommitTransaction ();
		}

		Category GetImportedTagsCategory ()
		{
			var defaultCategory = tagStore.GetTagByName (Catalog.GetString ("Imported Tags")) as Category;
			if (defaultCategory == null) {
				defaultCategory = tagStore.CreateCategory (null, Catalog.GetString ("Imported Tags"), false);
				defaultCategory.ThemeIconName = "gtk-new";
			}
			return defaultCategory;
		}

#endregion
	}
}
