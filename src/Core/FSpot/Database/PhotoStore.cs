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
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2004-2010 Novell, Inc.
// Copyright (C) 2009-2010 Mike Gemünde
// Copyright (C) 2004-2007 Larry Ewing
// Copyright (C) 2008-2010 Ruben Vermeersch
// Copyright (C) 2006-2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FSpot.Core;
using FSpot.Database.Jobs;
using FSpot.Imaging;
using FSpot.Query;
using FSpot.Resources.Lang;
using FSpot.Thumbnail;
using FSpot.Utils;

using Hyena;

using SerilogTimings;

namespace FSpot.Database
{
	public class PhotoStore : DbStore<Photo>
	{
		public int TotalPhotos {
			get {
				using (var reader = Database.Query ("SELECT COUNT(*) AS photo_count FROM photos")) {
					reader.Read ();
					return Convert.ToInt32 (reader["photo_count"]);
				}
			}
		}

		public IReadOnlyList<FolderCount> ListFolderCounts ()
		{
			var folders = new List<FolderCount> ();
			using (var reader = Database.Query (
				"SELECT base_uri, COUNT(*) AS count FROM photos GROUP BY base_uri ORDER BY base_uri ASC")) {
				while (reader.Read ()) {
					string directory = reader["base_uri"]?.ToString ();
					// A version with no directory is not a folder. Skip it instead of throwing.
					if (string.IsNullOrEmpty (directory))
						continue;
					folders.Add (new FolderCount (
						new SafeUri (directory, true),
						Convert.ToInt32 (reader["count"])));
				}
			}

			return folders;
		}

		#region fields

		readonly IImageFileFactory imageFileFactory;
		readonly IThumbnailService thumbnailService;

		#endregion

		// Constructor
		public PhotoStore (IImageFileFactory imageFileFactory, IThumbnailService thumbnailService, IDb db, bool isNew) : base (db, false)
		{
			this.imageFileFactory = imageFileFactory;
			this.thumbnailService = thumbnailService;

			if (!isNew)
				return;

			Database.Execute (
				"CREATE TABLE photos (\n" +
				"       id                      INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, \n" +
				"       time                    INTEGER NOT NULL, \n" +
				"       base_uri                STRING NOT NULL, \n" +
				"       filename                STRING NOT NULL, \n" +
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
				"CREATE TABLE photo_versions (\n" +
				"       photo_id        INTEGER, \n" +
				"       version_id      INTEGER, \n" +
				"       name            STRING, \n" +
				"       base_uri        STRING NOT NULL, \n" +
				"       filename        STRING NOT NULL, \n" +
				"       import_md5      TEXT NULL, \n" +
				"       protected       BOOLEAN, \n" +
				"       UNIQUE (photo_id, version_id)\n" +
				")");

			Database.Execute ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");
			Database.Execute ("CREATE INDEX idx_photo_versions_import_md5 ON photo_versions(import_md5)");
			Database.Execute ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");
			Database.Execute ("CREATE INDEX idx_photo_tags_tag ON photo_tags(tag_id)");
			Database.Execute ("CREATE INDEX idx_photos_time_desc ON photos(time DESC, filename ASC, id DESC)");
			Database.Execute ("CREATE INDEX idx_photos_time_asc ON photos(time ASC, filename ASC, id ASC)");
		}

		public bool HasDuplicate (IPhoto item)
		{
			var uri = item.DefaultVersion.Uri;

			// Check if the exact given uri already exists.
			const string query = "SELECT COUNT(*) AS count FROM photo_versions WHERE base_uri = ? AND filename = ?";
			using (var reader = Database.Query (new DatabaseCommand (query, uri.GetBaseUri ().ToString (), uri.GetFilename ()))) {
				reader.Read ();
				int count = Convert.ToInt32 (reader["count"]);
				if (count > 0)
					return true;
			}

			// Check by MD5. Won't import if there are photos with the same ImportMD5.
			// Double-quoted values are identifiers in modern SQLite (SQLITE_DQS=0),
			// which is how every import was failing with "no such column: <md5>".
			string hash = item.DefaultVersion.ImportMD5;
			if (!string.IsNullOrEmpty (hash)) {
				const string hashQuery = "SELECT COUNT(*) AS count FROM photo_versions WHERE import_md5 = ?";
				using (var reader = Database.Query (new DatabaseCommand (hashQuery, hash))) {
					reader.Read ();
					if (Convert.ToInt32 (reader["count"]) > 0)
						return true;
				}
			}

			// This is a very lame check to overcome the lack of duplicate detect data right after transition.
			//
			// Does filename matching if there are files with no hash for the original version.
			var condition = new ConditionWrapper ("version_id = 1 AND (import_md5 = '' OR import_md5 IS NULL)");
			var have_no_hashes = Count ("photo_versions", condition);
			if (have_no_hashes <= 0)
				// No matches
				return false;

			var name = uri.GetFilename ();
			DateTime? time = null;

			// Look for a filename match.
			using (var reader = Database.Query (new DatabaseCommand ("SELECT photos.id, photos.time, pv.filename FROM photos LEFT JOIN photo_versions AS pv ON pv.photo_id = photos.id WHERE pv.filename = ?", name))) {
				while (reader.Read ()) {
					Logger.Log.Debug ("Found one possible duplicate for {0}", reader["filename"]);
					if (!time.HasValue) {
						// Only read time when needed
						time = item.Time;
					}

					if (reader["time"].ToString () == DateTimeUtil.FromDateTime (time.Value).ToString ()) {
						Logger.Log.Debug ("Skipping duplicate", uri);

						// Schedule a hash calculation job on the existing file.
						CalculateHashJob.Create (Db.Jobs, Convert.ToUInt32 (reader["id"]));

						return true;
					}
				}
			}
			return false;
		}

		public Photo CreateFrom (IPhoto item, bool defaultVersionOnly, uint rollId)
		{
			Photo photo;

			long unix_time = DateTimeUtil.FromDateTime (item.Time);
			string description = item.Description ?? string.Empty;

			uint id = (uint)Database.Execute (
				new DatabaseCommand (
					"INSERT INTO photos (time, base_uri, filename, description, roll_id, default_version_id, rating) " +
					"VALUES (?, ?, ?, ?, ?, ?, ?)",
					unix_time,
					item.DefaultVersion.BaseUri.ToString (),
					item.DefaultVersion.Filename,
					description,
					rollId,
					Photo.OriginalVersionId,
					"0"
				));

			photo = new Photo (imageFileFactory, thumbnailService, id, unix_time);

			uint versionId = Photo.OriginalVersionId;
			var versions = defaultVersionOnly ? new List<IPhotoVersion> { item.DefaultVersion } : item.Versions;
			foreach (IPhotoVersion version in versions) {
				// rename original version to "Original" if we import default version only
				// this applies when a version is detached from another photo
				string name = defaultVersionOnly && versionId == Photo.OriginalVersionId ? Strings.Original : version.Name;
				photo.AddVersionUnsafely (versionId++, version.BaseUri, version.Filename, version.ImportMD5, name, true);
				InsertVersion (photo, photo.Versions.Last () as PhotoVersion);
			}
			photo.DefaultVersionId = versionId - 1;
			photo.AllVersionsLoaded = true;

			EmitAdded (photo);
			return photo;
		}

		void InsertVersion (DbItem photo, PhotoVersion version)
		{
			Database.Execute (new DatabaseCommand (
				"INSERT OR IGNORE INTO photo_versions (photo_id, version_id, name, base_uri, filename, protected, import_md5) " +
				"VALUES (?, ?, ?, ?, ?, ?, ?)",
				photo.Id,
				version.VersionId,
				version.Name,
				version.BaseUri.ToString (),
				version.Filename,
				version.IsProtected,
				(version.ImportMD5 != string.Empty ? version.ImportMD5 : null)));
		}

		void GetVersions (Photo photo)
		{
			using (var reader = Database.Query (new DatabaseCommand (
				"SELECT version_id, name, base_uri, filename, import_md5, protected " +
				"FROM photo_versions " +
				"WHERE photo_id = ?",
				photo.Id))) {

				while (reader.Read ()) {
					uint version_id = Convert.ToUInt32 (reader["version_id"]);
					string name = reader["name"]?.ToString ();
					string filename = reader["filename"]?.ToString () ?? "";
					SafeUri base_uri = DirectoryOrRoot (reader["base_uri"]);
					string import_md5 = reader["import_md5"]?.ToString ();
					bool is_protected = Convert.ToBoolean (reader["protected"]);

					photo.AddVersionUnsafely (version_id, base_uri, filename, import_md5, name, is_protected);
				}
			}
		}

		void GetTags (Photo photo)
		{
			using (var reader = Database.Query (new DatabaseCommand ("SELECT tag_id FROM photo_tags WHERE photo_id = ?", photo.Id))) {
				while (reader.Read ()) {
					uint tag_id = Convert.ToUInt32 (reader["tag_id"]);
					Tag tag = Db.Tags.Get (tag_id);
					if (tag != null)
						photo.AddTagUnsafely (tag);
				}
			}
		}

		void GetAllVersions (string ids)
		{
			using (var reader = Database.Query ("SELECT photo_id, version_id, name, base_uri, filename, import_md5, protected FROM photo_versions WHERE photo_id IN " + ids)) {
				while (reader.Read ()) {
					uint id = Convert.ToUInt32 (reader["photo_id"]);
					Photo photo = LookupInCache (id);

					if (photo == null)
						continue;

					if (photo.AllVersionsLoaded)
						continue;

					if (reader["version_id"] != null) {
						uint version_id = Convert.ToUInt32 (reader["version_id"]);
						string name = reader["name"]?.ToString ();
						string filename = reader["filename"]?.ToString () ?? "";
						SafeUri base_uri = DirectoryOrRoot (reader["base_uri"]);
						string import_md5 = reader["import_md5"]?.ToString ();
						bool is_protected = Convert.ToBoolean (reader["protected"]);

						photo.AddVersionUnsafely (version_id, base_uri, filename, import_md5, name, is_protected);
					}
				}
			}
		}

		void GetAllTags (string ids)
		{
			using (var reader = Database.Query ("SELECT photo_id, tag_id FROM photo_tags WHERE photo_id IN " + ids)) {
				while (reader.Read ()) {
					uint id = Convert.ToUInt32 (reader["photo_id"]);
					Photo photo = LookupInCache (id);

					if (photo == null)
						continue;

					if (photo.AllVersionsLoaded)
						continue;

					if (reader[1] != null) {
						uint tag_id = Convert.ToUInt32 (reader["tag_id"]);
						Tag tag = Db.Tags.Get (tag_id);
						if (tag != null)
							photo.AddTagUnsafely (tag);
					}
				}
			}
		}

		internal void ForgetCached ()
		{
			ClearCache ();
		}

		// The stand-in exists only so a photo with no directory can be loaded. A save writes the empty directory back.
		static string StoredBaseUri (SafeUri uri)
			=> uri == null || uri.Scheme == "missing" ? "" : uri.ToString ();

		// An empty directory is not a filesystem root. A file URI would aim a later delete at /filename.
		static SafeUri DirectoryOrRoot (object raw)
		{
			string text = raw?.ToString ();
			if (string.IsNullOrEmpty (text))
				return new SafeUri ("missing:///", true);
			return new SafeUri (text, true);
		}

		static string ExcludingRows (IReadOnlyCollection<int> excludedIndexes)
		{
			if (excludedIndexes == null || excludedIndexes.Count == 0)
				return "";
			var rows = new StringBuilder ();
			bool any = false;
			foreach (int index in excludedIndexes) {
				if (index < 0)
					continue;
				if (any)
					rows.Append (',');
				rows.Append (index + 1);
				any = true;
			}
			if (!any)
				return "";
			return " WHERE ids.rowid NOT IN (" + rows + ")";
		}

		internal void SetRatingOnQuery (string idTable, uint rating, IReadOnlyCollection<int> excludedIndexes)
		{
			Database.Execute (new DatabaseCommand (
				"UPDATE photos SET rating = ? WHERE id IN (SELECT ids.id FROM " + idTable + " AS ids" +
				ExcludingRows (excludedIndexes) + ")",
				rating.ToString ()));
		}

		internal uint? UniformRating (string idTable, IReadOnlyCollection<int> excludedIndexes)
		{
			using var reader = Database.Query (
				"SELECT MIN(photos.rating) AS low, MAX(photos.rating) AS high, COUNT(*) AS count FROM " +
				idTable + " AS ids JOIN photos ON photos.id = ids.id" + ExcludingRows (excludedIndexes));
			if (!reader.Read () || Convert.ToInt32 (reader["count"]) == 0)
				return null;
			object low = reader["low"];
			object high = reader["high"];
			if (low == null || high == null || low is DBNull || high is DBNull)
				return null;
			if (Convert.ToInt64 (low) != Convert.ToInt64 (high))
				return null;
			return (uint)Convert.ToInt64 (low);
		}

		internal void AddTagToQuery (string idTable, uint tagId, IReadOnlyCollection<int> excludedIndexes)
		{
			Database.Execute (new DatabaseCommand (
				"INSERT OR IGNORE INTO photo_tags (photo_id, tag_id) " +
				"SELECT ids.id, ? FROM " + idTable + " AS ids" + ExcludingRows (excludedIndexes),
				tagId));
		}

		internal void RemoveTagFromQuery (string idTable, uint tagId, IReadOnlyCollection<int> excludedIndexes)
		{
			Database.Execute (new DatabaseCommand (
				"DELETE FROM photo_tags WHERE tag_id = ? AND photo_id IN (SELECT ids.id FROM " +
				idTable + " AS ids" + ExcludingRows (excludedIndexes) + ")",
				tagId));
		}

		internal List<uint> TagIdsInQuery (string idTable, IReadOnlyCollection<int> excludedIndexes)
		{
			var ids = new List<uint> ();
			using var reader = Database.Query (
				"SELECT DISTINCT photo_tags.tag_id FROM photo_tags JOIN " + idTable +
				" AS ids ON ids.id = photo_tags.photo_id" + ExcludingRows (excludedIndexes));
			while (reader.Read ())
				ids.Add (Convert.ToUInt32 (reader[0]));
			return ids;
		}

		internal void RemoveFromQuery (string idTable, IReadOnlyCollection<int> excludedIndexes)
		{
			string ids = "SELECT ids.id FROM " + idTable + " AS ids" + ExcludingRows (excludedIndexes);
			bool useTransactions = !Database.InTransaction;
			if (useTransactions)
				Database.BeginTransaction ();
			Database.Execute ("DELETE FROM photo_tags WHERE photo_id IN (" + ids + ")");
			Database.Execute ("DELETE FROM photo_versions WHERE photo_id IN (" + ids + ")");
			if (Database.TableExists ("exports"))
				Database.Execute ("DELETE FROM exports WHERE image_id IN (" + ids + ")");
			Database.Execute ("DELETE FROM photos WHERE id IN (" + ids + ")");
			if (useTransactions)
				Database.CommitTransaction ();
			ClearCache ();
		}

		public override Photo Get (uint id)
		{
			Photo photo = LookupInCache (id);
			if (photo != null)
				return photo;

			using (var reader = Database.Query (new DatabaseCommand (
				"SELECT time, description, roll_id, default_version_id, rating " +
				"FROM photos " +
				"WHERE id = ?", id))) {

				if (reader.Read ()) {
					photo = new Photo (imageFileFactory, thumbnailService, id, Convert.ToInt64 (reader["time"]));
					photo.Description = reader["description"].ToString ();
					photo.RollId = Convert.ToUInt32 (reader["roll_id"]);
					photo.DefaultVersionId = Convert.ToUInt32 (reader["default_version_id"]);
					photo.Rating = Convert.ToUInt32 (reader["rating"]);
					AddToCache (photo);
				}

				if (photo == null)
					return null;

				GetTags (photo);
				GetVersions (photo);

				return photo;
			}
		}

		public Photo GetByUri (SafeUri uri)
		{
			Photo photo = null;

			var base_uri = uri.GetBaseUri ();
			var filename = uri.GetFilename ();
			// Catalogs store the directory with or without a trailing slash.
			string directory = base_uri.ToString ();
			string alternate = directory.EndsWith ("/", StringComparison.Ordinal)
				? directory.Substring (0, directory.Length - 1)
				: directory + "/";

			using (var reader = Database.Query (new DatabaseCommand (
				"SELECT id, time, description, roll_id, default_version_id, rating " +
				" FROM photos " +
				" LEFT JOIN photo_versions AS pv ON photos.id = pv.photo_id" +
				" WHERE ((photos.base_uri = ? OR photos.base_uri = ?) AND photos.filename = ?)" +
				" OR ((pv.base_uri = ? OR pv.base_uri = ?) AND pv.filename = ?)",
				directory, alternate, filename,
				directory, alternate, filename))) {

				if (reader.Read ()) {
					photo = new Photo (imageFileFactory, thumbnailService, Convert.ToUInt32 (reader["id"]),
						Convert.ToInt64 (reader["time"]));

					photo.Description = reader["description"].ToString ();
					photo.RollId = Convert.ToUInt32 (reader["roll_id"]);
					photo.DefaultVersionId = Convert.ToUInt32 (reader["default_version_id"]);
					photo.Rating = Convert.ToUInt32 (reader["rating"]);
				}

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
		}

		public void Remove (List<Tag> tags)
		{
			var photos = Query (new OrOperator (tags.Select (t => new TagTerm (t)).ToArray ()));

			foreach (Photo photo in photos)
				photo.RemoveCategory (tags);
			Commit (photos);

			foreach (Tag tag in tags)
				Db.Tags.Remove (tag);
		}

		public void Remove (Photo[] items)
		{
			EmitRemoved (items);

			var query_builder = new List<string> (items.Length);
			for (int i = 0; i < items.Length; i++) {
				query_builder.Add ($"{items[i].Id}");
				RemoveFromCache (items[i]);
			}

			string id_list = string.Join ("','", query_builder);
			Database.Execute ($"DELETE FROM photos WHERE id IN ('{id_list}')");
			Database.Execute ($"DELETE FROM photo_tags WHERE photo_id IN ('{id_list}')");
			Database.Execute ($"DELETE FROM photo_versions WHERE photo_id IN ('{id_list}')");
			if (Database.TableExists ("exports"))
				Database.Execute ($"DELETE FROM exports WHERE image_id IN ('{id_list}')");
		}

		public override void Remove (Photo item)
		{
			Remove (new[] { item });
		}

		public override void Commit (Photo item)
		{
			Commit (new[] { item });
		}

		public void Commit (Photo[] items)
		{
			using var op = Operation.Begin ("PhotoStore.Commit");

			bool use_transactions = !Database.InTransaction;
			if (use_transactions)
				Database.BeginTransaction ();

			var changes = new PhotosChanges ();
			foreach (DbItem item in items)
				changes |= Update ((Photo)item);

			if (use_transactions)
				Database.CommitTransaction ();

			EmitChanged (items, new PhotoEventArgs (items, changes));

			op.Complete ();
		}

		PhotoChanges Update (Photo photo)
		{
			PhotoChanges changes = photo.Changes;
			// Roll id is not part of the field update below. A freshly loaded photo
			// marks this flag while it is filled in, and writing it then stores the
			// same id. CreateFrom leaves the flag clear, so a later description
			// commit does not replace the roll the insert already stored.
			if (changes.RollIdChanged)
				Database.Execute (new DatabaseCommand (
					"UPDATE photos SET roll_id = ? WHERE id = ?",
					photo.RollId,
					photo.Id));

			// Update photo.
			if (changes.DescriptionChanged || changes.DefaultVersionIdChanged || changes.TimeChanged || changes.UriChanged || changes.RatingChanged || changes.MD5SumChanged)
				Database.Execute (
					new DatabaseCommand (
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
						StoredBaseUri (photo.VersionUri (Photo.OriginalVersionId).GetBaseUri ()),
						photo.VersionUri (Photo.OriginalVersionId).GetFilename (),
						$"{photo.Rating}",
						photo.Id
					)
				);

			// Update tags.
			if (changes.TagsRemoved != null)
				foreach (Tag tag in changes.TagsRemoved)
					Database.Execute (new DatabaseCommand (
						"DELETE FROM photo_tags WHERE photo_id = ? AND tag_id = ?",
						photo.Id,
						tag.Id));

			if (changes.TagsAdded != null)
				foreach (Tag tag in changes.TagsAdded)
					Database.Execute (new DatabaseCommand (
						"INSERT OR IGNORE INTO photo_tags (photo_id, tag_id) " +
						"VALUES (?, ?)",
						photo.Id,
						tag.Id));

			// Update versions.
			if (changes.VersionsRemoved != null)
				foreach (uint version_id in changes.VersionsRemoved)
					Database.Execute (new DatabaseCommand (
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
					Database.Execute (new DatabaseCommand (
						"UPDATE photo_versions SET name = ?, " +
						"base_uri = ?, filename = ?, protected = ?, import_md5 = ? " +
						"WHERE photo_id = ? AND version_id = ?",
						version.Name,
						StoredBaseUri (version.BaseUri),
						version.Filename,
						version.IsProtected,
						(version.ImportMD5 != string.Empty ? version.ImportMD5 : null),
						photo.Id,
						version_id));
				}

			photo.Changes = null;
			return changes;
		}

		public void CalculateMD5Sum (Photo photo)
		{
			foreach (uint version_id in photo.VersionIds) {
				PhotoVersion version = photo.GetVersion (version_id);

				// Don't overwrite MD5 sums that are already calculated.
				if (version.ImportMD5 != string.Empty && version.ImportMD5 != null)
					continue;

				string version_md5_sum = HashUtils.GenerateMD5 (version.Uri);
				version.ImportMD5 = version_md5_sum;
				photo.Changes.ChangeVersion (version_id);
			}

			Commit (photo);
		}

		public int Count (string tableName, params IQueryCondition[] conditions)
		{
			var query_builder = new StringBuilder ("SELECT COUNT(*) AS count FROM " + tableName + " ");
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

			using (var reader = Database.Query (query_builder.ToString ())) {
				reader.Read ();
				return Convert.ToInt32 (reader["count"]);
			}
		}

		public int[] IndicesOf (string tableName, uint[] items)
		{
			var query_builder = new StringBuilder ("SELECT ROWID AS row_id FROM ");
			query_builder.Append (tableName);
			query_builder.Append (" WHERE id IN (");
			for (int i = 0; i < items.Length; i++) {
				query_builder.Append (items[i]);
				query_builder.Append ((i != items.Length - 1) ? ", " : ")");
			}
			return IndicesOf (query_builder.ToString ());
		}

		public int IndexOf (string tableName, Photo photo)
		{
			string query = string.Format ("SELECT ROWID AS row_id FROM {0} WHERE id = {1}", tableName, photo.Id);
			return IndexOf (query);
		}

		public int IndexOf (string tableName, DateTime time, bool asc)
		{
			// The query table stores ordered ids. The time lives on photos.
			string direction = asc ? "ASC" : "DESC";
			string comparison = asc ? ">=" : "<=";
			string query = string.Format (
				"SELECT {0}.ROWID AS row_id FROM {0} JOIN photos ON photos.id = {0}.id " +
				"WHERE photos.time {1} {2} ORDER BY photos.time {3}, photos.filename ASC, photos.id {3} LIMIT 1",
				tableName,
				comparison,
				DateTimeUtil.FromDateTime (time),
				direction);
			return IndexOf (query);
		}

		int IndexOf (string query)
		{
			using var op = Operation.Begin ($"PhotoStore.IndexOf {query}");
			using var reader = Database.Query (query);
			int index = -1;
			if (reader.Read ())
				index = Convert.ToInt32 (reader["row_id"]);
			op.Complete ();
			return index - 1; //ROWID starts counting at 1
		}

		int[] IndicesOf (string query)
		{
			using var op = Operation.Begin ($"PhotoStore.IndicesOf {query}");
			var list = new List<int> ();
			using var reader = Database.Query (query);
			while (reader.Read ())
				list.Add (Convert.ToInt32 (reader["row_id"]) - 1);
			op.Complete ();
			return list.ToArray ();
		}

		static object populationTableLock = new object ();
		public Dictionary<int, int[]> PhotosPerMonth (params IQueryCondition[] conditions)
		{
			lock (populationTableLock) {
				using var op = Operation.Begin ($"PhotosPerMonth");
				var val = new Dictionary<int, int[]> ();

				//Sqlite is way more efficient querying to a temp then grouping than grouping at once
				Database.Execute ("DROP TABLE IF EXISTS population");
				var query_builder = new StringBuilder ("CREATE TEMPORARY TABLE population AS SELECT strftime('%Y%m', datetime(time, 'unixepoch')) AS month FROM photos");
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

				int minyear = int.MaxValue;
				int maxyear = int.MinValue;

				using (var reader = Database.Query ("SELECT COUNT (*) as count, month from population GROUP BY month")) {
					while (reader.Read ()) {
						string yyyymm = reader["month"].ToString ();
						int count = Convert.ToInt32 (reader["count"]);
						int year = Convert.ToInt32 (yyyymm.Substring (0, 4));
						maxyear = Math.Max (year, maxyear);
						minyear = Math.Min (year, minyear);
						int month = Convert.ToInt32 (yyyymm.Substring (4));
						if (!val.ContainsKey (year))
							val.Add (year, new int[12]);
						val[year][month - 1] = count;
					}
				}

				//Fill the blank
				for (int i = minyear; i <= maxyear; i++)
					if (!val.ContainsKey (i))
						val.Add (i, new int[12]);

				op.Complete ();
				return val;
			}
		}

		// Queries.
		public static string BuildQuery (params IQueryCondition[] conditions)
			=> "SELECT * " + BuildQueryBody (conditions);

		public static string BuildIdQuery (params IQueryCondition[] conditions)
			=> "SELECT photos.id " + BuildQueryBody (conditions);

		static string BuildQueryBody (params IQueryCondition[] conditions)
		{
			var query_builder = new StringBuilder ("FROM photos ");

			bool where_added = false;
			bool hidden_contained = false;
			foreach (IQueryCondition condition in conditions) {

				if (condition == null)
					continue;

				hidden_contained |= condition is HiddenTag;

				if (condition is IOrderCondition)
					continue;

				string sql_clause = condition.SqlClause ();

				if (sql_clause == null || sql_clause.Trim () == string.Empty)
					continue;
				query_builder.Append (where_added ? " AND " : " WHERE ");
				query_builder.Append (sql_clause);
				where_added = true;
			}

			/* if a HiddenTag condition is not explicitly given, we add one */
			if (!hidden_contained) {
				string sql_clause = HiddenTag.HideHiddenTag.SqlClause ();

				if (sql_clause != null && sql_clause.Trim () != string.Empty) {
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

				if (sql_clause == null || sql_clause.Trim () == string.Empty)
					continue;
				query_builder.Append (order_added ? " , " : "ORDER BY ");
				query_builder.Append (sql_clause);
				order_added = true;
			}

			return query_builder.ToString ();
		}

		public Photo[] Query (params IQueryCondition[] conditions)
		{
			return Query (BuildQuery (conditions));
		}

		public void QueryToTemp (string tempTable, params IQueryCondition[] conditions)
		{
			QueryToTemp (tempTable, BuildIdQuery (conditions));
		}

		public void QueryToTemp (string tempTable, string query)
		{
			using var op = Operation.Begin ($"QueryToTemp");
			Logger.Log.Debug ($"Query Started : {query}");
			Database.BeginTransaction ();
			Database.Execute ($"DROP TABLE IF EXISTS {tempTable}");
			Database.Execute ($"CREATE TEMPORARY TABLE {tempTable} AS {query}");
			// Need Execute here; Query fails on CREATE TEMPORARY TABLE AS.
			//Database.Query (string.Format ("CREATE TEMPORARY TABLE {0} AS {1}", temp_table, query)).Close ();
			Database.CommitTransaction ();
			op.Complete ();
		}

		public Photo[] QueryFromTemp (string tempTable)
		{
			return QueryFromTemp (tempTable, 0, -1);
		}

		public List<DateTime> TimesOf (string idTable)
		{
			var times = new List<DateTime> ();
			using var reader = Database.Query (
				$"SELECT photos.time FROM {idTable} AS ids JOIN photos ON photos.id = ids.id ORDER BY ids.rowid");
			while (reader.Read ())
				times.Add (DateTimeUtil.ToDateTime (Convert.ToInt64 (reader[0])));
			return times;
		}

		public List<DateTime> Times (params IQueryCondition[] conditions)
		{
			var times = new List<DateTime> ();
			using var reader = Database.Query ("SELECT photos.time " + BuildQueryBody (conditions));
			while (reader.Read ())
				times.Add (DateTimeUtil.ToDateTime (Convert.ToInt64 (reader[0])));
			return times;
		}

		public List<string> DefaultVersionPaths (string idTable)
		{
			var paths = new List<string> ();
			using var reader = Database.Query (
				"SELECT versions.base_uri, versions.filename " +
				$"FROM {idTable} AS ids JOIN photos ON photos.id = ids.id " +
				"JOIN photo_versions AS versions ON versions.photo_id = photos.id AND versions.version_id = photos.default_version_id " +
				"ORDER BY ids.rowid");
			while (reader.Read ()) {
				string baseUri = reader[0]?.ToString ();
				string filename = reader[1]?.ToString () ?? "";
				if (string.IsNullOrEmpty (baseUri)) {
					// Keep one slot per row so the caller's index still addresses this photo.
					paths.Add ("");
					continue;
				}
				SafeUri uri = new SafeUri (baseUri, true).Append (filename);
				string local = uri.LocalPath;
				paths.Add (!string.IsNullOrEmpty (local) ? local : uri.AbsolutePath);
			}
			return paths;
		}

		internal static string PageClause (int offset, int limit)
		{
			if (limit < 0)
				return "";
			// rowid on a freshly filled id table is the ordered position, starting at 1.
			return $" WHERE ids.rowid >= {offset + 1} AND ids.rowid < {offset + 1 + limit}";
		}

		public Photo[] QueryFromTemp (string tempTable, int offset, int limit)
		{
			// A range on that rowid does not walk the rows a deep OFFSET would skip.
			string bounds = PageClause (offset, limit);
			return Query (
				"SELECT photos.id, photos.time, photos.description, photos.roll_id, photos.default_version_id, photos.rating " +
				$"FROM {tempTable} AS ids JOIN photos ON photos.id = ids.id{bounds} ORDER BY ids.rowid");
		}

		public Photo[] Query (string query)
		{
			return Query (new DatabaseCommand (query));
		}

		Photo[] Query (DatabaseCommand query)
		{
			using var op = Operation.Begin ($"PhotoStore.Query {query}");
			var new_photos = new List<Photo> ();
			var query_result = new List<Photo> ();

			using (var reader = Database.Query (query)) {
				while (reader.Read ()) {
					uint id = Convert.ToUInt32 (reader["id"]);
					Photo photo = LookupInCache (id);

					if (photo == null) {
						photo = new Photo (imageFileFactory, thumbnailService, id, Convert.ToInt64 (reader["time"]));
						photo.Description = reader["description"].ToString ();
						photo.RollId = Convert.ToUInt32 (reader["roll_id"]);
						photo.DefaultVersionId = Convert.ToUInt32 (reader["default_version_id"]);
						photo.Rating = Convert.ToUInt32 (reader["rating"]);
						new_photos.Add (photo);
					}

					query_result.Add (photo);
				}
			}

			bool need_load = false;
			string photo_ids = "(";
			foreach (Photo photo in new_photos) {
				AddToCache (photo);
				photo_ids = photo_ids + Convert.ToString (photo.Id) + ",";
				need_load |= !photo.AllVersionsLoaded;
			}

			photo_ids = photo_ids + "-1)";

			if (need_load) {
				GetAllTags (photo_ids);
				GetAllVersions (photo_ids);
				foreach (Photo photo in new_photos)
					photo.AllVersionsLoaded = true;
			}

			foreach (Photo photo in new_photos)
				photo.Changes = null;

			op.Complete ();
			return query_result.ToArray ();
		}

		public Photo[] Query (SafeUri uri)
		{
			string filename = uri.GetFilename ();

			/* query by file */
			if (!string.IsNullOrEmpty (filename)) {
				return Query (new DatabaseCommand (
					"SELECT id, " +
						"time, " +
						"base_uri, " +
						"filename, " +
						"description, " +
						"roll_id, " +
						"default_version_id, " +
						"rating " +
					"FROM photos " +
					"WHERE base_uri LIKE ?" +
					"AND filename LIKE ?",
					uri.GetBaseUri ().ToString (),
					filename));
			}

			/* query by directory */
			return Query (new DatabaseCommand (
				"SELECT id, " +
					"time, " +
					"base_uri, " +
					"filename, " +
					"description, " +
					"roll_id, " +
					"default_version_id, " +
					"rating " +
				"FROM photos " +
				"WHERE base_uri LIKE ?" +
				"AND base_uri NOT LIKE ?",
				uri + "%",
				uri + "/%/%"));
		}
	}
}
