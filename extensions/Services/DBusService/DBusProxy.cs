using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using FSpot;
using FSpot.Query;
using FSpot.Utils;
using NDesk.DBus;

namespace DBusService {
	public delegate void RemoteUp();
	public delegate void RemoteDown();

	public class DBusException : Exception {
		public DBusException (string message)
			: base (message)
		{}

		public DBusException (string message, params object [] format_params)
			: this (string.Format (message, format_params))
		{}
	}

	public class DBusProxy {
		public event RemoteUp RemoteUp;
		public event RemoteDown RemoteDown;

		internal void OnRemoteUp ()
		{
			if (RemoteUp != null)
				RemoteUp ();
		}

		internal void OnRemoteDown ()
		{
			if (RemoteDown != null)
				RemoteDown ();
		}
	}

	public class DBusProxyFactory {
		private const string SERVICE_PATH = "org.gnome.FSpot";
		private const string TAG_PROXY_PATH = "/org/gnome/FSpot/TagRemoteControl";
		private const string PHOTO_PROXY_PATH = "/org/gnome/FSpot/PhotoRemoteControl";

		private static TagProxy tag_remote;
		private static PhotoProxy photo_remote;

		public static void Load (Db db)
		{
			tag_remote = new TagProxy (db.Tags);
			Bus.Session.Register (SERVICE_PATH, new ObjectPath (TAG_PROXY_PATH), tag_remote);
			tag_remote.OnRemoteUp ();

			photo_remote = new PhotoProxy (db);
			Bus.Session.Register (SERVICE_PATH, new ObjectPath (PHOTO_PROXY_PATH), photo_remote);
			photo_remote.OnRemoteUp ();
		}

		public static void EmitRemoteDown ()
		{
			tag_remote.OnRemoteDown ();
			photo_remote.OnRemoteDown ();
		}

	}

	[Interface ("org.gnome.FSpot.TagRemoteControl")]
	public interface ITagRemoteControl {
		// info, included for backward compatibility with times
		// where this was embedded in f-spot core
		bool IsReadOnly ();

		// get all
		string[] GetTagNames ();
		uint[] GetTagIds ();

		// get info for one tag
		IDictionary<string, object> GetTagByName (string name);
		IDictionary<string, object> GetTagById (int id);

		event RemoteUp RemoteUp;
		event RemoteDown RemoteDown;

		// tag creators
		int CreateTag (string name);
		int CreateTagWithParent (string parent, string name);

		// tag removers
		bool RemoveTagByName (string name);
		bool RemoveTagById (int id);
	}

	// Class exposing all photos on the dbus
	public class TagProxy : DBusProxy, ITagRemoteControl {
		protected TagStore tag_store;

		public TagStore Store {
			get { return tag_store; }
		}

		internal TagProxy (TagStore store)
		{
			tag_store = store;
		}

		#region Interface methods
		public bool IsReadOnly () {
			return false;
		}

		public string[] GetTagNames ()
		{
			List<string> tags = new List<string> ();
			AddTagNameToList (tags, tag_store.RootCategory);

			return tags.ToArray ();
		}

		public uint[] GetTagIds ()
		{
			List<uint> tags = new List<uint> ();
			AddTagIdToList (tags, tag_store.RootCategory);

			return tags.ToArray ();
		}

		public IDictionary<string, object> GetTagByName (string name)
		{
			Tag t = tag_store.GetTagByName (name);

			if (t == null)
				throw new DBusException ("Tag with name {0} does not exist.", name);

			return CreateDictFromTag (t);
		}

		public IDictionary<string, object> GetTagById (int id)
		{
			Tag t = tag_store.GetTagById (id);

			if (t == null)
				throw new DBusException ("Tag with id {0} does not exist.", id);

			return CreateDictFromTag (t);
		}

		public int CreateTag (string name)
		{
			return CreateTagPriv (null, name);
		}

		public int CreateTagWithParent (string parent, string name)
		{
			Tag parent_tag = tag_store.GetTagByName (parent);

			if (!(parent_tag is Category))
				parent_tag = null;

			return CreateTagPriv (parent_tag as Category, name);
		}

		public bool RemoveTagByName (string name)
		{
			Tag tag = tag_store.GetTagByName (name);

			return RemoveTag (tag);
		}

		public bool RemoveTagById (int id)
		{
			Tag tag = tag_store.GetTagById (id);

			return RemoveTag (tag);
		}

		#endregion

		#region Helper methods
		private void AddTagNameToList (List<string> list, Tag tag)
		{
			if (tag != tag_store.RootCategory)
				list.Add (tag.Name);

			if (tag is Category) {
				foreach (Tag child in (tag as Category).Children)
					AddTagNameToList (list, child);
			}
		}

		private void AddTagIdToList (List<uint> list, Tag tag)
		{
			if (tag != tag_store.RootCategory)
				list.Add (tag.Id);

			if (tag is Category) {
				foreach (Tag child in (tag as Category).Children)
					AddTagIdToList (list, child);
			}
		}

		private IDictionary<string, object> CreateDictFromTag (Tag t)
		{
			Dictionary<string, object> result = new Dictionary<string, object> ();

			result.Add ("Id", t.Id);
			result.Add ("Name", t.Name);

			StringBuilder builder = new StringBuilder ();

			if (t is Category) {
				foreach (Tag child in (t as Category).Children) {
					if (builder.Length > 0)
						builder.Append (",");

					builder.Append (child.Name);
				}
			}

			result.Add ("Children", builder.ToString ());

			return result;
		}

		private int CreateTagPriv (Category parent_tag, string name)
		{
			try {
				Tag created = tag_store.CreateCategory (parent_tag, name, false);
				return (int)created.Id;
			}
			catch {
				throw new DBusException ("Failed to create tag.");
			}
		}

		private bool RemoveTag (Tag t)
		{
			if (t == null)
				return false;

			try {
				// remove tags from photos first
				Core.Database.Photos.Remove (new Tag [] { t });
				// then remove tag
				tag_store.Remove (t);
				return true;
			}
			catch {
				return false;
			}
		}
		#endregion
	}


	[Interface ("org.gnome.FSpot.PhotoRemoteControl")]
	public interface IPhotoRemoteControl {
		// info; included for backward compatibility
		// with previous version where this was embedded in f-spot
		bool IsReadOnly ();

		// get all
		uint[] GetPhotoIds ();

		// import prepare
		void PrepareRoll ();
		void FinishRoll ();

		// import
		int ImportPhoto (string path, bool copy, string []tags);

		// photo properties
		IDictionary<string, object> GetPhotoProperties (uint id);

		// photo remove
		void RemovePhoto (uint id);

		// query
		uint[] Query (string []tags);
		uint[] QueryByDate (long start_time, long end_time);

		// events
		event RemoteUp RemoteUp;
		event RemoteDown RemoteDown;
	}

	// Class exposing all photos on the dbus
	public class PhotoProxy : DBusProxy, IPhotoRemoteControl {
		protected Db db;
		private Roll current_roll;

		public Db Database {
			get { return db; }
		}

		internal PhotoProxy (Db db)
		{
			this.db = db;
		}

		public bool IsReadOnly ()
		{
			return false;
		}

		public uint[] GetPhotoIds ()
		{
			List<uint> ids = new List<uint> ();

			foreach (Photo p in QueryAll ())
				ids.Add (p.Id);

			return ids.ToArray ();
		}

		public IDictionary<string, object> GetPhotoProperties (uint id)
		{
			Dictionary<string, object> dict = new Dictionary<string, object> ();

			Photo p = db.Photos.Get (id) as Photo;

			if (p == null)
				throw new DBusException ("Photo with id {0} does not exist.", id);

			dict.Add ("Uri", p.DefaultVersionUri.ToString());
			dict.Add ("Id", p.Id);
			dict.Add ("Name", p.Name);
			dict.Add ("Description", p.Description ?? string.Empty);

			StringBuilder builder = new StringBuilder ();

			foreach (Tag t in p.Tags) {
				if (builder.Length > 0)
					builder.Append (",");

				builder.AppendFormat (t.Name);
			}

			dict.Add ("Tags", builder.ToString ());

			return dict;
		}

		public uint[] Query (string []tags)
		{
			List<Tag> tag_list = GetTagsByNames (tags);

			Photo []photos = db.Photos.Query (tag_list.ToArray ());

			uint []ids = new uint[photos.Length];

			for (int i = 0; i < ids.Length; i++)
				ids[i] = photos[i].Id;

			return ids;
		}

		public uint[] QueryByDate (long start_time, long end_time)
		{
			DateTime start = DbUtils.DateTimeFromUnixTime (start_time);
			DateTime end = DbUtils.DateTimeFromUnixTime (end_time);

			Photo []photos = db.Photos.Query (new DateRange (start, end));
			uint []ids = new uint[photos.Length];

			for (int i = 0; i < ids.Length; i++)
				ids[i] = photos[i].Id;

			return ids;
		}

		protected Photo[] QueryAll ()
		{
			return db.Photos.Query ((Tag [])null);
		}

		protected List<Tag> GetTagsByNames (string []names)
		{
			// add tags that exist in tag store
			List<Tag> tag_list = new List<Tag> ();

			foreach (string tag_name in names) {
				Tag t = db.Tags.GetTagByName (tag_name);

				if (t == null)
					continue;

				tag_list.Add (t);
			}

			return tag_list;
		}

		public void PrepareRoll ()
		{
			if (current_roll != null)
				return;

			current_roll = db.Rolls.Create ();
		}

		public void FinishRoll ()
		{
			current_roll = null;
		}

		public int ImportPhoto (string path, bool copy, string []tags)
		{
			if (current_roll == null)
				throw new DBusException ("You must use PrepareRoll before you can import a photo.");

			// add tags that exist in tag store
			List<Tag> tag_list = GetTagsByNames (tags);

			Gdk.Pixbuf pixbuf = null;

			// FIXME: this is more or less a copy of the file import backend code
			// this should be streamlined
			try {
				string new_path = path;

				if (copy)
					new_path = FileImportBackend.ChooseLocation (path);

				if (new_path != path)
					System.IO.File.Copy (path, new_path);

				Photo created = db.Photos.CreateOverDBus (new_path, path, current_roll.Id, out pixbuf);

				try {
					File.SetAttributes (new_path, File.GetAttributes (new_path) & ~FileAttributes.ReadOnly);
					DateTime create = File.GetCreationTime (path);
					File.SetCreationTime (new_path, create);
					DateTime mod = File.GetLastWriteTime (path);
					File.SetLastWriteTime (new_path, mod);
				} catch (IOException) {
					// we don't want an exception here to be fatal.
				}

				// attach tags we got
				if (tag_list.Count > 0) {
					created.AddTag (tag_list.ToArray ());
					db.Photos.Commit (created);
				}

				return (int)created.Id;
			// indicate failure
			} catch {
				throw new DBusException ("Failed to import the photo.");
			}
		}

		public void RemovePhoto (uint id)
		{
			Photo p = db.Photos.Get (id) as Photo;

			if (p == null)
				throw new DBusException ("Photo with id {0} does not exist.", id);

			db.Photos.RemoveOverDBus (p);
		}
	}
}
