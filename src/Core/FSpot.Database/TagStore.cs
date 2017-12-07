//
// TagStore.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Ettore Perazzoli <ettore@src.gnome.org>
//   Stephane Delcroix <stephane@delcroix.org>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2003-2009 Novell, Inc.
// Copyright (C) 2003 Ettore Perazzoli
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2004-2006 Larry Ewing
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
using System.Collections;
using System.Collections.Generic;
using FSpot;
using FSpot.Core;
using FSpot.Database.Jobs;
using FSpot.Query;
using FSpot.Settings;
using FSpot.Utils;
using Hyena;
using Hyena.Data.Sqlite;
using Mono.Unix;

namespace FSpot.Database
{
	public class InvalidTagOperationException : InvalidOperationException {

		public InvalidTagOperationException (Tag t, string message) : base (message)
		{
			Tag = t;
		}

		public Tag Tag { get; set; }
	}

	// Sorts tags into an order that it will be safe to delete
	// them in (eg children first).
	public class TagRemoveComparer : IComparer {
		public int Compare (object obj1, object obj2)
		{
			Tag t1 = obj1 as Tag;
			Tag t2 = obj2 as Tag;

			return Compare (t1, t2);
		}

		public int Compare (Tag t1, Tag t2)
		{
			if (t1.IsAncestorOf (t2))
				return 1;

			if (t2.IsAncestorOf (t1))
				return -1;

			return 0;
		}
	}

	public class TagStore : DbStore<Tag>, IDisposable
	{
		bool disposed;
		Tag hidden;

		public Category RootCategory { get; private set; }
		public Tag Hidden {
			get { return hidden; }
			private set {
				hidden = value;
				HiddenTag.Tag = value;
			}
		}
		const string STOCK_ICON_DB_PREFIX = "stock_icon:";

		static void SetIconFromString (Tag tag, string iconString)
		{
			if (iconString == null) {
				tag.Icon = null;
				// IconWasCleared automatically set already, override
				// it in this case since it was NULL in the db.
				tag.IconWasCleared = false;
			} else if (iconString == string.Empty)
				tag.Icon = null;
			else if (iconString.StartsWith (STOCK_ICON_DB_PREFIX, StringComparison.Ordinal))
				tag.ThemeIconName = iconString.Substring (STOCK_ICON_DB_PREFIX.Length);
			else
				tag.Icon = GdkUtils.Deserialize (Convert.FromBase64String (iconString));
		}

		public Tag GetTagByName (string name)
		{
			foreach (Tag t in item_cache.Values)
				if (t.Name.ToLower () == name.ToLower ())
					return t;

			return null;
		}

		public Tag GetTagById (int id)
		{
			foreach (Tag t in item_cache.Values)
				if (t.Id == id)
					return t;
			return null;
		}

		public Tag [] GetTagsByNameStart (string s)
		{
			List <Tag> l = new List<Tag> ();
			foreach (Tag t in item_cache.Values) {
				if (t.Name.ToLower ().StartsWith (s.ToLower ()))
					l.Add (t);
			}

			if (l.Count == 0)
				return null;

			l.Sort ((t1, t2) => t2.Popularity.CompareTo (t1.Popularity));

			return l.ToArray ();
		}

		// In this store we keep all the items (i.e. the tags) in memory at all times.  This is
		// mostly to simplify handling of the parent relationship between tags, but it also makes it
		// a little bit faster.  We achieve this by passing "true" as the cache_is_immortal to our
		// base class.
		void LoadAllTags ()
		{

			// Pass 1, get all the tags.

			IDataReader reader = Database.Query ("SELECT id, name, is_category, sort_priority, icon FROM tags");

			while (reader.Read ()) {
				uint id = Convert.ToUInt32 (reader ["id"]);
				string name = reader ["name"].ToString ();
				bool is_category = (Convert.ToUInt32 (reader ["is_category"]) != 0);

				Tag tag;
				if (is_category)
					tag = new Category (null, id, name);
				else
					tag = new Tag (null, id, name);

				if (reader ["icon"] != null)
					try {
						SetIconFromString (tag, reader ["icon"].ToString ());
					} catch (Exception ex) {
						Log.Exception ("Unable to load icon for tag " + name, ex);
					}

				tag.SortPriority = Convert.ToInt32 (reader["sort_priority"]);
				AddToCache (tag);
			}

			reader.Dispose ();

			// Pass 2, set the parents.
			reader = Database.Query ("SELECT id, category_id FROM tags");

			while (reader.Read ()) {
				uint id = Convert.ToUInt32 (reader ["id"]);
				uint category_id = Convert.ToUInt32 (reader ["category_id"]);

				Tag tag = Get (id);
				if (tag == null)
					throw new Exception (string.Format ("Cannot find tag {0}", id));
				if (category_id == 0)
					tag.Category = RootCategory;
				else {
					tag.Category = Get (category_id) as Category;
					if (tag.Category == null)
						Log.Warning ("Tag Without category found");
				}

			}
			reader.Dispose ();

			//Pass 3, set popularity
			reader = Database.Query ("SELECT tag_id, COUNT (*) AS popularity FROM photo_tags GROUP BY tag_id");
			while (reader.Read ()) {
				Tag t = Get (Convert.ToUInt32 (reader ["tag_id"]));
				if (t != null)
					t.Popularity = Convert.ToInt32 (reader ["popularity"]);
			}
			reader.Dispose ();

			if (Db.Meta.HiddenTagId.Value != null)
				Hidden = LookupInCache ((uint)Db.Meta.HiddenTagId.ValueAsInt);
		}

		void CreateTable ()
		{
			Database.Execute (
				"CREATE TABLE tags (\n" +
				"	id		INTEGER PRIMARY KEY NOT NULL, \n" +
				"	name		TEXT UNIQUE, \n" +
				"	category_id	INTEGER, \n" +
				"	is_category	BOOLEAN, \n" +
				"	sort_priority	INTEGER, \n" +
				"	icon		TEXT\n" +
				")");
		}

		void CreateDefaultTags ()
		{
			Category favorites_category = CreateCategory (RootCategory, Catalog.GetString ("Favorites"), false);
			favorites_category.ThemeIconName = "emblem-favorite";
			favorites_category.SortPriority = -10;
			Commit (favorites_category);

			Tag hidden_tag = CreateTag (RootCategory, Catalog.GetString ("Hidden"), false);
			hidden_tag.ThemeIconName = "emblem-readonly";
			hidden_tag.SortPriority = -9;
			Hidden = hidden_tag;
			Commit (hidden_tag);
			Db.Meta.HiddenTagId.ValueAsInt = (int) hidden_tag.Id;
			Db.Meta.Commit (Db.Meta.HiddenTagId);

			Tag people_category = CreateCategory (RootCategory, Catalog.GetString ("People"), false);
			people_category.ThemeIconName = "emblem-people";
			people_category.SortPriority = -8;
			Commit (people_category);

			Tag places_category = CreateCategory (RootCategory, Catalog.GetString ("Places"), false);
			places_category.ThemeIconName = "emblem-places";
			places_category.SortPriority = -8;
			Commit (places_category);

			Tag events_category = CreateCategory (RootCategory, Catalog.GetString ("Events"), false);
			events_category.ThemeIconName = "emblem-event";
			events_category.SortPriority = -7;
			Commit (events_category);
		}

		// Constructor
		public TagStore (IDb db, bool isNew)
			: base (db, true)
		{
			// The label for the root category is used in new and edit tag dialogs
			RootCategory = new Category (null, 0, Catalog.GetString ("(None)"));

			if (! isNew) {
				LoadAllTags ();
			} else {
				CreateTable ();
				CreateDefaultTags ();
			}
		}

		uint InsertTagIntoTable (Category parentCategory, string name, bool isCategory, bool autoicon)
		{
	
			uint parent_category_id = parentCategory.Id;
			String default_tag_icon_value = autoicon ? null : string.Empty;
	
			long id = Database.Execute (new HyenaSqliteCommand ("INSERT INTO tags (name, category_id, is_category, sort_priority, icon)"
				+ "VALUES (?, ?, ?, 0, ?)",
				name,
				parent_category_id,
				isCategory ? 1 : 0,
				default_tag_icon_value));

			// The table in the database is setup to be an INTEGER.
			return (uint) id;
		}

		public Tag CreateTag (Category category, string name, bool autoicon)
		{
			if (category == null)
				category = RootCategory;

			uint id = InsertTagIntoTable (category, name, false, autoicon);

			Tag tag = new Tag (category, id, name);
			tag.IconWasCleared = !autoicon;

			AddToCache (tag);
			EmitAdded (tag);

			return tag;
		}

		public Category CreateCategory (Category parentCategory, string name, bool autoicon)
		{
			if (parentCategory == null)
				parentCategory = RootCategory;

			uint id = InsertTagIntoTable (parentCategory, name, true, autoicon);

			Category new_category = new Category (parentCategory, id, name);
			new_category.IconWasCleared = !autoicon;

			AddToCache (new_category);
			EmitAdded (new_category);

			return new_category;
		}

		public override Tag Get (uint id)
		{
		    return id == 0 ? RootCategory : LookupInCache (id);
		}

		public override void Remove (Tag item)
		{
			Category category = item as Category;
			if (category != null &&
				category.Children != null &&
				category.Children.Count > 0)
				throw new InvalidTagOperationException (category, "Cannot remove category that contains children");

			RemoveFromCache (item);

			item.Category = null;

			Database.Execute (new HyenaSqliteCommand ("DELETE FROM tags WHERE id = ?", item.Id));

			EmitRemoved (item);
		}

		string GetIconString (Tag tag)
		{
			if (tag.ThemeIconName != null)
				return STOCK_ICON_DB_PREFIX + tag.ThemeIconName;
			if (tag.Icon == null) {
				if (tag.IconWasCleared)
					return string.Empty;
				return null;
			}

			byte [] data = GdkUtils.Serialize (tag.Icon);
			return Convert.ToBase64String (data);
		}

		public override void Commit (Tag item)
		{
			Commit (item, false);
		}

		public void Commit (Tag tag, bool updateXmp)
		{
			Commit (new Tag[] {tag}, updateXmp);
		}

		public void Commit (Tag [] tags, bool updateXmp)
		{
			// TODO.
			bool use_transactions = updateXmp;//!Database.InTransaction && update_xmp;

			//if (use_transactions)
			//	Database.BeginTransaction ();

			// FIXME: this hack is used, because HyenaSqliteConnection does not support
			// the InTransaction propery

			if (use_transactions) {
				try {
					Database.BeginTransaction ();
				} catch {
					use_transactions = false;
				}
			}

			foreach (Tag tag in tags) {
				Database.Execute (new HyenaSqliteCommand ("UPDATE tags SET name = ?, category_id = ?, "
							+ "is_category = ?, sort_priority = ?, icon = ? WHERE id = ?",
								  tag.Name,
								  tag.Category.Id,
								  tag is Category ? 1 : 0,
								  tag.SortPriority,
								  GetIconString (tag),
								  tag.Id));
	
				if (updateXmp && Preferences.Get<bool> (Preferences.METADATA_EMBED_IN_IMAGE)) {
					Photo [] photos = Db.Photos.Query (new TagTerm (tag));
					foreach (Photo p in photos)
						if (p.HasTag (tag)) // the query returns all the pics of the tag and all its child. this avoids updating child tags
							SyncMetadataJob.Create (Db.Jobs, p);
				}
			}

			if (use_transactions)
				Database.CommitTransaction ();

			EmitChanged (tags);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing) {
				// free managed resources
				foreach (Tag tag in item_cache.Values) {
					tag.Dispose ();
				}
				item_cache.Clear ();
				if (RootCategory != null) {
					RootCategory.Dispose ();
					RootCategory = null;
				}
			}
			// free unmanaged resources
		}
	}
}
