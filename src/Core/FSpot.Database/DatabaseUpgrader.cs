using System;
using System.Collections.Generic;
using System.Data.SQLite;
//using System.Diagnostics;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using Dapper;

using FSpot.Models;

namespace FSpot.Database
{
	public class DatabaseUpgrader
	{
		string ConnectionString { get; }
		string DatabaseLocation { get; }

		List<Export> Exports { get; set; }
		List<Job> Jobs { get; set; }
		List<Meta> Meta { get; set; }
		List<Photo> Photos { get; set; }
		List<PhotoTag> PhotoTags { get; set; }
		List<PhotoVersion> PhotoVersions { get; set; }
		List<Roll> Rolls { get; set; }
		List<Tag> Tags { get; set; }

		public DatabaseUpgrader (string oldDatabaseLocation)
		{
			DatabaseLocation = oldDatabaseLocation;
			ConnectionString = $"DataSource={DatabaseLocation};Version=3;";
			DefaultTypeMap.MatchNamesWithUnderscores = true;
		}

		public List<T> GetAll<T> (string tableName)
		{
			var allItems = Enumerable.Empty<T> ();

			using (var connection = new SQLiteConnection (ConnectionString)) {
				allItems = connection.Query<T> ($"Select * from {tableName}");
			}

			return allItems.ToList ();
		}

		void CollectOldData ()
		{
			var exports = GetAll<Models.Old.Export> ("exports");
			var jobs = GetAll<Models.Old.Job> ("jobs");
			var meta = GetAll<Models.Old.Meta> ("meta");
			var photos = GetAll<Models.Old.Photo> ("photos");
			var photoTags = GetAll<Models.Old.PhotoTag> ("photo_tags");
			var photoVersions = GetAll<Models.Old.PhotoVersion> ("photo_versions");
			var rolls = GetAll<Models.Old.Roll> ("rolls");
			var tags = GetAll<Models.Old.Tag> ("tags");

			Exports = ConvertTo<Models.Old.Export, Export> (exports);
			Jobs = ConvertTo<Models.Old.Job, Job> (jobs);
			Meta = ConvertTo<Models.Old.Meta, Meta> (meta);
			Photos = ConvertTo<Models.Old.Photo, Photo> (photos);
			PhotoTags = ConvertTo<Models.Old.PhotoTag, PhotoTag> (photoTags);
			PhotoVersions = ConvertTo<Models.Old.PhotoVersion, PhotoVersion> (photoVersions);
			Rolls = ConvertTo<Models.Old.Roll, Roll> (rolls);
			Tags = ConvertTo<Models.Old.Tag, Tag> (tags);
		}

		public int Migrate ()
		{
			//var sw = Stopwatch.StartNew ();
			CollectOldData ();
			//Console.WriteLine ($"Collect Old Data took: {sw.ElapsedMilliseconds} ms");
			//sw.Restart ();
			UpdateJoinIds ();
			//Console.WriteLine ($"Update Join Ids took: {sw.ElapsedMilliseconds} ms");

			//sw.Restart ();
			int count = -1;
			try {
				using var context = new FSpotContext ();
				Exports.ForEach ((item) => context.Entry (item).State = EntityState.Added);
				Jobs.ForEach ((item) => context.Entry (item).State = EntityState.Added);
				Meta.ForEach ((item) => context.Entry (item).State = EntityState.Added);
				Photos.ForEach ((item) => context.Entry (item).State = EntityState.Added);
				PhotoTags.ForEach ((item) => context.Entry (item).State = EntityState.Added);
				PhotoVersions.ForEach ((item) => context.Entry (item).State = EntityState.Added);
				Rolls.ForEach ((item) => context.Entry (item).State = EntityState.Added);
				Tags.ForEach ((item) => context.Entry (item).State = EntityState.Added);
				count = context.SaveChanges ();
			} catch (Exception ex) {
				Console.WriteLine ($"[Migrate] Exception: {ex.Message}");
				return count;
			}

			//Console.WriteLine ($"Insert into new database took: {sw.ElapsedMilliseconds} ms");
			return count;
		}

		List<T2> ConvertTo<T1, T2> (List<T1> oldModels) where T1 : IConvert
		{
			var newModels = new List<T2> ();

			foreach (var oldModel in oldModels)
				newModels.Add ((T2)oldModel.Convert ());

			return newModels;
		}

		void UpdateJoinIds ()
		{
			foreach (var photoTag in PhotoTags) {
				photoTag.Photo = Photos.First (x => x.OldId == photoTag.OldPhotoId);
				photoTag.Tag = Tags.First (x => x.OldId == photoTag.OldTagId);
			}

			foreach (var photo in Photos) {
				photo.Roll = Rolls.First (x => x.OldId == photo.OldRollId);
			}

			foreach (var photoVersion in PhotoVersions) {
				photoVersion.Photo = Photos.First (x => x.OldId == photoVersion.OldPhotoId);
			}
		}
	}
}
