using Gdk;
using Gnome;
using Gtk;
using Mono.Data.SqliteClient;
using System.Collections;
using System.IO;
using System;


// FIXME: This is to workaround the currently busted GTK# bindings.
using System.Runtime.InteropServices;

public class PixbufSerializer {
	[DllImport("libgdk_pixbuf-2.0-0.dll")]
	static extern unsafe bool gdk_pixdata_deserialize(ref Gdk.Pixdata raw, uint stream_length, byte [] stream, out IntPtr error);

	public static unsafe Pixbuf Deserialize (byte [] data)
	{
		Pixdata pixdata = new Pixdata ();

		IntPtr error = IntPtr.Zero;
		bool raw_ret = gdk_pixdata_deserialize (ref pixdata, (uint) data.Length, data, out error);
		bool ret = raw_ret;

		if (error != IntPtr.Zero)
			throw new GLib.GException (error);

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
		}
		get {
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
		if (icon_string == null || icon_string == "")
			tag.Icon = null;
		else if (icon_string.StartsWith (STOCK_ICON_DB_PREFIX))
			tag.StockIconName = icon_string.Substring (STOCK_ICON_DB_PREFIX.Length);
		else
			tag.Icon = PixbufSerializer.Deserialize (Convert.FromBase64String (icon_string));
	}

	// In this store we keep all the items (i.e. the tags) in memory at all times.  This is
	// mostly to simplify handling of the parent relationship between tags, but it also makes it
	// a little bit faster.  We achieve this by passing "true" as the cache_is_immortal to our
	// base class.
	private void LoadAllTags ()
	{
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		// Pass 1, get all the tags.

		command.CommandText = "SELECT id, name, is_category, sort_priority, icon FROM tags";
		SqliteDataReader reader = command.ExecuteReader ();

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
		command.Dispose ();

		// Pass 2, set the parents.

		command.CommandText = "SELECT id, category_id FROM tags";
		reader = command.ExecuteReader ();

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
		command.Dispose ();
	}


	private void CreateTable ()
	{
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText =
			"CREATE TABLE tags (                               " +
			"	id            INTEGER PRIMARY KEY NOT NULL," +
			"       name          TEXT UNIQUE,                 " +
			"       category_id   INTEGER,			   " +
			"       is_category   BOOLEAN,			   " +
			"       sort_priority INTEGER,			   " +
			"       icon          TEXT			   " +
			")";

		command.ExecuteNonQuery ();
		command.Dispose ();
	}


	private void CreateDefaultTags ()
	{
		Tag favorites_tag = CreateTag (RootCategory, "Favorites");
		favorites_tag.StockIconName = "f-spot-favorite.png";
		favorites_tag.SortPriority = -10;
		Commit (favorites_tag);

		Tag hidden_tag = CreateTag (RootCategory, "Hidden");
		hidden_tag.StockIconName = "f-spot-hidden.png";
		hidden_tag.SortPriority = -9;
		Commit (hidden_tag);

		Tag people_category = CreateCategory (RootCategory, "People");
		people_category.StockIconName = "f-spot-people.png";
		people_category.SortPriority = -8;
		Commit (people_category);

		Tag places_category = CreateCategory (RootCategory, "Places");
		places_category.StockIconName = "f-spot-places.png";
		places_category.SortPriority = -8;
		Commit (places_category);

		Tag events_category = CreateCategory (RootCategory, "Events");
		events_category.StockIconName = "f-spot-events.png";
		events_category.SortPriority = -7;
		Commit (events_category);

		Tag other_category = CreateCategory (RootCategory, "Other");
		other_category.StockIconName = "f-spot-other.png";
		other_category.SortPriority = -6;
		Commit (other_category);
	}


	// Constructor

	public TagStore (SqliteConnection connection, bool is_new)
		: base (connection, true)
	{
		root_category = new Category (null, 0, "root");

		if (! is_new) {
			LoadAllTags ();
		} else {
			CreateTable ();
			CreateDefaultTags ();
		}
	}

	private uint InsertTagIntoTable (Category parent_category, string name, bool is_category)
	{
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		uint parent_category_id = parent_category.Id;

		command.CommandText = String.Format
			("INSERT INTO tags (name, category_id, is_category, sort_priority) " +
			 "            VALUES ('{0}', {1}, {2}, 0)                          ",
			 SqlString (name),
			 parent_category_id,
			 is_category ? 1 : 0);

		command.ExecuteScalar ();
		command.Dispose ();

		return (uint) Connection.LastInsertRowId;
	}

	public Tag CreateTag (Category category, string name)
	{
		if (category == null)
			category = RootCategory;

		uint id = InsertTagIntoTable (category, name, false);

		Tag tag = new Tag (category, id, name);
		AddToCache (tag);

		return tag;
	}

	public Category CreateCategory (Category parent_category, string name)
	{
		if (parent_category == null)
			parent_category = RootCategory;

		uint id = InsertTagIntoTable (parent_category, name, true);

		Category new_category = new Category (parent_category, id, name);
		AddToCache (new_category);

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
		RemoveFromCache (item);
		
		((Tag)item).Category = null;
		
		// FIXME this should either throw an exception or make one
		// command, not recurse.
		if (item is Category) {
			Category category = (Category)item;
			foreach (Tag tag in category.Children)
				Remove (tag);
		}

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("DELETE FROM tags WHERE id = {0}", item.Id);
		command.ExecuteNonQuery ();

		command.Dispose ();
	}


	private string GetIconString (Tag tag)
	{
		if (tag.StockIconName != null)
			return STOCK_ICON_DB_PREFIX + tag.StockIconName;
		if (tag.Icon == null)
			return "";

		byte [] data = PixbufSerializer.Serialize (tag.Icon);
		return Convert.ToBase64String (data);
	}

	public override void Commit (DbItem item)
	{
		Tag tag = item as Tag;

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("UPDATE tags SET          " +
						     "    name = '{0}',        " +
						     "    category_id = {1},   " +
						     "    is_category = {2},   " +
						     "    sort_priority = {3}, " +
						     "    icon = '{4}'	       " +
						     "WHERE id = {5}           ",
						     SqlString (tag.Name),
						     tag.Category.Id,
						     tag is Category ? 1 : 0,
						     tag.SortPriority,
						     SqlString (GetIconString (tag)),
						     tag.Id);
		command.ExecuteNonQuery ();

		command.Dispose ();
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
