//
// MetaStore.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2020 Stephen Shaw
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
using System.Linq;

using FSpot.Models;

namespace FSpot.Database
{
	public class MetaStore : DbStore<Meta>
	{
		public const string Version = "F-Spot Version";
		public const string Hidden = "Hidden Tag Id";

		public Meta FSpotVersion {
			get { return GetByName (Version); }
		}

		public Meta HiddenTagId {
			get { return GetByName (Hidden); }
		}

		public MetaStore (IDb db) : base (db)
		{
		}

		Meta GetByName (string name)
		{
			using var context = new FSpotContext ();
			var item = context.Meta.Where (x => string.Equals (x.Name, name)).First ();

			if (item != null)
				return item;

			// Otherwise make it and return it
			return Create (name, null);
		}

		void CreateDefaultItems ()
		{
			//Create (version, FSpotConfiguration.Version);

			// Get the hidden tag id, if it exists
			string table = Database.Query<string> ("SELECT name FROM sqlite_master WHERE type='table' AND name='tags'");
			if (!string.IsNullOrEmpty (table)) {
				string id = Database.Query<string> ("SELECT id FROM tags WHERE name = 'Hidden'");
				Create (Hidden, id);
			}
		}

		void LoadAllItems ()
		{
			using var context = new FSpotContext ();
			var metaItems = context.Meta;

			if (FSpotVersion.Data != FSpotConfiguration.Version) {
				FSpotVersion.Data = FSpotConfiguration.Version;
				Commit (FSpotVersion);
			}
		}

		Meta Create (string name, string data)
		{
			using var context = new FSpotContext ();
			var item = new Meta { Name = name, Data = data };
			var result = context.Add (item);
			//uint id = (uint)Database.Execute (new HyenaSqliteCommand ("INSERT INTO meta (name, data) VALUES (?, ?)", name, data ?? "NULL"));

			context.SaveChanges ();
			//FIXME This smells bad. This line used to be *before* the
			//Command.executeNonQuery. It smells of a bug, but there might
			//have been a reason for this

			EmitAdded (item);

			return item;
		}

		public override void Commit (Meta item)
		{
			using var context = new FSpotContext ();
			context.Update (item);
			context.SaveChanges ();

			EmitChanged (item);
		}

		public override Meta Get (Guid id)
		{
			using var context = new FSpotContext ();
			var item = context.Meta.Find (id);
			return item;
		}

		public override void Remove (Meta item)
		{
			using var context = new FSpotContext ();
			context.Remove (item);

			EmitRemoved (item);
		}
	}
}
