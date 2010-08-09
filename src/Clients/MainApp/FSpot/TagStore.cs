using Gdk;
using Gtk;
using Mono.Unix;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Data;
using System;
using FSpot;
using FSpot.Core;
using FSpot.Database;
using FSpot.Jobs;
using FSpot.Query;
using FSpot.Utils;
using Hyena;

using Hyena.Data.Sqlite;

namespace FSpot {
public class InvalidTagOperationException : InvalidOperationException {
	public Tag tag;

	public InvalidTagOperationException (Tag t, string message) : base (message)
	{
		tag = t;
	}

	public Tag Tag {
		get {
			return tag;
		}
	}

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
		else if (t2.IsAncestorOf (t1))
			return -1;
		else
			return 0;
	}
}

public class TagStore : DbStore<Tag> {
	Category root_category;
	public Category RootCategory {
		get {
			return root_category;
		}
	}

	private const string STOCK_ICON_DB_PREFIX = "stock_icon:";

	static void SetIconFromString (Tag tag, string icon_string)
	{
		if (icon_string == null) {
			tag.Icon = null;
			// IconWasCleared automatically set already, override
			// it in this case since it was NULL in the db.
			tag.IconWasCleared = false;
		} else if (icon_string == String.Empty)
			tag.Icon = null;
		else if (icon_string.StartsWith (STOCK_ICON_DB_PREFIX))
			tag.ThemeIconName = icon_string.Substring (STOCK_ICON_DB_PREFIX.Length);
		else
			tag.Icon = GdkUtils.Deserialize (Convert.FromBase64String (icon_string));
	}

	private Tag hidden;
	public Tag Hidden {
		get {
			return hidden;
		}
	}

	public Tag GetTagByName (string name)
	{
		foreach (Tag t in this.item_cache.Values)
			if (t.Name.ToLower () == name.ToLower ())
				return t;

		return null;
	}

	public Tag GetTagById (int id)
	{
		foreach (Tag t in this.item_cache.Values)
			if (t.Id == id)
				return t;
		return null;
	}

	public Tag [] GetTagsByNameStart (string s)
	{
		List <Tag> l = new List<Tag> ();
		foreach (Tag t in this.item_cache.Values) {
			if (t.Name.ToLower ().StartsWith (s.ToLower ()))
				l.Add (t);
		}

		if (l.Count == 0)
			return null;

		l.Sort (delegate (Tag t1, Tag t2) {return t2.Popularity.CompareTo (t1.Popularity); });

		return l.ToArray ();
	}

	// In this store we keep all the items (i.e. the tags) in memory at all times.  This is
	// mostly to simplify handling of the parent relationship between tags, but it also makes it
	// a little bit faster.  We achieve this by passing "true" as the cache_is_immortal to our
	// base class.
	private void LoadAllTags ()
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

		reader.Close ();

		// Pass 2, set the parents.
		reader = Database.Query ("SELECT id, category_id FROM tags");

		while (reader.Read ()) {
			uint id = Convert.ToUInt32 (reader ["id"]);
			uint category_id = Convert.ToUInt32 (reader ["category_id"]);

			Tag tag = Get (id) as Tag;
			if (tag == null)
				throw new Exception (String.Format ("Cannot find tag {0}", id));
			if (category_id == 0)
				tag.Category = RootCategory;
			else {
				tag.Category = Get (category_id) as Category;
				if (tag.Category == null)
					Log.Warning ("Tag Without category found");
			}

		}
		reader.Close ();

		//Pass 3, set popularity
		reader = Database.Query ("SELECT tag_id, COUNT (*) AS popularity FROM photo_tags GROUP BY tag_id");
		while (reader.Read ()) {
			Tag t = Get (Convert.ToUInt32 (reader ["tag_id"])) as Tag;
			if (t != null)
				t.Popularity = Convert.ToInt32 (reader ["popularity"]);
		}
		reader.Close ();

		if (FSpot.App.Instance.Database.Meta.HiddenTagId.Value != null)
			hidden = LookupInCache ((uint) FSpot.App.Instance.Database.Meta.HiddenTagId.ValueAsInt) as Tag;
	}


	private void CreateTable ()
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

	private void CreateDefaultTags ()
	{
		Category favorites_category = CreateCategory (RootCategory, Catalog.GetString ("Favorites"), false);
		favorites_category.ThemeIconName = "emblem-favorite";
		favorites_category.SortPriority = -10;
		Commit (favorites_category);

		Tag hidden_tag = CreateTag (RootCategory, Catalog.GetString ("Hidden"), false);
		hidden_tag.ThemeIconName = "emblem-readonly";
		hidden_tag.SortPriority = -9;
		this.hidden = hidden_tag;
		Commit (hidden_tag);
		FSpot.App.Instance.Database.Meta.HiddenTagId.ValueAsInt = (int) hidden_tag.Id;
		FSpot.App.Instance.Database.Meta.Commit (FSpot.App.Instance.Database.Meta.HiddenTagId);

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

	public TagStore (FSpotDatabaseConnection database, bool is_new)
		: base (database, true)
	{
		// The label for the root category is used in new and edit tag dialogs
		root_category = new Category (null, 0, Catalog.GetString ("(None)"));

		if (! is_new) {
			LoadAllTags ();
		} else {
			CreateTable ();
			CreateDefaultTags ();
		}
	}

	private uint InsertTagIntoTable (Category parent_category, string name, bool is_category, bool autoicon)
	{

		uint parent_category_id = parent_category.Id;
		String default_tag_icon_value = autoicon ? null : String.Empty;

		int id = Database.Execute (new HyenaSqliteCommand ("INSERT INTO tags (name, category_id, is_category, sort_priority, icon)"
                          + "VALUES (?, ?, ?, 0, ?)",
						  name,
						  parent_category_id,
						  is_category ? 1 : 0,
						  default_tag_icon_value));


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

	public Category CreateCategory (Category parent_category, string name, bool autoicon)
	{
		if (parent_category == null)
			parent_category = RootCategory;

		uint id = InsertTagIntoTable (parent_category, name, true, autoicon);

		Category new_category = new Category (parent_category, id, name);
		new_category.IconWasCleared = !autoicon;

		AddToCache (new_category);
		EmitAdded (new_category);

		return new_category;
	}

	public override Tag Get (uint id)
	{
		if (id == 0)
			return RootCategory;
		else
			return LookupInCache (id);
	}

	public override void Remove (Tag tag)
	{
		Category category = tag as Category;
		if (category != null &&
		    category.Children != null &&
		    category.Children.Count > 0)
			throw new InvalidTagOperationException (category, "Cannot remove category that contains children");

		RemoveFromCache (tag);

		tag.Category = null;

		Database.Execute (new HyenaSqliteCommand ("DELETE FROM tags WHERE id = ?", tag.Id));

		EmitRemoved (tag);
	}


	private string GetIconString (Tag tag)
	{
		if (tag.ThemeIconName != null)
			return STOCK_ICON_DB_PREFIX + tag.ThemeIconName;
		if (tag.Icon == null) {
			if (tag.IconWasCleared)
				return String.Empty;
			return null;
		}

		byte [] data = GdkUtils.Serialize (tag.Icon);
		return Convert.ToBase64String (data);
	}

	public override void Commit (Tag tag)
	{
		Commit (tag, false);
	}

	public void Commit (Tag tag, bool update_xmp)
	{
		Commit (new Tag[] {tag}, update_xmp);
	}

	public void Commit (Tag [] tags, bool update_xmp)
	{

		// TODO.
		bool use_transactions = update_xmp;//!Database.InTransaction && update_xmp;

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

			if (update_xmp && Preferences.Get<bool> (Preferences.METADATA_EMBED_IN_IMAGE)) {
				Photo [] photos = App.Instance.Database.Photos.Query (new Tag [] { tag });
				foreach (Photo p in photos)
					if (p.HasTag (tag)) // the query returns all the pics of the tag and all its child. this avoids updating child tags
						SyncMetadataJob.Create (App.Instance.Database.Jobs, p);
			}
		}

		if (use_transactions)
			Database.CommitTransaction ();

		EmitChanged (tags);
	}
}
}