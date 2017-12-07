//
// MetaStore.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006 Gabriel Burt
// Copyright (C) 2009-2010 Ruben Vermeersch
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

using FSpot.Core;
using FSpot.Settings;

using Hyena.Data.Sqlite;

namespace FSpot.Database
{
    public class MetaItem : DbItem {
    
    	public string Name { get; set; }
    	public string Value { get; set; }

    	public int ValueAsInt {
    		get { return System.Int32.Parse (Value); }
    		set { Value = value.ToString (); }
    	}
    
    	public MetaItem (uint id, string name, string data) : base (id)
    	{
    		Name = name;
    		Value = data;
    	}
    }

    public class MetaStore : DbStore<MetaItem> {
    	private const string version = "F-Spot Version";
    	private const string db_version = "F-Spot Database Version";
    	private const string hidden = "Hidden Tag Id";
    
    	public MetaItem FSpotVersion {
    		get { return GetByName (version); }
    	}
    
    	public MetaItem DatabaseVersion {
    		get { return GetByName (db_version); }
    	}
    
    	public MetaItem HiddenTagId {
    		get { return GetByName (hidden); }
    	}
    
    	private MetaItem GetByName (string name)
    	{
    		foreach (MetaItem i in item_cache.Values)
    			if (i.Name == name)
    				return i;
    
    		// Otherwise make it and return it
    		return Create (name, null);
    	}
    
    	private void CreateTable ()
    	{
    		Database.Execute (
    			"CREATE TABLE meta (\n" +
    			"	id	INTEGER PRIMARY KEY NOT NULL, \n" +
    			"	name	TEXT UNIQUE NOT NULL, \n" +
    			"	data	TEXT\n" +
    			")");
    	}
    
    	private void CreateDefaultItems (bool is_new)
    	{
    		Create (version, Defines.VERSION);
    		Create (db_version, (is_new) ? FSpot.Database.Updater.LatestVersion.ToString () : "0");
    
    		// Get the hidden tag id, if it exists
		string table = Database.Query<string> ("SELECT name FROM sqlite_master WHERE type='table' AND name='tags'");
		if (!string.IsNullOrEmpty(table)) {
    			string id = Database.Query<string> ("SELECT id FROM tags WHERE name = 'Hidden'");
    			Create (hidden, id);
		}
    	}
    
    	private void LoadAllItems ()
    	{
    		Hyena.Data.Sqlite.IDataReader reader = Database.Query("SELECT id, name, data FROM meta");
    
    		while (reader.Read ()) {
    			uint id = Convert.ToUInt32 (reader ["id"]);
    
    			string name = reader ["name"].ToString ();
    
    			string data = null;
    			if (reader ["data"] != null)
    				data = reader ["data"].ToString ();
    
    			MetaItem item = new MetaItem (id, name, data);
    
    			AddToCache (item);
    		}
    
    		reader.Dispose ();
    
    		if (FSpotVersion.Value != Defines.VERSION) {
    			FSpotVersion.Value = Defines.VERSION;
    			Commit (FSpotVersion);
    		}
    	}
    
    	private MetaItem Create (string name, string data)
    	{
    
    		uint id = (uint)Database.Execute(new HyenaSqliteCommand("INSERT INTO meta (name, data) VALUES (?, ?)", name, data ?? "NULL" ));
    
    		//FIXME This smells bad. This line used to be *before* the
    		//Command.executeNonQuery. It smells of a bug, but there might
    		//have been a reason for this
    
    		MetaItem item = new MetaItem (id, name, data);
    
    
    		AddToCache (item);
    		EmitAdded (item);
    
    		return item;
    	}
    
    	public override void Commit (MetaItem item)
    	{
    		Database.Execute(new HyenaSqliteCommand("UPDATE meta SET data = ? WHERE name = ?", item.Value, item.Name));
    
    		EmitChanged (item);
    	}
    
    	public override MetaItem Get (uint id)
    	{
    		return LookupInCache (id);
    	}
    
    	public override void Remove (MetaItem item)
    	{
    		RemoveFromCache (item);
    
    		Database.Execute (new HyenaSqliteCommand ("DELETE FROM meta WHERE id = ?", item.Id));
    
    		EmitRemoved (item);
    	}
    
    	// Constructor
    
    	public MetaStore (IDb db, bool is_new)
    		: base (db, true)
    	{
    		if (is_new || !Database.TableExists ("meta")) {
    			CreateTable ();
    			CreateDefaultItems (is_new);
    		} else
    			LoadAllItems ();
    	}
    }
}
