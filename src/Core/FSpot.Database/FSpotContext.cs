using Microsoft.EntityFrameworkCore;

namespace FSpot.Database
{
	public partial class FSpotContext : DbContext
	{
		public FSpotContext ()
		{
		}

		public FSpotContext (DbContextOptions<FSpotContext> options)
			: base (options)
		{
		}

		public virtual DbSet<Exports> Exports { get; set; }
		public virtual DbSet<Jobs> Jobs { get; set; }
		public virtual DbSet<Meta> Meta { get; set; }
		public virtual DbSet<PhotoTags> PhotoTags { get; set; }
		public virtual DbSet<PhotoVersions> PhotoVersions { get; set; }
		public virtual DbSet<Photos> Photos { get; set; }
		public virtual DbSet<Rolls> Rolls { get; set; }
		public virtual DbSet<Tags> Tags { get; set; }

		protected override void OnConfiguring (DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
				optionsBuilder.UseSqlite ("DataSource=photos.db");
		}

		protected override void OnModelCreating (ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Exports> (entity => {
				entity.ToTable ("exports");

				entity.Property (e => e.Id)
					.HasColumnName ("id")
					.ValueGeneratedNever ();

				entity.Property (e => e.ExportToken)
					.IsRequired ()
					.HasColumnName ("export_token");

				entity.Property (e => e.ExportType)
					.IsRequired ()
					.HasColumnName ("export_type");

				entity.Property (e => e.ImageId).HasColumnName ("image_id");

				entity.Property (e => e.ImageVersionId).HasColumnName ("image_version_id");
			});

			modelBuilder.Entity<Jobs> (entity => {
				entity.ToTable ("jobs");

				entity.Property (e => e.Id)
					.HasColumnName ("id")
					.ValueGeneratedNever ();

				entity.Property (e => e.JobOptions)
					.IsRequired ()
					.HasColumnName ("job_options");

				entity.Property (e => e.JobPriority).HasColumnName ("job_priority");

				entity.Property (e => e.JobType)
					.IsRequired ()
					.HasColumnName ("job_type");

				entity.Property (e => e.RunAt).HasColumnName ("run_at");
			});

			modelBuilder.Entity<Meta> (entity => {
				entity.ToTable ("meta");

				entity.HasIndex (e => e.Name)
					.IsUnique ();

				entity.Property (e => e.Id)
					.HasColumnName ("id")
					.ValueGeneratedNever ();

				entity.Property (e => e.Data).HasColumnName ("data");

				entity.Property (e => e.Name)
					.IsRequired ()
					.HasColumnName ("name");
			});

			modelBuilder.Entity<PhotoTags> (entity => {
				entity.HasNoKey ();

				entity.ToTable ("photo_tags");

				entity.HasIndex (e => new { e.PhotoId, e.TagId })
					.IsUnique ();

				entity.Property (e => e.PhotoId).HasColumnName ("photo_id");

				entity.Property (e => e.TagId).HasColumnName ("tag_id");
			});

			modelBuilder.Entity<PhotoVersions> (entity => {
				entity.HasNoKey ();

				entity.ToTable ("photo_versions");

				entity.HasIndex (e => e.ImportMd5)
					.HasName ("idx_photo_versions_import_md5");

				entity.HasIndex (e => new { e.PhotoId, e.VersionId })
					.IsUnique ();

				entity.Property (e => e.BaseUri)
					.IsRequired ()
					.HasColumnName ("base_uri")
					.HasColumnType ("STRING");

				entity.Property (e => e.Filename)
					.IsRequired ()
					.HasColumnName ("filename")
					.HasColumnType ("STRING");

				entity.Property (e => e.ImportMd5).HasColumnName ("import_md5");

				entity.Property (e => e.Name)
					.HasColumnName ("name")
					.HasColumnType ("STRING");

				entity.Property (e => e.PhotoId).HasColumnName ("photo_id");

				entity.Property (e => e.Protected)
					.HasColumnName ("protected")
					.HasColumnType ("BOOLEAN");

				entity.Property (e => e.VersionId).HasColumnName ("version_id");
			});

			modelBuilder.Entity<Photos> (entity => {
				entity.ToTable ("photos");

				entity.Property (e => e.Id)
					.HasColumnName ("id")
					.ValueGeneratedNever ();

				entity.Property (e => e.BaseUri)
					.IsRequired ()
					.HasColumnName ("base_uri")
					.HasColumnType ("STRING");

				entity.Property (e => e.DefaultVersionId).HasColumnName ("default_version_id");

				entity.Property (e => e.Description)
					.IsRequired ()
					.HasColumnName ("description");

				entity.Property (e => e.Filename)
					.IsRequired ()
					.HasColumnName ("filename")
					.HasColumnType ("STRING");

				entity.Property (e => e.Rating).HasColumnName ("rating");

				entity.Property (e => e.RollId).HasColumnName ("roll_id");

				entity.Property (e => e.Time).HasColumnName ("time");
			});

			modelBuilder.Entity<Rolls> (entity => {
				entity.ToTable ("rolls");

				entity.Property (e => e.Id)
					.HasColumnName ("id")
					.ValueGeneratedNever ();

				entity.Property (e => e.Time).HasColumnName ("time");
			});

			modelBuilder.Entity<Tags> (entity => {
				entity.ToTable ("tags");

				entity.HasIndex (e => e.Name)
					.IsUnique ();

				entity.Property (e => e.Id)
					.HasColumnName ("id")
					.ValueGeneratedNever ();

				entity.Property (e => e.CategoryId).HasColumnName ("category_id");

				entity.Property (e => e.Icon).HasColumnName ("icon");

				entity.Property (e => e.IsCategory)
					.HasColumnName ("is_category")
					.HasColumnType ("BOOLEAN");

				entity.Property (e => e.Name).HasColumnName ("name");

				entity.Property (e => e.SortPriority).HasColumnName ("sort_priority");
			});

			OnModelCreatingPartial (modelBuilder);
		}

		partial void OnModelCreatingPartial (ModelBuilder modelBuilder);
	}
}
