//
// TagStore.cs
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
	public class InvalidFaceOperationException : InvalidOperationException {

		public InvalidFaceOperationException (Face face, string message) : base (message)
		{
			Face = face;
		}

		public Face Face { get; set; }
	}
	
	public class FaceStore : DbStore<Face> {

		private const string STOCK_ICON_DB_PREFIX = "stock_icon:";

		public Face GetFaceByName (string name)
		{
			foreach (Face face in this.item_cache.Values)
				if (face.Name.Equals (name, StringComparison.OrdinalIgnoreCase))
					return face;
	
			return null;
		}
	
		public Face GetFaceById (int id)
		{
			foreach (Face face in this.item_cache.Values)
				if (face.Id == id)
					return face;
			return null;
		}
	
		public Face [] GetFacesByNameStart (string s)
		{
			List <Face> faces = new List<Face> ();
			foreach (Face face in this.item_cache.Values) {
				if (face.Name.ToLower ().StartsWith (s.ToLower ()))
					faces.Add (face);
			}
	
			if (faces.Count == 0)
				return null;
	
			faces.Sort (delegate (Face f1, Face f2) { return f1.CompareTo (f2); });
	
			return faces.ToArray ();
		}

		public Face [] GetFacesByPhoto (Photo photo)
		{
			List <Face> faces = new List<Face> ();
			FaceLocationStore face_location_store = App.Instance.Database.FaceLocations;
			Dictionary<uint, FaceLocation> face_locations = face_location_store.GetFaceLocationsByPhoto (photo);
			foreach (FaceLocation face_location in face_locations.Values) {
				if (!item_cache.ContainsKey (face_location.FaceId))
					throw new Exception ("FaceLocation refers to nonexistent Face.");

				faces.Add ((Face) item_cache [face_location.FaceId]);
			}

			return faces.ToArray ();
		}

		public Face [] GetAll ()
		{
			List <Face> faces = new List<Face> ();
			foreach (Face face in item_cache.Values)
				faces.Add (face);
			return faces.ToArray ();
		}

		public ICollection<string> GetAllNames ()
		{
			ICollection<string> names = new List<string> ();
			foreach (Face face in item_cache.Values)
				names.Add (face.Name);
			return names;
		}

		// In this store we keep all the items (i.e. the faces) in memory at all times.  This is
		// mostly to make it a little bit faster.  We achieve this by passing "true" as the
		// cache_is_immortal to our base class.
		private void LoadAllFaces ()
		{
			IDataReader reader = Database.Query ("SELECT id, name FROM faces");
			
			while (reader.Read ()) {
				uint id = Convert.ToUInt32 (reader ["id"]);
				string name = reader ["name"].ToString ();

				AddToCache (new Face(id, name));
			}
			
			reader.Dispose ();
		}
	
		private void CreateTable ()
		{
			Database.Execute (
				"CREATE TABLE faces (\n" +
				"	id		INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, \n" +
				"	name		TEXT NOT NULL \n" +
				")");
		}
		
		// Constructor
		public FaceStore (FSpotDatabaseConnection database, bool is_new)
			: base (database, true)
		{
			if (is_new)
				CreateTable ();
			else
				LoadAllFaces ();
		}
	
		private uint InsertFaceIntoTable (string name)
		{

			int id = Database.Execute (new HyenaSqliteCommand ("INSERT INTO faces (name) " +
			                                                   "VALUES (?)",
			                                                   name));
			
			return (uint) id;
		}
	
		public Face CreateFace (string name)
		{
			Face face = GetFaceByName (name);
			if (face != null)
				return face;

			uint id = InsertFaceIntoTable (name);
	
			face = new Face (id, name);
			face.IconWasCleared = true;
	
			AddToCache (face);
			EmitAdded (face);
	
			return face;
		}
		
		public override Face Get (uint id)
		{
		    return LookupInCache (id);
		}

		public override void Remove (Face face)
		{	
			Remove (new Face [] {face});
		}

		public void Remove (Face [] faces)
		{
			ICollection<uint> face_ids = new List<uint> (faces.Length);
			foreach (Face face in faces) {
				face_ids.Add (face.Id);
				
				RemoveFromCache (face);
			}
			
			string command = String.Format ("DELETE FROM faces WHERE id IN ({0})",
			                                String.Join (", ", face_ids));
			Database.Execute (command);
			
			EmitRemoved (faces);
		}

		private string GetIconString (Face face)
		{
			if (face.Icon == null) {
				if (face.IconWasCleared)
					return String.Empty;
				return null;
			}
			
			return Convert.ToBase64String (GdkUtils.Serialize (face.Icon));
		}
		
		public override void Commit (Face face)
		{
			Commit (new Face[] {face});
		}
	
		public void Commit (Face [] faces)
		{
	
			// TODO.
			bool use_transactions = faces.Length > 1; //!Database.InTransaction && faces.Length > 1;
			
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
			
			foreach (Face face in faces) {
				Database.Execute (new HyenaSqliteCommand ("UPDATE faces SET name = ? " +
				                                          "WHERE id = ?",
				                                          face.Name,
				                                          face.Id));
			}
			
			if (use_transactions)
				Database.CommitTransaction ();
			
			EmitChanged (faces);
		}
	}
}
