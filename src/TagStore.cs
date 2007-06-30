using Gdk;
using Gnome;
using Gtk;
using Mono.Unix;
using Mono.Data.SqliteClient;
using System.Collections;
using System.IO;
using System;
using Banshee.Database;
using FSpot;
using FSpot.Jobs;
using FSpot.Query;

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
		pixdata.FromPixbuf (pixbuf, true); // FIXME GTK# shouldn't this be a constructor or something?

		uint data_length;
		IntPtr raw_data = gdk_pixdata_serialize (ref pixdata, out data_length);

		byte [] data = new byte [data_length];
		Marshal.Copy (raw_data, data, 0, (int) data_length);
		
		GLib.Marshaller.Free (raw_data);

		return data;
	}
}


public class Tag : DbItem, IComparable {
	private string name;
	public string Name {
		set {
			name = value;
		}
		get {
			return name;
		}
	}

	private Category category;
	public Category Category {
		set {
			if (Category != null)
				Category.RemoveChild (this);

			category = value;
			if (category != null)
				category.AddChild (this);
		}
		get {
			return category;
		}
	}

	private int sort_priority;
	public int SortPriority {
		set {
			sort_priority = value;
		}
		get {
			return sort_priority;
		}
	}

	// Icon.  If stock_icon_name is not null, then we save the name of the icon instead
	// of the actual icon data.

	private string stock_icon_name;
	public string StockIconName {
		set {
			stock_icon_name = value;
			Icon = PixbufUtils.LoadFromAssembly (value);
		}

		get {
			return stock_icon_name;
		}
	}

	private Pixbuf icon;
	public Pixbuf Icon {
		set {
			// If a custom icon is set, of course it means we are not using a stock
			// icon.
			stock_icon_name = null;
			icon = value;
			cached_icon_size = 0;
		}
		get {
			return icon;
		}
	}

	public enum IconSize {
		Hidden = 0,
		Small = 16,
		Medium = 24,
		Large = 48
	};

	private static IconSize tag_icon_size = IconSize.Large;
	public static IconSize TagIconSize {
		get { return tag_icon_size; }
		set { tag_icon_size = value; }
	}

	private Pixbuf cached_icon;
	private int cached_icon_size=0;
	// We can use a SizedIcon everywhere we were using an Icon
	public Pixbuf SizedIcon {
		get {
			//Do not resize Stock Icons or not displayed icons
			if ((int) tag_icon_size == 0)
				return null;
			if ((int) tag_icon_size == cached_icon_size)
				return cached_icon;
			if (Math.Max (icon.Width, icon.Height) >= (int) tag_icon_size) {
				cached_icon = icon.ScaleSimple ((int) tag_icon_size, (int) tag_icon_size, InterpType.Bilinear);
				cached_icon_size = (int) tag_icon_size;
				return cached_icon;
			}
			else
				return icon;
		}	
	}


	// You are not supposed to invoke these constructors outside of the TagStore class.
	public Tag (Category category, uint id, string name)
		: base (id)
	{
		Category = category;
		Name = name;
	}


	// IComparer.
	public int CompareTo (object obj)
	{
		Tag tag = obj as Tag;

		if (Category == tag.Category) {
			if (SortPriority == tag.SortPriority)
				return Name.CompareTo (tag.Name);
			else
				return SortPriority - tag.SortPriority;
		} else {
			return Category.CompareTo (tag.Category);
		}
	}
	
	public bool IsAncestorOf (Tag tag)
	{
		for (Category parent = tag.Category; parent != null; parent = parent.Category) {
			if (parent == this)
				return true;
		}

		return false;
	}
}


// A Category is a Tag which has contains sub-Tags (we use the same terminology as Photoshop Album).
public class Category : Tag {
	ArrayList children;
	bool children_need_sort;
	public Tag [] Children {
		get {
			if (children_need_sort)
				children.Sort ();
			return (Tag []) children.ToArray (typeof (Tag));
		}
		set {
			children = new ArrayList (value);
			children_need_sort = true;
		}
	}

	// Appends all of this categories descendents to the list
	public void AddDescendentsTo (ArrayList list)
	{
		foreach (Tag tag in children) {
			if (! list.Contains (tag))
				list.Add (tag);

			if (! (tag is Category))
				continue;

			Category cat = tag as Category;

			cat.AddDescendentsTo (list);
		}
	}

	public void AddChild (Tag child)
	{
		children.Add (child);
		children_need_sort = true;
	}

	public void RemoveChild (Tag child)
	{
		children.Remove (child);
		children_need_sort = true;
	}

	public Category (Category category, uint id, string name)
		: base (category, id, name)
	{
		children = new ArrayList ();
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
			tag.StockIconName = icon_string.Substring (STOCK_ICON_DB_PREFIX.Length);
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
		ArrayList l = new ArrayList ();
		foreach (Tag t in this.item_cache.Values) {
			if (t.Name.ToLower ().StartsWith (s.ToLower ()))
				l.Add (t);
		}

		if (l.Count == 0)
			return null;

		return (Tag []) (l.ToArray (typeof (Tag)));
	}

	// In this store we keep all the items (i.e. the tags) in memory at all times.  This is
	// mostly to simplify handling of the parent relationship between tags, but it also makes it
	// a little bit faster.  We achieve this by passing "true" as the cache_is_immortal to our
	// base class.
	private void LoadAllTags ()
	{

		// Pass 1, get all the tags.

		SqliteDataReader reader = Database.Query("SELECT id, name, is_category, sort_priority, icon FROM tags");

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
				SetIconFromString (tag, reader [4].ToString ());

			tag.SortPriority = Convert.ToInt32 (reader[3]);
			AddToCache (tag);
		}

		reader.Close ();

		// Pass 2, set the parents.

		reader = Database.Query("SELECT id, category_id FROM tags");

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
		favorites_category.StockIconName = "f-spot-favorite.png";
		favorites_category.SortPriority = -10;
		Commit (favorites_category);

		Tag hidden_tag = CreateTag (RootCategory, Catalog.GetString ("Hidden"));
		hidden_tag.StockIconName = "f-spot-hidden.png";
		hidden_tag.SortPriority = -9;
		this.hidden = hidden_tag;
		Commit (hidden_tag);
		FSpot.Core.Database.Meta.HiddenTagId.ValueAsInt = (int) hidden_tag.Id;
		FSpot.Core.Database.Meta.Commit (FSpot.Core.Database.Meta.HiddenTagId);

		Tag people_category = CreateCategory (RootCategory, Catalog.GetString ("People"));
		people_category.StockIconName = "f-spot-people.png";
		people_category.SortPriority = -8;
		Commit (people_category);

		Tag places_category = CreateCategory (RootCategory, Catalog.GetString ("Places"));
		places_category.StockIconName = "f-spot-places.png";
		places_category.SortPriority = -8;
		Commit (places_category);

		Tag events_category = CreateCategory (RootCategory, Catalog.GetString ("Events"));
		events_category.StockIconName = "f-spot-events.png";
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
		if (tag.StockIconName != null)
			return STOCK_ICON_DB_PREFIX + tag.StockIconName;
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

		Database.ExecuteNonQuery (new DbCommand ("UPDATE tags SET name = :name, category_id = :category_id, "
                    + "is_category = :is_category, sort_priority = :sort_priority, icon = :icon WHERE id = :id",
						  "name", tag.Name,
						  "category_id", tag.Category.Id,
						  "is_category", tag is Category ? 1 : 0,
						  "sort_priority", tag.SortPriority,
						  "icon", GetIconString (tag),
						  "id", tag.Id));
		
		EmitChanged (tag);

		if (update_xmp && (bool)Preferences.Get(Preferences.METADATA_EMBED_IN_IMAGE)) {
			Photo [] photos = Core.Database.Photos.Query (new Tag [] { tag });
			foreach (Photo p in photos) {
				SyncMetadataJob.Create (Core.Database.Jobs, p);
			}
		}
	}



#if TEST_TAG_STORE

	private static void Dump (Category category, int indent)
	{
		foreach (Tag tag in category.Children) {
			for (int i = 0; i < indent; i ++)
				Console.Write ("\t");

			Console.Write (tag.Name);
			if (tag is Category)
				Console.Write (" (category)");
			Console.Write ("\n");

			if (tag is Category)
				Dump (tag as Category, indent + 1);
		}
	}

	static void Main (string [] args)
	{
		Program program = new Program ("TagStoreTest", "0.0", Modules.UI, args);

		const string path = "/tmp/TagStoreTest.db";

		try {
			File.Delete (path);
		} catch {}

		Db db = new Db (path, true);

		Category people_category = db.Tags.CreateCategory (null, "People");
		Tag anna_tag = db.Tags.CreateTag (people_category, "Anna");
		Tag ettore_tag = db.Tags.CreateTag (people_category, "Ettore");
		Tag miggy_tag = db.Tags.CreateTag (people_category, "Miggy");
		miggy_tag.SortPriority = -1;
		db.Tags.Commit (miggy_tag);

		Category places_category = db.Tags.CreateCategory (null, "Places");
		Tag milan_tag = db.Tags.CreateTag (places_category, "Milan");
		Tag boston_tag = db.Tags.CreateTag (places_category, "Boston");

		Category exotic_category = db.Tags.CreateCategory (places_category, "Exotic");
		Tag bengalore_tag = db.Tags.CreateTag (exotic_category, "Bengalore");
		Tag manila_tag = db.Tags.CreateTag (exotic_category, "Manila");
		Tag tokyo_tag = db.Tags.CreateTag (exotic_category, "Tokyo");

		tokyo_tag.Category = places_category;
		tokyo_tag.Name = "Paris";
		db.Tags.Commit (tokyo_tag);

		db.Dispose ();

		db = new Db (path, false);
		Dump (db.Tags.RootCategory, 0);
	}

#endif
}
