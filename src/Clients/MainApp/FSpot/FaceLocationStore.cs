//
// FaceLocationStore.cs
//
// Author:
//   Ettore Perazzoli <ettore@src.gnome.org>
//   Stephane Delcroix <stephane@delcroix.org>
//   Larry Ewing <lewing@novell.com>
//   Valentín Barros <valentin@sanva.net>
//
// Copyright (C) 2003-2009 Novell, Inc.
// Copyright (C) 2003 Ettore Perazzoli
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2004-2006 Larry Ewing
// Copyright (C) 2013 Valentín Barros
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

using Mono.Unix;

using System;
using System.Collections;
using System.Collections.Generic;

using FSpot;
using FSpot.Core;
using FSpot.Database;
using FSpot.Jobs;
using FSpot.Utils;

using Hyena;
using Hyena.Data.Sqlite;

namespace FSpot {
	public class FaceLocationStore : DbStore<FaceLocation> {
		private Dictionary<uint, Dictionary<uint, FaceLocation>> face_index;
		private Dictionary<uint, Dictionary<uint, FaceLocation>> photo_index;

		public Dictionary<uint, FaceLocation> GetFaceLocationsByFace (Face face)
		{
			if (!face_index.ContainsKey (face.Id))
				return new Dictionary<uint, FaceLocation> ();

			return face_index [face.Id];
		}

		public Dictionary<uint, FaceLocation> GetFaceLocationsByPhoto (Photo photo)
		{
			if (!photo_index.ContainsKey (photo.Id))
				return new Dictionary<uint, FaceLocation> ();

			return photo_index [photo.Id];
		}

		// In this store we keep all the items (i.e. the faces locations) in memory at all times.
		// This is mostly to make it a little bit faster.  We achieve this by passing "true" as the
		// cache_is_immortal to our base class.
		private void LoadAllFaceLocations ()
		{
			IDataReader reader = Database.Query ("SELECT id, face_id, photo_id, geometry " +
								"FROM face_locations");
			
			while (reader.Read ()) {
				uint id = Convert.ToUInt32 (reader ["id"]);
				uint face_id = Convert.ToUInt32 (reader ["face_id"]);
				uint photo_id = Convert.ToUInt32 (reader ["photo_id"]);
				string geometry = reader ["geometry"].ToString ();

				AddToCache (new FaceLocation(id, face_id, photo_id, geometry));
			}
			
			reader.Dispose ();
		}
	
		private void CreateTable ()
		{
			Database.Execute (
				"CREATE TABLE face_locations (\n" +
				"	id		INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, \n" +
				"	face_id		INTEGER NOT NULL, \n" +
				"	photo_id	INTEGER NOT NULL, \n" +
				"	geometry	TEXT NOT NULL \n" +
				")");
		}
		
		// Constructor
		public FaceLocationStore (FSpotDatabaseConnection database, bool is_new)
			: base (database, true)
		{
			face_index = new Dictionary<uint, Dictionary<uint, FaceLocation>> ();
			photo_index = new Dictionary<uint, Dictionary<uint, FaceLocation>> ();

			if (is_new)
				CreateTable ();
			else
				LoadAllFaceLocations ();
		}
	
		private uint InsertFaceLocationIntoTable (uint face_id, uint photo_id, string geometry)
		{
			int id = Database.Execute (new HyenaSqliteCommand ("INSERT INTO face_locations (face_id, photo_id, geometry) " +
			                                                   "VALUES (?, ?, ?)",
			                                                   face_id,
			                                                   photo_id,
			                                                   geometry));
			
			return (uint) id;
		}
	
		public FaceLocation CreateFaceLocation (uint face_id, uint photo_id, string geometry)
		{
			FaceLocation face_location;

			if (face_index.ContainsKey (face_id) && face_index [face_id].ContainsKey (photo_id)) {
				face_location = face_index [face_id] [photo_id];

				if (!face_location.Geometry.Equals (geometry, StringComparison.Ordinal)) {
					face_location.Geometry = geometry;
					Commit (face_location);
				}

				return face_location;
			}
			
			uint id = InsertFaceLocationIntoTable (face_id, photo_id, geometry);
	
			face_location = new FaceLocation (id, face_id, photo_id, geometry);

			AddToCache (face_location);
			EmitAdded (face_location);
	
			return face_location;
		}
		
		public override FaceLocation Get (uint id)
		{
		    return LookupInCache (id);
		}

		public override void Remove (FaceLocation face_location)
		{	
			Remove (new FaceLocation [] {face_location});
		}

		public void Remove (FaceLocation [] face_locations)
		{
			ICollection<uint> face_location_ids = new List<uint> (face_locations.Length);
			foreach (FaceLocation face_location in face_locations) {
				face_location_ids.Add (face_location.Id);

				RemoveFromCache (face_location);
			}

			string command = String.Format ("DELETE FROM face_locations WHERE id IN ({0})",
			                                String.Join (", ", face_location_ids));
			Database.Execute (command);

			EmitRemoved (face_locations);
		}
		
		public override void Commit (FaceLocation face_location)
		{
			Commit (new FaceLocation[] {face_location});
		}
	
		public void Commit (FaceLocation [] face_locations)
		{
	
			// TODO.
			bool use_transactions = face_locations.Length > 1; //!Database.InTransaction && face_locations.Length > 1;
			
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
			
			foreach (FaceLocation face_location in face_locations) {
				Database.Execute (new HyenaSqliteCommand ("UPDATE face_locations SET " +
									"face_id = ?, photo_id = ?, geometry = ? " +
									"WHERE id = ?",
									face_location.FaceId,
									face_location.PhotoId,
									face_location.Geometry,
									face_location.Id));
			}
			
			if (use_transactions)
				Database.CommitTransaction ();
			
			EmitChanged (face_locations);
		}

		protected new void AddToCache (FaceLocation face_location)
		{
			if (!face_index.ContainsKey (face_location.FaceId))
				face_index.Add (face_location.FaceId, new Dictionary<uint, FaceLocation> ());
			face_index [face_location.FaceId].Add (face_location.PhotoId, face_location);

			if (!photo_index.ContainsKey (face_location.PhotoId))
				photo_index.Add (face_location.PhotoId, new Dictionary<uint, FaceLocation> ());
			photo_index [face_location.PhotoId].Add (face_location.FaceId, face_location);

			base.AddToCache (face_location);
		}

		protected new void RemoveFromCache (FaceLocation face_location)
		{
			if (face_index.ContainsKey (face_location.FaceId)) {
				face_index [face_location.FaceId].Remove (face_location.PhotoId);

				if (face_index [face_location.FaceId].Count == 0)
					face_index.Remove (face_location.FaceId);
			}

			if (photo_index.ContainsKey (face_location.PhotoId)) {
				photo_index [face_location.PhotoId].Remove (face_location.FaceId);

				if (photo_index [face_location.PhotoId].Count == 0)
					photo_index.Remove (face_location.PhotoId);
			}

			base.RemoveFromCache (face_location);
		}
	}
}
