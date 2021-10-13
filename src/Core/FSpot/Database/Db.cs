// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2013-2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena;

//using Hyena;

//using FSpot.Imaging;
//using FSpot.Thumbnail;

// A Store maps to a SQL table.  We have separate stores (i.e. SQL tables) for tags, photos and imports.

namespace FSpot.Database
{
	// The Database puts the stores together.
	public class Db : IDb
	{
		//string Path { get; set; }

		//readonly IImageFileFactory imageFileFactory;
		//readonly IThumbnailService thumbnailService;

		public FSpotContext Context { get; private set; }
		public bool Empty { get; private set; }
		public TagStore Tags { get; private set; }
		public RollStore Rolls { get; private set; }
		public ExportStore Exports { get; private set; }
		public JobStore Jobs { get; private set; }
		public PhotoStore Photos { get; private set; }
		public MetaStore Meta { get; private set; }

		public Db (/*IImageFileFactory imageFileFactory, IThumbnailService thumbnailService, IUpdaterUI updaterDialog*/)
		{
			//this.imageFileFactory = imageFileFactory;
			//this.thumbnailService = thumbnailService;
			//this.updaterDialog = updaterDialog;
		}

		//public string Repair ()
		//{
		//	string backup_path = path;
		//	int i = 0;

		//	while (File.Exists (backup_path)) {
		//		backup_path = $"{Path.GetFileNameWithoutExtension (path)}-{DateTime.Now.ToString ("yyyyMMdd")}-{i++}{Path.GetExtension (path)}";
		//	}

		//	File.Move (path, backup_path);
		//	Init (path, true);

		//	return backup_path;
		//}

		public void Init (string path)
		{
			uint timer = Log.DebugTimerStart ();
			//Path = path;
			Context = new FSpotContext();

			// Load or create the meta table
			Meta = new MetaStore ();

			// Update the database schema if necessary
			//Updater.Run (Database, updaterDialog);

			Tags = new TagStore ();
			Rolls = new RollStore ();
			Exports = new ExportStore ();
			Jobs = new JobStore ();
			Photos = new PhotoStore (/*imageFileFactory, thumbnailService, */);

			Log.DebugTimerPrint (timer, "Db Initialization took {0}");
		}
	}
}
