// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Models;
using FSpot.Resources.Lang;
using FSpot.Settings;
using Microsoft.EntityFrameworkCore;

namespace FSpot.Database
{
	public partial class FSpotContext : DbContext
	{
		const string DbName = FSpotConfiguration.DatabaseName;

		public FSpotContext ()
		{
			Database.Migrate ();
		}

		public FSpotContext (DbContextOptions<FSpotContext> options) : base (options)
		{
		}

		public DbSet<Export> Exports { get; set; }
		public DbSet<Job> Jobs { get; set; }
		public DbSet<Meta> Meta { get; set; }
		public DbSet<PhotoTag> PhotoTags { get; set; }
		public DbSet<PhotoVersion> PhotoVersions { get; set; }
		public DbSet<Photo> Photos { get; set; }
		public DbSet<Roll> Rolls { get; set; }
		public DbSet<Tag> Tags { get; set; }

		protected override void OnConfiguring (DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
				optionsBuilder.UseSqlite ($"DataSource={DbName};");
		}

		protected override void OnModelCreating (ModelBuilder modelBuilder)
		{
			// ExportStore - none

			// JobStore - none

			// PhotoStore - none

			// RollStore - none

			// TagStore
			modelBuilder.Entity<Tag> ().HasData (
				new Tag (Constants.RootCategory) {
					Id = Constants.FavoriteTagGuid,
					Name = Strings.Favorites,
					SortPriority = -10,
					Icon = "emblem-favorite",
					IsCategory = true,
					CategoryId = Constants.RootCategory.Id
				},
				new Tag (Constants.RootCategory) {
					Id = Constants.HiddenTagGuid,
					Name = Strings.Hidden,
					SortPriority = -9,
					Icon = "emblem-readonly",
					IsCategory = false,
					CategoryId = Constants.RootCategory.Id
				},
				new Tag (Constants.RootCategory) {
					Id = Constants.PeopleTagGuid,
					Name = Strings.People,
					SortPriority = -8,
					Icon = "emblem-people",
					IsCategory = true,
					CategoryId = Constants.RootCategory.Id
				},
				new Tag (Constants.RootCategory) {
					Id = Constants.PlacesTagGuid,
					Name = Strings.Places,
					SortPriority = -7,
					Icon = "emblem-places",
					IsCategory = true,
					CategoryId = Constants.RootCategory.Id
				},
				new Tag (Constants.RootCategory) {
					Id = Constants.EventsTagGuid,
					Name = Strings.Events,
					SortPriority = -6,
					Icon = "emblem-event",
					IsCategory = true,
					CategoryId = Constants.RootCategory.Id
				}
			);

			// MetaStore
			modelBuilder.Entity<Meta> ().HasData (
				new Meta {
					Id = Constants.MetaVersionGuid,
					Name = MetaStore.Version,
					Data = FSpotConfiguration.Version
				},
				//new Meta {
				//	Id = Constants.Meta2,
				//	Name = MetaStore.DbVersion,
				//	Data =
				//},
				new Meta {
					Id = Constants.MetaHiddenTagGuid,
					Name = MetaStore.Hidden,
					Data = Constants.HiddenTagGuid.ToString ()
				}
			);
		}
	}
}
