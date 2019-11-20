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

		#region ctors

		public Db (IImageFileFactory imageFileFactory, IThumbnailService thumbnailService, IUpdaterUI updaterDialog)
		{
			this.imageFileFactory = imageFileFactory;
			this.thumbnailService = thumbnailService;
			this.updaterDialog = updaterDialog;
		}

		#endregion

		public string Repair ()
		{
			string backup_path = path;
			int i = 0;

			while (File.Exists (backup_path)) {
				backup_path = string.Format ("{0}-{1}-{2}{3}", Path.GetFileNameWithoutExtension (path), DateTime.Now.ToString ("yyyyMMdd"), i++, Path.GetExtension (path));
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
