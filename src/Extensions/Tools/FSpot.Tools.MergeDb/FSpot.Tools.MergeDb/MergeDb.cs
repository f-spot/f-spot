// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2008-2009 Stephane Delcroix
// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using FSpot.Database;
using FSpot.Extensions;
using FSpot.Imaging;
using FSpot.Models;
using FSpot.Query;
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.Utils;

using Gtk;

using Hyena;
using Hyena.Widgets;

using Mono.Unix;

namespace FSpot.Tools.MergeDb
{
	public class MergeDb : ICommand
	{
		Db from_db;
		Db to_db;
		List<Roll> new_rolls;
		MergeDbDialog mdd;

		Dictionary<Guid, Tag> tag_map; //Key is a TagId from from_db, Value is a Tag from to_db
		Dictionary<Guid, Guid> roll_map;

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
				string msg = Catalog.GetString ("Error opening the selected file");
				string desc = string.Format (Catalog.GetString ("The file you selected is not a valid or supported database.\n\nReceived exception \"{0}\"."), ex.Message);

				var md = new HigMessageDialog (mdd.Dialog, DialogFlags.DestroyWithParent,
										Gtk.MessageType.Error,
										ButtonsType.Ok,
										msg,
										desc);
				md.Run ();
				md.Destroy ();

				Log.Exception (ex);
			}
		}

		void FillRolls ()
		{
			var from_rolls = new List<Roll> (from_db.Rolls.GetRolls ());
			var to_rolls = to_db.Rolls.GetRolls ();
			foreach (Roll tr in to_rolls)
				foreach (Roll fr in from_rolls.ToArray ())
					if (tr.UtcTime == fr.UtcTime)
						from_rolls.Remove (fr);
			new_rolls = from_rolls;
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
			Log.Warning ($"Will merge db {path} into main f-spot db {Path.Combine (Configuration.BaseDirectory, Configuration.DatabaseName)}");

			Db from_db = new Db (App.Instance.Container.Resolve<IImageFileFactory> (), App.Instance.Container.Resolve<IThumbnailService> (), new UpdaterUI ());

			from_db.Init (path, true);
			//MergeDb mdb = new MergeDb (from_db, to_db);

		}

		void DoMerge (PhotoQuery query, IEnumerable<Roll> rolls, bool copy)
		{
			tag_map = new Dictionary<Guid, Tag> ();
			roll_map = new Dictionary<Guid, Guid> ();

			Log.Warning ("Merging tags");
			MergeTags (from_db.Tags.RootCategory);

			Log.Warning ("Creating the rolls");
			CreateRolls (rolls);

			Log.Warning ("Importing photos");
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

		void CreateRolls (IEnumerable<Roll> rolls)
		{
			if (rolls == null)
				rolls = from_db.Rolls.GetRolls ();

			RollStore from_store = from_db.Rolls;
			RollStore to_store = to_db.Rolls;

			foreach (Roll roll in rolls) {
				if (from_store.PhotosInRoll (roll) == 0)
					continue;
				roll_map[roll.Id] = to_store.Create (roll.UtcTime).Id;
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
			Log.Warning ($"Importing {photo.Name}");
			PhotoStore to_store = to_db.Photos;

			string photoPath = photo.VersionUri (Photo.OriginalVersionId).AbsolutePath;

			while (!File.Exists (photoPath)) {
				Log.Debug ("Not found, trying the mappings...");
				foreach (string key in PathMap.Keys) {
					string path = photoPath;
					path = path.Replace (key, PathMap[key]);
					Log.Debug ($"Replaced path {path}");
					if (File.Exists (path)) {
						photoPath = path;
						break; ;
					}
				}

				if (File.Exists (photoPath)) {
					Log.Debug ("Exists!!!");
					continue;
				}

				string[] parts = photoPath.Split ('/');
				if (parts.Length > 6) {
					string folder = string.Join ("/", parts, 0, parts.Length - 4);
					var pfd = new PickFolderDialog (mdd.Dialog, folder);
					string new_folder = pfd.Run ();
					pfd.Dialog.Destroy ();
					if (new_folder == null) //Skip
						return;

					Log.Debug ($"{folder} maps to {new_folder}");

					PathMap[folder] = new_folder;

				} else
					Log.Debug ("point me to the file");

				Log.Debug ($"FNF: {photoPath}");
			}

			string destination;
			Photo newp;

			if (copy)
				destination = FindImportDestination (new SafeUri (photoPath), photo.UtcTime).AbsolutePath;
			else
				destination = photoPath;

			var dest_uri = new SafeUri (photoPath);

			photo.DefaultVersionId = 1;
			photo.DefaultVersion.Uri = dest_uri;

			if (string.IsNullOrEmpty (photo.DefaultVersion.ImportMD5)) {
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
				Log.Warning ($"Tagging with {t.Name}");
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
			newp.UtcTime = photo.UtcTime;
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
