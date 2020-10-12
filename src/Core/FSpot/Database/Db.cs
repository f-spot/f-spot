//
// Db.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Hyena;

using FSpot.Imaging;
using FSpot.Thumbnail;

// A Store maps to a SQL table.  We have separate stores (i.e. SQL tables) for tags, photos and imports.

namespace FSpot.Database
{
	// The Database puts the stores together.
	public class Db : IDb, IDisposable
	{
		string path;
		bool disposed;

		readonly IImageFileFactory imageFileFactory;
		readonly IThumbnailService thumbnailService;
		readonly IUpdaterUI updaterDialog;

		public bool Empty { get; private set; }
		public TagStore Tags { get; private set; }
		public RollStore Rolls { get; private set; }
		public ExportStore Exports { get; private set; }
		public JobStore Jobs { get; private set; }
		public PhotoStore Photos { get; private set; }
		public MetaStore Meta { get; private set; }

		// This affects how often the database writes data back to disk, and
		// therefore how likely corruption is in the event of power loss.
		public bool Sync {
			set {
				string query = "PRAGMA synchronous = " + (value ? "ON" : "OFF");
				Database.Execute (query);
			}
		}

		public FSpotDatabaseConnection Database { get; private set; }

		public Db (IImageFileFactory imageFileFactory, IThumbnailService thumbnailService, IUpdaterUI updaterDialog)
		{
			this.imageFileFactory = imageFileFactory;
			this.thumbnailService = thumbnailService;
			this.updaterDialog = updaterDialog;
		}

		public string Repair ()
		{
			string backup_path = path;
			int i = 0;

			while (File.Exists (backup_path)) {
				backup_path = $"{Path.GetFileNameWithoutExtension (path)}-{DateTime.Now.ToString ("yyyyMMdd")}-{i++}{Path.GetExtension (path)}";
			}

			File.Move (path, backup_path);
			Init (path, true);

			return backup_path;
		}

		public void Init (string path, bool createIfMissing)
		{
			uint timer = Log.DebugTimerStart ();
			bool new_db = !File.Exists (path);
			this.path = path;

			if (new_db && !createIfMissing)
				throw new Exception (path + ": File not found");

			Database = new FSpotDatabaseConnection (path);

			// Load or create the meta table
			Meta = new MetaStore (this, new_db);

			// Update the database schema if necessary
			Updater.Run (Database, updaterDialog);

			Database.BeginTransaction ();

			Tags = new TagStore (this, new_db);
			Rolls = new RollStore (this, new_db);
			Exports = new ExportStore (this, new_db);
			Jobs = new JobStore (this, new_db);
			Photos = new PhotoStore (imageFileFactory, thumbnailService, this, new_db);

			Database.CommitTransaction ();

			Empty = new_db;
			Log.DebugTimerPrint (timer, "Db Initialization took {0}");
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			// free managed resources
			if (disposing) {
				if (Tags != null) {
					Tags.Dispose ();
					Tags = null;
				}
				if (Database != null) {
					Database.Dispose ();
					Database = null;
				}
			}
			// free unmanaged resources
		}

		public void BeginTransaction ()
		{
			Database.BeginTransaction ();
		}

		public void CommitTransaction ()
		{
			Database.CommitTransaction ();
		}

		public void RollbackTransaction ()
		{
			Database.RollbackTransaction ();
		}
	}
}
