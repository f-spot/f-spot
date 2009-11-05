/*
 * FSpot.MergeDb.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.IO;
using System.Collections.Generic;

using Gtk;

using FSpot;
using FSpot.Extensions;
using FSpot.Utils;
using FSpot.Query;
using FSpot.UI.Dialog;
using Mono.Unix;

namespace MergeDbExtension
{
	public class MergeDb : ICommand
	{

		Db from_db;
		Db to_db;
		Roll [] new_rolls;
		MergeDbDialog mdd;

		Dictionary<uint, Tag> tag_map; //Key is a TagId from from_db, Value is a Tag from to_db
		Dictionary<uint, uint> roll_map;

		public void Run (object o, EventArgs e)
		{
			from_db = new Db ();
			from_db.ExceptionThrown += HandleDbException;
			to_db = App.Instance.Database;

			//ShowDialog ();
			mdd = new MergeDbDialog (this);
			mdd.FileChooser.FileSet += HandleFileSet;
			mdd.Dialog.Response += HandleResponse;
			mdd.ShowAll ();
		}

		void HandleDbException (Exception e)
		{
			Log.Exception (e);
		}


		internal Db FromDb {
			get { return from_db; }
		}

		void HandleFileSet (object o, EventArgs e)
		{
			try {
				string tempfilename = System.IO.Path.GetTempFileName ();
				System.IO.File.Copy (mdd.FileChooser.Filename, tempfilename, true);

				from_db.Init (tempfilename, true);

				FillRolls ();
				mdd.Rolls = new_rolls;

				mdd.SetSensitive ();

			} catch (Exception ex) {
				string msg = Catalog.GetString ("Error opening the selected file");
				string desc = String.Format (Catalog.GetString ("The file you selected is not a valid or supported database.\n\nReceived exception \"{0}\"."), ex.Message);

				HigMessageDialog md = new HigMessageDialog (mdd.Dialog, DialogFlags.DestroyWithParent,
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
			List<Roll> from_rolls = new List<Roll> (from_db.Rolls.GetRolls ());
			Roll [] to_rolls = to_db.Rolls.GetRolls ();
			foreach (Roll tr in to_rolls)
				foreach (Roll fr in from_rolls.ToArray ())
					if (tr.Time == fr.Time)
						from_rolls.Remove (fr);
			new_rolls = from_rolls.ToArray ();

		}

		void HandleResponse (object obj, ResponseArgs args) {
			if (args.ResponseId == ResponseType.Accept) {
				PhotoQuery query = new PhotoQuery (from_db.Photos);
				query.RollSet = mdd.ActiveRolls == null ? null : new RollSet (mdd.ActiveRolls);
				DoMerge (query, mdd.ActiveRolls, mdd.Copy);
			}
			mdd.Dialog.Destroy ();
		}


		public static void Merge (string path, Db to_db)
		{
			Log.Warning ("Will merge db {0} into main f-spot db {1}", path, FSpot.Global.BaseDirectory + "/photos.db" );
			Db from_db = new Db ();
			from_db.Init (path, true);
			//MergeDb mdb = new MergeDb (from_db, to_db);

		}

		void DoMerge (PhotoQuery query, Roll [] rolls, bool copy)
		{
			tag_map = new Dictionary<uint, Tag> ();
			roll_map = new Dictionary<uint, uint> ();

			Log.Warning ("Merging tags");
			MergeTags (from_db.Tags.RootCategory);

			Log.Warning ("Creating the rolls");
			CreateRolls (rolls);

			Log.Warning ("Importing photos");
			ImportPhotos (query, copy);

		}

		void MergeTags (Tag tag_to_merge)
		{
			TagStore from_store = from_db.Tags;
			TagStore to_store = to_db.Tags;

			if (tag_to_merge != from_store.RootCategory) { //Do not merge RootCategory
				Tag dest_tag = to_store.GetTagByName (tag_to_merge.Name);
				if (dest_tag == null) {
					Category parent = (tag_to_merge.Category == from_store.RootCategory) ?
							to_store.RootCategory :
							to_store.GetTagByName (tag_to_merge.Category.Name) as Category;
					dest_tag = to_store.CreateTag (parent, tag_to_merge.Name, false);
					//FIXME: copy the tag icon and commit
				}
				tag_map [tag_to_merge.Id] = dest_tag;
			}

			if (!(tag_to_merge is Category))
				return;

			foreach (Tag t in (tag_to_merge as Category).Children)
				MergeTags (t);
		}

		void CreateRolls (Roll [] rolls)
		{
			if (rolls == null)
				rolls = from_db.Rolls.GetRolls ();
			RollStore from_store = from_db.Rolls;
			RollStore to_store = to_db.Rolls;

			foreach (Roll roll in rolls) {
				if (from_store.PhotosInRoll (roll) == 0)
					continue;
				roll_map [roll.Id] = (to_store.Create (roll.Time).Id);
			}
		}

		void ImportPhotos (PhotoQuery query, bool copy)
		{
			foreach (Photo p in query.Photos)
				ImportPhoto (p, copy);
		}

		Dictionary<string, string> path_map = null;
		Dictionary<string, string> PathMap {
			get {
				if (path_map == null)
					path_map = new Dictionary<string, string> ();
				return path_map;
			}
		}

		void ImportPhoto (Photo photo, bool copy)
		{
			Log.Warning ("Importing {0}", photo.Name);
			PhotoStore from_store = from_db.Photos;
			PhotoStore to_store = to_db.Photos;

			string photo_path = photo.VersionUri (Photo.OriginalVersionId).AbsolutePath;

			while (!System.IO.File.Exists (photo_path)) {
				Log.Debug ("Not found, trying the mappings...");
				foreach (string key in PathMap.Keys) {
					string path = photo_path;
					path = path.Replace (key, PathMap [key]);
					Log.Debug ("Replaced path {0}", path);
					if (System.IO.File.Exists (path)) {
						photo_path = path;
						break;;
					}
				}

				if (System.IO.File.Exists (photo_path)) {
					Log.Debug ("Exists!!!");
					continue;
				}

				string [] parts = photo_path.Split (new char[] {'/'});
				if (parts.Length > 6) {
					string folder = String.Join ("/", parts, 0, parts.Length - 4);
					PickFolderDialog pfd = new PickFolderDialog (mdd.Dialog, folder);
					string new_folder = pfd.Run ();
					pfd.Dialog.Destroy ();
					if (new_folder == null) //Skip
						return;
					Log.Debug ("{0} maps to {1}", folder, new_folder);

					PathMap[folder] = new_folder;

				} else
					Console.WriteLine ("point me to the file");
				Console.WriteLine ("FNF: {0}", photo_path);

			}

			string destination;
			Gdk.Pixbuf pixbuf;
			Photo newp;

			if (copy)
				destination = FileImportBackend.ChooseLocation (photo_path, null);
			else
				destination = photo_path;

			// Don't copy if we are already home
			if (photo_path == destination)
				newp = to_store.Create (UriUtils.PathToFileUri (destination), roll_map [photo.RollId], out pixbuf);
			else {
				System.IO.File.Copy (photo_path, destination);

				newp = to_store.Create (UriUtils.PathToFileUri (destination), UriUtils.PathToFileUri (photo_path), roll_map [photo.RollId], out pixbuf);
				try {
					File.SetAttributes (destination, File.GetAttributes (destination) & ~FileAttributes.ReadOnly);
					DateTime create = File.GetCreationTime (photo_path);
					File.SetCreationTime (destination, create);
					DateTime mod = File.GetLastWriteTime (photo_path);
					File.SetLastWriteTime (destination, mod);
				} catch (IOException) {
					// we don't want an exception here to be fatal.
				}
			}

			if (newp == null)
				return;

			foreach (Tag t in photo.Tags) {
				Log.Warning ("Tagging with {0}", t.Name);
				newp.AddTag (tag_map [t.Id]);
			}

			foreach (uint version_id in photo.VersionIds)
				if (version_id != Photo.OriginalVersionId) {
					PhotoVersion version = photo.GetVersion (version_id) as PhotoVersion;
					uint newv = newp.AddVersion (version.Uri, version.Name, version.IsProtected);
					if (version_id == photo.DefaultVersionId)
						newp.DefaultVersionId = newv;
				}

			//FIXME Import extra info (time, description, rating)
			newp.Time = photo.Time;
			newp.Description = photo.Description;
			newp.Rating = photo.Rating;

			to_store.Commit (newp);
		}
	}
}
