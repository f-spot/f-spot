using Gdk;
using Gnome;
using Gtk;
using Mono.Unix;
using Mono.Data.SqliteClient;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;
using Banshee.Database;
using FSpot;
using FSpot.Jobs;
using FSpot.Query;
using FSpot.Utils;

// FIXME: This is to workaround the currently busted GTK# bindings.
using System.Runtime.InteropServices;

public class PixbufSerializer {
	[DllImport("libgdk_pixbuf-2.0-0.dll")]
	static extern unsafe bool gdk_pixdata_deserialize(ref Gdk.Pixdata raw, uint stream_length, byte [] stream, out IntPtr error);

	public static unsafe Pixbuf Deserialize (byte [] data)
	{
		Pixdata pixdata = new Pixdata ();

		pixdata.Deserialize ((uint) data.Length, data);

		return Pixbuf.FromPixdata (pixdata, true);
	}

	[DllImport("libgdk_pixbuf-2.0-0.dll")]
	static extern IntPtr gdk_pixdata_serialize(ref Gdk.Pixdata raw, out uint stream_length_p);

	public static byte [] Serialize (Pixbuf pixbuf)
	{
		Pixdata pixdata = new Pixdata ();
		IntPtr raw_pixdata = pixdata.FromPixbuf (pixbuf, false); // FIXME GTK# shouldn't this be a constructor or something?
									//       It's probably because we need the IntPtr to free it afterwards

		uint data_length;
		IntPtr raw_data = gdk_pixdata_serialize (ref pixdata, out data_length);

		byte [] data = new byte [data_length];
		Marshal.Copy (raw_data, data, 0, (int) data_length);
		
		GLib.Marshaller.Free (new IntPtr[] { raw_data, raw_pixdata });
		
		return data;
	}
}

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

public class TagStore : DbStore {
	Category root_category;
	public Category RootCategory {
		get {
			return root_category;
		}
	}

	private const string STOCK_ICON_DB_PREFIX = "stock_icon:";

	private void SetIconFromString (Tag tag, string icon_string)
	{
		if (icon_string == null || icon_string == String.Empty)
			tag.Icon = null;
		else if (icon_string.StartsWith (STOCK_ICON_DB_PREFIX))
			tag.ThemeIconName = icon_string.Substring (STOCK_ICON_DB_PREFIX.Length);
		else
			tag.Icon = PixbufSerializer.Deserialize (Convert.FromBase64String (icon_string));
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

		SqliteDataReader reader = Database.Query ("SELECT id, name, is_category, sort_priority, icon FROM tags");

		while (reader.Read ()) {
			uint id = Convert.ToUInt32 (reader [0]);
			string name = reader [1].ToString ();
			bool is_category = (Convert.ToUInt32 (reader [2]) != 0);

			Tag tag;
			if (is_category)
				tag = new Category (null, id, name);
			else
				tag = new Tag (null, id, name);

			if (reader [4] != null)
				try {
					SetIconFromString (tag, reader [4].ToString ());
				} catch (Exception ex) {
					Log.Exception ("Unable to load icon for tag " + name, ex);
				}

			tag.SortPriority = Convert.ToInt32 (reader[3]);
			AddToCache (tag);
		}

		reader.Close ();

		// Pass 2, set the parents.
		reader = Database.Query ("SELECT id, category_id FROM tags");

		while (reader.Read ()) {
			uint id = Convert.ToUInt32 (reader [0]);
			uint category_id = Convert.ToUInt32 (reader [1]);

			Tag tag = Get (id) as Tag;
			if (tag == null)
				throw new Exception (String.Format ("Cannot find tag {0}", id));
			if (category_id == 0)
				tag.Category = RootCategory;
			else {
				tag.Category = Get (category_id) as Category;
				if (tag.Category == null)
					Console.WriteLine ("Tag Without category found");
			}

		}
		reader.Close ();

		//Pass 3, set popularity
		reader = Database.Query ("SELECT tag_id, COUNT (*) as popularity FROM photo_tags GROUP BY tag_id");
		while (reader.Read ()) {
			Tag t = Get (Convert.ToUInt32 (reader [0])) as Tag;
			if (t != null)
				t.Popularity = Convert.ToInt32 (reader [1]);
		}
		reader.Close ();

		if (FSpot.Core.Database.Meta.HiddenTagId.Value != null)
			hidden = LookupInCache ((uint) FSpot.Core.Database.Meta.HiddenTagId.ValueAsInt) as Tag;
	}


	private void CreateTable ()
	{

		Database.ExecuteNonQuery ("CREATE TABLE tags (                            " +
				   "	id            INTEGER PRIMARY KEY NOT NULL," +
				   "       name          TEXT UNIQUE,                 " +
				   "       category_id   INTEGER,			   " +
				   "       is_category   BOOLEAN,			   " +
				   "       sort_priority INTEGER,			   " +
				   "       icon          TEXT			   " +
				   ")");

	}


	private void CreateDefaultTags ()
	{
		Category favorites_category = CreateCategory (RootCategory, Catalog.GetString ("Favorites"));
		favorites_category.ThemeIconName = "emblem-favorite";
		favorites_category.SortPriority = -10;
		Commit (favorites_category);

		Tag hidden_tag = CreateTag (RootCategory, Catalog.GetString ("Hidden"));
		hidden_tag.ThemeIconName = "emblem-readonly";
		hidden_tag.SortPriority = -9;
		this.hidden = hidden_tag;
		Commit (hidden_tag);
		FSpot.Core.Database.Meta.HiddenTagId.ValueAsInt = (int) hidden_tag.Id;
		FSpot.Core.Database.Meta.Commit (FSpot.Core.Database.Meta.HiddenTagId);

		Tag people_category = CreateCategory (RootCategory, Catalog.GetString ("People"));
		people_category.ThemeIconName = "emblem-people";
		people_category.SortPriority = -8;
		Commit (people_category);

		Tag places_category = CreateCategory (RootCategory, Catalog.GetString ("Places"));
		places_category.ThemeIconName = "emblem-places";
		places_category.SortPriority = -8;
		Commit (places_category);

		Tag events_category = CreateCategory (RootCategory, Catalog.GetString ("Events"));
		events_category.ThemeIconName = "emblem-event";
		events_category.SortPriority = -7;
		Commit (events_category);
	}


	// Constructor

	public TagStore (QueuedSqliteDatabase database, bool is_new)
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

	private uint InsertTagIntoTable (Category parent_category, string name, bool is_category)
	{

		uint parent_category_id = parent_category.Id;

		int id = Database.Execute (new DbCommand ("INSERT INTO tags (name, category_id, is_category, sort_priority)"
                          + "VALUES (:name, :category_id, :is_category, 0)",
						  "name", name,
						  "category_id", parent_category_id,
						  "is_category", is_category ? 1 : 0));


		return (uint) id;
	}

	public Tag CreateTag (Category category, string name)
	{
		if (category == null)
			category = RootCategory;

		uint id = InsertTagIntoTable (category, name, false);

		Tag tag = new Tag (category, id, name);

		AddToCache (tag);
		EmitAdded (tag);
		
		return tag;
	}

	public Category CreateCategory (Category parent_category, string name)
	{
		if (parent_category == null)
			parent_category = RootCategory;

		uint id = InsertTagIntoTable (parent_category, name, true);

		Category new_category = new Category (parent_category, id, name);

		AddToCache (new_category);
		EmitAdded (new_category);

		return new_category;
	}

	public override DbItem Get (uint id)
	{
		if (id == 0)
			return RootCategory;
		else
			return LookupInCache (id) as Tag;
	}
	
	public override void Remove (DbItem item)
	{
		Category category = item as Category;
		if (category != null && 
		    category.Children != null && 
		    category.Children.Length > 0)
			throw new InvalidTagOperationException (category, "Cannot remove category that contains children");

		RemoveFromCache (item);
		
		((Tag)item).Category = null;
		

		Database.ExecuteNonQuery (new DbCommand ("DELETE FROM tags WHERE id = :id", "id", item.Id));

		EmitRemoved (item);
	}


	private string GetIconString (Tag tag)
	{
		if (tag.ThemeIconName != null)
			return STOCK_ICON_DB_PREFIX + tag.ThemeIconName;
		if (tag.Icon == null)
			return String.Empty;

		byte [] data = PixbufSerializer.Serialize (tag.Icon);
		return Convert.ToBase64String (data);
	}

	public override void Commit (DbItem item)
	{
		Commit (item, false);	
	}

	public void Commit (DbItem item, bool update_xmp)
	{
		Tag tag = item as Tag;

		bool use_transactions = !Database.InTransaction && update_xmp;

		if (use_transactions)
			Database.BeginTransaction ();

		Database.ExecuteNonQuery (new DbCommand ("UPDATE tags SET name = :name, category_id = :category_id, "
                    + "is_category = :is_category, sort_priority = :sort_priority, icon = :icon WHERE id = :id",
						  "name", tag.Name,
						  "category_id", tag.Category.Id,
						  "is_category", tag is Category ? 1 : 0,
						  "sort_priority", tag.SortPriority,
						  "icon", GetIconString (tag),
						  "id", tag.Id));
		
		if (update_xmp && Preferences.Get<bool> (Preferences.METADATA_EMBED_IN_IMAGE)) {
			Photo [] photos = Core.Database.Photos.Query (new Tag [] { tag });
			foreach (Photo p in photos)
				if (p.HasTag (tag)) // the query returns all the pics of the tag and all its child. this avoids updating child tags
					SyncMetadataJob.Create (Core.Database.Jobs, p);
		}

		if (use_transactions)
			Database.CommitTransaction ();

		EmitChanged (tag);
	}
}
