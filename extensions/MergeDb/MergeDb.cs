/* FSpot.MergeDb.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;
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
		[Glade.Widget] Gtk.Dialog mergedb_dialog;
		[Glade.Widget] Gtk.Button apply_button;
		[Glade.Widget] Gtk.Button cancel_button;
		[Glade.Widget] Gtk.FileChooserButton db_filechooser;
		[Glade.Widget] Gtk.RadioButton newrolls_radio;
		[Glade.Widget] Gtk.RadioButton allrolls_radio;
		[Glade.Widget] Gtk.RadioButton singleroll_radio;
		[Glade.Widget] Gtk.ComboBox rolls_combo;


		Db from_db;
		Db to_db;
		PhotoQuery query;
		Roll [] new_rolls;

		Dictionary<uint, Tag> tag_map; //Key is a TagId from from_db, Value is a Tag from to_db
		Dictionary<uint, uint> roll_map; 

		public void Run (object o, EventArgs e)
		{
			from_db = new Db ();
			from_db.ExceptionThrown += HandleDbException;
			to_db = Core.Database;

			ShowDialog ();
		}

		void HandleDbException (Exception e)
		{
			Log.Exception (e);
		}

		public void ShowDialog () {
			Glade.XML xml = new Glade.XML (null, "MergeDb.glade", "mergedb_dialog", "f-spot");
			xml.Autoconnect (this);
			mergedb_dialog.Modal = false;
			mergedb_dialog.TransientFor = null;

			db_filechooser.FileSet += HandleFileSet;

			newrolls_radio.Toggled += HandleRollsChanged;
			allrolls_radio.Toggled += HandleRollsChanged;
			singleroll_radio.Toggled += HandleRollsChanged;

			rolls_combo.Changed += HandleRollsChanged;

			mergedb_dialog.Response += HandleResponse;
			mergedb_dialog.ShowAll ();
		}

		void HandleFileSet (object o, EventArgs e)
		{
			try {
				from_db.Init (db_filechooser.Filename, true);
				Log.Debug ("HE");
				query = new PhotoQuery (from_db.Photos);
			
				CheckRolls ();
				rolls_combo.Active = 0;

				newrolls_radio.Sensitive = true;
				allrolls_radio.Sensitive = true;
				singleroll_radio.Sensitive = true;

				apply_button.Sensitive = true;

				newrolls_radio.Active = true;
				HandleRollsChanged (null, null);
			} catch (Exception ex) {
				string msg = Catalog.GetString ("Error opening the selected file");
				string desc = String.Format (Catalog.GetString ("The file you selected is not a valid or supported database.\n\nReceived exception \"{0}\"."), ex.Message);
				
				HigMessageDialog md = new HigMessageDialog (mergedb_dialog, DialogFlags.DestroyWithParent, 
									    Gtk.MessageType.Error,
									    ButtonsType.Ok, 
									    msg,
									    desc);
				md.Run ();
				md.Destroy ();

				Log.Exception (ex);
			}
		}

		void CheckRolls ()
		{
			List<Roll> from_rolls = new List<Roll> (from_db.Rolls.GetRolls ());
			Roll [] to_rolls = to_db.Rolls.GetRolls ();
			foreach (Roll tr in to_rolls)
				foreach (Roll fr in from_rolls.ToArray ())
					if (tr.Time == fr.Time)
						from_rolls.Remove (fr);
			new_rolls = from_rolls.ToArray ();

			foreach (Roll r in from_rolls) {
				uint numphotos = from_db.Rolls.PhotosInRoll (r);
				DateTime date = r.Time.ToLocalTime ();
				rolls_combo.AppendText (String.Format ("{0} ({1})", date.ToString("%dd %MMM, %HH:%mm"), numphotos));
			}
		}

		void HandleRollsChanged (object o, EventArgs e)
		{
			rolls_combo.Sensitive = singleroll_radio.Active;

			if (allrolls_radio.Active)
				query.RollSet = null;

			if (newrolls_radio.Active)
				query.RollSet = new RollSet (new_rolls);

			if (singleroll_radio.Active) {
				Console.WriteLine (rolls_combo.Active);
				query.RollSet = new RollSet (new_rolls [rolls_combo.Active]);
			}
		}

		void HandleResponse (object obj, ResponseArgs args) {
			if (args.ResponseId == ResponseType.Accept) {
				Roll [] mergerolls = singleroll_radio.Active ? new Roll [] {new_rolls [rolls_combo.Active]} : new_rolls;
				DoMerge (query, mergerolls, false);
			}
			mergedb_dialog.Destroy ();
		}


		public static void Merge (string path, Db to_db)
		{
			Log.WarningFormat ("Will merge db {0} into main f-spot db {1}", path, FSpot.Global.BaseDirectory + "/photos.db" );
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
					dest_tag = to_store.CreateTag (parent, tag_to_merge.Name);
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

		void ImportPhoto (Photo photo, bool copy)
		{
			Log.WarningFormat ("Importing {0}", photo.Name);
			PhotoStore from_store = from_db.Photos;
			PhotoStore to_store = to_db.Photos;

			Gdk.Pixbuf pixbuf;
			Photo newp = to_store.Create (photo.VersionUri (Photo.OriginalVersionId), roll_map [photo.RollId], out pixbuf);


			foreach (Tag t in photo.Tags) {
				Log.WarningFormat ("Tagging with {0}", t.Name);
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

