//
// FSpotContext.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2020 Stephen Shaw
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

using Microsoft.EntityFrameworkCore;

using FSpot.Models;

namespace FSpot.Database
{
	public partial class FSpotContext : DbContext
	{
		public const string DbName = "photos_new.db";

		public FSpotContext ()
		{
			Database.Migrate ();
		}

		public FSpotContext (DbContextOptions<FSpotContext> options) : base (options)
		{
		}

		public virtual DbSet<Export> Exports { get; set; }
		public virtual DbSet<Job> Jobs { get; set; }
		public virtual DbSet<Meta> Meta { get; set; }
		public virtual DbSet<PhotoTag> PhotoTags { get; set; }
		public virtual DbSet<PhotoVersion> PhotoVersions { get; set; }
		public virtual DbSet<Photo> Photos { get; set; }
		public virtual DbSet<Roll> Rolls { get; set; }
		public virtual DbSet<Tag> Tags { get; set; }

		protected override void OnConfiguring (DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
				optionsBuilder.UseSqlite ($"DataSource={DbName};");
		}

		protected override void OnModelCreating (ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Meta> ().HasData (
				new Meta { Name = MetaStore.Version, Data = FSpotConfiguration.Version }
				//new Meta { Name = MetaStore.DbVersion, Data = }
			);

			modelBuilder.Entity<Tag> ().HasData (
				new Tag {  }
			);
		}
	}
}
