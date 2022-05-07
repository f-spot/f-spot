// Copyright (C) 2022 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using SerilogTimings;

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

		public Db (/*IImageFileFactory imageFileFactory, IThumbnailService thumbnailService*/)
		{
			//this.imageFileFactory = imageFileFactory;
			//this.thumbnailService = thumbnailService;
		}

		//public string Repair ()
		//{
		//}

		public void Init (string path)
		{
			using var op = Operation.Begin ("Db Initialization");
			Context = new FSpotContext ();

			Meta = new MetaStore ();
			Tags = new TagStore ();
			Rolls = new RollStore ();
			Exports = new ExportStore ();
			Jobs = new JobStore ();
			Photos = new PhotoStore (/*imageFileFactory, thumbnailService, */);

			op.Complete ();
		}
	}
}
