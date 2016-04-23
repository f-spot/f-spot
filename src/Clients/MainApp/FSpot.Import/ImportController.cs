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
using System.Linq;
using System.Threading;
using FSpot.Core;
using FSpot.Database;
using FSpot.FileSystem;
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.Utils;
using Hyena;

namespace FSpot.Import
{
	public class ImportController
	{
		#region fields

		readonly IFileSystem fileSystem;
		readonly IDb db;
		readonly IList<Tag> tagsToAttach;
		readonly bool duplicateDetect;
		readonly bool copyFiles;
		readonly bool removeOriginals;

		PhotoFileTracker photo_file_tracker;
		MetadataImporter metadata_importer;
		Stack<SafeUri> created_directories;
		List<uint> imported_photos;
		List<SafeUri> failedImports = new List<SafeUri> ();

		#endregion

		#region props

		public Roll CreatedRoll { get; private set; }
		public IEnumerable<SafeUri> FailedImports { get { return failedImports.AsEnumerable(); } }
		public int PhotosImported { get { return imported_photos.Count; } }

		#endregion

		#region ctors

		public ImportController (IFileSystem fileSystem, IDb db, IList<Tag> tagsToAttach, bool duplicateDetect, bool copyFiles, bool removeOriginals)
		{
			this.fileSystem = fileSystem;
			this.db = db;
			this.tagsToAttach = tagsToAttach;
			this.duplicateDetect = duplicateDetect;
			this.copyFiles = copyFiles;
			this.removeOriginals = removeOriginals;
		}

		#endregion

		public void DoImport (IBrowsableCollection photos, Action<int, int> reportProgress, CancellationToken token)
		{
			db.Sync = false;
			created_directories = new Stack<SafeUri> ();
			imported_photos = new List<uint> ();
			photo_file_tracker = new PhotoFileTracker (fileSystem);
			metadata_importer = new MetadataImporter (db.Tags);

			CreatedRoll = db.Rolls.Create ();

			EnsureDirectory (Global.PhotoUri);

			try {
				int i = 0;
				int total = photos.Count;
				foreach (var info in photos.Items) {
					if (token.IsCancellationRequested) {
						RollbackImport ();
						return;
					}

					reportProgress (i++, total);
					try {
						ImportPhoto (info, CreatedRoll);
					} catch (Exception e) {
						Log.DebugFormat ("Failed to import {0}", info.DefaultVersion.Uri);
						Log.DebugException (e);
						failedImports.Add (info.DefaultVersion.Uri);
					}
				}

				FinishImport ();
			} catch (Exception e) {
				RollbackImport ();
				throw e;
			} finally {
				Cleanup ();
			}
		}

		void ImportPhoto (IPhoto item, DbItem roll)
		{
			if (item is IInvalidPhotoCheck && (item as IInvalidPhotoCheck).IsInvalid) {
				throw new Exception ("Failed to parse metadata, probably not a photo");
			}

			// Do duplicate detection
			if (duplicateDetect && db.Photos.HasDuplicate (item)) {
				return;
			}

			if (copyFiles) {
				var destinationBase = FindImportDestination (item, Global.PhotoUri);
				EnsureDirectory (destinationBase);
				// Copy into photo folder.
				photo_file_tracker.CopyIfNeeded (item, destinationBase);
			}

			// Import photo
			var photo = db.Photos.CreateFrom (item, false, roll.Id);

			bool needs_commit = false;

			// Add tags
			if (tagsToAttach.Count > 0) {
				photo.AddTag (tagsToAttach);
				needs_commit = true;
			}

			// Import XMP metadata
			needs_commit |= metadata_importer.Import (photo, item);

			if (needs_commit) {
				db.Photos.Commit (photo);
			}

			// Prepare thumbnail (Import is I/O bound anyway)
			App.Instance.Container.Resolve<IThumbnailLoader> ().Request (item.DefaultVersion.Uri, ThumbnailSize.Large, 10);

			imported_photos.Add (photo.Id);
		}

		void RollbackImport ()
		{
			// Remove photos
			foreach (var id in imported_photos) {
				db.Photos.Remove (db.Photos.Get (id));
			}

			foreach (var uri in photo_file_tracker.CopiedFiles) {
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
			metadata_importer.Cancel ();

			// Remove created roll
			db.Rolls.Remove (CreatedRoll);
		}

		void Cleanup ()
		{
			if (imported_photos != null && imported_photos.Count == 0)
				db.Rolls.Remove (CreatedRoll);
			//FIXME: we are cleaning a cache that is never used, that smells...
			Photo.ResetMD5Cache ();
			GC.Collect ();
			db.Sync = true;
		}

		void FinishImport ()
		{
			if (removeOriginals) {
				foreach (var uri in photo_file_tracker.OriginalFiles) {
					try {
						var file = GLib.FileFactory.NewForUri (uri);
						file.Delete (null);
					} catch (Exception) {
						Log.WarningFormat ("Failed to remove original file: {0}", uri);
					}
				}
			}
		}

		internal static SafeUri FindImportDestination (IPhoto item, SafeUri baseUri)
		{
			DateTime time = item.Time;
			return baseUri
				.Append (time.Year.ToString ())
				.Append (String.Format ("{0:D2}", time.Month))
				.Append (String.Format ("{0:D2}", time.Day));
		}

		static void EnsureDirectory (SafeUri uri)
		{
			var parts = uri.AbsolutePath.Split('/');
			var current = new SafeUri (uri.Scheme + ":///", true);
			for (int i = 0; i < parts.Length; i++) {
				current = current.Append (parts [i]);
				var file = GLib.FileFactory.NewForUri (current);
				if (!file.Exists) {
					file.MakeDirectory (null);
				}
			}
		}
	}
}
