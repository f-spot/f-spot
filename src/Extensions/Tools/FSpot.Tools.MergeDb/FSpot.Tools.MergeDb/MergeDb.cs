//
// MergeDb.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
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

using System;
using System.Collections.Generic;
using System.IO;

using FSpot.Core;
using FSpot.Database;
using FSpot.Extensions;
using FSpot.Imaging;
using FSpot.Query;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.Utils;

using Gtk;

using Hyena;
using Hyena.Widgets;


namespace FSpot.Tools.MergeDb
{
	public class MergeDb : ICommand
	{
		Db from_db;
		Db to_db;
		Roll[] new_rolls;
		MergeDbDialog mdd;

		Dictionary<uint, Tag> tag_map; //Key is a TagId from from_db, Value is a Tag from to_db
		Dictionary<uint, uint> roll_map;

		public void Run (object o, EventArgs e)
		{
			from_db = new Db (App.Instance.Container.Resolve<IImageFileFactory> (), App.Instance.Container.Resolve<IThumbnailService> (), new UpdaterUI ());
			to_db = App.Instance.Database;

			//ShowDialog ();
			mdd = new MergeDbDialog (this);
			mdd.FileChooser.FileSet += HandleFileSet;
			mdd.Dialog.Response += HandleResponse;
			mdd.ShowAll ();
		}

		internal Db FromDb {
			get { return from_db; }
		}

		void HandleFileSet (object o, EventArgs e)
		{
			try {
				string tempfilename = Path.GetTempFileName ();
				File.Copy (mdd.FileChooser.Filename, tempfilename, true);

				from_db.Init (tempfilename, true);

				FillRolls ();
				mdd.Rolls = new_rolls;

				mdd.SetSensitive ();

			} catch (Exception ex) {
				string msg = Strings.ErrorOpeningTheSelectedFile;
				string desc = string.Format (Strings.TheFileSelectedNotValidOrSupportedDataaseReceivedExceptionX, ex.Message);

				var md = new HigMessageDialog (mdd.Dialog, DialogFlags.DestroyWithParent,
										Gtk.MessageType.Error,
										ButtonsType.Ok,
										msg,
										desc);
				md.Run ();
				md.Destroy ();

				Logger.Log.Error (ex, "");
			}
		}

		void FillRolls ()
		{
			var from_rolls = new List<Roll> (from_db.Rolls.GetRolls ());
			Roll[] to_rolls = to_db.Rolls.GetRolls ();
			foreach (Roll tr in to_rolls)
				foreach (Roll fr in from_rolls.ToArray ())
					if (tr.Time == fr.Time)
						from_rolls.Remove (fr);
			new_rolls = from_rolls.ToArray ();

		}

		void HandleResponse (object obj, ResponseArgs args)
		{
			if (args.ResponseId == ResponseType.Accept) {
				var query = new PhotoQuery (from_db.Photos);
				query.RollSet = mdd.ActiveRolls == null ? null : new RollSet (mdd.ActiveRolls);
				DoMerge (query, mdd.ActiveRolls, mdd.Copy);
			}
			mdd.Dialog.Destroy ();
		}


		public static void Merge (string path, Db to_db)
		{
			Logger.Log.Warning ($"Will merge db {path} into main f-spot db {Path.Combine (FSpotConfiguration.BaseDirectory, FSpotConfiguration.DatabaseName)}");
			var from_db = new Db (App.Instance.Container.Resolve<IImageFileFactory> (), App.Instance.Container.Resolve<IThumbnailService> (), new UpdaterUI ());
			from_db.Init (path, true);
			//MergeDb mdb = new MergeDb (from_db, to_db);

		}

		void DoMerge (PhotoQuery query, Roll[] rolls, bool copy)
		{
			tag_map = new Dictionary<uint, Tag> ();
			roll_map = new Dictionary<uint, uint> ();

			Logger.Log.Warning ("Merging tags");
			MergeTags (from_db.Tags.RootCategory);

			Logger.Log.Warning ("Creating the rolls");
			CreateRolls (rolls);

			Logger.Log.Warning ("Importing photos");
			ImportPhotos (query, copy);
		}

		void MergeTags (Tag tagToMerge)
		{
			TagStore fromStore = from_db.Tags;
			TagStore to_store = to_db.Tags;

			if (tagToMerge != fromStore.RootCategory) { //Do not merge RootCategory
				Tag dest_tag = to_store.GetTagByName (tagToMerge.Name);
				if (dest_tag == null) {
					Category parent = (tagToMerge.Category == fromStore.RootCategory) ?
							to_store.RootCategory :
							to_store.GetTagByName (tagToMerge.Category.Name) as Category;
					dest_tag = to_store.CreateTag (parent, tagToMerge.Name, false);
					//FIXME: copy the tag icon and commit
				}
				tag_map[tagToMerge.Id] = dest_tag;
			}

			if (!(tagToMerge is Category))
				return;

			foreach (var t in (tagToMerge as Category).Children)
				MergeTags (t);
		}

		void CreateRolls (Roll[] rolls)
		{
			if (rolls == null)
				rolls = from_db.Rolls.GetRolls ();

			RollStore from_store = from_db.Rolls;
			RollStore to_store = to_db.Rolls;

			foreach (Roll roll in rolls) {
				if (from_store.PhotosInRoll (roll) == 0)
					continue;
				roll_map[roll.Id] = to_store.Create (roll.Time).Id;
			}
		}

		void ImportPhotos (PhotoQuery query, bool copy)
		{
			foreach (var p in query.Photos)
				ImportPhoto (p, copy);
		}

		Dictionary<string, string> path_map = null;
		Dictionary<string, string> PathMap {
			get {
				return path_map ?? new Dictionary<string, string> ();
			}
		}

		void ImportPhoto (Photo photo, bool copy)
		{
			Logger.Log.Warning ($"Importing {photo.Name}");
			PhotoStore to_store = to_db.Photos;

			string photoPath = photo.VersionUri (Photo.OriginalVersionId).AbsolutePath;

			while (!File.Exists (photoPath)) {
				Logger.Log.Debug ("Not found, trying the mappings...");
				foreach (string key in PathMap.Keys) {
					string path = photoPath;
					path = path.Replace (key, PathMap[key]);
					Logger.Log.Debug ($"Replaced path {path}");
					if (File.Exists (path)) {
						photoPath = path;
						break; ;
					}
				}

				if (File.Exists (photoPath)) {
					Logger.Log.Debug ("Exists!!!");
					continue;
				}

				string[] parts = photoPath.Split (new char[] { '/' });
				if (parts.Length > 6) {
					string folder = string.Join ("/", parts, 0, parts.Length - 4);
					var pfd = new PickFolderDialog (mdd.Dialog, folder);
					string new_folder = pfd.Run ();
					pfd.Dialog.Destroy ();
					if (new_folder == null) //Skip
						return;

					Logger.Log.Debug ($"{folder} maps to {new_folder}");

					PathMap[folder] = new_folder;

				} else
					Logger.Log.Debug ("point me to the file");

				Logger.Log.Debug ($"FNF: {photoPath}");
			}

			string destination;
			Photo newp;

			if (copy)
				destination = FindImportDestination (new SafeUri (photoPath), photo.Time).AbsolutePath;
			else
				destination = photoPath;

			var dest_uri = new SafeUri (photoPath);

			photo.DefaultVersionId = 1;
			photo.DefaultVersion.Uri = dest_uri;

			if (photo.DefaultVersion.ImportMD5 == string.Empty) {
				(photo.DefaultVersion as PhotoVersion).ImportMD5 = HashUtils.GenerateMD5 (photo.DefaultVersion.Uri);
			}

			if (photoPath != destination) {
				File.Copy (photoPath, destination);

				try {
					File.SetAttributes (destination, File.GetAttributes (destination) & ~FileAttributes.ReadOnly);
					DateTime create = File.GetCreationTime (photoPath);
					File.SetCreationTime (destination, create);
					DateTime mod = File.GetLastWriteTime (photoPath);
					File.SetLastWriteTime (destination, mod);
				} catch (IOException) {
					// we don't want an exception here to be fatal.
				}
			}

			//FIXME simplify the following code by letting CreateFrom import all versions
			//      instead of looping over all versions here
			newp = to_store.CreateFrom (photo, true, roll_map[photo.RollId]);

			if (newp == null)
				return;

			foreach (Tag t in photo.Tags) {
				Logger.Log.Warning ($"Tagging with {t.Name}");
				newp.AddTag (tag_map[t.Id]);
			}

			foreach (uint version_id in photo.VersionIds) {
				if (version_id != Photo.OriginalVersionId) {
					var version = photo.GetVersion (version_id) as PhotoVersion;
					uint newv = newp.AddVersion (version.BaseUri, version.Filename, version.Name, version.IsProtected);
					if (version_id == photo.DefaultVersionId)
						newp.DefaultVersionId = newv;
				}
			}

			//FIXME Import extra info (time, description, rating)
			newp.Time = photo.Time;
			newp.Description = photo.Description;
			newp.Rating = photo.Rating;

			to_store.Commit (newp);
		}

		SafeUri FindImportDestination (SafeUri uri, DateTime time)
		{
			// Find a new unique location inside the photo folder
			string name = uri.GetFilename ();

			var destinationUri = FSpotConfiguration.PhotoUri.Append (time.Year.ToString ())
													  .Append ($"{time.Month:D2}")
													  .Append ($"{time.Day:D2}");
			EnsureDirectory (destinationUri);

			// If the destination we'd like to use is the file itself return that
			if (destinationUri.Append (name) == uri)
				return uri;

			// Find an unused name
			int i = 1;
			var dest = destinationUri.Append (name);
			var fileInfo = new FileInfo (dest.AbsolutePath);
			while (fileInfo.Exists) {
				var filename = uri.GetFilenameWithoutExtension ();
				var extension = uri.GetExtension ();
				dest = destinationUri.Append ($"{filename}-{i++}{extension}");
				fileInfo = new FileInfo (dest.AbsolutePath);
			}

			return dest;
		}

		void EnsureDirectory (SafeUri uri)
			=> Directory.CreateDirectory (uri.AbsolutePath);
	}
}
