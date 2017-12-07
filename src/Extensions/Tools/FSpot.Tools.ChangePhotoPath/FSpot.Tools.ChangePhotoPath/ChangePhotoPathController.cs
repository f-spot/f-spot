//
// ChangePhotoPathController.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2008-2009 Novell, Inc.
// Copyright (C) 2008-2009 Stephane Delcroix
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

//
// ChangePhotoPath.IChangePhotoPathController.cs: The logic to change the photo path in photos.db
//
// Author:
//   Bengt Thuree (bengt@thuree.com)
//
// Copyright (C) 2007
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;

using FSpot;
using FSpot.Core;
using FSpot.Database;
using FSpot.Utils;

using Hyena;

/*
	Need to
		1) Find old base path, assuming starting /YYYY/MM/DD so look for /YY (/19 or /20)
		2) Confirm old base path and display new base path
		3) For each Photo, check each version, and change every instance of old base path to new path

Consider!!!
photo_store.Commit(photo) is using db.ExecuteNonQuery, which is not waiting for the command to finish. On my test set of 20.000 photos,
it took SQLite another 1 hour or so to commit all rows after this extension had finished its execution.

Consider 2!!!
A bit of mixture between URI and path. Old and New base path are in String path. Rest in URI.

*/

namespace FSpot.Tools.ChangePhotoPath
{
	public enum ProcessResult {
		Ok, Cancelled, Error, SamePath, NoPhotosFound, Processing
	}

	public class ChangePathController
	{
		PhotoStore photo_store = FSpot.App.Instance.Database.Photos;
		List<uint> photo_id_array;
		List<uint> version_id_array;
		StringCollection old_path_array, new_path_array;
		int total_photos;
		string orig_base_path;

		private const string BASE2000 = "/20";
		private const string BASE1900 = "/19";
		private const string BASE1800 = "/18";

		private IChangePhotoPathGui gui_controller;


		private bool user_cancelled;
		public bool UserCancelled {
		  get {return user_cancelled;}
		  set {user_cancelled = value;}
		}

		public ChangePathController (IChangePhotoPathGui gui)
		{
			gui_controller = gui;
			total_photos = photo_store.TotalPhotos;
			orig_base_path = EnsureEndsWithOneDirectorySeparator (FindOrigBasePath());			// NOT URI
			string new_base_path = EnsureEndsWithOneDirectorySeparator (FSpot.Settings.Global.PhotoUri.LocalPath);	// NOT URI
			gui_controller.DisplayDefaultPaths (orig_base_path, new_base_path);
			user_cancelled = false;
		}

		private string EnsureEndsWithOneDirectorySeparator (string tmp_str)
		{
			if ( (tmp_str == null) || (tmp_str.Length == 0) )
				return string.Format ("{0}", Path.DirectorySeparatorChar);
			while (tmp_str.EndsWith(string.Format ("{0}", Path.DirectorySeparatorChar)))
				tmp_str = tmp_str.Remove (tmp_str.Length-1, 1);
			return string.Format ("{0}{1}", tmp_str, Path.DirectorySeparatorChar);
		}


		// Should always return TRUE, since path always ends with "/"
		public bool CanWeRun ()
		{
			return (orig_base_path != null);
		}

		private string IsThisPhotoOnOrigBasePath (string check_this_path)
		{
			int i;
			i = check_this_path.IndexOf(BASE2000);
			if (i > 0)
				return (check_this_path.Substring(0, i));
			i = check_this_path.IndexOf(BASE1900);
			if (i > 0)
				return (check_this_path.Substring(0, i));
			i = check_this_path.IndexOf(BASE1800);
			if (i > 0)
				return (check_this_path.Substring(0, i));
			return null;
		}

		private string FindOrigBasePath()
		{
			string res_path = null;

			foreach ( IPhoto photo in photo_store.Query ( "SELECT * FROM photos " ) ) {
				string tmp_path = (photo as Photo).DefaultVersion.Uri.AbsolutePath;
				res_path = IsThisPhotoOnOrigBasePath (tmp_path);
				if (res_path != null)
					break;
			}
			return res_path;
		}

		private void InitializeArrays()
		{
			photo_id_array = new List<uint> ();
			version_id_array = new List<uint> ();
			old_path_array = new StringCollection();
			new_path_array = new StringCollection();
		}

		private void AddVersionToArrays ( uint photo_id, uint version_id, string old_path, string new_path)
		{
			photo_id_array.Add (photo_id);
			version_id_array.Add (version_id);
			old_path_array.Add (old_path);
			new_path_array.Add (new_path);
		}

		private string CreateNewPath (string old_base, string new_base, PhotoVersion version)
		{
			return string.Format ("{0}{1}", new_base, version.Uri.AbsolutePath.Substring(old_base.Length));
		}

		private bool ChangeThisVersionUri (PhotoVersion version, string old_base, string new_base)
		{
			// Change to path from URI, since easier to compare with old_base which is not in URI format.
			string tmp_path = System.IO.Path.GetDirectoryName (version.Uri.AbsolutePath);
			return ( tmp_path.StartsWith (old_base) );
		}

		private void SearchVersionUriToChange (Photo photo, string old_base, string new_base)
		{
				foreach (uint version_id in photo.VersionIds) {

					PhotoVersion version = photo.GetVersion (version_id) as PhotoVersion;
					if ( ChangeThisVersionUri (version, old_base, new_base) )
						AddVersionToArrays (	photo.Id,
									version_id,
									version.Uri.AbsolutePath,
									CreateNewPath (old_base, new_base, version));
//					else
//						System.Console.WriteLine ("L : {0}", version.Uri.AbsolutePath);
				}
		}

		public bool SearchUrisToChange (string old_base, string new_base)
		{
			int count = 0;

			foreach ( IPhoto ibrows in photo_store.Query ( "SELECT * FROM photos " ) ) {
				count++;
				if (gui_controller.UpdateProgressBar ("Scanning through database", "Checking photo", total_photos))
				    return false;
				SearchVersionUriToChange ((ibrows as Photo), old_base, new_base);
			}
			return true;
		}

		public bool StillOnSamePhotoId (int old_index, int current_index, List<uint> array)
		{
			try {
				return (array[old_index] == array[current_index]);
			} catch {
				return true; // return true if out of index.
			}
		}

		public void UpdateThisUri (int index, string path, ref Photo photo)
		{
			if (photo == null)
				photo = photo_store.Get (photo_id_array[index]);
			PhotoVersion version = photo.GetVersion ( (uint) version_id_array[index]) as PhotoVersion;
			version.BaseUri = new SafeUri ( path ).GetBaseUri ();
			version.Filename = new SafeUri ( path ).GetFilename ();
			photo.Changes.UriChanged = true;
			photo.Changes.ChangeVersion ( (uint) version_id_array[index] );
		}

		// FIXME: Refactor, try to use one common method....
		public void RevertAllUris (int last_index)
		{
			gui_controller.remove_progress_dialog();
			Photo photo = null;
			for (int k = last_index; k >= 0; k--) {
				if (gui_controller.UpdateProgressBar ("Reverting changes to database", "Reverting photo", last_index))
					{} // do nothing, ignore trying to abort the revert...
				if ( (photo != null) && !StillOnSamePhotoId (k+1, k, photo_id_array) ) {
					photo_store.Commit (photo);
					photo = null;
				}

				UpdateThisUri (k, old_path_array[k], ref photo);
				Log.DebugFormat ("R : {0} - {1}", k, old_path_array[k]);
			}
			if (photo != null)
				photo_store.Commit (photo);
			Log.Debug ("Changing path failed due to above error. Have reverted any modification that took place.");
		}

		public ProcessResult ChangeAllUris ( ref int  last_index)
		{
			gui_controller.remove_progress_dialog();
			Photo photo = null;
			last_index = 0;
			try {
				photo = null;
				for (last_index = 0; last_index < photo_id_array.Count; last_index++) {

					if (gui_controller.UpdateProgressBar ("Changing photos base path", "Changing photo", photo_id_array.Count)) {
						Log.Debug("User aborted the change of paths...");
						return ProcessResult.Cancelled;
					}

					if ( (photo != null) && !StillOnSamePhotoId (last_index-1, last_index, photo_id_array) ) {
						photo_store.Commit (photo);
						photo = null;
					}

					UpdateThisUri (last_index, new_path_array[last_index], ref photo);
					Log.DebugFormat ("U : {0} - {1}", last_index, new_path_array[last_index]);

					// DEBUG ONLY
					// Cause an TEST exception on 6'th URI to be changed.
					// float apa = last_index / (last_index-6);
				}
				if (photo != null)
					photo_store.Commit (photo);
			} catch (Exception e) {
				Log.Exception(e);
				return ProcessResult.Error;
			}
			return ProcessResult.Ok;
		}


		public ProcessResult ProcessArrays()
		{
			int last_index = 0;
			ProcessResult tmp_res;
			tmp_res = ChangeAllUris(ref last_index);
			if (!(tmp_res == ProcessResult.Ok))
				RevertAllUris(last_index);
			return tmp_res;
		}

/*
		public void CheckIfUpdated (int test_index, StringCollection path_array)
		{
			Photo photo = photo_store.Get ( (uint) photo_id_array[test_index]) as Photo;
			PhotoVersion version = photo.GetVersion ( (uint) version_id_array[test_index]) as PhotoVersion;
			if (version.Uri.AbsolutePath.ToString() == path_array[ test_index ])
				Log.DebugFormat ("Test URI ({0}) matches --- Should be finished", test_index);
			else
				Log.DebugFormat ("Test URI ({0}) DO NOT match --- Should NOT BE finished", test_index);
		}
*/

/*
Check paths are different
If (Scan all photos) // user might cancel
	If (Check there are photos on old path)
		ChangePathsOnPhotos
*/

		public bool NewOldPathSame (ref string newpath, ref string oldpath)
		{
			string p1 = EnsureEndsWithOneDirectorySeparator(newpath);
			string p2 = EnsureEndsWithOneDirectorySeparator(oldpath);
			return (p1 == p2);
		}

		public ProcessResult ChangePathOnPhotos (string old_base, string new_base)
		{
			ProcessResult tmp_res = ProcessResult.Processing;
			InitializeArrays();

			if (NewOldPathSame (ref new_base, ref old_base))
				tmp_res = ProcessResult.SamePath;

			if ( (tmp_res == ProcessResult.Processing) && (!SearchUrisToChange (old_base, new_base)) )
				tmp_res = ProcessResult.Cancelled;

			if ( (tmp_res == ProcessResult.Processing) && (photo_id_array.Count == 0) )
				tmp_res = ProcessResult.NoPhotosFound;

			if (tmp_res == ProcessResult.Processing)
				tmp_res = ProcessArrays();

//			if (res)
//				CheckIfUpdated (photo_id_array.Count-1, new_path_array);
//			else
//				CheckIfUpdated (0, old_path_array);

			return tmp_res;
		}
	}
}
