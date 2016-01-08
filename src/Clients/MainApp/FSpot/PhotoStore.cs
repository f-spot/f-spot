//
// PhotoStore.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephen Shaw <sshaw@decriptor.com>
//   Mike Gemünde <mike@gemuende.de>
//   Larry Ewing <lewing@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2014 Daniel Köb
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2009-2010 Mike Gemünde
// Copyright (C) 2004-2007 Larry Ewing
// Copyright (C) 2008-2010 Ruben Vermeersch
// Copyright (C) 2006-2009 Stephane Delcroix
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FSpot;
using FSpot.Core;
using FSpot.Database;
using FSpot.Jobs;
using FSpot.Query;

using Hyena;
using Hyena.Data.Sqlite;
using System.Data;
using FSpot.Utils;

namespace FSpot {
        public class PhotoStore : DbStore<Photo> {
         public int TotalPhotos {
                 get {
                         Hyena.Data.Sqlite.IDataReader reader = Database.Query("SELECT COUNT(*) AS photo_count FROM photos");
                         reader.Read ();
                         int total = Convert.ToInt32 (reader ["photo_count"]);
                         reader.Dispose ();
                         return total;
                 }
         }
        
         // FIXME this is a hack.  Since we don't have Gnome.ThumbnailFactory.SaveThumbnail() in
         // GTK#, and generate them by ourselves directly with Gdk.Pixbuf, we have to make sure here
         // that the "large" thumbnail directory exists.
         private static void EnsureThumbnailDirectory ()
         {
                 string large_thumbnail_file_name_template = Gnome.Thumbnail.PathForUri ("file:///boo", Gnome.ThumbnailSize.Large);
                 string large_thumbnail_directory_path = System.IO.Path.GetDirectoryName (large_thumbnail_file_name_template);
        
                 if (! System.IO.File.Exists (large_thumbnail_directory_path))
                         System.IO.Directory.CreateDirectory (large_thumbnail_directory_path);
         }

         // Constructor
         public PhotoStore (FSpotDatabaseConnection database, bool is_new)
                 : base (database, false)
         {
                 EnsureThumbnailDirectory ();
        
                 if (! is_new)
                         return;
        
                 Database.Execute (
                         "CREATE TABLE photos (\n" +
                         "       id                      INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, \n" +
                         "       time                    INTEGER NOT NULL, \n" +
                         "       base_uri                STRING NOT NULL, \n" +
                     "   filename                STRING NOT NULL, \n" +
                         "       description             TEXT NOT NULL, \n" +
                         "       roll_id                 INTEGER NOT NULL, \n" +
                         "       default_version_id      INTEGER NOT NULL, \n" +
                         "       rating                  INTEGER NULL \n" +
                         ")");
        
                 Database.Execute (
                         "CREATE TABLE photo_tags (\n" +
                         "       photo_id        INTEGER, \n" +
                         "       tag_id          INTEGER, \n" +
                         "       UNIQUE (photo_id, tag_id)\n" +
                         ")");
        
                 Database.Execute (
                         "CREATE TABLE photo_versions (\n"+
                         "       photo_id        INTEGER, \n" +
                         "       version_id      INTEGER, \n" +
                         "       name            STRING, \n" +
                         "       base_uri                STRING NOT NULL, \n" +
                     "   filename                STRING NOT NULL, \n" +
                         "       import_md5              TEXT NULL, \n" +
                         "       protected       BOOLEAN, \n" +
                         "       UNIQUE (photo_id, version_id)\n" +
                         ")");
        
                 Database.Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");
                 Database.Execute ("CREATE INDEX idx_photo_versions_import_md5 ON photo_versions(import_md5)");
                 Database.Execute ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");
         }
        
         public bool HasDuplicate (IPhoto item) {
                 var uri = item.DefaultVersion.Uri;
        
                 // Check if the exact given uri already exists.
                 var query = "SELECT COUNT(*) AS count FROM photo_versions WHERE base_uri = ? AND filename = ?";
                 var reader = Database.Query (new HyenaSqliteCommand (query, uri.GetBaseUri ().ToString (), uri.GetFilename ()));
                 reader.Read ();
                 int count = Convert.ToInt32 (reader ["count"]);
                 reader.Dispose();
                 if (count > 0)
                         return true;
        
                 // Check by MD5. Won't import if there are photos with the same ImportMD5.
                 string hash = item.DefaultVersion.ImportMD5;
                 var condition = new ConditionWrapper (String.Format ("import_md5 = \"{0}\"", hash));
                 var dupes_by_hash = Count ("photo_versions", condition);
                 if (dupes_by_hash > 0)
                         return true;
        
                 // This is a very lame check to overcome the lack of duplicate detect data right after transition.
                 //
                 // Does filename matching if there are files with no hash for the original version.
                 condition = new ConditionWrapper ("version_id = 1 AND (import_md5 = \"\" OR import_md5 IS NULL)");
                 var have_no_hashes = Count ("photo_versions", condition);
                 if (have_no_hashes > 0) {
                         var name = uri.GetFilename ();
                         DateTime? time = null;
        
                         // Look for a filename match.
                         reader = Database.Query (new HyenaSqliteCommand ("SELECT photos.id, photos.time, pv.filename FROM photos LEFT JOIN photo_versions AS pv ON pv.photo_id = photos.id WHERE pv.filename = ?", name));
                         while (reader.Read ()) {
                                 Log.DebugFormat ("Found one possible duplicate for {0}", reader["filename"].ToString ());
                                 if (!time.HasValue) {
                                         // Only read time when needed
                                         time = item.Time;
                                 }
        
                                 if (reader["time"].ToString () == DateTimeUtil.FromDateTime (time.Value).ToString ()) {
                                         Log.Debug ("Skipping duplicate", uri);
        
                                         // Schedule a hash calculation job on the existing file.
                                         CalculateHashJob.Create (FSpot.App.Instance.Database.Jobs, Convert.ToUInt32 (reader["id"]));
        
                                         return true;
                                 }
                         }
                         reader.Dispose ();
                 }
        
                 // No matches
                 return false;
         }
        
         public Photo CreateFrom (IPhoto item, bool defaultVersionOnly, uint roll_id)
         {
                 Photo photo;
        
                 long unix_time = DateTimeUtil.FromDateTime (item.Time);
                 string description = item.Description ?? String.Empty;
        
                 uint id = (uint) Database.Execute (
                         new HyenaSqliteCommand (
                                 "INSERT INTO photos (time, base_uri, filename, description, roll_id, default_version_id, rating) "      +
                                 "VALUES (?, ?, ?, ?, ?, ?, ?)",
                                 unix_time,
                                 item.DefaultVersion.BaseUri.ToString (),
                                 item.DefaultVersion.Filename,
                                 description,
                                 roll_id,
                                 Photo.OriginalVersionId,
                                 "0"
                         )
                 );
        
                 photo = new Photo (id, unix_time);

                 uint versionId = Photo.OriginalVersionId;
                 IEnumerable<IPhotoVersion> versions = defaultVersionOnly ? new[] { item.DefaultVersion } : item.Versions;
                 foreach (IPhotoVersion version in versions) {
                         photo.AddVersionUnsafely (versionId++, version.BaseUri, version.Filename, version.ImportMD5, version.Name, true);
                         InsertVersion (photo, photo.Versions.Last () as PhotoVersion);
                 }
                 photo.DefaultVersionId = versionId - 1;
                 photo.AllVersionsLoaded = true;
        
                 EmitAdded (photo);
                 return photo;
         }
        
         private void InsertVersion (Photo photo, PhotoVersion version)
         {
                 Database.Execute (new HyenaSqliteCommand (
                         "INSERT OR IGNORE INTO photo_versions (photo_id, version_id, name, base_uri, filename, protected, import_md5) " +
                         "VALUES (?, ?, ?, ?, ?, ?, ?)",
                         photo.Id,
                         version.VersionId,
                         version.Name,
                         version.BaseUri.ToString (),
                         version.Filename,
                         version.IsProtected,
                         (version.ImportMD5 != String.Empty ? version.ImportMD5 : null)));
         }
        
        
         private void GetVersions (Photo photo)
         {
                 Hyena.Data.Sqlite.IDataReader reader = Database.Query(
                         new HyenaSqliteCommand("SELECT version_id, name, base_uri, filename, import_md5, protected " +
                                       "FROM photo_versions " +
                                       "WHERE photo_id = ?",
                                       photo.Id
                         )
                 );
        
                 while (reader.Read ()) {
                         uint version_id = Convert.ToUInt32 (reader ["version_id"]);
                         string name = reader["name"].ToString ();
                         var base_uri = new SafeUri (reader ["base_uri"].ToString (), true);
                         var filename = reader ["filename"].ToString ();
                         string import_md5 = reader["import_md5"] != null ? reader ["import_md5"].ToString () : null;
                         bool is_protected = Convert.ToBoolean (reader["protected"]);
        
                         photo.AddVersionUnsafely (version_id, base_uri, filename, import_md5, name, is_protected);
                 }
                 reader.Dispose();
         }
        
         private void GetTags (Photo photo)
         {
                 Hyena.Data.Sqlite.IDataReader reader = Database.Query(new HyenaSqliteCommand("SELECT tag_id FROM photo_tags WHERE photo_id = ?", photo.Id));
        
                 while (reader.Read ()) {
                         uint tag_id = Convert.ToUInt32 (reader ["tag_id"]);
                         Tag tag = App.Instance.Database.Tags.Get (tag_id);
                         photo.AddTagUnsafely (tag);
                 }
                 reader.Dispose();
         }
        
         private void GetAllVersions  (string ids) {
                 Hyena.Data.Sqlite.IDataReader reader = Database.Query ("SELECT photo_id, version_id, name, base_uri, filename, import_md5, protected FROM photo_versions WHERE photo_id IN " + ids);
        
                 while (reader.Read ()) {
                         uint id = Convert.ToUInt32 (reader ["photo_id"]);
                         Photo photo = LookupInCache (id);
        
                         if (photo == null) {
                                 //Console.WriteLine ("Photo {0} not found", id);
                                 continue;
                         }
        
                         if (photo.AllVersionsLoaded) {
                                 //Console.WriteLine ("Photo {0} already Loaded", photo);
                                 continue;
                         }
        
                         if (reader ["version_id"] != null) {
                                 uint version_id = Convert.ToUInt32 (reader ["version_id"]);
                                 string name = reader["name"].ToString ();
                                 var base_uri = new SafeUri (reader ["base_uri"].ToString (), true);
                                 var filename = reader ["filename"].ToString ();
                                 string import_md5 = reader["import_md5"] != null ? reader ["import_md5"].ToString () : null;
                                 bool is_protected = Convert.ToBoolean (reader["protected"]);
        
                                 photo.AddVersionUnsafely (version_id, base_uri, filename, import_md5, name, is_protected);
                         }
        
                         /*
                         string directory_path = null;
                         if (reader [3] != null)
                                 directory_path = reader [3].ToString ();
                         System.Console.WriteLine ("directory_path = {0}", directory_path);
                         */
                 }
                 reader.Dispose();
         }
        
         private void GetAllTags (string ids) {
                 Hyena.Data.Sqlite.IDataReader reader = Database.Query ("SELECT photo_id, tag_id FROM photo_tags WHERE photo_id IN " + ids);
        
                 while (reader.Read ()) {
                         uint id = Convert.ToUInt32 (reader ["photo_id"]);
                         Photo photo = LookupInCache (id);
        
                         if (photo == null) {
                                 //Console.WriteLine ("Photo {0} not found", id);
                                 continue;
                         }
        
                         if (photo.AllVersionsLoaded) {
                                 //Console.WriteLine ("Photo {0} already Loaded", photo.Id);
                                 continue;
                         }
        
                         if (reader [1] != null) {
                                 uint tag_id = Convert.ToUInt32 (reader ["tag_id"]);
                                 Tag tag = App.Instance.Database.Tags.Get (tag_id);
                                 photo.AddTagUnsafely (tag);
                         }
                 }
                 reader.Dispose();
         }
        
         public override Photo Get (uint id)
         {
                 Photo photo = LookupInCache (id);
                 if (photo != null)
                         return photo;
        
                 Hyena.Data.Sqlite.IDataReader reader = Database.Query(
                         new HyenaSqliteCommand("SELECT time, description, roll_id, default_version_id, rating " +
                                       "FROM photos " +
                                       "WHERE id = ?", id
                                      )
                 );
        
                 if (reader.Read ()) {
                         photo = new Photo (id, Convert.ToInt64 (reader ["time"]));
                         photo.Description = reader["description"].ToString ();
                         photo.RollId = Convert.ToUInt32 (reader["roll_id"]);
                         photo.DefaultVersionId = Convert.ToUInt32 (reader["default_version_id"]);
                         photo.Rating = Convert.ToUInt32 (reader ["rating"]);
                         AddToCache (photo);
                 }
                 reader.Dispose();
        
                 if (photo == null)
                         return null;
        
                 GetTags (photo);
                 GetVersions (photo);
        
                 return photo;
         }
        
         public Photo GetByUri (SafeUri uri)
         {
                 Photo photo = null;
        
                 var base_uri = uri.GetBaseUri ();
                 var filename = uri.GetFilename ();
        
                 Hyena.Data.Sqlite.IDataReader reader =
                         Database.Query (new HyenaSqliteCommand ("SELECT id, time, description, roll_id, default_version_id, rating " +
                                                        " FROM photos " +
                                                        " LEFT JOIN photo_versions AS pv ON photos.id = pv.photo_id" +
                                                        " WHERE (photos.base_uri = ? AND photos.filename = ?)" +
                                                        " OR (pv.base_uri = ? AND pv.filename = ?)",
                                                        base_uri.ToString (), filename,
                                                        base_uri.ToString (), filename));
        
                 if (reader.Read ()) {
                         photo = new Photo (Convert.ToUInt32 (reader ["id"]),
                                            Convert.ToInt64 (reader ["time"]));
        
                         photo.Description = reader["description"].ToString ();
                         photo.RollId = Convert.ToUInt32 (reader["roll_id"]);
                         photo.DefaultVersionId = Convert.ToUInt32 (reader["default_version_id"]);
                         photo.Rating = Convert.ToUInt32 (reader ["rating"]);
                 }
        
                 reader.Dispose();
        
                 if (photo == null)
                         return null;
        
                 Photo cached = LookupInCache (photo.Id);
        
                 if (cached != null)
                         return cached;
        
                 AddToCache (photo);
        
                 GetTags (photo);
                 GetVersions (photo);
        
                 return photo;
         }
        
         public void Remove (Tag []tags)
         {
                 Photo [] photos = Query (tags, String.Empty, null, null);
        
                 foreach (Photo photo in photos)
                         photo.RemoveCategory (tags);
                 Commit (photos);
        
                 foreach (Tag tag in tags)
                         App.Instance.Database.Tags.Remove (tag);
        
         }
        
         public void Remove (Photo []items)
         {
                 EmitRemoved (items);
        
                 List<string> query_builder = new List<string> (items.Length);
                 for (int i = 0; i < items.Length; i++) {
                         query_builder.Add (String.Format ("{0}", items[i].Id));
                         RemoveFromCache (items[i]);
                 }
        
                 String id_list = String.Join ("','", query_builder.ToArray ());
                 Database.Execute (String.Format ("DELETE FROM photos WHERE id IN ('{0}')", id_list));
                 Database.Execute (String.Format ("DELETE FROM photo_tags WHERE photo_id IN ('{0}')", id_list));
                 Database.Execute (String.Format ("DELETE FROM photo_versions WHERE photo_id IN ('{0}')", id_list));
        
         }
        
         public override void Remove (Photo item)
         {
                 Remove (new Photo [] { (Photo)item });
         }
        
         public override void Commit (Photo item)
         {
                 Commit (new Photo [] {item});
         }
        
         public void Commit (Photo [] items)
         {
                 uint timer = Log.DebugTimerStart ();
                 // Only use a transaction for multiple saves. Avoids recursive transactions.
        
                 // TODO.
                 bool use_transactions = true; //!Database.InTransaction && items.Length > 1;
        
                 //if (use_transactions)
                 //      Database.BeginTransaction ();
        
                 // FIXME: this hack is used, because HyenaSqliteConnection does not support
                 // the InTransaction propery
                 try {
                         Database.BeginTransaction ();
                 } catch {
                         use_transactions = false;
                 }
        
                 PhotosChanges changes = new PhotosChanges ();
                 foreach (DbItem item in items)
                         changes |= Update ((Photo)item);
        
                 if (use_transactions)
                         Database.CommitTransaction ();
        
                 EmitChanged (items, new PhotoEventArgs (items, changes));
                 Log.DebugTimerPrint (timer, "Commit took {0}");
         }
        
         private PhotoChanges Update (Photo photo) {
                 PhotoChanges changes = photo.Changes;
                 // Update photo.
                 if (changes.DescriptionChanged || changes.DefaultVersionIdChanged || changes.TimeChanged || changes.UriChanged || changes.RatingChanged || changes.MD5SumChanged )
        
                         Database.Execute (
                                 new HyenaSqliteCommand (
                                         "UPDATE photos " +
                                         "SET description = ?, " +
                                         "    default_version_id = ?, " +
                                         "    time = ?, " +
                                         "    base_uri = ?, " +
                                         "    filename = ?, " +
                                         "    rating = ? " +
                                         "WHERE id = ? ",
                                         photo.Description,
                                         photo.DefaultVersionId,
                                         DateTimeUtil.FromDateTime (photo.Time),
                                         photo.VersionUri (Photo.OriginalVersionId).GetBaseUri ().ToString (),
                                         photo.VersionUri (Photo.OriginalVersionId).GetFilename (),
                                         String.Format ("{0}", photo.Rating),
                                         photo.Id
                                 )
                         );
        
                 // Update tags.
                 if (changes.TagsRemoved != null)
                         foreach (Tag tag in changes.TagsRemoved)
                                 Database.Execute (new HyenaSqliteCommand (
                                         "DELETE FROM photo_tags WHERE photo_id = ? AND tag_id = ?",
                                         photo.Id,
                                         tag.Id));
        
                 if (changes.TagsAdded != null)
                         foreach (Tag tag in changes.TagsAdded)
                                 Database.Execute (new HyenaSqliteCommand (
                                         "INSERT OR IGNORE INTO photo_tags (photo_id, tag_id) " +
                                         "VALUES (?, ?)",
                                         photo.Id,
                                         tag.Id));
        
                 // Update versions.
                 if (changes.VersionsRemoved != null)
                         foreach (uint version_id in changes.VersionsRemoved)
                                 Database.Execute (new HyenaSqliteCommand (
                                         "DELETE FROM photo_versions WHERE photo_id = ? AND version_id = ?",
                                         photo.Id,
                                         version_id));
        
                 if (changes.VersionsAdded != null)
                         foreach (uint version_id in changes.VersionsAdded) {
                                 PhotoVersion version = photo.GetVersion (version_id);
                                 InsertVersion (photo, version);
                         }
                 if (changes.VersionsModified != null)
                         foreach (uint version_id in changes.VersionsModified) {
                                 PhotoVersion version = photo.GetVersion (version_id);
                                 Database.Execute (new HyenaSqliteCommand (
                                         "UPDATE photo_versions SET name = ?, " +
                                         "base_uri = ?, filename = ?, protected = ?, import_md5 = ? " +
                                         "WHERE photo_id = ? AND version_id = ?",
                                         version.Name,
                                         version.BaseUri.ToString (),
                                         version.Filename,
                                         version.IsProtected,
                                         (version.ImportMD5 != String.Empty ? version.ImportMD5 : null),
                                         photo.Id,
                                         version_id));
                         }
                 photo.Changes = null;
                 return changes;
         }
        
         public void CalculateMD5Sum (Photo photo) {
                 foreach (uint version_id in photo.VersionIds) {
                         PhotoVersion version = photo.GetVersion (version_id);
        
                         // Don't overwrite MD5 sums that are already calculated.
                         if (version.ImportMD5 != String.Empty && version.ImportMD5 != null)
                                 continue;
        
                         string version_md5_sum = HashUtils.GenerateMD5 (version.Uri);
                         version.ImportMD5 = version_md5_sum;
                         photo.Changes.ChangeVersion (version_id);
                 }
        
                 Commit (photo);
         }
        
         public int Count (string table_name, params IQueryCondition [] conditions)
         {
                 StringBuilder query_builder = new StringBuilder ("SELECT COUNT(*) AS count FROM " + table_name + " ");
                 bool where_added = false;
                 foreach (IQueryCondition condition in conditions) {
                         if (condition == null)
                                 continue;
                         if (condition is IOrderCondition)
                                 continue;
                         query_builder.Append (where_added ? " AND " : " WHERE ");
                         query_builder.Append (condition.SqlClause ());
                         where_added = true;
                 }
        
                 Hyena.Data.Sqlite.IDataReader reader = Database.Query (query_builder.ToString());
                 reader.Read ();
                 int count = Convert.ToInt32 (reader ["count"]);
                 reader.Dispose();
                 return count;
         }
        
         public int [] IndicesOf (string table_name, uint [] items)
         {
                 StringBuilder query_builder = new StringBuilder ("SELECT ROWID AS row_id FROM ");
                 query_builder.Append (table_name);
                 query_builder.Append (" WHERE id IN (");
                 for (int i = 0; i < items.Length; i++) {
                         query_builder.Append (items [i]);
                         query_builder.Append ((i != items.Length - 1) ? ", " : ")" );
                 }
                 return IndicesOf (query_builder.ToString ());
         }
        
         public int IndexOf (string table_name, Photo photo)
         {
                 string query = String.Format ("SELECT ROWID AS row_id FROM {0} WHERE id = {1}", table_name, photo.Id);
                 return IndexOf (query);
         }
        
         public int IndexOf (string table_name, DateTime time, bool asc)
         {
                 string query = String.Format ("SELECT ROWID AS row_id FROM {0} WHERE time {2} {1} ORDER BY time {3} LIMIT 1",
                                 table_name,
                                 DateTimeUtil.FromDateTime (time),
                                 asc ? ">=" : "<=",
                                 asc ? "ASC" : "DESC");
                 return IndexOf (query);
         }
        
         private int IndexOf (string query)
         {
                 uint timer = Log.DebugTimerStart ();
                 Hyena.Data.Sqlite.IDataReader reader = Database.Query (query);
                 int index = - 1;
                 if (reader.Read ())
                         index = Convert.ToInt32 (reader ["row_id"]);
                 reader.Dispose();
                 Log.DebugTimerPrint (timer, "IndexOf took {0} : " + query);
                 return index - 1; //ROWID starts counting at 1
         }
        
         int [] IndicesOf (string query)
         {
                 uint timer = Log.DebugTimerStart ();
                 List<int> list = new List<int> ();
                 Hyena.Data.Sqlite.IDataReader reader = Database.Query (query);
                 while (reader.Read ())
                         list.Add (Convert.ToInt32 (reader ["row_id"]) - 1);
                 reader.Dispose ();
                 Log.DebugTimerPrint (timer, "IndicesOf took {0} : " + query);
                 return list.ToArray ();
         }
        
         public Dictionary<int,int[]> PhotosPerMonth (params IQueryCondition [] conditions)
         {
                 uint timer = Log.DebugTimerStart ();
                 Dictionary<int, int[]> val = new Dictionary<int, int[]> ();
        
                 //Sqlite is way more efficient querying to a temp then grouping than grouping at once
                 Database.Execute ("DROP TABLE IF EXISTS population");
                 StringBuilder query_builder = new StringBuilder ("CREATE TEMPORARY TABLE population AS SELECT strftime('%Y%m', datetime(time, 'unixepoch')) AS month FROM photos");
                 bool where_added = false;
                 foreach (IQueryCondition condition in conditions) {
                         if (condition == null)
                                 continue;
                         if (condition is IOrderCondition)
                                 continue;
                         query_builder.Append (where_added ? " AND " : " WHERE ");
                         query_builder.Append (condition.SqlClause ());
                         where_added = true;
                 }
                 Database.Execute (query_builder.ToString ());

                 int minyear = Int32.MaxValue;
                 int maxyear = Int32.MinValue;

		// FIXME: There appears to be a race condition here where it tries to query the population
		// table before Database.Execute (query_builder.ToString ()); creates it.
                 Hyena.Data.Sqlite.IDataReader reader = Database.Query ("SELECT COUNT (*) as count, month from population GROUP BY month");
                 while (reader.Read ()) {
                         string yyyymm = reader ["month"].ToString ();
                         int count = Convert.ToInt32 (reader ["count"]);
                         int year = Convert.ToInt32 (yyyymm.Substring (0,4));
                         maxyear = Math.Max (year, maxyear);
                         minyear = Math.Min (year, minyear);
                         int month = Convert.ToInt32 (yyyymm.Substring (4));
                         if (!val.ContainsKey (year))
                                 val.Add (year, new int[12]);
                         val[year][month-1] = count;
                 }
                 reader.Dispose ();
        
                 //Fill the blank
                 for (int i = minyear; i <= maxyear; i++)
                         if (!val.ContainsKey (i))
                                 val.Add (i, new int[12]);
        
                 Log.DebugTimerPrint (timer, "PhotosPerMonth took {0}");
                 return val;
         }
        
         // Queries.
         [Obsolete ("drop this, use IQueryCondition correctly instead")]
         public Photo [] Query (Tag [] tags) {
                 return Query (tags, null, null, null, null);
         }
        
         private string BuildQuery (params IQueryCondition [] conditions)
         {
                 StringBuilder query_builder = new StringBuilder ("SELECT * FROM photos ");
        
                 bool where_added = false;
                 bool hidden_contained = false;
                 foreach (IQueryCondition condition in conditions) {
        
                         if (condition == null)
                                 continue;
        
                         if (condition is HiddenTag)
                                 hidden_contained = true;
        
                         if (condition is IOrderCondition)
                                 continue;
        
                         string sql_clause = condition.SqlClause ();
        
                         if (sql_clause == null || sql_clause.Trim () == String.Empty)
                                 continue;
                         query_builder.Append (where_added ? " AND " : " WHERE ");
                         query_builder.Append (sql_clause);
                         where_added = true;
                 }
        
                 /* if a HiddenTag condition is not explicitly given, we add one */
                 if ( ! hidden_contained) {
                         string sql_clause = HiddenTag.HideHiddenTag.SqlClause ();
        
                         if (sql_clause != null && sql_clause.Trim () != String.Empty) {
                                 query_builder.Append (where_added ? " AND " : " WHERE ");
                                 query_builder.Append (sql_clause);
                         }
                 }
        
                 bool order_added = false;
                 foreach (IQueryCondition condition in conditions) {
                         if (condition == null)
                                 continue;
        
                         if (!(condition is IOrderCondition))
                                 continue;
        
                         string sql_clause = condition.SqlClause ();
        
                         if (sql_clause == null || sql_clause.Trim () == String.Empty)
                                 continue;
                         query_builder.Append (order_added ? " , " : "ORDER BY ");
                         query_builder.Append (sql_clause);
                         order_added = true;
                 }
        
                 return query_builder.ToString ();
         }
        
         public Photo [] Query (params IQueryCondition [] conditions)
         {
                 return Query (BuildQuery (conditions));
         }
        
         public void QueryToTemp (string temp_table, params IQueryCondition [] conditions)
         {
                 QueryToTemp (temp_table, BuildQuery (conditions));
         }
        
         public void QueryToTemp(string temp_table, string query)
         {
                 uint timer = Log.DebugTimerStart ();
                 Log.DebugFormat ("Query Started : {0}", query);
                 Database.BeginTransaction ();
                 Database.Execute (String.Format ("DROP TABLE IF EXISTS {0}", temp_table));
                 Database.Execute (String.Format ("CREATE TEMPORARY TABLE {0} AS {1}", temp_table, query));
                 // For Hyena.Data.Sqlite, we need to call Execute. Calling Query here does fail.
                 //Database.Query (String.Format ("CREATE TEMPORARY TABLE {0} AS {1}", temp_table, query)).Close ();
                 Database.CommitTransaction ();
                 Log.DebugTimerPrint (timer, "QueryToTemp took {0} : " + query);
         }
        
         public Photo [] QueryFromTemp (string temp_table)
         {
                 return QueryFromTemp (temp_table, 0, -1);
         }
        
         public Photo [] QueryFromTemp (string temp_table, int offset, int limit)
         {
                 return Query (String.Format ("SELECT * FROM {0} LIMIT {1} OFFSET {2}", temp_table, limit, offset));
         }
        
         public Photo [] Query (string query)
         {
                 return Query (new HyenaSqliteCommand (query));
         }
        
         private Photo [] Query (HyenaSqliteCommand query)
         {
                 uint timer = Log.DebugTimerStart ();
                 Hyena.Data.Sqlite.IDataReader reader = Database.Query(query);
        
                 List<Photo> new_photos = new List<Photo> ();
                 List<Photo> query_result = new List<Photo> ();
                 while (reader.Read ()) {
                         uint id = Convert.ToUInt32 (reader ["id"]);
                         Photo photo = LookupInCache (id);
        
                         if (photo == null) {
                                 photo = new Photo (id, Convert.ToInt64 (reader ["time"]));
                                 photo.Description = reader["description"].ToString ();
                                 photo.RollId = Convert.ToUInt32 (reader["roll_id"]);
                                 photo.DefaultVersionId = Convert.ToUInt32 (reader["default_version_id"]);
                                 photo.Rating = Convert.ToUInt32 (reader ["rating"]);
                                 new_photos.Add (photo);
                         }
        
                         query_result.Add (photo);
                 }
                 reader.Dispose();
        
                 bool need_load = false;
                 string photo_ids = "(";
                 foreach (Photo photo in new_photos) {
                         AddToCache (photo);
                         photo_ids = photo_ids + Convert.ToString(photo.Id) + ",";
                         need_load |= !photo.AllVersionsLoaded;
                 }
        
                 photo_ids = photo_ids + "-1)";
        
                 if (need_load) {
                         GetAllTags (photo_ids);
                         GetAllVersions (photo_ids);
                         foreach (Photo photo in new_photos)
                                 photo.AllVersionsLoaded = true;
                 } else {
                         //Console.WriteLine ("Skipped Loading Data");
                 }
        
                 foreach (Photo photo in new_photos)
                         photo.Changes = null;
        
                 Log.DebugTimerPrint (timer, "Query took {0} : " + query.Text);
                 return query_result.ToArray ();
         }
        
         public Photo [] Query (SafeUri uri)
         {
                 string filename = uri.GetFilename ();
        
                 /* query by file */
                 if ( ! String.IsNullOrEmpty (filename)) {
                         return Query (new HyenaSqliteCommand (
                         "SELECT id, "                   +
                                 "time, "                        +
                                 "base_uri, "            +
                                 "filename, "            +
                                 "description, "         +
                                 "roll_id, "             +
                                 "default_version_id, "  +
                                 "rating "               +
                         "FROM photos "                          +
                         "WHERE base_uri LIKE ?"         +
                         "AND filename LIKE ?",
                          uri.GetBaseUri ().ToString (),
                         filename));
                 }
        
                 /* query by directory */
                 return Query (new HyenaSqliteCommand (
                         "SELECT id, "                   +
                                 "time, "                        +
                                 "base_uri, "            +
                                 "filename, "            +
                                 "description, "         +
                                 "roll_id, "             +
                                 "default_version_id, "  +
                                 "rating "               +
                         "FROM photos "                          +
                         "WHERE base_uri LIKE ?"         +
                         "AND base_uri NOT LIKE ?",
                         uri.ToString () + "%",
                         uri.ToString () + "/%/%"));
         }
        
         [Obsolete ("drop this, use IQueryCondition correctly instead")]
         public Photo [] Query (Tag [] tags, string extra_condition, DateRange range, RollSet importidrange)
         {
                 return Query (OrTerm.FromTags(tags), extra_condition, range, importidrange, null);
         }
        
         [Obsolete ("drop this, use IQueryCondition correctly instead")]
         public Photo [] Query (Tag [] tags, string extra_condition, DateRange range, RollSet importidrange, RatingRange ratingrange)
         {
                 return Query (OrTerm.FromTags(tags), extra_condition, range, importidrange, ratingrange);
         }
        
         [Obsolete ("drop this, use IQueryCondition correctly instead")]
         public Photo [] Query (Term searchexpression, string extra_condition, DateRange range, RollSet importidrange, RatingRange ratingrange)
         {
                 bool hide = (extra_condition == null);
        
                 // The SQL query that we want to construct is:
                 //
                 // SELECT photos.id
                 //        photos.time
                 //        photos.uri,
                 //        photos.description,
                 //        photos.roll_id,
                 //        photos.default_version_id
                 //        photos.rating
                 //                  FROM photos, photo_tags
                 //                  WHERE photos.time >= time1 AND photos.time <= time2
                 //                              AND photos.rating >= rat1 AND photos.rating <= rat2
                 //                              AND photos.id NOT IN (select photo_id FROM photo_tags WHERE tag_id = HIDDEN)
                 //                              AND photos.id IN (select photo_id FROM photo_tags where tag_id IN (tag1, tag2..)
                 //                              AND extra_condition_string
                 //                  GROUP BY photos.id
        
                 StringBuilder query_builder = new StringBuilder ();
                 List<string> where_clauses = new List<string> ();
                 query_builder.Append ("SELECT id, "                     +
                                              "time, "                   +
                                              "base_uri, "                       +
                                              "filename, "                       +
                                              "description, "            +
                                              "roll_id, "                +
                                              "default_version_id, "     +
                                              "rating "                  +
                                       "FROM photos ");
        
                 if (range != null) {
                         where_clauses.Add (String.Format ("time >= {0} AND time <= {1}",
                                                           DateTimeUtil.FromDateTime (range.Start),
                                                           DateTimeUtil.FromDateTime (range.End)));
        
                 }
        
                 if (ratingrange != null) {
                         where_clauses.Add (ratingrange.SqlClause ());
                 }
        
                 if (importidrange != null) {
                         where_clauses.Add (importidrange.SqlClause ());
                 }
        
                 if (hide && App.Instance.Database.Tags.Hidden != null) {
                         where_clauses.Add (String.Format ("id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {0})",
                                                           App.Instance.Database.Tags.Hidden.Id));
                 }
        
                 if (searchexpression != null) {
                         where_clauses.Add (searchexpression.SqlCondition ());
                 }
        
                 if (extra_condition != null && extra_condition.Trim () != String.Empty) {
                         where_clauses.Add (extra_condition);
                 }
        
                 if (where_clauses.Count > 0) {
                         query_builder.Append (" WHERE ");
                         query_builder.Append (String.Join (" AND ", where_clauses.ToArray ()));
                 }
                 query_builder.Append (" ORDER BY time");
                 return Query (query_builder.ToString ());
         }
        }
}
